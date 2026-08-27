"use client";

import { useState } from "react";
import {
  Terminal,
  Server,
  Key,
  Shield,
  Zap,
  CheckCircle,
  AlertCircle,
  ArrowRight,
  Sparkles,
  Flame,
} from "lucide-react";
import TerminalViewer from "@/components/TerminalViewer";
import Link from "next/link";

export default function AutoTerminalPage() {
  const [ip, setIp] = useState("");
  const [sshPort, setSshPort] = useState("22");
  const [rootUser, setRootUser] = useState("root");
  const [rootPassword, setRootPassword] = useState("");
  const [name, setName] = useState("");

  // Puertos
  const [sslPort, setSslPort] = useState("443");
  const [dropbearPort, setDropbearPort] = useState("80");
  const [badvpnPort, setBadvpnPort] = useState("7300");
  const [openSshPort, setOpenSshPort] = useState("22");
  const [bannerText, setBannerText] = useState("TATO-VPN");

  const [loading, setLoading] = useState(false);
  const [logs, setLogs] = useState("");
  const [status, setStatus] = useState<"idle" | "running" | "success" | "error">(
    "idle"
  );
  const [errorMsg, setErrorMsg] = useState("");

  const handleStartProvision = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!ip || !rootPassword) {
      setErrorMsg("Debes ingresar la IP y la contraseña de root.");
      return;
    }

    setErrorMsg("");
    setLoading(true);
    setStatus("running");
    setLogs(
      `[>] Conectando vía SSH a ${ip}:${sshPort} con usuario '${rootUser}'...\n` +
        `[>] Validando credenciales y sistema operativo de la máquina virtual...\n` +
        `[>] Configurando Banner Rojo '${bannerText}' y puertos (SSL ${sslPort}, Dropbear ${dropbearPort}, SSH ${openSshPort}, BadVPN ${badvpnPort})...\n`
    );

    try {
      const res = await fetch("/api/vps-provision", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: name || `VPS-${ip}`,
          ip,
          sshPort: Number(sshPort),
          rootUser,
          rootPassword,
          sslPort: Number(sslPort),
          dropbearPort: Number(dropbearPort),
          badvpnPort: Number(badvpnPort),
          openSshPort: Number(openSshPort),
          bannerText,
        }),
      });

      const data = await res.json();

      if (!res.ok || !data.success) {
        setStatus("error");
        setErrorMsg(data.error || "Error durante el aprovisionamiento");
        setLogs(
          (prev) =>
            prev +
            `\n[✗] ERROR: ${data.error}\n${data.details || ""}\n`
        );
      } else {
        setStatus("success");
        setLogs(
          (prev) =>
            prev +
            `\n${data.output}\n\n[✓] APROVISIONAMIENTO EXITOSO!\nLa VPS está registrada en PostgreSQL y lista para emitir usuarios para TatoVPN_C#.`
        );
      }
    } catch (err: any) {
      setStatus("error");
      setErrorMsg(err.message);
      setLogs((prev) => prev + `\n[✗] Error de red: ${err.message}\n`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Header */}
      <div>
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-tato-green-dark/20 border border-tato-green-neon/30 text-tato-green-neon text-xs font-semibold mb-2">
          <Sparkles className="w-3.5 h-3.5" />
          <span>One-Click VPS Setup</span>
        </div>
        <h2 className="text-2xl font-black text-white flex items-center gap-2">
          <Terminal className="w-6 h-6 text-tato-green-neon" />
          <span>Asistente de Aprovisionamiento Rápido SSH</span>
        </h2>
        <p className="text-xs text-gray-400 mt-1 max-w-2xl">
          Ingresa la IP y la contraseña de tu máquina virtual. El panel se conectará por SSH, abrirá los puertos configurados, instalará el Banner Rojo y dejará la VPS lista en la base de datos para crear usuarios.
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Provision Form */}
        <div className="lg:col-span-5 bg-tato-900 border border-tato-800 rounded-2xl p-6 shadow-xl space-y-4">
          <h3 className="text-sm font-bold text-white border-b border-tato-800 pb-3 flex items-center gap-2">
            <Server className="w-4 h-4 text-tato-orange" />
            <span>Datos de Acceso a la VM</span>
          </h3>

          {errorMsg && (
            <div className="p-3 rounded-xl bg-red-500/10 border border-red-500/30 text-red-400 text-xs flex items-center gap-2">
              <AlertCircle className="w-4 h-4 shrink-0" />
              <span>{errorMsg}</span>
            </div>
          )}

          <form onSubmit={handleStartProvision} className="space-y-4">
            <div>
              <label className="block text-xs font-semibold text-gray-300 mb-1">
                Etiqueta / Nombre VPS (Opcional)
              </label>
              <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Mi Servidor DigitalOcean / AWS / Vultr"
                className="w-full px-3.5 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white placeholder-gray-500 focus:outline-none focus:border-tato-green-neon"
              />
            </div>

            <div className="grid grid-cols-3 gap-3">
              <div className="col-span-2">
                <label className="block text-xs font-semibold text-gray-300 mb-1">
                  Dirección IP de la VM *
                </label>
                <input
                  type="text"
                  value={ip}
                  onChange={(e) => setIp(e.target.value)}
                  required
                  placeholder="142.93.100.5"
                  className="w-full px-3.5 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white font-mono placeholder-gray-500 focus:outline-none focus:border-tato-green-neon"
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-gray-300 mb-1">
                  Puerto SSH
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
                  Contraseña Root *
                </label>
                <input
                  type="password"
                  value={rootPassword}
                  onChange={(e) => setRootPassword(e.target.value)}
                  required
                  placeholder="••••••••••••"
                  className="w-full px-3.5 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white placeholder-gray-500 focus:outline-none focus:border-tato-green-neon"
                />
              </div>
            </div>

            {/* Banner SSH Rojo */}
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

            {/* Configured Ports */}
            <div className="p-3 rounded-xl bg-tato-850/60 border border-tato-800 space-y-2">
              <p className="text-[11px] font-bold uppercase text-tato-yellow">
                Puertos a Instalar y Abrir:
              </p>
              <div className="grid grid-cols-4 gap-2">
                <div>
                  <span className="text-[10px] text-gray-400 block">SSL/TLS</span>
                  <input
                    type="number"
                    value={sslPort}
                    onChange={(e) => setSslPort(e.target.value)}
                    className="w-full px-2 py-1 bg-tato-900 border border-tato-700 rounded text-xs text-white font-mono"
                  />
                </div>
                <div>
                  <span className="text-[10px] text-gray-400 block">Dropbear</span>
                  <input
                    type="number"
                    value={dropbearPort}
                    onChange={(e) => setDropbearPort(e.target.value)}
                    className="w-full px-2 py-1 bg-tato-900 border border-tato-700 rounded text-xs text-white font-mono"
                  />
                </div>
                <div>
                  <span className="text-[10px] text-gray-400 block">OpenSSH</span>
                  <input
                    type="number"
                    value={openSshPort}
                    onChange={(e) => setOpenSshPort(e.target.value)}
                    className="w-full px-2 py-1 bg-tato-900 border border-tato-700 rounded text-xs text-white font-mono"
                  />
                </div>
                <div>
                  <span className="text-[10px] text-gray-400 block">BadVPN</span>
                  <input
                    type="number"
                    value={badvpnPort}
                    onChange={(e) => setBadvpnPort(e.target.value)}
                    className="w-full px-2 py-1 bg-tato-900 border border-tato-700 rounded text-xs text-white font-mono"
                  />
                </div>
              </div>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full py-3 px-4 bg-gradient-to-r from-tato-green-dark via-tato-green-neon to-tato-green-light text-black font-extrabold rounded-xl text-xs shadow-lg shadow-tato-green-neon/20 hover:shadow-neon-green transition flex items-center justify-center gap-2 disabled:opacity-50"
            >
              {loading ? (
                <span>Ejecutando Aprovisionamiento...</span>
              ) : (
                <>
                  <Zap className="w-4 h-4 fill-black" />
                  <span>Iniciar Aprovisionamiento Automático</span>
                </>
              )}
            </button>
          </form>

          {status === "success" && (
            <div className="mt-4 p-4 rounded-xl bg-tato-green-dark/20 border border-tato-green-neon/50 text-tato-green-neon space-y-2">
              <div className="flex items-center gap-2 font-bold text-xs">
                <CheckCircle className="w-4 h-4" />
                <span>¡Servidor Listo y Configurado!</span>
              </div>
              <p className="text-[11px] text-gray-300">
                Los servicios Stunnel (443), OpenSSH (22), Dropbear (80, 442) y BadVPN están activos con Banner Rojo.
              </p>
              <Link
                href="/users"
                className="inline-flex items-center gap-1 text-xs font-bold text-black bg-tato-green-neon hover:bg-tato-green-light px-3 py-1.5 rounded-lg transition mt-1"
              >
                <span>Crear Usuario para esta VPS</span>
                <ArrowRight className="w-3.5 h-3.5" />
              </Link>
            </div>
          )}
        </div>

        {/* Terminal Output */}
        <div className="lg:col-span-7">
          <TerminalViewer
            logs={logs}
            title="Consola en Vivo SSH & Setup"
            isExecuting={loading}
          />
        </div>
      </div>
    </div>
  );
}
