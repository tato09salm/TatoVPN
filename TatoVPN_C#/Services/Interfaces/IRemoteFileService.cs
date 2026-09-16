using miVPN.Models;

namespace miVPN.Services.Interfaces;

public interface IRemoteFileService : IDisposable
{
    bool IsConnected { get; }
    string CurrentPath { get; }
    Task<(bool Success, string ErrorMessage)> ConectarAsync(string host, int port, string username, string password);
    void Desconectar();
    Task<List<ArchivoRemoto>> ListarDirectorioAsync(string? ruta = null);
    Task<string> CambiarDirectorioAsync(string nuevaRuta);
    Task<string> SubirDirectorioPadreAsync();
    Task DescargarArchivoAsync(string rutaRemota, string rutaLocal, Action<double>? onProgreso = null, CancellationToken ct = default);
    Task SubirArchivoAsync(string rutaLocal, string rutaRemota, Action<double>? onProgreso = null, CancellationToken ct = default);
    Task EliminarAsync(string rutaRemota, bool esCarpeta);
    Task CrearCarpetaAsync(string rutaRemota);
    Task RenombrarAsync(string rutaAntigua, string rutaNueva);
}
