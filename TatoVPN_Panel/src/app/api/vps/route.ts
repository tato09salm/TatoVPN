import { NextResponse } from "next/server";
import { prisma } from "@/lib/prisma";
import { getSession } from "@/lib/auth";
import { executeSshCommand } from "@/lib/ssh";
import { generateVpsSetupScript } from "@/lib/scripts/vpsSetupScript";

export async function GET() {
  const session = await getSession();
  if (!session) {
    return NextResponse.json({ error: "No autorizado" }, { status: 401 });
  }

  try {
    const servers = await prisma.vpsServer.findMany({
      include: {
        _count: {
          select: { vpnAccounts: true },
        },
      },
      orderBy: { createdAt: "desc" },
    });

    return NextResponse.json({ servers });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}

export async function POST(request: Request) {
  const session = await getSession();
  if (!session) {
    return NextResponse.json({ error: "No autorizado" }, { status: 401 });
  }

  try {
    const body = await request.json();
    const {
      name,
      ip,
      sshPort = 22,
      rootUser = "root",
      rootPassword,
      location = "VPS Principal",
      sslPort = 443,
      openSshPort = 22,
      badvpnPort = 7300,
      dropbearPort = 80,
      socksPort = 1080,
      bannerText = "TATO-VPN",
      autoSetup = true,
    } = body;

    if (!name || !ip) {
      return NextResponse.json(
        { error: "Nombre e IP son requeridos" },
        { status: 400 }
      );
    }

    const existing = await prisma.vpsServer.findUnique({
      where: { ip: ip.trim() },
    });

    if (existing) {
      return NextResponse.json(
        { error: `Ya existe un servidor con la IP ${ip}` },
        { status: 400 }
      );
    }

    const server = await prisma.vpsServer.create({
      data: {
        name: name.trim(),
        ip: ip.trim(),
        sshPort: Number(sshPort),
        rootUser: rootUser.trim(),
        rootPassword: rootPassword || null,
        location: location.trim(),
        sslPort: Number(sslPort),
        openSshPort: Number(openSshPort),
        badvpnPort: Number(badvpnPort),
        dropbearPort: Number(dropbearPort),
        socksPort: Number(socksPort),
        status: autoSetup && rootPassword ? "CONFIGURING" : "ONLINE",
        isConfigured: false,
      },
    });

    let setupResult: { success: boolean; output: string; error?: string } | null = null;

    // Si autoSetup está activo y se suministró contraseña de root, ejecutar el script inmediatamente
    if (autoSetup && rootPassword) {
      const script = generateVpsSetupScript({
        ssl: Number(sslPort),
        dropbear: Number(dropbearPort),
        badvpn: Number(badvpnPort),
        openssh: Number(openSshPort),
        bannerText: bannerText || "TATO-VPN",
      });

      setupResult = await executeSshCommand(
        {
          host: ip.trim(),
          port: Number(sshPort),
          username: rootUser.trim(),
          password: rootPassword,
          timeout: 180000,
        },
        script
      );

      await prisma.vpsServer.update({
        where: { id: server.id },
        data: {
          status: setupResult.success ? "ONLINE" : "ERROR",
          isConfigured: setupResult.success,
          lastChecked: new Date(),
        },
      });

      await prisma.vpsTaskLog.create({
        data: {
          vpsId: server.id,
          action: "AUTO_SETUP_ON_CREATE",
          status: setupResult.success ? "SUCCESS" : "FAILED",
          output: (setupResult.output || "") + (setupResult.error ? `\nERROR: ${setupResult.error}` : ""),
        },
      });
    }

    return NextResponse.json(
      {
        success: true,
        server,
        setupExecuted: !!setupResult,
        setupSuccess: setupResult?.success,
        setupOutput: setupResult?.output,
        setupError: setupResult?.error,
      },
      { status: 201 }
    );
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}
