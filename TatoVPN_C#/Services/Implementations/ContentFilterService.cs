using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using miVPN.Services.Interfaces;

namespace miVPN.Services.Implementations;

public class ContentFilterService : IContentFilterService
{
    private readonly ILoggerService _logger;
    private HashSet<string> _blockedDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _sitiosActivados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _dominiosPersonalizados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    
    // Diccionario temporal para guardar todos los dominios por nombre de "sitio" a partir de categorias_filtro.json
    // Key: Nombre del sitio (ej. "Facebook") -> Value: List de dominios
    private Dictionary<string, List<string>> _sitiosDisponibles = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

    public bool HabilitarFiltrado { get; set; } = true;
    public bool HabilitarInspeccionSNI { get; set; } = false;

    public ContentFilterService(ILoggerService logger)
    {
        _logger = logger;
    }

    public void CargarCategorias(string rutaCategoriasJson)
    {
        try
        {
            if (!File.Exists(rutaCategoriasJson)) return;
            string json = File.ReadAllText(rutaCategoriasJson);
            
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("categorias", out var cats))
            {
                foreach (var cat in cats.EnumerateArray())
                {
                    if (cat.TryGetProperty("sitios", out var sitios))
                    {
                        foreach (var sitio in sitios.EnumerateArray())
                        {
                            string nombre = sitio.GetProperty("nombre").GetString() ?? "";
                            var dominios = sitio.GetProperty("dominios").EnumerateArray()
                                .Select(d => d.GetString() ?? "")
                                .Where(d => !string.IsNullOrEmpty(d))
                                .ToList();
                            
                            if (!string.IsNullOrEmpty(nombre))
                            {
                                _sitiosDisponibles[nombre] = dominios;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Log($"⚠️ Error al cargar categorías del filtro: {ex.Message}");
        }
    }

    public void CargarConfiguracion(string rutaJson)
    {
        try
        {
            if (!File.Exists(rutaJson)) return;
            string json = File.ReadAllText(rutaJson);
            
            using var doc = JsonDocument.Parse(json);
            
            _sitiosActivados.Clear();
            _dominiosPersonalizados.Clear();

            if (doc.RootElement.TryGetProperty("habilitarFiltrado", out var propHab))
            {
                HabilitarFiltrado = propHab.GetBoolean();
            }

            if (doc.RootElement.TryGetProperty("habilitarSNI", out var propSni))
            {
                HabilitarInspeccionSNI = propSni.GetBoolean();
            }

            if (doc.RootElement.TryGetProperty("sitiosActivados", out var sitios))
            {
                foreach (var s in sitios.EnumerateArray())
                {
                    _sitiosActivados.Add(s.GetString() ?? "");
                }
            }

            if (doc.RootElement.TryGetProperty("dominiosPersonalizados", out var dominios))
            {
                foreach (var d in dominios.EnumerateArray())
                {
                    string rawDom = d.GetString() ?? "";
                    string norm = NormalizarDominio(rawDom);
                    if (norm != null)
                    {
                        _dominiosPersonalizados.Add(norm);
                    }
                }
            }

            ReconstruirHashSetDominios();
        }
        catch (Exception ex)
        {
            _logger.Log($"⚠️ Error al cargar la configuración del filtro: {ex.Message}");
        }
    }

    public void GuardarConfiguracion(string rutaJson)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var data = new
            {
                habilitarFiltrado = HabilitarFiltrado,
                habilitarSNI = HabilitarInspeccionSNI,
                sitiosActivados = _sitiosActivados.ToArray(),
                dominiosPersonalizados = _dominiosPersonalizados.ToArray()
            };
            
            File.WriteAllText(rutaJson, JsonSerializer.Serialize(data, options));
        }
        catch (Exception ex)
        {
            _logger.Log($"⚠️ Error al guardar la configuración del filtro: {ex.Message}");
        }
    }

    private void ReconstruirHashSetDominios()
    {
        _blockedDomains.Clear();

        // 1. Añadir dominios de sitios predefinidos activados
        foreach (var sitio in _sitiosActivados)
        {
            if (_sitiosDisponibles.TryGetValue(sitio, out var dominios))
            {
                foreach (var d in dominios)
                {
                    _blockedDomains.Add(d);
                }
            }
        }

        // 2. Añadir dominios personalizados
        foreach (var d in _dominiosPersonalizados)
        {
            _blockedDomains.Add(d);
        }
    }

    public bool EstaBloqueado(string dominioConsultado)
    {
        if (!HabilitarFiltrado || string.IsNullOrWhiteSpace(dominioConsultado)) return false;
        
        try
        {
            dominioConsultado = dominioConsultado.Trim().ToLowerInvariant();
            if (dominioConsultado.StartsWith("www.")) dominioConsultado = dominioConsultado.Substring(4);

            // Chequeo exacto
            if (_blockedDomains.Contains(dominioConsultado))
            {
                _logger.Log($"🚫 Bloqueado por filtro de contenido: {dominioConsultado}");
                return true;
            }

            // Chequeo por subdominios (si ejemplo.com está bloqueado, www.ejemplo.com también debe estarlo)
            int dotIndex = dominioConsultado.IndexOf('.');
            while (dotIndex > 0)
            {
                string parentDomain = dominioConsultado.Substring(dotIndex + 1);
                if (_blockedDomains.Contains(parentDomain))
                {
                    _logger.Log($"🚫 Bloqueado por filtro de contenido (subdominio): {dominioConsultado} -> {parentDomain}");
                    return true;
                }
                dotIndex = dominioConsultado.IndexOf('.', dotIndex + 1);
            }

            return false;
        }
        catch (Exception ex)
        {
            // Fail-open
            _logger.Log($"⚠️ Excepción en el filtro de contenido (se permitirá acceso a {dominioConsultado}): {ex.Message}");
            return false;
        }
    }

    private string NormalizarDominio(string dominio)
    {
        if (string.IsNullOrWhiteSpace(dominio)) return null;
        
        dominio = dominio.Trim().ToLowerInvariant();
        
        if (dominio.StartsWith("http://")) dominio = dominio.Substring(7);
        if (dominio.StartsWith("https://")) dominio = dominio.Substring(8);
        
        int pathIndex = dominio.IndexOf('/');
        if (pathIndex > -1) dominio = dominio.Substring(0, pathIndex);
        
        int queryIndex = dominio.IndexOf('?');
        if (queryIndex > -1) dominio = dominio.Substring(0, queryIndex);
        
        if (dominio.StartsWith("www.")) dominio = dominio.Substring(4);
        
        var regex = new System.Text.RegularExpressions.Regex(@"^[a-z0-9]([a-z0-9-]*[a-z0-9])?(\.[a-z0-9]([a-z0-9-]*[a-z0-9])?)+$");
        if (!regex.IsMatch(dominio))
        {
            return null;
        }
        
        return dominio;
    }

    public string AgregarDominioPersonalizado(string dominio)
    {
        string norm = NormalizarDominio(dominio);
        if (norm == null)
        {
            throw new ArgumentException("El formato del dominio no es válido.");
        }
        
        _dominiosPersonalizados.Add(norm);
        ReconstruirHashSetDominios();
        
        return norm;
    }

    public void QuitarDominioPersonalizado(string dominio)
    {
        _dominiosPersonalizados.Remove(dominio);
        ReconstruirHashSetDominios();
    }

    public void ActivarSitio(string categoria, string sitio, bool activar)
    {
        if (activar)
            _sitiosActivados.Add(sitio);
        else
            _sitiosActivados.Remove(sitio);
            
        ReconstruirHashSetDominios();
    }

    public IEnumerable<string> GetSitiosActivados() => _sitiosActivados;
    public IEnumerable<string> GetDominiosPersonalizados() => _dominiosPersonalizados;
}
