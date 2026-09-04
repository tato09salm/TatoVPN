namespace miVPN.Services.Interfaces;

/// <summary>
/// Servicio para la gestión y blindaje de DNS en adaptadores de red físicos de Windows utilizando WMI (System.Management).
/// Elimina fugas DNS provocadas por Smart Multi-Homed Name Resolution forzando 127.0.0.1 de forma exclusiva.
/// </summary>
public interface IDnsManagerService : IDisposable
{
    /// <summary>
    /// Guarda el estado original de los servidores DNS de todos los adaptadores físicos activos
    /// y los fuerza a utilizar exclusivamente 127.0.0.1 (Loopback).
    /// </summary>
    bool ForceLoopbackDnsOnPhysicalAdapters();

    /// <summary>
    /// Restaura la configuración DNS original (estática o DHCP) en los adaptadores físicos modificados.
    /// </summary>
    void RestorePhysicalAdaptersDns();
}
