"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Flame, Lock, Mail, ArrowRight, ShieldCheck, AlertCircle } from "lucide-react";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("admin@tatovpn.com");
  const [password, setPassword] = useState("admin123");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      const res = await fetch("/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password }),
      });

      const data = await res.json();

      if (!res.ok) {
        throw new Error(data.error || "Error al iniciar sesión");
      }

      router.push("/");
      router.refresh();
    } catch (err: any) {
      setError(err.message || "Credenciales incorrectas");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen w-full flex items-center justify-center p-4 bg-grid-pattern relative overflow-hidden bg-tato-950">
      {/* Dynamic Background Glows */}
      <div className="absolute top-1/4 left-1/3 -translate-x-1/2 -translate-y-1/2 w-96 h-96 bg-tato-orange/15 rounded-full blur-3xl pointer-events-none" />
      <div className="absolute bottom-1/4 right-1/3 translate-x-1/2 translate-y-1/2 w-96 h-96 bg-tato-green-dark/20 rounded-full blur-3xl pointer-events-none" />

      {/* Login Card */}
      <div className="w-full max-w-md relative z-10">
        <div className="bg-tato-900/90 backdrop-blur-xl border border-tato-800 rounded-3xl p-8 shadow-2xl shadow-black/80">
          {/* Logo & Header */}
          <div className="text-center mb-8">
            <div className="inline-flex p-3 rounded-2xl bg-gradient-to-tr from-tato-orange via-amber-500 to-tato-yellow text-black mb-4 shadow-neon-orange">
              <Flame className="w-8 h-8" />
            </div>
            <h2 className="text-2xl font-black text-white tracking-wider flex items-center justify-center gap-1.5">
              TATO<span className="text-tato-orange">VPN</span>
              <span className="text-xs px-2 py-0.5 rounded bg-tato-orange/10 border border-tato-orange/30 text-tato-orange font-bold uppercase tracking-normal">
                Panel
              </span>
            </h2>
            <p className="text-xs text-gray-400 mt-1">
              Acceso Administrativo al Gestor de Servidores y Cuentas
            </p>
          </div>

          {/* Error Message */}
          {error && (
            <div className="mb-6 p-3 rounded-xl bg-red-500/10 border border-red-500/30 text-red-400 text-xs flex items-center gap-2">
              <AlertCircle className="w-4 h-4 shrink-0" />
              <span>{error}</span>
            </div>
          )}

          {/* Login Form */}
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-xs font-semibold text-gray-300 mb-1.5">
                Correo Electrónico
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-gray-500">
                  <Mail className="w-4 h-4" />
                </div>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                  placeholder="admin@tatovpn.com"
                  className="w-full pl-10 pr-4 py-2.5 bg-tato-850 border border-tato-700 rounded-xl text-sm text-white placeholder-gray-500 focus:outline-none focus:border-tato-orange focus:ring-1 focus:ring-tato-orange transition font-mono"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-gray-300 mb-1.5">
                Contraseña
              </label>
              <div className="relative">
                <div className="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-gray-500">
                  <Lock className="w-4 h-4" />
                </div>
                <input
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                  placeholder="••••••••"
                  className="w-full pl-10 pr-4 py-2.5 bg-tato-850 border border-tato-700 rounded-xl text-sm text-white placeholder-gray-500 focus:outline-none focus:border-tato-orange focus:ring-1 focus:ring-tato-orange transition"
                />
              </div>
            </div>

            <div className="pt-2">
              <button
                type="submit"
                disabled={loading}
                className="w-full py-3 px-4 bg-gradient-to-r from-tato-orange via-amber-500 to-tato-orange bg-[length:200%_auto] hover:bg-right transition-all duration-300 text-black font-bold rounded-xl text-sm shadow-lg shadow-tato-orange/20 hover:shadow-neon-orange flex items-center justify-center gap-2 group disabled:opacity-50"
              >
                <span>{loading ? "Accediendo..." : "Iniciar Sesión"}</span>
                <ArrowRight className="w-4 h-4 group-hover:translate-x-1 transition-transform" />
              </button>
            </div>
          </form>

          {/* Quick Notice Info */}
          <div className="mt-6 pt-5 border-t border-tato-800 flex items-center justify-between text-[11px] text-gray-400">
            <div className="flex items-center gap-1.5">
              <ShieldCheck className="w-3.5 h-3.5 text-tato-green-neon" />
              <span>PostgreSQL Conectado</span>
            </div>
            <span className="font-mono text-gray-500">v1.0 Ready</span>
          </div>
        </div>
      </div>
    </div>
  );
}
