namespace miVPN.Services.Interfaces;

public interface IContentFilterService
{
    bool HabilitarFiltrado { get; set; }
    bool HabilitarInspeccionSNI { get; set; }
    void CargarConfiguracion(string rutaJson);
    void GuardarConfiguracion(string rutaJson);
    bool EstaBloqueado(string dominioConsultado);
    string AgregarDominioPersonalizado(string dominio);
    void QuitarDominioPersonalizado(string dominio);
    void ActivarSitio(string categoria, string sitio, bool activar);
    
    System.Collections.Generic.IEnumerable<string> GetSitiosActivados();
    System.Collections.Generic.IEnumerable<string> GetDominiosPersonalizados();
    
    // Método utilitario para recargar las categorías completas (fuente de verdad de reglas)
    void CargarCategorias(string rutaCategoriasJson);
}
