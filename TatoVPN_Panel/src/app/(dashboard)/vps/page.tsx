"use client";

import { useState, useEffect } from "react";
import {
  Server,
  Plus,
  Play,
  Terminal,
  Trash2,
  Edit2,
  RefreshCw,
  CheckCircle,
  AlertTriangle,
  Zap,
  Globe,
  Radio,
  Flame,
} from "lucide-react";
import Modal from "@/components/Modal";
import TerminalViewer from "@/components/TerminalViewer";

interface Vps {
  id: string;
  name: string;
  ip: string;
  sshPort: number;
  rootUser: string;
  rootPassword?: string;
  status: string;
  location: string;
  sslPort: number;
  openSshPort: number;
  badvpnPort: number;
  dropbearPort: number;
  isConfigured: boolean;
  lastChecked?: string;
  _count?: { vpnAccounts: number };
}

export default function VpsPage() {
  const [servers, setServers] = useState<Vps[]>([]);
  const [loading, setLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingServer, setEditingServer] = useState<Vps | null>(null);

  // Form State
  const [name, setName] = useState("");
  const [ip, setIp] = useState("");
  const [sshPort, setSshPort] = useState("22");
  const [rootUser, setRootUser] = useState("root");
  const [rootPassword, setRootPassword] = useState("");
  const [sslPort, setSslPort] = useState("443");
  const [dropbearPort, setDropbearPort] = useState("80");
  const [badvpnPort, setBadvpnPort] = useState("7300");
  const [openSshPort, setOpenSshPort] = useState("22");
  const [bannerText, setBannerText] = useState("TATO-VPN");
  const [autoSetup, setAutoSetup] = useState(true);
  const [location, setLocation] = useState("USA / Miami");
  const [formError, setFormError] = useState("");
  const [saving, setSaving] = useState(false);

  // Terminal & Execution State
  const [terminalOpen, setTerminalOpen] = useState(false);
  const [terminalLogs, setTerminalLogs] = useState("");
  const [isExecuting, setIsExecuting] = useState(false);
  const [executingServerName, setExecutingServerName] = useState("");

  const loadServers = async () => {
    try {
      setLoading(true);
      const res = await fetch("/api/vps");
      const data = await res.json();
      if (res.ok) {
        setServers(data.servers || []);
      }
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadServers();
  }, []);

  const openCreateModal = () => {
    setEditingServer(null);
    setName("");
    setIp("");
    setSshPort("22");
    setRootUser("root");
    setRootPassword("");
    setSslPort("443");
    setDropbearPort("80");
    setBadvpnPort("7300");
    setOpenSshPort("22");
    setBannerText("TATO-VPN");
    setAutoSetup(true);
    setLocation("USA / Dallas");
    setFormError("");
    setIsModalOpen(true);
  };

  const openEditModal = (s: Vps) => {
    setEditingServer(s);
    setName(s.name);
    setIp(s.ip);
    setSshPort(String(s.sshPort));
    setRootUser(s.rootUser);
    setRootPassword(s.rootPassword || "");
    setSslPort(String(s.sslPort));
    setDropbearPort(String(s.dropbearPort));
    setBadvpnPort(String(s.badvpnPort));
    setOpenSshPort(String(s.openSshPort || 22));
    setBannerText("TATO-VPN");
    setAutoSetup(false);
    setLocation(s.location);
    setFormError("");
    setIsModalOpen(true);
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError("");
    setSaving(true);

    if (autoSetup && !rootPassword) {
      setFormError("Para configurar y abrir puertos automáticamente debes ingresar la contraseña de root.");
      setSaving(false);
      return;
    }

    if (autoSetup) {
      setTerminalOpen(true);
      setExecutingServerName(name || ip);
      setIsExecuting(true);
      setTerminalLogs(
        `[>] Guardando servidor y ejecutando Auto-Setup en ${ip}...\n` +
          `[>] Configurando puertos (SSL: ${sslPort}, Dropbear: ${dropbearPort}, BadVPN: ${badvpnPort})...\n` +
          `[>] Aplicando Banner Rojo '${bannerText}'...\n`
      );
    }

    try {
      const payload = {
        name,
        ip,
        sshPort: Number(sshPort),
        rootUser,
        rootPassword,
        sslPort: Number(sslPort),
        dropbearPort: Number(dropbearPort),
        badvpnPort: Number(badvpnPort),
        openSshPort: Number(openSshPort),
        socksPort: 1080,
        bannerText,
        autoSetup,
        location,
      };

      const url = editingServer ? `/api/vps/${editingServer.id}` : "/api/vps";
      const method = editingServer ? "PUT" : "POST";

      const res = await fetch(url, {
        method,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });

      const data = await res.json();
      if (!res.ok) throw new Error(data.error || "Error al guardar el servidor");

      if (autoSetup && data.setupExecuted) {
        if (data.setupSuccess) {
          setTerminalLogs((prev) => prev + `\n${data.setupOutput}\n[✓] VPS CONFIGURADA Y PUERTOS ABIERTOS!`);
        } else {
          setTerminalLogs((prev) => prev + `\n[✗] ERROR DURANTE EL SETUP:\n${data.setupError || data.setupOutput}`);
        }
      }

      setIsModalOpen(false);
      loadServers();
    } catch (err: any) {
      setFormError(err.message);
      if (autoSetup) {
        setTerminalLogs((prev) => prev + `\n[✗] Error: ${err.message}\n`);
      }
    } finally {
      setSaving(false);
      setIsExecuting(false);
    }
  };

  const handleDelete = async (id: string, sName: string) => {
    if (!confirm(`¿Estás seguro de eliminar el servidor '${sName}'?`)) return;
    try {
      const res = await fetch(`/api/vps/${id}`, { method: "DELETE" });
      if (res.ok) loadServers();
    } catch (err) {
      console.error(err);
    }
  };

  const handleTestConnection = async (s: Vps) => {
    setTerminalOpen(true);
    setExecutingServerName(s.name);
    setIsExecuting(true);
    setTerminalLogs(`[>] Conectando por SSH a ${s.ip}:${s.sshPort} como ${s.rootUser}...\n`);

    try {
      const res = await fetch(`/api/vps/${s.id}/test`, { method: "POST" });
      const data = await res.json();

      if (res.ok && data.success) {
        setTerminalLogs(
          (prev) =>
            prev +
            `\n[✓] CONEXIÓN EXITOSA!\nDetalles del servidor:\n${data.output}\n`
        );
      } else {
        setTerminalLogs(
          (prev) =>
            prev +
            `\n[✗] ERROR DE CONEXIÓN:\n${data.error || "Fallo de autenticación SSH"}\n`
        );
      }
      loadServers();
    } catch (err: any) {
      setTerminalLogs((prev) => prev + `\n[✗] Error: ${err.message}\n`);
    } finally {
      setIsExecuting(false);
    }
  };

  const handleRunSetup = async (s: Vps) => {
    const customBanner = prompt("Texto del Banner Rojo:", "TATO-VPN") || "TATO-VPN";

    setTerminalOpen(true);
    setExecutingServerName(s.name);
    setIsExecuting(true);
    setTerminalLogs(
      `[>] Iniciando Auto-Setup de TatoVPN en ${s.name} (${s.ip})...\n[>] Instalando y configurando Stunnel, OpenSSH, Dropbear, BadVPN UDP y Banner '${customBanner}'...\n`
    );

    try {
      const res = await fetch(`/api/vps/${s.id}/setup`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ bannerText: customBanner }),
      });
      const data = await res.json();

      if (res.ok && data.success) {
        setTerminalLogs(
          (prev) => prev + `\n${data.output}\n[✓] SETUP COMPLETADO CON ÉXITO!`
        );
      } else {
        setTerminalLogs(
          (prev) =>
            prev +
            `\n[✗] HUBO UN ERROR DURANTE EL SETUP:\n${
              data.error || data.output || "Error desconocido"
            }\n`
        );
      }
      loadServers();
    } catch (err: any) {
      setTerminalLogs((prev) => prev + `\n[✗] Error de red: ${err.message}\n`);
    } finally {
      setIsExecuting(false);
    }
  };

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-black text-white flex items-center gap-2">
            <Server className="w-6 h-6 text-tato-green-neon" />
            <span>Servidores VPS</span>
          </h2>
          <p className="text-xs text-gray-400 mt-1">
            Administra tus máquinas virtuales, puertos variables (SSL, Dropbear, OpenSSH, BadVPN) y Banner Rojo para TatoVPN_C#.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={loadServers}
            className="p-2.5 rounded-xl bg-tato-850 hover:bg-tato-800 border border-tato-700 text-gray-300 hover:text-white transition"
            title="Refrescar"
          >
            <RefreshCw className={`w-4 h-4 ${loading ? "animate-spin" : ""}`} />
          </button>
          <button
            onClick={openCreateModal}
            className="px-4 py-2.5 rounded-xl bg-gradient-to-r from-tato-green-dark to-tato-green-neon text-black font-bold text-xs shadow-lg shadow-tato-green-neon/20 hover:shadow-neon-green transition flex items-center gap-2"
          >
            <Plus className="w-4 h-4" />
            <span>Agregar Servidor VPS</span>
          </button>
        </div>
      </div>

      {/* Terminal View Section if open */}
      {terminalOpen && (
        <div className="mb-6">
          <div className="flex items-center justify-between mb-2">
            <span className="text-xs font-mono text-tato-green-neon">
              Terminal en Vivo: {executingServerName}
            </span>
            <button
              onClick={() => setTerminalOpen(false)}
              className="text-xs text-gray-400 hover:text-white underline font-mono"
            >
              [Ocultar Terminal]
            </button>
          </div>
          <TerminalViewer
            logs={terminalLogs}
            title={`Consola SSH - ${executingServerName}`}
            isExecuting={isExecuting}
          />
        </div>
      )}

      {/* Server List */}
      {loading && servers.length === 0 ? (
        <div className="text-center py-20 text-gray-500 font-mono text-xs">
          Cargando servidores desde PostgreSQL...
        </div>
      ) : servers.length === 0 ? (
        <div className="p-12 text-center rounded-2xl bg-tato-900 border border-dashed border-tato-800">
          <Server className="w-12 h-12 text-gray-600 mx-auto mb-3" />
          <h3 className="text-sm font-bold text-white">No tienes servidores VPS registrados</h3>
          <p className="text-xs text-gray-400 mt-1 max-w-sm mx-auto">
            Agrega tu primera máquina virtual para abrir puertos automáticamente con Banner Rojo y comenzar a emitir usuarios.
          </p>
          <button
            onClick={openCreateModal}
            className="mt-4 px-4 py-2 bg-tato-green-neon text-black font-bold rounded-xl text-xs hover:bg-tato-green-light transition"
          >
            + Agregar VPS Ahora
          </button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {servers.map((s) => (
            <div
              key={s.id}
              className="rounded-2xl bg-tato-900/90 border border-tato-800 p-5 flex flex-col justify-between hover:border-tato-700 transition shadow-lg relative overflow-hidden group"
            >
              {/* Status Light Bar */}
              <div
                className={`absolute top-0 left-0 right-0 h-1 ${
                  s.status === "ONLINE"
                    ? "bg-tato-green-neon"
                    : s.status === "CONFIGURING"
                    ? "bg-tato-yellow"
                    : "bg-red-500"
                }`}
              />

              <div>
                {/* Header */}
                <div className="flex items-start justify-between gap-2 mb-3">
                  <div>
                    <div className="flex items-center gap-2">
                      <h3 className="font-extrabold text-white text-base">{s.name}</h3>
                      <span
                        className={`text-[10px] font-bold px-2 py-0.5 rounded-full border ${
                          s.status === "ONLINE"
                            ? "bg-tato-green-dark/20 text-tato-green-neon border-tato-green-dark/40"
                            : "bg-red-500/10 text-red-400 border-red-500/30"
                        }`}
                      >
                        {s.status}
                      </span>
                    </div>
                    <p className="text-xs text-gray-400 flex items-center gap-1 mt-0.5">
                      <Globe className="w-3 h-3 text-tato-orange" />
                      <span>{s.location}</span>
                    </p>
                  </div>

                  <div className="flex items-center gap-1">
                    <button
                      onClick={() => openEditModal(s)}
                      className="p-1.5 rounded-lg bg-tato-850 hover:bg-tato-800 text-gray-400 hover:text-white transition"
                      title="Editar"
                    >
                      <Edit2 className="w-3.5 h-3.5" />
                    </button>
                    <button
                      onClick={() => handleDelete(s.id, s.name)}
                      className="p-1.5 rounded-lg bg-tato-850 hover:bg-red-500/20 text-gray-400 hover:text-red-400 transition"
                      title="Eliminar"
                    >
                      <Trash2 className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>

                {/* IP & Credentials Info */}
                <div className="p-3 rounded-xl bg-tato-850 border border-tato-800 font-mono text-xs space-y-1 text-gray-300 mb-4">
                  <div className="flex justify-between">
                    <span className="text-gray-500">IP Host:</span>
                    <span className="text-white font-bold">{s.ip}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-500">SSH Root:</span>
                    <span>
                      {s.rootUser}:{s.sshPort}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-500">Usuarios VPN:</span>
                    <span className="text-tato-yellow font-bold">
                      {s._count?.vpnAccounts || 0} activos
                    </span>
                  </div>
                </div>

                {/* Ports Badges */}
                <div className="space-y-1.5 mb-4">
                  <span className="text-[11px] font-semibold uppercase text-gray-400 block">
                    Puertos Activos:
                  </span>
                  <div className="flex flex-wrap gap-1.5 text-[11px] font-mono">
                    <span className="px-2 py-0.5 rounded bg-tato-800 text-tato-orange border border-tato-orange/30">
                      SSL: {s.sslPort}
                    </span>
                    <span className="px-2 py-0.5 rounded bg-tato-800 text-tato-green-neon border border-tato-green-neon/30">
                      SSH: {s.openSshPort}
                    </span>
                    <span className="px-2 py-0.5 rounded bg-tato-800 text-tato-yellow border border-tato-yellow/30">
                      Dropbear: {s.dropbearPort}, 442
                    </span>
                    <span className="px-2 py-0.5 rounded bg-tato-800 text-purple-400 border border-purple-500/30">
                      BadVPN: {s.badvpnPort}
                    </span>
                  </div>
                </div>
              </div>

              {/* Action Buttons */}
              <div className="pt-3 border-t border-tato-800 flex gap-2">
                <button
                  onClick={() => handleTestConnection(s)}
                  className="flex-1 py-2 px-2.5 rounded-lg bg-tato-850 hover:bg-tato-800 border border-tato-700 text-gray-200 text-xs font-semibold flex items-center justify-center gap-1.5 transition"
                >
                  <Radio className="w-3.5 h-3.5 text-tato-green-neon" />
                  <span>Probar SSH</span>
                </button>
                <button
                  onClick={() => handleRunSetup(s)}
                  className="flex-1 py-2 px-2.5 rounded-lg bg-tato-orange/15 hover:bg-tato-orange/25 border border-tato-orange/40 text-tato-orange text-xs font-bold flex items-center justify-center gap-1.5 transition shadow-sm hover:shadow-neon-orange"
                >
                  <Zap className="w-3.5 h-3.5" />
                  <span>Auto-Setup</span>
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Modal Agregar / Editar VPS */}
      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title={editingServer ? "Editar Servidor VPS" : "Agregar Nuevo Servidor VPS"}
        subtitle="Registra y abre los puertos automáticamente con Banner personalizado"
        maxWidth="lg"
      >
        {formError && (
          <div className="mb-4 p-3 rounded-xl bg-red-500/10 border border-red-500/30 text-red-400 text-xs">
            {formError}
          </div>
        )}

        <form onSubmit={handleSave} className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold text-gray-300 mb-1">
                Nombre del Servidor
              </label>
              <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
                placeholder="VPS Premium 1"
                className="w-full px-3.5 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white placeholder-gray-500 focus:outline-none focus:border-tato-green-neon"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-gray-300 mb-1">
                Ubicación / Región
              </label>
              <input
                type="text"
                value={location}
                onChange={(e) => setLocation(e.target.value)}
                placeholder="USA / Miami"
                className="w-full px-3.5 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white placeholder-gray-500 focus:outline-none focus:border-tato-green-neon"
              />
            </div>
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div className="col-span-2">
              <label className="block text-xs font-semibold text-gray-300 mb-1">
                Dirección IP o Dominio *
              </label>
              <input
                type="text"
                value={ip}
                onChange={(e) => setIp(e.target.value)}
                required
                placeholder="192.168.1.1"
                className="w-full px-3.5 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white font-mono placeholder-gray-500 focus:outline-none focus:border-tato-green-neon"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-gray-300 mb-1">
                Puerto SSH Base
              </label>
              <input
                type="number"
                value={sshPort}
                onChange={(e) => setSshPort(e.target.value)}
                required
                className="w-full px-3.5 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white font-mono focus:outline-none focus:border-tato-green-neon"
              />
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-semibold text-gray-300 mb-1">
                Usuario Root
              </label>
              <input
                type="text"
                value={rootUser}
                onChange={(e) => setRootUser(e.target.value)}
                required
                className="w-full px-3.5 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white font-mono focus:outline-none focus:border-tato-green-neon"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-gray-300 mb-1">
                Contraseña Root (SSH) *
              </label>
              <input
                type="password"
                value={rootPassword}
                onChange={(e) => setRootPassword(e.target.value)}
                placeholder="Password de la VM"
                className="w-full px-3.5 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white placeholder-gray-500 focus:outline-none focus:border-tato-green-neon"
              />
            </div>
          </div>

          {/* Banner Rojo */}
          <div>
            <label className="block text-xs font-semibold text-gray-300 mb-1 flex items-center gap-1.5">
              <Flame className="w-3.5 h-3.5 text-red-500" />
              <span>Mensaje del Banner SSH (Color Rojo)</span>
            </label>
            <input
              type="text"
              value={bannerText}
              onChange={(e) => setBannerText(e.target.value)}
              placeholder="TATO-VPN"
              className="w-full px-3.5 py-2 bg-tato-850 border border-red-500/40 rounded-xl text-xs text-red-400 font-bold focus:outline-none focus:border-red-500"
            />
          </div>

          {/* Configuration Ports */}
          <div className="p-3.5 rounded-xl bg-tato-850/60 border border-tato-800 space-y-3">
            <p className="text-xs font-bold text-tato-yellow">
              Puertos Variables a Configurar y Abrir
            </p>
            <div className="grid grid-cols-4 gap-2">
              <div>
                <label className="block text-[11px] text-gray-400 mb-1">SSL/TLS</label>
                <input
                  type="number"
                  value={sslPort}
                  onChange={(e) => setSslPort(e.target.value)}
                  placeholder="443"
                  className="w-full px-2.5 py-1.5 bg-tato-900 border border-tato-700 rounded-lg text-xs text-white font-mono"
                />
              </div>
              <div>
                <label className="block text-[11px] text-gray-400 mb-1">Dropbear</label>
                <input
                  type="number"
                  value={dropbearPort}
                  onChange={(e) => setDropbearPort(e.target.value)}
                  placeholder="80"
                  className="w-full px-2.5 py-1.5 bg-tato-900 border border-tato-700 rounded-lg text-xs text-white font-mono"
                />
              </div>
              <div>
                <label className="block text-[11px] text-gray-400 mb-1">OpenSSH</label>
                <input
                  type="number"
                  value={openSshPort}
                  onChange={(e) => setOpenSshPort(e.target.value)}
                  placeholder="22"
                  className="w-full px-2.5 py-1.5 bg-tato-900 border border-tato-700 rounded-lg text-xs text-white font-mono"
                />
              </div>
              <div>
                <label className="block text-[11px] text-gray-400 mb-1">BadVPN UDP</label>
                <input
                  type="number"
                  value={badvpnPort}
                  onChange={(e) => setBadvpnPort(e.target.value)}
                  placeholder="7300"
                  className="w-full px-2.5 py-1.5 bg-tato-900 border border-tato-700 rounded-lg text-xs text-white font-mono"
                />
              </div>
            </div>
          </div>

          {/* Auto-Setup Checkbox */}
          <div className="p-3 rounded-xl bg-tato-850 border border-tato-green-dark/40 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Zap className="w-4 h-4 text-tato-green-neon" />
              <div>
                <p className="text-xs font-semibold text-white">
                  Abrir y configurar puertos automáticamente por SSH
                </p>
                <p className="text-[10px] text-gray-400">
                  Instala Stunnel, Dropbear, BadVPN y el Banner Rojo en la VM
                </p>
              </div>
            </div>
            <input
              type="checkbox"
              checked={autoSetup}
              onChange={(e) => setAutoSetup(e.target.checked)}
              className="w-4 h-4 accent-tato-green-neon rounded cursor-pointer"
            />
          </div>

          <div className="flex justify-end gap-3 pt-3">
            <button
              type="button"
              onClick={() => setIsModalOpen(false)}
              className="px-4 py-2 rounded-xl bg-tato-850 text-gray-400 hover:text-white text-xs font-semibold"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={saving}
              className="px-5 py-2 rounded-xl bg-tato-green-neon text-black text-xs font-bold hover:bg-tato-green-light transition disabled:opacity-50"
            >
              {saving ? "Configurando VPS..." : editingServer ? "Actualizar VPS" : "Guardar y Configurar"}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
