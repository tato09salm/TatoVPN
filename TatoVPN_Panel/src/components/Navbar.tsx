"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { LogOut, User, Zap, Terminal } from "lucide-react";
import Link from "next/link";

interface NavbarProps {
  userEmail?: string;
  userName?: string;
}

export default function Navbar({
  userEmail = "admin@tatovpn.com",
  userName = "Admin",
}: NavbarProps) {
  const router = useRouter();
  const [loggingOut, setLoggingOut] = useState(false);

  const handleLogout = async () => {
    setLoggingOut(true);
    try {
      await fetch("/api/auth/logout", { method: "POST" });
      router.push("/login");
      router.refresh();
    } catch (err) {
      console.error(err);
    } finally {
      setLoggingOut(false);
    }
  };

  return (
    <header className="h-16 bg-tato-900/80 backdrop-blur-md border-b border-tato-800 px-6 flex items-center justify-between sticky top-0 z-30">
      <div className="flex items-center gap-3">
        <h1 className="text-sm font-medium text-gray-300">
          Panel de Control <span className="text-tato-orange font-bold">•</span>{" "}
          <span className="text-white font-semibold">TatoVPN System</span>
        </h1>
      </div>

      <div className="flex items-center gap-4">
        {/* Quick Launch Terminal Assistant */}
        <Link
          href="/terminal"
          className="flex items-center gap-2 px-3 py-1.5 rounded-lg bg-tato-850 hover:bg-tato-800 border border-tato-green-dark/40 text-tato-green-neon text-xs font-semibold transition shadow-sm hover:shadow-neon-green"
        >
          <Terminal className="w-3.5 h-3.5" />
          <span>Configurar VPS Rápido</span>
        </Link>

        {/* User Info Badge */}
        <div className="flex items-center gap-3 pl-2 border-l border-tato-800">
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 rounded-full bg-tato-800 border border-tato-orange/40 flex items-center justify-center text-tato-orange">
              <User className="w-4 h-4" />
            </div>
            <div className="hidden sm:block text-left">
              <p className="text-xs font-medium text-white leading-tight">
                {userName}
              </p>
              <p className="text-[10px] text-gray-400 font-mono">{userEmail}</p>
            </div>
          </div>

          {/* Logout Button */}
          <button
            onClick={handleLogout}
            disabled={loggingOut}
            title="Cerrar sesión"
            className="p-2 rounded-lg bg-tato-850 text-gray-400 hover:text-red-400 hover:bg-red-500/10 border border-transparent hover:border-red-500/30 transition text-xs flex items-center gap-1"
          >
            <LogOut className="w-4 h-4" />
          </button>
        </div>
      </div>
    </header>
  );
}
