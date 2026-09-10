using System.Runtime.InteropServices;
using miVPN.Services.Interfaces;

namespace miVPN.Services.Implementations;

/// <summary>
/// Implementación de seguridad perimetral local utilizando la API nativa de Windows Firewall
/// a través de COM Interop (INetFwPolicy2 / HNetCfg.FwPolicy2 y HNetCfg.FWRule).
/// Mitiga la fuga de tráfico UDP (QUIC/HTTP-3, WebRTC) y forza la resolución de nombres por el túnel.
/// </summary>
public class FirewallService : IFirewallService
{
    private readonly ILoggerService _logger;
    private bool _rulesApplied = false;

    // Nombres identificadores de las reglas administradas por TatoVPN
    public const string RuleNameBlockUdp = "TatoVPN_Block_Outbound_UDP";
    public const string RuleNameAllowLocalDns = "TatoVPN_Allow_Local_DNS_UDP";

    // Constantes de la API de Windows Firewall (INetFwRule / NET_FW_*)
    private const int NET_FW_IP_PROTOCOL_UDP = 17;
    private const int NET_FW_RULE_DIR_OUT = 2;
    private const int NET_FW_ACTION_BLOCK = 0;
    private const int NET_FW_ACTION_ALLOW = 1;
    private const int NET_FW_PROFILE2_ALL = 0x7FFFFFFF;

    public FirewallService(ILoggerService logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Aplica las reglas del firewall para contener tráfico UDP no autorizado.
    /// Bloquea todos los puertos UDP salientes excepto el puerto 53 (1-52, 54-65535).
    /// </summary>
    public bool ApplyUdpContainmentRules()
    {
        try
        {
            RemoveUdpContainmentRules();

            Type? policyType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            Type? ruleType = Type.GetTypeFromProgID("HNetCfg.FWRule");

            if (policyType == null || ruleType == null)
            {
                _logger.Log("⚠️ No se pudo instanciar la interfaz COM HNetCfg.FwPolicy2 del Firewall de Windows.");
                return false;
            }

            dynamic? fwPolicy = Activator.CreateInstance(policyType);
            if (fwPolicy == null)
            {
                _logger.Log("⚠️ Error al crear instancia COM de INetFwPolicy2.");
                return false;
            }

            try
            {
                dynamic rules = fwPolicy.Rules;

                // 1. Regla de Bloqueo de UDP saliente (excluyendo el puerto 53 para resolución DNS local)
                // Al bloquear los rangos 1-52 y 54-65535, se neutraliza QUIC (UDP 443) y WebRTC UDP
                dynamic udpBlockRule = Activator.CreateInstance(ruleType)!;
                udpBlockRule.Name = RuleNameBlockUdp;
                udpBlockRule.Description = "TatoVPN Security: Bloqueo de evasión UDP y mitigación QUIC/HTTP3";
                udpBlockRule.Protocol = NET_FW_IP_PROTOCOL_UDP;
                udpBlockRule.RemotePorts = "1-52,54-65535";
                udpBlockRule.Direction = NET_FW_RULE_DIR_OUT;
                udpBlockRule.Action = NET_FW_ACTION_BLOCK;
                udpBlockRule.Profiles = NET_FW_PROFILE2_ALL;
                udpBlockRule.Enabled = true;

                rules.Add(udpBlockRule);
                Marshal.ReleaseComObject(udpBlockRule);

                // 2. Regla explícita de Permiso para DNS Local (Loopback / Wintun puerto 53)
                dynamic dnsAllowRule = Activator.CreateInstance(ruleType)!;
                dnsAllowRule.Name = RuleNameAllowLocalDns;
                dnsAllowRule.Description = "TatoVPN Security: Permitir resolución DNS local en loopback";
                dnsAllowRule.Protocol = NET_FW_IP_PROTOCOL_UDP;
                dnsAllowRule.RemotePorts = "53";
                dnsAllowRule.RemoteAddresses = "127.0.0.1,10.255.0.1,10.255.0.2";
                dnsAllowRule.Direction = NET_FW_RULE_DIR_OUT;
                dnsAllowRule.Action = NET_FW_ACTION_ALLOW;
                dnsAllowRule.Profiles = NET_FW_PROFILE2_ALL;
                dnsAllowRule.Enabled = true;

                rules.Add(dnsAllowRule);
                Marshal.ReleaseComObject(dnsAllowRule);

                Marshal.ReleaseComObject(rules);

                _rulesApplied = true;
                _logger.Log("🛡️ Reglas de Firewall COM aplicadas: UDP/QUIC saliente mitigado (puerto 53 local protegido).");
                return true;
            }
            finally
            {
                if (fwPolicy != null)
                {
                    Marshal.ReleaseComObject(fwPolicy);
                }
            }
        }
        catch (COMException comEx)
        {
            _logger.Log($"⚠️ Error COM al configurar Firewall de Windows: {comEx.Message} (HRESULT: 0x{comEx.ErrorCode:X})");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Log($"⚠️ Error general en FirewallService: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Remueve de manera atómica las reglas temporales creadas en Windows Firewall.
    /// </summary>
    public void RemoveUdpContainmentRules()
    {
        EmergencyCleanupFirewallRules();
        _rulesApplied = false;
    }

    /// <summary>
    /// Limpieza estática de emergencia invocable desde ProcessExit o UnhandledException.
    /// </summary>
    public static void EmergencyCleanupFirewallRules()
    {
        try
        {
            // Intento 1: Netsh (más confiable y directo)
            try
            {
                using var proc1 = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall delete rule name=\"{RuleNameBlockUdp}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                proc1?.WaitForExit(2000);
            }
            catch { }

            try
            {
                using var proc2 = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall delete rule name=\"{RuleNameAllowLocalDns}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                proc2?.WaitForExit(2000);
            }
            catch { }

            // Intento 2: COM Interop
            Type? policyType = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            if (policyType == null) return;

            dynamic? fwPolicy = Activator.CreateInstance(policyType);
            if (fwPolicy == null) return;

            try
            {
                dynamic rules = fwPolicy.Rules;

                try { rules.Remove(RuleNameBlockUdp); } catch { }
                try { rules.Remove(RuleNameAllowLocalDns); } catch { }

                Marshal.ReleaseComObject(rules);
            }
            finally
            {
                Marshal.ReleaseComObject(fwPolicy);
            }
        }
        catch { }
    }

    public void Dispose()
    {
        if (_rulesApplied)
        {
            RemoveUdpContainmentRules();
        }
        GC.SuppressFinalize(this);
    }
}
