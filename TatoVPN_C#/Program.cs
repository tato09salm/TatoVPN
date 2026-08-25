using System.Runtime.InteropServices;
using miVPN.Services.Implementations;

namespace miVPN;

static class Program
{
    private const string MutexName = "TatoVPN_SingleInstance_Mutex_9A8B7C6D";
    public static Mutex? AppMutex = null;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool ChangeWindowMessageFilter(uint message, uint dwFlag);

    public const uint MSGFLT_ADD = 1;
    public static readonly IntPtr HWND_BROADCAST = (IntPtr)0xffff;
    public static uint WM_SHOWME = 0;

    public static void ReleaseMutex()
    {
        try
        {
            if (AppMutex != null)
            {
                AppMutex.ReleaseMutex();
                AppMutex.Dispose();
                AppMutex = null;
            }
        }
        catch { }
    }

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        WM_SHOWME = RegisterWindowMessage("TatoVPN_WM_SHOWME_RESTORE");
        try { ChangeWindowMessageFilter(WM_SHOWME, MSGFLT_ADD); } catch { }

        bool isRestart = args != null && args.Contains("--restart");
        bool createdNew = false;

        try
        {
            AppMutex = new Mutex(true, MutexName, out createdNew);
            if (!createdNew && isRestart)
            {
                try
                {
                    createdNew = AppMutex.WaitOne(3000, false);
                }
                catch (AbandonedMutexException)
                {
                    createdNew = true;
                }
            }
        }
        catch (AbandonedMutexException)
        {
            createdNew = true;
        }
        catch
        {
            createdNew = true;
        }

        if (!createdNew)
        {
            // Otra instancia ya se encuentra en ejecución: enviar mensaje para mostrar la ventana y salir inmediatamente
            PostMessage(HWND_BROADCAST, WM_SHOWME, IntPtr.Zero, IntPtr.Zero);
            return;
        }

        AppDomain.CurrentDomain.ProcessExit += (s, e) => TunVpnService.EmergencyCleanup();
        Application.ApplicationExit += (s, e) => TunVpnService.EmergencyCleanup();

        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
        finally
        {
            ReleaseMutex();
            TunVpnService.EmergencyCleanup();
            Environment.Exit(0);
        }
    }    
}