"use client";

import { useState, useEffect } from "react";
import Link from "next/link";
import {
  Users,
  Plus,
  Trash2,
  Edit2,
  RefreshCw,
  Eye,
  EyeOff,
  Copy,
  Check,
  Server,
  Calendar,
  Shield,
  Key,
  ExternalLink,
  Sparkles,
  UserCheck,
} from "lucide-react";
import Modal from "@/components/Modal";

interface VpnAccount {
  id: string;
  username: string;
  password: string;
  vpsId: string;
  connectionLimit: number;
  expirationDate?: string | null;
  isActive: boolean;
  notes?: string | null;
  createdAt: string;
  vps: {
    id: string;
    name: string;
    ip: string;
    sshPort: number;
    sslPort: number;
    dropbearPort: number;
    badvpnPort: number;
    location?: string;
  };
}

interface VpsOption {
  id: string;
  name: string;
  ip: string;
  sslPort: number;
  openSshPort: number;
}

export default function UsersPage() {
  const [accounts, setAccounts] = useState<VpnAccount[]>([]);
  const [servers, setServers] = useState<VpsOption[]>([]);
  const [loading, setLoading] = useState(true);
  const [filterVps, setFilterVps] = useState("");
  const [searchTerm, setSearchTerm] = useState("");

  // Visible Passwords State (Set of IDs)
  const [visiblePasswords, setVisiblePasswords] = useState<Record<string, boolean>>({});
  const [copiedId, setCopiedId] = useState<string | null>(null);

  // Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingAccount, setEditingAccount] = useState<VpnAccount | null>(null);

  // Form State
  const [serverMode, setServerMode] = useState<"vps" | "external">("vps");
  const [externalIp, setExternalIp] = useState("");
  const [externalPort, setExternalPort] = useState("443");
  const [externalType, setExternalType] = useState<"SSL" | "SSH" | "DROPBEAR" | "CUSTOM">("SSL");
  const [serverName, setServerName] = useState("");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [vpsId, setVpsId] = useState("");
  const [expirationDays, setExpirationDays] = useState("30");
  const [connectionLimit, setConnectionLimit] = useState("999");
  const [notes, setNotes] = useState("");
  const [syncToLinux, setSyncToLinux] = useState(true);
  const [formError, setFormError] = useState("");
  const [saving, setSaving] = useState(false);

  // Copy Config Modal
  const [configModalOpen, setConfigModalOpen] = useState(false);
  const [activeAccountConfig, setActiveAccountConfig] = useState<VpnAccount | null>(null);

  const loadData = async () => {
    try {
      setLoading(true);
      const [usersRes, vpsRes] = await Promise.all([
        fetch("/api/vpn-users"),
        fetch("/api/vps"),
      ]);

      const usersData = await usersRes.json();
      const vpsData = await vpsRes.json();

      if (usersRes.ok) setAccounts(usersData.accounts || []);
      if (vpsRes.ok) setServers(vpsData.servers || []);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const generateRandomPassword = () => {
    const chars = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    let pass = "";
    for (let i = 0; i < 8; i++) {
      pass += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    setPassword(pass);
  };

  const openCreateModal = () => {
    setEditingAccount(null);
    setServerMode("vps");
    setExternalIp("");
    setExternalPort("443");
    setExternalType("SSL");
    setServerName("");
    setUsername("");
    generateRandomPassword();
    setVpsId(servers.length > 0 ? servers[0].id : "");
    setExpirationDays("30");
    setConnectionLimit("999");
    setNotes("");
    setSyncToLinux(true);
    setFormError("");
    setIsModalOpen(true);
  };

  const openEditModal = (acc: VpnAccount) => {
    setEditingAccount(acc);
    setServerMode("vps");
    setUsername(acc.username);
    setPassword(acc.password);
    setVpsId(acc.vpsId);
    setConnectionLimit(String(acc.connectionLimit));
    setNotes(acc.notes || "");
    setSyncToLinux(true);
    setFormError("");
    setIsModalOpen(true);
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError("");
    setSaving(true);

    try {
      const payload = {
        username,
        password,
        vpsId: serverMode === "vps" ? vpsId : undefined,
        mode: serverMode,
        externalIp: serverMode === "external" ? externalIp : undefined,
        externalPort: serverMode === "external" ? Number(externalPort) : undefined,
        externalType: serverMode === "external" ? externalType : undefined,
        serverName: serverMode === "external" ? serverName : undefined,
        expirationDays: editingAccount ? undefined : expirationDays,
        connectionLimit: Number(connectionLimit),
        notes,
        syncToLinux: serverMode === "vps" ? syncToLinux : false,
      };

      const url = editingAccount ? `/api/vpn-users/${editingAccount.id}` : "/api/vpn-users";
      const method = editingAccount ? "PUT" : "POST";

      const res = await fetch(url, {
        method,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });

      const data = await res.json();
      if (!res.ok) throw new Error(data.error || "Error al guardar el usuario");

      setIsModalOpen(false);
      loadData();
    } catch (err: any) {
      setFormError(err.message);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id: string, uname: string) => {
    if (!confirm(`¿Estás seguro de eliminar al usuario VPN '${uname}'?`)) return;
    try {
      const res = await fetch(`/api/vpn-users/${id}`, { method: "DELETE" });
      if (res.ok) loadData();
    } catch (err) {
      console.error(err);
    }
  };

  const togglePasswordVisibility = (id: string) => {
    setVisiblePasswords((prev) => ({
      ...prev,
      [id]: !prev[id],
    }));
  };

  const copyCreds = (acc: VpnAccount) => {
    const text = `Host: ${acc.vps.ip}\nPuerto SSH: ${acc.vps.sshPort}\nPuerto SSL/TLS: ${acc.vps.sslPort}\nUsuario: ${acc.username}\nContraseña: ${acc.password}`;
    navigator.clipboard.writeText(text);
    setCopiedId(acc.id);
    setTimeout(() => setCopiedId(null), 2000);
  };

  const openConfigModal = (acc: VpnAccount) => {
    setActiveAccountConfig(acc);
    setConfigModalOpen(true);
  };

  const filteredAccounts = accounts.filter((acc) => {
    const matchesVps = filterVps ? acc.vpsId === filterVps : true;
    const matchesSearch =
      acc.username.toLowerCase().includes(searchTerm.toLowerCase()) ||
      acc.vps.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      acc.vps.ip.includes(searchTerm);
    return matchesVps && matchesSearch;
  });

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-black text-white flex items-center gap-2">
            <Users className="w-6 h-6 text-tato-yellow" />
            <span>Usuarios VPN</span>
          </h2>
          <p className="text-xs text-gray-400 mt-1">
            Crea, edita y sincroniza cuentas SSH / SSL para usar en la aplicación cliente TatoVPN_C#.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <Link
            href="/admins"
            className="px-3.5 py-2 rounded-xl bg-tato-850 hover:bg-tato-800 border border-tato-orange/30 text-tato-orange text-xs font-semibold transition flex items-center gap-2"
          >
            <UserCheck className="w-3.5 h-3.5" />
            <span>Usuarios del Panel (Web)</span>
          </Link>
          <button
            onClick={loadData}
            className="p-2.5 rounded-xl bg-tato-850 hover:bg-tato-800 border border-tato-700 text-gray-300 hover:text-white transition"
            title="Refrescar"
          >
            <RefreshCw className={`w-4 h-4 ${loading ? "animate-spin" : ""}`} />
          </button>
          <button
            onClick={openCreateModal}
            className="px-4 py-2.5 rounded-xl bg-gradient-to-r from-tato-orange via-amber-500 to-tato-orange bg-[length:200%_auto] hover:bg-right text-black font-bold text-xs shadow-lg shadow-tato-orange/20 hover:shadow-neon-orange transition flex items-center gap-2"
          >
            <Plus className="w-4 h-4" />
            <span>Crear Usuario VPN</span>
          </button>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="flex flex-col sm:flex-row items-center gap-4 bg-tato-900/80 p-4 rounded-2xl border border-tato-800">
        <div className="flex-1 w-full">
          <input
            type="text"
            placeholder="Buscar por usuario o IP de VPS..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full px-4 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white placeholder-gray-500 focus:outline-none focus:border-tato-yellow"
          />
        </div>
        <div className="w-full sm:w-64">
          <select
            value={filterVps}
            onChange={(e) => setFilterVps(e.target.value)}
            className="w-full px-3 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white focus:outline-none focus:border-tato-yellow"
          >
            <option value="">Todas las VPS ({servers.length})</option>
            {servers.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name} ({s.ip})
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Users Table */}
      <div className="bg-tato-900 border border-tato-800 rounded-2xl overflow-hidden shadow-xl">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-tato-850/80 text-gray-400 font-semibold border-b border-tato-800">
              <tr>
                <th className="py-3.5 px-4">Usuario VPN</th>
                <th className="py-3.5 px-4">Contraseña</th>
                <th className="py-3.5 px-4">Servidor VPS</th>
                <th className="py-3.5 px-4">Expiración</th>
                <th className="py-3.5 px-4 text-center">Límite</th>
                <th className="py-3.5 px-4 text-center">Estado</th>
                <th className="py-3.5 px-4 text-right">Acciones</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-tato-800">
              {loading && accounts.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-gray-500 font-mono">
                    Cargando cuentas desde PostgreSQL...
                  </td>
                </tr>
              ) : filteredAccounts.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-12 text-center">
                    <Users className="w-8 h-8 text-gray-600 mx-auto mb-2" />
                    <p className="text-gray-400">No se encontraron usuarios</p>
                  </td>
                </tr>
              ) : (
                filteredAccounts.map((acc) => {
                  const isPassVisible = visiblePasswords[acc.id];
                  const isExp =
                    acc.expirationDate && new Date(acc.expirationDate) < new Date();

                  return (
                    <tr key={acc.id} className="hover:bg-tato-850/50 transition">
                      {/* Username */}
                      <td className="py-3.5 px-4 font-mono font-bold text-white">
                        <div className="flex items-center gap-2">
                          <span className="w-2 h-2 rounded-full bg-tato-orange"></span>
                          <span>{acc.username}</span>
                        </div>
                      </td>

                      {/* Password */}
                      <td className="py-3.5 px-4 font-mono">
                        <div className="flex items-center gap-2">
                          <span className="text-gray-300 font-bold bg-tato-850 px-2 py-1 rounded border border-tato-800">
                            {isPassVisible ? acc.password : "••••••••"}
                          </span>
                          <button
                            onClick={() => togglePasswordVisibility(acc.id)}
                            className="text-gray-400 hover:text-white p-1 rounded"
                            title={isPassVisible ? "Ocultar" : "Ver Contraseña"}
                          >
                            {isPassVisible ? (
                              <EyeOff className="w-3.5 h-3.5 text-tato-yellow" />
                            ) : (
                              <Eye className="w-3.5 h-3.5" />
                            )}
                          </button>
                        </div>
                      </td>

                      {/* VPS */}
                      <td className="py-3.5 px-4 font-mono">
                        <div>
                          <div className="flex items-center gap-1.5">
                            <span className="text-white font-semibold">{acc.vps.name}</span>
                            {acc.vps.location === "Servidor Externo / Manual" && (
                              <span className="px-1.5 py-0.2 text-[9px] font-bold bg-blue-500/20 text-blue-300 border border-blue-500/30 rounded">
                                Manual
                              </span>
                            )}
                          </div>
                          <span className="text-gray-400 text-[11px] block">
                            {acc.vps.ip}:{acc.vps.sslPort || acc.vps.sshPort || 443}
                          </span>
                        </div>
                      </td>

                      {/* Expiration */}
                      <td className="py-3.5 px-4">
                        {acc.expirationDate ? (
                          <div className="font-mono">
                            <span
                              className={isExp ? "text-red-400 font-bold" : "text-gray-300"}
                            >
                              {new Date(acc.expirationDate).toLocaleDateString()}
                            </span>
                            {isExp && (
                              <span className="text-[10px] text-red-400 block font-sans">
                                Expirado
                              </span>
                            )}
                          </div>
                        ) : (
                          <span className="text-tato-green-neon font-semibold text-[11px]">
                            Ilimitado
                          </span>
                        )}
                      </td>

                      {/* Limit */}
                      <td className="py-3.5 px-4 text-center font-mono">
                        <span className="px-2 py-0.5 rounded bg-tato-800 text-gray-300 border border-tato-700">
                          {acc.connectionLimit} Disp.
                        </span>
                      </td>

                      {/* Status */}
                      <td className="py-3.5 px-4 text-center">
                        <span
                          className={`px-2 py-0.5 rounded-full text-[10px] font-bold border ${
                            acc.isActive && !isExp
                              ? "bg-tato-green-dark/20 text-tato-green-neon border-tato-green-dark/40"
                              : "bg-red-500/10 text-red-400 border-red-500/30"
                          }`}
                        >
                          {acc.isActive && !isExp ? "ACTIVO" : "INACTIVO"}
                        </span>
                      </td>

                      {/* Actions */}
                      <td className="py-3.5 px-4 text-right">
                        <div className="flex items-center justify-end gap-1.5">
                          {/* Copy Creds button */}
                          <button
                            onClick={() => copyCreds(acc)}
                            className="p-1.5 rounded-lg bg-tato-850 hover:bg-tato-800 text-gray-400 hover:text-white transition"
                            title="Copiar Credenciales"
                          >
                            {copiedId === acc.id ? (
                              <Check className="w-3.5 h-3.5 text-tato-green-neon" />
                            ) : (
                              <Copy className="w-3.5 h-3.5" />
                            )}
                          </button>

                          {/* Open TatoVPN C# Config Format */}
                          <button
                            onClick={() => openConfigModal(acc)}
                            className="p-1.5 rounded-lg bg-tato-orange/10 hover:bg-tato-orange/20 text-tato-orange transition border border-tato-orange/30"
                            title="Ver Payload TatoVPN_C#"
                          >
                            <ExternalLink className="w-3.5 h-3.5" />
                          </button>

                          {/* Edit button */}
                          <button
                            onClick={() => openEditModal(acc)}
                            className="p-1.5 rounded-lg bg-tato-850 hover:bg-tato-800 text-gray-400 hover:text-white transition"
                            title="Editar"
                          >
                            <Edit2 className="w-3.5 h-3.5" />
                          </button>

                          {/* Delete button */}
                          <button
                            onClick={() => handleDelete(acc.id, acc.username)}
                            className="p-1.5 rounded-lg bg-tato-850 hover:bg-red-500/20 text-gray-400 hover:text-red-400 transition"
                            title="Eliminar"
                          >
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal Crear / Editar Usuario */}
      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title={editingAccount ? "Editar Usuario VPN" : "Crear Nuevo Usuario VPN"}
        subtitle="Genera credenciales de acceso para TatoVPN_C#"
        maxWidth="md"
      >
        {formError && (
          <div className="mb-4 p-3 rounded-xl bg-red-500/10 border border-red-500/30 text-red-400 text-xs">
            {formError}
          </div>
        )}

        <form onSubmit={handleSave} className="space-y-4">
          {!editingAccount && (
            <div className="flex bg-tato-950 p-1 rounded-xl border border-tato-800">
              <button
                type="button"
                onClick={() => setServerMode("vps")}
                className={`flex-1 py-2 px-2 text-xs font-bold rounded-lg transition flex items-center justify-center gap-1.5 ${
                  serverMode === "vps"
                    ? "bg-tato-orange text-black shadow-md"
                    : "text-gray-400 hover:text-white"
                }`}
              >
                <Server className="w-3.5 h-3.5" />
                <span>VPS Registrada</span>
              </button>
              <button
                type="button"
                onClick={() => setServerMode("external")}
                className={`flex-1 py-2 px-2 text-xs font-bold rounded-lg transition flex items-center justify-center gap-1.5 ${
                  serverMode === "external"
                    ? "bg-tato-orange text-black shadow-md"
                    : "text-gray-400 hover:text-white"
                }`}
              >
                <ExternalLink className="w-3.5 h-3.5" />
                <span>Servidor Externo / Manual (SSH/SSL)</span>
              </button>
            </div>
          )}

          {serverMode === "vps" ? (
            <div>
              <label className="block text-xs font-semibold text-gray-300 mb-1">
                Seleccionar Servidor VPS
              </label>
              {servers.length > 0 ? (
                <select
                  value={vpsId}
                  onChange={(e) => setVpsId(e.target.value)}
                  required
                  disabled={!!editingAccount}
                  className="w-full px-3.5 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white focus:outline-none focus:border-tato-orange"
                >
                  {servers.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.name} ({s.ip})
                    </option>
                  ))}
                </select>
              ) : (
                <div className="p-3 rounded-xl bg-amber-500/10 border border-amber-500/30 text-amber-300 text-xs">
                  No hay VPS registradas aún. Puedes cambiar a la pestaña <b>"Servidor Externo / Manual"</b> para agregar cuentas SSH o SSL directamente con IP y Puerto.
                </div>
              )}
            </div>
          ) : (
            /* Modo Servidor Externo / Manual */
            <div className="space-y-3 p-3.5 rounded-xl bg-tato-950/70 border border-tato-800">
              <div className="flex items-center justify-between">
                <span className="text-[11px] font-bold text-tato-orange flex items-center gap-1.5">
                  <Shield className="w-3.5 h-3.5" />
                  Datos de Conexión del Servidor Externo
                </span>
                <span className="text-[10px] text-gray-400">Sin droplet ni root requerido</span>
              </div>

              <div>
                <label className="block text-xs font-semibold text-gray-300 mb-1">
                  Dirección IP o Host
                </label>
                <input
                  type="text"
                  value={externalIp}
                  onChange={(e) => setExternalIp(e.target.value)}
                  required={serverMode === "external"}
                  placeholder="ej. 143.110.185.147 o midominio.com"
                  className="w-full px-3.5 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white font-mono placeholder-gray-500 focus:outline-none focus:border-tato-orange"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-gray-300 mb-1">
                    Tipo / Protocolo
                  </label>
                  <select
                    value={externalType}
                    onChange={(e) => {
                      const val = e.target.value as any;
                      setExternalType(val);
                      if (val === "SSL") setExternalPort("443");
                      else if (val === "SSH") setExternalPort("22");
                      else if (val === "DROPBEAR") setExternalPort("80");
                    }}
                    className="w-full px-3.5 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white focus:outline-none focus:border-tato-orange"
                  >
                    <option value="SSL">SSL / TLS (Puerto 443)</option>
                    <option value="SSH">SSH Directo (Puerto 22)</option>
                    <option value="DROPBEAR">Dropbear (Puerto 80 / 442)</option>
                    <option value="CUSTOM">Puerto Personalizado</option>
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-semibold text-gray-300 mb-1">
                    Puerto de Conexión
                  </label>
                  <input
                    type="number"
                    min="1"
                    max="65535"
                    value={externalPort}
                    onChange={(e) => setExternalPort(e.target.value)}
                    required={serverMode === "external"}
                    placeholder="443"
                    className="w-full px-3.5 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white font-mono focus:outline-none focus:border-tato-orange"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-gray-300 mb-1">
                  Nombre del Servidor (Opcional)
                </label>
                <input
                  type="text"
                  value={serverName}
                  onChange={(e) => setServerName(e.target.value)}
                  placeholder="ej. India1 SSL, Servidor Externo Demo"
                  className="w-full px-3.5 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white placeholder-gray-500 focus:outline-none focus:border-tato-orange"
                />
              </div>
            </div>
          )}

          <div>
            <label className="block text-xs font-semibold text-gray-300 mb-1">
              Nombre de Usuario
            </label>
            <input
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
              disabled={!!editingAccount}
              placeholder="cliente01"
              className="w-full px-3.5 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white font-mono placeholder-gray-500 focus:outline-none focus:border-tato-orange"
            />
          </div>

          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="block text-xs font-semibold text-gray-300">
                Contraseña
              </label>
              <button
                type="button"
                onClick={generateRandomPassword}
                className="text-[11px] text-tato-orange hover:underline flex items-center gap-1 font-semibold"
              >
                <Sparkles className="w-3 h-3" />
                <span>Generar Aleatoria</span>
              </button>
            </div>
            <input
              type="text"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              placeholder="Contraseña"
              className="w-full px-3.5 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white font-mono placeholder-gray-500 focus:outline-none focus:border-tato-orange"
            />
          </div>

          {!editingAccount && (
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-semibold text-gray-300 mb-1">
                  Días de Validez
                </label>
                <select
                  value={expirationDays}
                  onChange={(e) => setExpirationDays(e.target.value)}
                  className="w-full px-3.5 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white focus:outline-none focus:border-tato-orange"
                >
                  <option value="7">7 Días (Demo)</option>
                  <option value="15">15 Días</option>
                  <option value="30">30 Días (1 Mes)</option>
                  <option value="60">60 Días (2 Meses)</option>
                  <option value="90">90 Días (3 Meses)</option>
                  <option value="365">365 Días (1 Año)</option>
                  <option value="">Ilimitado</option>
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold text-gray-300 mb-1">
                  Límite Conexiones
                </label>
                <input
                  type="number"
                  min="1"
                  max="9999"
                  placeholder="999"
                  value={connectionLimit}
                  onChange={(e) => setConnectionLimit(e.target.value)}
                  className="w-full px-3.5 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white font-mono focus:outline-none focus:border-tato-orange"
                />
              </div>
            </div>
          )}

          <div>
            <label className="block text-xs font-semibold text-gray-300 mb-1">
              Notas / Comentarios (Opcional)
            </label>
            <input
              type="text"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              placeholder="Cliente Juan - Pago mensual"
              className="w-full px-3.5 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white placeholder-gray-500 focus:outline-none focus:border-tato-orange"
            />
          </div>

          {serverMode === "vps" ? (
            <div className="p-3 rounded-xl bg-tato-850 border border-tato-800 flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Key className="w-4 h-4 text-tato-green-neon" />
                <div>
                  <p className="text-xs font-semibold text-white">Sincronizar vía SSH en Linux</p>
                  <p className="text-[10px] text-gray-400">
                    Ejecuta useradd / chpasswd en la VPS automáticamente
                  </p>
                </div>
              </div>
              <input
                type="checkbox"
                checked={syncToLinux}
                onChange={(e) => setSyncToLinux(e.target.checked)}
                className="w-4 h-4 accent-tato-orange rounded cursor-pointer"
              />
            </div>
          ) : (
            <div className="p-3 rounded-xl bg-tato-850/60 border border-tato-800 flex items-center gap-2 text-[11px] text-gray-300">
              <Shield className="w-4 h-4 text-tato-orange flex-shrink-0" />
              <span>Cuenta manual guardada en el panel para emitir y conectar en <b>TatoVPN_C#</b>.</span>
            </div>
          )}

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
              className="px-5 py-2 rounded-xl bg-tato-orange text-black text-xs font-bold hover:bg-tato-orange-light transition disabled:opacity-50"
            >
              {saving ? "Creando..." : editingAccount ? "Actualizar Usuario" : "Crear Usuario"}
            </button>
          </div>
        </form>
      </Modal>

      {/* Modal Payload TatoVPN_C# */}
      <Modal
        isOpen={configModalOpen}
        onClose={() => setConfigModalOpen(false)}
        title="Datos de Conexión TatoVPN_C#"
        subtitle="Copia estos datos para utilizarlos directamente en el cliente de escritorio C#"
        maxWidth="lg"
      >
        {activeAccountConfig && (
          <div className="space-y-4">
            <div className="p-4 rounded-xl bg-tato-950 border border-tato-800 font-mono text-xs text-tato-green-neon space-y-2 select-text">
              <p className="text-gray-400 font-bold border-b border-tato-800 pb-1">
                // Configuración Directa TatoVPN:
              </p>
              <p>SSH Host: <span className="text-white">{activeAccountConfig.vps.ip}</span></p>
              <p>SSH Port: <span className="text-white">{activeAccountConfig.vps.sshPort}</span></p>
              <p>SSL/TLS Port: <span className="text-white">{activeAccountConfig.vps.sslPort}</span></p>
              <p>Username: <span className="text-white">{activeAccountConfig.username}</span></p>
              <p>Password: <span className="text-white">{activeAccountConfig.password}</span></p>
              <p>Socks Port: <span className="text-white">1080</span></p>
            </div>

            <div className="flex justify-end gap-3">
              <button
                onClick={() => {
                  const text = `Host: ${activeAccountConfig.vps.ip}\nPuerto SSH: ${activeAccountConfig.vps.sshPort}\nPuerto SSL/TLS: ${activeAccountConfig.vps.sslPort}\nUsuario: ${activeAccountConfig.username}\nContraseña: ${activeAccountConfig.password}`;
                  navigator.clipboard.writeText(text);
                  alert("¡Datos de conexión copiados al portapapeles!");
                }}
                className="px-4 py-2 rounded-xl bg-tato-orange text-black text-xs font-bold hover:bg-tato-orange-light transition flex items-center gap-2"
              >
                <Copy className="w-3.5 h-3.5" />
                <span>Copiar Datos Completos</span>
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
}
