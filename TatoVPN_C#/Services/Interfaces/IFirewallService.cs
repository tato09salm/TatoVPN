namespace miVPN.Services.Interfaces;

/// <summary>
/// Servicio para la administración de reglas de seguridad en el Firewall de Windows mediante COM Interop (INetFwPolicy2).
/// Bloquea la evasión de protocolos (QUIC/HTTP3, WebRTC UDP) y mitiga fugas de paquetes.
/// </summary>
public interface IFirewallService : IDisposable
{
    /// <summary>
    /// Aplica las reglas del firewall para contener tráfico UDP no autorizado y permitir exclusivamente el puerto 53 local.
    /// </summary>
    bool ApplyUdpContainmentRules();

    /// <summary>
    /// Remueve las reglas temporales creadas por TatoVPN.
    /// </summary>
    void RemoveUdpContainmentRules();
}
