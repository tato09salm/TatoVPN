namespace miVPN.Models;

public class ArchivoRemoto
{
    public string Nombre { get; set; } = string.Empty;
    public string RutaCompleta { get; set; } = string.Empty;
    public bool EsCarpeta { get; set; }
    public long Tamano { get; set; }
    public DateTime FechaModificacion { get; set; }

    public string Tipo => EsCarpeta ? "Carpeta de archivos" : ObtenerTipoArchivo(Nombre);

    public string TamanoFormateado
    {
        get
        {
            if (EsCarpeta) return "--";
            if (Tamano < 1024) return $"{Tamano} B";
            if (Tamano < 1024 * 1024) return $"{(Tamano / 1024.0):F1} KB";
            if (Tamano < 1024 * 1024 * 1024) return $"{(Tamano / (1024.0 * 1024.0)):F2} MB";
            return $"{(Tamano / (1024.0 * 1024.0 * 1024.0)):F2} GB";
        }
    }

    private static string ObtenerTipoArchivo(string nombre)
    {
        string ext = Path.GetExtension(nombre).ToLowerInvariant();
        return ext switch
        {
            ".txt" => "Documento de texto",
            ".pdf" => "Documento PDF",
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "Archivo comprimido",
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp" or ".ico" => "Imagen",
            ".mp4" or ".mkv" or ".avi" or ".mov" or ".wmv" => "Video",
            ".mp3" or ".wav" or ".ogg" or ".flac" or ".m4a" => "Audio",
            ".json" or ".xml" or ".yaml" or ".yml" or ".ini" or ".cfg" or ".conf" => "Archivo de configuración/datos",
            ".exe" or ".msi" or ".bat" or ".cmd" or ".ps1" => "Ejecutable / Script",
            ".dll" or ".so" or ".dylib" => "Biblioteca de vínculos",
            ".cs" or ".js" or ".ts" or ".py" or ".html" or ".css" or ".cpp" or ".c" or ".h" => "Código fuente",
            ".doc" or ".docx" => "Documento Word",
            ".xls" or ".xlsx" or ".csv" => "Hoja de cálculo",
            _ => string.IsNullOrEmpty(ext) ? "Archivo" : $"Archivo {ext.TrimStart('.').ToUpperInvariant()}"
        };
    }
}
