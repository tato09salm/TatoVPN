"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  Server,
  Users,
  Terminal,
  Shield,
  LayoutDashboard,
  Settings,
  Flame,
  Radio,
  UserCheck,
} from "lucide-react";

export default function Sidebar() {
  const pathname = usePathname();

  const navItems = [
    {
      name: "Dashboard",
      href: "/",
      icon: LayoutDashboard,
      color: "text-tato-orange",
      hoverBorder: "hover:border-tato-orange/50",
    },
    {
      name: "Servidores VPS",
      href: "/vps",
      icon: Server,
      color: "text-tato-green-neon",
      hoverBorder: "hover:border-tato-green-neon/50",
    },
    {
      name: "Usuarios VPN",
      href: "/users",
      icon: Users,
      color: "text-tato-yellow",
      hoverBorder: "hover:border-tato-yellow/50",
    },
    {
      name: "Usuarios del Panel",
      href: "/admins",
      icon: UserCheck,
      color: "text-tato-orange-light",
      hoverBorder: "hover:border-tato-orange/50",
    },
    {
      name: "Auto-Terminal SSH",
      href: "/terminal",
      icon: Terminal,
      color: "text-tato-green-light",
      hoverBorder: "hover:border-tato-green-light/50",
    },
    {
      name: "Ajustes & BD",
      href: "/settings",
      icon: Settings,
      color: "text-gray-400",
      hoverBorder: "hover:border-gray-500",
    },
  ];

  return (
    <aside className="w-64 bg-tato-900 border-r border-tato-800 flex flex-col justify-between shrink-0 min-h-screen">
      <div>
        {/* Brand Header */}
        <div className="p-6 border-b border-tato-800 flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-tato-orange to-amber-500 flex items-center justify-center shadow-neon-orange">
            <Flame className="w-6 h-6 text-black" />
          </div>
          <div>
            <div className="flex items-center gap-1.5">
              <span className="font-extrabold text-lg text-white tracking-wider">
                TATO<span className="text-tato-orange">VPN</span>
              </span>
              <span className="text-[10px] uppercase font-bold px-1.5 py-0.5 bg-tato-orange/10 border border-tato-orange/30 text-tato-orange rounded">
                PANEL
              </span>
            </div>
            <p className="text-[11px] text-gray-400 font-mono">v1.0 Core Engine</p>
          </div>
        </div>

        {/* Server Status Live Box */}
        <div className="mx-4 my-4 p-3 rounded-lg bg-tato-850 border border-tato-700/60 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Radio className="w-4 h-4 text-tato-green-neon animate-pulse" />
            <div>
              <p className="text-xs font-semibold text-gray-200">Servicio Activo</p>
              <p className="text-[10px] text-tato-green-light">TatoVPN_C# Link Ready</p>
            </div>
          </div>
          <span className="w-2.5 h-2.5 rounded-full bg-tato-green-neon shadow-neon-green"></span>
        </div>

        {/* Navigation Menu */}
        <nav className="px-3 space-y-1.5 mt-2">
          {navItems.map((item) => {
            const isActive = pathname === item.href;
            const Icon = item.icon;

            return (
              <Link
                key={item.href}
                href={item.href}
                className={`flex items-center gap-3 px-3.5 py-2.5 rounded-lg text-sm font-medium transition-all duration-200 border ${
                  isActive
                    ? "bg-tato-800 border-tato-orange/60 text-white shadow-sm"
                    : "border-transparent text-gray-400 hover:text-white hover:bg-tato-850 " +
                      item.hoverBorder
                }`}
              >
                <Icon
                  className={`w-5 h-5 ${
                    isActive ? "text-tato-orange" : item.color
                  }`}
                />
                <span>{item.name}</span>
                {isActive && (
                  <span className="ml-auto w-1.5 h-5 rounded-full bg-tato-orange"></span>
                )}
              </Link>
            );
          })}
        </nav>
      </div>

      {/* Footer Info */}
      <div className="p-4 m-3 rounded-xl bg-gradient-to-b from-tato-850 to-tato-950 border border-tato-800">
        <div className="flex items-center gap-2 mb-2">
          <Shield className="w-4 h-4 text-tato-yellow" />
          <span className="text-xs font-semibold text-gray-300">
            PostgreSQL Sync
          </span>
        </div>
        <p className="text-[11px] text-gray-400 font-mono">
          DB: <span className="text-tato-yellow font-semibold">tatovpn</span> (5432)
        </p>
      </div>
    </aside>
  );
}
