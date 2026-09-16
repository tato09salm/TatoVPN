using System.Diagnostics;
using System.ServiceProcess;

namespace miVPN.Services.Implementations;

public enum WindowsOpenSshStatus
{
    NotInstalled,
    InstalledStopped,
    InstalledRunning
}

/// <summary>
/// Servicio para verificar, instalar y orquestar el Servidor OpenSSH nativo de Windows (sshd)
/// utilizado como backend para el módulo de Conexión Remota (SFTP).
/// </summary>
public static class WindowsOpenSshService
{
    public const string SshdServiceName = "sshd";
    public const int OpenSshPort = 22;

    /// <summary>
    /// Verifica el estado actual del Servidor OpenSSH de Windows.
    /// Funciona sin requerir permisos de administrador.
    /// </summary>
    public static WindowsOpenSshStatus GetStatus()
    {
        try
        {
            using var sc = new ServiceController(SshdServiceName);
            var status = sc.Status;
            if (status == ServiceControllerStatus.Running || status == ServiceControllerStatus.StartPending)
            {
                return WindowsOpenSshStatus.InstalledRunning;
            }
            return WindowsOpenSshStatus.InstalledStopped;
        }
        catch (InvalidOperationException)
        {
            // El servicio sshd no está registrado en el Service Control Manager
            // Verificamos si los binarios existen en System32\OpenSSH\sshd.exe
            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            bool binaryExists = File.Exists(Path.Combine(winDir, "System32", "OpenSSH", "sshd.exe")) ||
                                File.Exists(Path.Combine(winDir, "Sysnative", "OpenSSH", "sshd.exe"));

            return binaryExists ? WindowsOpenSshStatus.InstalledStopped : WindowsOpenSshStatus.NotInstalled;
        }
        catch
        {
            return WindowsOpenSshStatus.NotInstalled;
        }
    }

    /// <summary>
    /// Instala el Servidor OpenSSH como Característica Opcional de Windows y lo inicia en modo Automático.
    /// Requiere elevación de Administrador (UAC explícito).
    /// </summary>
    public static async Task<(bool Success, string Message)> InstallAndStartAsync()
    {
        try
        {
            // Comando PowerShell para instalar la característica e iniciar el servicio
            string script = "Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0; " +
                            "Set-Service -Name sshd -StartupType Automatic; " +
                            "Start-Service sshd";

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = true,
                Verb = "runas", // Muestra el diálogo UAC explícito de Windows
                WindowStyle = ProcessWindowStyle.Normal
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return (false, "No se pudo iniciar el proceso con permisos de administrador.");
            }

            await process.WaitForExitAsync();

            var status = GetStatus();
            if (status == WindowsOpenSshStatus.InstalledRunning)
            {
                return (true, "Servidor OpenSSH de Windows instalado y en ejecución correctamente.");
            }
            else if (status == WindowsOpenSshStatus.InstalledStopped)
            {
                return (true, "Servidor OpenSSH instalado. El servicio está detenido pero listo para activarse.");
            }
            else
            {
                return (false, "El proceso de instalación terminó pero no se detectó el servicio 'sshd' activo.");
            }
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223) // ERROR_CANCELLED
        {
            return (false, "La instalación fue cancelada por el usuario (se requieren permisos de administrador).");
        }
        catch (Exception ex)
        {
            return (false, $"Error al instalar Servidor OpenSSH: {ex.Message}");
        }
    }

    /// <summary>
    /// Inicia el servicio sshd de Windows y lo configura en inicio automático.
    /// Si la app no tiene permisos suficientes, solicita elevación mediante UAC.
    /// </summary>
    public static async Task<(bool Success, string Message)> StartServiceAsync()
    {
        // 1. Intentar inicio directo con ServiceController (si la app ya corre elevada)
        try
        {
            using var sc = new ServiceController(SshdServiceName);
            if (sc.Status != ServiceControllerStatus.Running && sc.Status != ServiceControllerStatus.StartPending)
            {
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
            }
            return (true, "Servicio OpenSSH iniciado correctamente.");
        }
        catch
        {
            // 2. Si falla por falta de permisos, pedir elevación UAC para ejecutar Start-Service sshd
            try
            {
                string script = "Set-Service -Name sshd -StartupType Automatic; Start-Service sshd";
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = true,
                    Verb = "runas", // UAC explícito
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                }

                if (GetStatus() == WindowsOpenSshStatus.InstalledRunning)
                {
                    return (true, "Servicio OpenSSH iniciado correctamente.");
                }

                return (false, "No se pudo iniciar el servicio sshd de OpenSSH.");
            }
            catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                return (false, "Operación cancelada por el usuario (UAC denegado).");
            }
            catch (Exception ex)
            {
                return (false, $"Error al iniciar el servicio: {ex.Message}");
            }
        }
    }
}
