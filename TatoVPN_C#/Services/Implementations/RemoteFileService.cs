using System.Net.Sockets;
using miVPN.Models;
using miVPN.Services.Interfaces;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;

namespace miVPN.Services.Implementations;

public class RemoteFileService : IRemoteFileService
{
    private SftpClient? _sftpClient;
    private string _currentPath = "/";

    public bool IsConnected => _sftpClient != null && _sftpClient.IsConnected;
    public string CurrentPath => _currentPath;

    public async Task<(bool Success, string ErrorMessage)> ConectarAsync(string host, int port, string username, string password)
    {
        return await Task.Run(() =>
        {
            Desconectar();

            if (string.IsNullOrWhiteSpace(host))
                return (false, "El host o túnel remoto no puede estar vacío.");

            if (port <= 0 || port > 65535)
                return (false, "El puerto ingresado no es válido.");

            if (string.IsNullOrWhiteSpace(username))
                username = "tatouser";

            try
            {
                var keyboardAuth = new KeyboardInteractiveAuthenticationMethod(username);
                keyboardAuth.AuthenticationPrompt += (s, e) =>
                {
                    foreach (var prompt in e.Prompts)
                    {
                        if (prompt.Request.IndexOf("password", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            prompt.Response = password;
                        }
                    }
                };

                var passwordAuth = new PasswordAuthenticationMethod(username, password);

                var connectionInfo = new ConnectionInfo(host.Trim(), port, username.Trim(), passwordAuth, keyboardAuth)
                {
                    Timeout = TimeSpan.FromSeconds(15)
                };

                _sftpClient = new SftpClient(connectionInfo)
                {
                    OperationTimeout = TimeSpan.FromSeconds(30)
                };

                _sftpClient.Connect();

                if (_sftpClient.IsConnected)
                {
                    _currentPath = string.IsNullOrWhiteSpace(_sftpClient.WorkingDirectory) ? "/" : _sftpClient.WorkingDirectory;
                    return (true, string.Empty);
                }

                return (false, "No se pudo establecer la sesión SFTP.");
            }
            catch (SshAuthenticationException)
            {
                Desconectar();
                return (false, "Error de autenticación: Usuario o contraseña incorrectos en el servidor remoto.");
            }
            catch (SocketException ex)
            {
                Desconectar();
                return (false, $"No se pudo conectar al host o puerto ({ex.Message}). Verifica que el Modo Servidor esté encendido en la laptop remota y que el túnel esté activo.");
            }
            catch (SshOperationTimeoutException)
            {
                Desconectar();
                return (false, "Tiempo de espera agotado al conectar. Verifica tu conexión a internet o si el túnel remoto sigue abierto.");
            }
            catch (Exception ex)
            {
                Desconectar();
                return (false, $"Error al conectar: {ex.Message}");
            }
        });
    }

    public void Desconectar()
    {
        try
        {
            if (_sftpClient != null)
            {
                if (_sftpClient.IsConnected)
                {
                    _sftpClient.Disconnect();
                }
                _sftpClient.Dispose();
            }
        }
        catch { }
        finally
        {
            _sftpClient = null;
            _currentPath = "/";
        }
    }

    public async Task<List<ArchivoRemoto>> ListarDirectorioAsync(string? ruta = null)
    {
        return await Task.Run(() =>
        {
            if (_sftpClient == null || !_sftpClient.IsConnected)
                throw new InvalidOperationException("No hay una sesión SFTP activa.");

            string targetPath = string.IsNullOrWhiteSpace(ruta) ? _sftpClient.WorkingDirectory : ruta;
            if (string.IsNullOrWhiteSpace(targetPath))
                targetPath = "/";

            var files = _sftpClient.ListDirectory(targetPath);
            var result = new List<ArchivoRemoto>();

            foreach (var f in files)
            {
                if (f.Name == "." || f.Name == "..")
                    continue;

                result.Add(new ArchivoRemoto
                {
                    Nombre = f.Name,
                    RutaCompleta = f.FullName,
                    EsCarpeta = f.IsDirectory,
                    Tamano = f.Length,
                    FechaModificacion = f.LastWriteTime
                });
            }

            _currentPath = targetPath;

            return result
                .OrderByDescending(r => r.EsCarpeta)
                .ThenBy(r => r.Nombre, StringComparer.OrdinalIgnoreCase)
                .ToList();
        });
    }

    public async Task<string> CambiarDirectorioAsync(string nuevaRuta)
    {
        return await Task.Run(() =>
        {
            if (_sftpClient == null || !_sftpClient.IsConnected)
                throw new InvalidOperationException("No hay una sesión SFTP activa.");

            _sftpClient.ChangeDirectory(nuevaRuta);
            _currentPath = _sftpClient.WorkingDirectory;
            return _currentPath;
        });
    }

    public async Task<string> SubirDirectorioPadreAsync()
    {
        return await Task.Run(() =>
        {
            if (_sftpClient == null || !_sftpClient.IsConnected)
                throw new InvalidOperationException("No hay una sesión SFTP activa.");

            try
            {
                _sftpClient.ChangeDirectory("..");
                _currentPath = _sftpClient.WorkingDirectory;
            }
            catch
            {
                // Si falla ".." (por ejemplo en raíz o rutas absolutas)
                string p = _currentPath.TrimEnd('/');
                int lastSlash = p.LastIndexOf('/');
                string parent = lastSlash > 0 ? p.Substring(0, lastSlash) : "/";
                _sftpClient.ChangeDirectory(parent);
                _currentPath = _sftpClient.WorkingDirectory;
            }

            return _currentPath;
        });
    }

    public async Task DescargarArchivoAsync(string rutaRemota, string rutaLocal, Action<double>? onProgreso = null, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            if (_sftpClient == null || !_sftpClient.IsConnected)
                throw new InvalidOperationException("No hay una sesión SFTP activa.");

            var fileInfo = _sftpClient.Get(rutaRemota);
            ulong totalBytes = (ulong)fileInfo.Length;

            using var fs = new FileStream(rutaLocal, FileMode.Create, FileAccess.Write, FileShare.None);
            _sftpClient.DownloadFile(rutaRemota, fs, transferred =>
            {
                if (ct.IsCancellationRequested)
                    throw new OperationCanceledException(ct);

                if (totalBytes > 0)
                {
                    double pct = Math.Min(100.0, (double)transferred / totalBytes * 100.0);
                    onProgreso?.Invoke(pct);
                }
            });
        }, ct);
    }

    public async Task SubirArchivoAsync(string rutaLocal, string rutaRemota, Action<double>? onProgreso = null, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            if (_sftpClient == null || !_sftpClient.IsConnected)
                throw new InvalidOperationException("No hay una sesión SFTP activa.");

            using var fs = new FileStream(rutaLocal, FileMode.Open, FileAccess.Read, FileShare.Read);
            ulong totalBytes = (ulong)fs.Length;

            _sftpClient.UploadFile(fs, rutaRemota, transferred =>
            {
                if (ct.IsCancellationRequested)
                    throw new OperationCanceledException(ct);

                if (totalBytes > 0)
                {
                    double pct = Math.Min(100.0, (double)transferred / totalBytes * 100.0);
                    onProgreso?.Invoke(pct);
                }
            });
        }, ct);
    }

    public async Task EliminarAsync(string rutaRemota, bool esCarpeta)
    {
        await Task.Run(() =>
        {
            if (_sftpClient == null || !_sftpClient.IsConnected)
                throw new InvalidOperationException("No hay una sesión SFTP activa.");

            if (esCarpeta)
            {
                EliminarDirectorioRecursivo(rutaRemota);
            }
            else
            {
                _sftpClient.DeleteFile(rutaRemota);
            }
        });
    }

    private void EliminarDirectorioRecursivo(string path)
    {
        if (_sftpClient == null || !_sftpClient.IsConnected) return;

        var items = _sftpClient.ListDirectory(path);
        foreach (var item in items)
        {
            if (item.Name == "." || item.Name == "..") continue;
            if (item.IsDirectory)
            {
                EliminarDirectorioRecursivo(item.FullName);
            }
            else
            {
                _sftpClient.DeleteFile(item.FullName);
            }
        }
        _sftpClient.DeleteDirectory(path);
    }

    public async Task CrearCarpetaAsync(string rutaRemota)
    {
        await Task.Run(() =>
        {
            if (_sftpClient == null || !_sftpClient.IsConnected)
                throw new InvalidOperationException("No hay una sesión SFTP activa.");

            _sftpClient.CreateDirectory(rutaRemota);
        });
    }

    public async Task RenombrarAsync(string rutaAntigua, string rutaNueva)
    {
        await Task.Run(() =>
        {
            if (_sftpClient == null || !_sftpClient.IsConnected)
                throw new InvalidOperationException("No hay una sesión SFTP activa.");

            _sftpClient.RenameFile(rutaAntigua, rutaNueva);
        });
    }

    public void Dispose()
    {
        Desconectar();
        GC.SuppressFinalize(this);
    }
}
