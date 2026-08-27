"use client";

import { useState } from "react";
import {
  Settings,
  Database,
  Shield,
  Key,
  CheckCircle,
  AlertCircle,
  HardDrive,
  Lock,
  UserCheck,
  ArrowRight,
  ShieldCheck,
} from "lucide-react";
import Link from "next/link";

export default function SettingsPage() {
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [msg, setMsg] = useState<{ type: "success" | "error"; text: string } | null>(
    null
  );
  const [loading, setLoading] = useState(false);

  const handleUpdatePassword = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!currentPassword || !newPassword) {
      setMsg({ type: "error", text: "Ingresa la contraseña actual y la nueva" });
      return;
    }

    if (newPassword.length < 6) {
      setMsg({
        type: "error",
        text: "La nueva contraseña debe tener al menos 6 caracteres",
      });
      return;
    }

    if (newPassword !== confirmPassword) {
      setMsg({ type: "error", text: "Las contraseñas no coinciden" });
      return;
    }

    setLoading(true);
    setMsg(null);

    try {
      const res = await fetch("/api/auth/change-password", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          currentPassword,
          newPassword,
        }),
      });

      const data = await res.json();
      if (!res.ok) {
        throw new Error(data.error || "Error al actualizar contraseña");
      }

      setMsg({
        type: "success",
        text: "Contraseña administrativa actualizada correctamente",
      });
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
    } catch (err: any) {
      setMsg({ type: "error", text: err.message });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-6 max-w-5xl mx-auto">
      {/* Header */}
      <div>
        <h2 className="text-2xl font-black text-white flex items-center gap-2">
          <Settings className="w-6 h-6 text-gray-400" />
          <span>Configuración del Panel & Base de Datos</span>
        </h2>
        <p className="text-xs text-gray-400 mt-1">
          Ajustes del servidor web, conexión PostgreSQL, usuarios de acceso y seguridad administrativa.
        </p>
      </div>

      {/* Quick Link Card: Panel Users */}
      <div className="rounded-2xl bg-gradient-to-r from-tato-900 via-tato-850 to-tato-900 border border-tato-orange/30 p-6 shadow-xl flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-4">
          <div className="p-3 rounded-2xl bg-tato-orange/15 border border-tato-orange/30 text-tato-orange shrink-0 shadow-neon-orange">
            <UserCheck className="w-6 h-6" />
          </div>
          <div>
            <h3 className="text-sm font-bold text-white flex items-center gap-2">
              <span>Gestión de Usuarios del Panel</span>
              <span className="text-[10px] font-bold px-2 py-0.5 rounded bg-tato-orange/20 text-tato-orange border border-tato-orange/40">
                Nuevo Módulo
              </span>
            </h3>
            <p className="text-xs text-gray-400 mt-0.5">
              Crea nuevos usuarios, asigna roles y gestiona credenciales de acceso a <code className="text-tato-yellow">http://localhost:3000/login</code>.
            </p>
          </div>
        </div>

        <Link
          href="/admins"
          className="px-4 py-2.5 rounded-xl bg-tato-orange hover:bg-tato-orange-light text-black font-bold text-xs transition flex items-center justify-center gap-2 shrink-0 shadow-lg shadow-tato-orange/20"
        >
          <span>Administrar Usuarios</span>
          <ArrowRight className="w-4 h-4" />
        </Link>
      </div>

      {/* DB Connection Status Card */}
      <div className="rounded-2xl bg-tato-900 border border-tato-800 p-6 shadow-xl space-y-4">
        <div className="flex items-center justify-between border-b border-tato-800 pb-4">
          <div className="flex items-center gap-3">
            <div className="p-2.5 rounded-xl bg-tato-yellow/10 border border-tato-yellow/30 text-tato-yellow">
              <Database className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-white">
                Base de Datos PostgreSQL (Activa)
              </h3>
              <p className="text-xs text-gray-400">
                Sincronización mediante Prisma ORM
              </p>
            </div>
          </div>
          <span className="flex items-center gap-1.5 px-3 py-1 rounded-full bg-tato-green-dark/20 border border-tato-green-neon/30 text-tato-green-neon text-xs font-semibold">
            <CheckCircle className="w-3.5 h-3.5" />
            <span>Conectado</span>
          </span>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 font-mono text-xs">
          <div className="p-3.5 rounded-xl bg-tato-850 border border-tato-800">
            <span className="text-gray-400 block text-[11px]">Host / Puerto</span>
            <span className="text-white font-bold">localhost:5432</span>
          </div>
          <div className="p-3.5 rounded-xl bg-tato-850 border border-tato-800">
            <span className="text-gray-400 block text-[11px]">Nombre de Base de Datos</span>
            <span className="text-tato-yellow font-bold">tatovpn</span>
          </div>
          <div className="p-3.5 rounded-xl bg-tato-850 border border-tato-800">
            <span className="text-gray-400 block text-[11px]">Usuario DB</span>
            <span className="text-tato-orange font-bold">postgres</span>
          </div>
        </div>
      </div>

      {/* Security & Admin Credentials Card */}
      <div className="rounded-2xl bg-tato-900 border border-tato-800 p-6 shadow-xl space-y-4">
        <div className="flex items-center gap-3 border-b border-tato-800 pb-4">
          <div className="p-2.5 rounded-xl bg-tato-orange/10 border border-tato-orange/30 text-tato-orange">
            <Lock className="w-5 h-5" />
          </div>
          <div>
            <h3 className="text-sm font-bold text-white">Cambiar Mi Contraseña de Acceso</h3>
            <p className="text-xs text-gray-400">
              Actualiza la contraseña del usuario actualmente en sesión
            </p>
          </div>
        </div>

        {msg && (
          <div
            className={`p-3 rounded-xl text-xs flex items-center gap-2 border ${
              msg.type === "success"
                ? "bg-tato-green-dark/20 text-tato-green-neon border-tato-green-neon/40"
                : "bg-red-500/10 text-red-400 border-red-500/30"
            }`}
          >
            {msg.type === "success" ? (
              <CheckCircle className="w-4 h-4 shrink-0" />
            ) : (
              <AlertCircle className="w-4 h-4 shrink-0" />
            )}
            <span>{msg.text}</span>
          </div>
        )}

        <form onSubmit={handleUpdatePassword} className="space-y-4 max-w-md">
          <div>
            <label className="block text-xs font-semibold text-gray-300 mb-1">
              Contraseña Actual *
            </label>
            <input
              type="password"
              required
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              placeholder="••••••••"
              className="w-full px-3.5 py-2.5 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white focus:outline-none focus:border-tato-orange"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-gray-300 mb-1">
              Nueva Contraseña (Mínimo 6 caracteres) *
            </label>
            <input
              type="password"
              required
              minLength={6}
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              placeholder="••••••••"
              className="w-full px-3.5 py-2.5 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white focus:outline-none focus:border-tato-orange"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-gray-300 mb-1">
              Confirmar Nueva Contraseña *
            </label>
            <input
              type="password"
              required
              minLength={6}
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              placeholder="••••••••"
              className="w-full px-3.5 py-2.5 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white focus:outline-none focus:border-tato-orange"
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="px-5 py-2.5 rounded-xl bg-tato-orange text-black font-bold text-xs hover:bg-tato-orange-light transition shadow-lg shadow-tato-orange/20"
          >
            {loading ? "Actualizando..." : "Actualizar Contraseña"}
          </button>
        </form>
      </div>
    </div>
  );
}
