import { prisma } from "@/lib/prisma";
import StatCard from "@/components/StatCard";
import {
  Server,
  Users,
  ShieldCheck,
  Zap,
  Terminal,
  ArrowUpRight,
  HardDrive,
  Activity,
  PlusCircle,
} from "lucide-react";
import Link from "next/link";

export const dynamic = "force-dynamic";

export default async function DashboardPage() {
  const [
    totalServers,
    onlineServers,
    totalAccounts,
    activeAccounts,
    totalAdmins,
    recentServers,
    recentUsers,
  ] = await Promise.all([
    prisma.vpsServer.count(),
    prisma.vpsServer.count({ where: { status: "ONLINE" } }),
    prisma.vpnAccount.count(),
    prisma.vpnAccount.count({ where: { isActive: true } }),
    prisma.adminUser.count(),
    prisma.vpsServer.findMany({
      take: 4,
      orderBy: { createdAt: "desc" },
      include: { _count: { select: { vpnAccounts: true } } },
    }),
    prisma.vpnAccount.findMany({
      take: 5,
      orderBy: { createdAt: "desc" },
      include: { vps: { select: { name: true, ip: true } } },
    }),
  ]);

  return (
    <div className="space-y-8 max-w-7xl mx-auto">
      {/* Welcome Banner */}
      <div className="relative overflow-hidden rounded-3xl bg-gradient-to-r from-tato-900 via-tato-850 to-tato-900 border border-tato-800 p-8 shadow-xl">
        <div className="absolute right-0 top-0 bottom-0 w-1/3 bg-gradient-to-l from-tato-orange/10 to-transparent pointer-events-none" />
        <div className="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-6">
          <div>
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-tato-orange/10 border border-tato-orange/30 text-tato-orange text-xs font-semibold mb-3">
              <Zap className="w-3.5 h-3.5" />
              <span>TatoVPN Node Controller</span>
            </div>
            <h2 className="text-2xl md:text-3xl font-black text-white tracking-wide">
              Panel Administrativo <span className="text-tato-orange">TatoVPN</span>
            </h2>
            <p className="text-sm text-gray-400 mt-1 max-w-xl">
              Administra tus servidores VPS, aprovisiona puertos y gestiona cuentas SSH / SSL / BadVPN listas para conectar con <span className="text-white font-semibold">TatoVPN_C#</span>.
            </p>
          </div>

          <div className="flex flex-wrap gap-3">
            <Link
              href="/admins"
              className="px-4 py-2.5 rounded-xl bg-tato-850 hover:bg-tato-800 border border-tato-orange/40 text-tato-orange text-xs font-bold transition flex items-center gap-2 shadow-sm"
            >
              <PlusCircle className="w-4 h-4" />
              <span>Usuarios del Panel ({totalAdmins})</span>
            </Link>
            <Link
              href="/users"
              className="px-4 py-2.5 rounded-xl bg-tato-orange hover:bg-tato-orange-light text-black text-xs font-bold transition flex items-center gap-2 shadow-lg shadow-tato-orange/20"
            >
              <PlusCircle className="w-4 h-4" />
              <span>Nuevo Usuario VPN</span>
            </Link>
          </div>
        </div>
      </div>

      {/* Stats Overview */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
        <StatCard
          title="Total Servidores VPS"
          value={totalServers}
          subtitle={`${onlineServers} Activos / Online`}
          icon={Server}
          variant="orange"
        />
        <StatCard
          title="VPS Online"
          value={onlineServers}
          subtitle="Listos para conexión"
          icon={Activity}
          variant="green"
        />
        <StatCard
          title="Total Cuentas VPN"
          value={totalAccounts}
          subtitle={`${activeAccounts} Cuentas activas`}
          icon={Users}
          variant="yellow"
        />
        <StatCard
          title="Compatibilidad C#"
          value="100%"
          subtitle="SSH • SSL • BadVPN"
          icon={ShieldCheck}
          variant="blue"
        />
      </div>

      {/* Two Column Section */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* Recent VPS Servers */}
        <div className="bg-tato-900/90 border border-tato-800 rounded-2xl p-6 flex flex-col justify-between shadow-lg">
          <div>
            <div className="flex items-center justify-between mb-5">
              <div className="flex items-center gap-2.5">
                <div className="p-2 rounded-lg bg-tato-green-dark/20 text-tato-green-neon border border-tato-green-dark/30">
                  <HardDrive className="w-4 h-4" />
                </div>
                <div>
                  <h3 className="text-sm font-bold text-white">Servidores VPS Recientes</h3>
                  <p className="text-xs text-gray-400">Nodos configurados en el panel</p>
                </div>
              </div>
              <Link
                href="/vps"
                className="text-xs text-tato-orange hover:underline font-semibold flex items-center gap-1"
              >
                <span>Ver Todos</span>
                <ArrowUpRight className="w-3.5 h-3.5" />
              </Link>
            </div>

            {recentServers.length === 0 ? (
              <div className="text-center py-10 border border-dashed border-tato-800 rounded-xl">
                <Server className="w-8 h-8 text-gray-600 mx-auto mb-2" />
                <p className="text-xs text-gray-400">No hay servidores registrados aún</p>
                <Link
                  href="/vps"
                  className="mt-3 inline-block text-xs text-tato-green-neon font-bold hover:underline"
                >
                  + Agregar primera VPS
                </Link>
              </div>
            ) : (
              <div className="space-y-3">
                {recentServers.map((server) => (
                  <div
                    key={server.id}
                    className="p-3.5 rounded-xl bg-tato-850 border border-tato-800 flex items-center justify-between hover:border-tato-700 transition"
                  >
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="text-xs font-bold text-white">{server.name}</span>
                        <span
                          className={`w-2 h-2 rounded-full ${
                            server.status === "ONLINE"
                              ? "bg-tato-green-neon shadow-neon-green"
                              : "bg-gray-500"
                          }`}
                        />
                      </div>
                      <p className="text-[11px] text-gray-400 font-mono mt-0.5">
                        IP: {server.ip} • SSH: {server.sshPort} • SSL: {server.sslPort}
                      </p>
                    </div>
                    <div className="text-right">
                      <span className="text-xs font-bold text-tato-yellow">
                        {server._count.vpnAccounts}
                      </span>
                      <span className="text-[10px] text-gray-400 block">usuarios</span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Recent VPN Accounts */}
        <div className="bg-tato-900/90 border border-tato-800 rounded-2xl p-6 flex flex-col justify-between shadow-lg">
          <div>
            <div className="flex items-center justify-between mb-5">
              <div className="flex items-center gap-2.5">
                <div className="p-2 rounded-lg bg-tato-yellow/10 text-tato-yellow border border-tato-yellow/30">
                  <Users className="w-4 h-4" />
                </div>
                <div>
                  <h3 className="text-sm font-bold text-white">Últimas Cuentas VPN</h3>
                  <p className="text-xs text-gray-400">Credenciales generadas para clientes</p>
                </div>
              </div>
              <Link
                href="/users"
                className="text-xs text-tato-orange hover:underline font-semibold flex items-center gap-1"
              >
                <span>Ver Todos</span>
                <ArrowUpRight className="w-3.5 h-3.5" />
              </Link>
            </div>

            {recentUsers.length === 0 ? (
              <div className="text-center py-10 border border-dashed border-tato-800 rounded-xl">
                <Users className="w-8 h-8 text-gray-600 mx-auto mb-2" />
                <p className="text-xs text-gray-400">No hay usuarios VPN creados todavía</p>
                <Link
                  href="/users"
                  className="mt-3 inline-block text-xs text-tato-orange font-bold hover:underline"
                >
                  + Crear primer usuario
                </Link>
              </div>
            ) : (
              <div className="space-y-3">
                {recentUsers.map((user) => (
                  <div
                    key={user.id}
                    className="p-3.5 rounded-xl bg-tato-850 border border-tato-800 flex items-center justify-between hover:border-tato-700 transition"
                  >
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="text-xs font-bold text-white font-mono">
                          {user.username}
                        </span>
                        <span
                          className={`text-[10px] px-1.5 py-0.2 rounded font-bold ${
                            user.isActive
                              ? "bg-tato-green-dark/20 text-tato-green-neon border border-tato-green-dark/30"
                              : "bg-red-500/10 text-red-400 border border-red-500/30"
                          }`}
                        >
                          {user.isActive ? "ACTIVO" : "INACTIVO"}
                        </span>
                      </div>
                      <p className="text-[11px] text-gray-400 font-mono mt-0.5">
                        VPS: {user.vps.name} ({user.vps.ip})
                      </p>
                    </div>
                    <div className="text-right font-mono text-[11px] text-gray-300">
                      {user.expirationDate ? (
                        <span>Exp: {new Date(user.expirationDate).toLocaleDateString()}</span>
                      ) : (
                        <span className="text-tato-green-light">Ilimitado</span>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
