import { NextResponse } from "next/server";
import { prisma } from "@/lib/prisma";
import { getSession } from "@/lib/auth";

export async function GET() {
  const session = await getSession();
  if (!session) {
    return NextResponse.json({ error: "No autorizado" }, { status: 401 });
  }

  try {
    const totalServers = await prisma.vpsServer.count();
    const onlineServers = await prisma.vpsServer.count({
      where: { status: "ONLINE" },
    });
    const totalAccounts = await prisma.vpnAccount.count();
    const activeAccounts = await prisma.vpnAccount.count({
      where: { isActive: true },
    });

    const recentAccounts = await prisma.vpnAccount.findMany({
      take: 5,
      orderBy: { createdAt: "desc" },
      include: {
        vps: {
          select: { name: true, ip: true },
        },
      },
    });

    const servers = await prisma.vpsServer.findMany({
      take: 4,
      orderBy: { createdAt: "desc" },
      include: {
        _count: {
          select: { vpnAccounts: true },
        },
      },
    });

    return NextResponse.json({
      stats: {
        totalServers,
        onlineServers,
        totalAccounts,
        activeAccounts,
      },
      recentAccounts,
      servers,
    });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}
