import { NextResponse } from "next/server";
import { prisma } from "@/lib/prisma";
import { getSession } from "@/lib/auth";
import { executeSshCommand } from "@/lib/ssh";
import { generateVpsSetupScript } from "@/lib/scripts/vpsSetupScript";

export async function GET(
  request: Request,
  { params }: { params: { id: string } }
) {
  const session = await getSession();
  if (!session) {
    return NextResponse.json({ error: "No autorizado" }, { status: 401 });
  }

  try {
    const server = await prisma.vpsServer.findUnique({
      where: { id: params.id },
      include: {
        vpnAccounts: {
          orderBy: { createdAt: "desc" },
        },
        logs: {
          orderBy: { createdAt: "desc" },
          take: 10,
        },
      },
    });

    if (!server) {
      return NextResponse.json({ error: "Servidor no encontrado" }, { status: 404 });
    }

    return NextResponse.json({ server });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}

export async function PUT(
  request: Request,
  { params }: { params: { id: string } }
) {
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
      location = "VPS",
      sslPort = 443,
      openSshPort = 22,
      badvpnPort = 7300,
      dropbearPort = 80,
      socksPort = 1080,
      bannerText = "TATO-VPN",
      autoSetup = false,
      status,
    } = body;

    const currentServer = await prisma.vpsServer.findUnique({
      where: { id: params.id },
    });

    if (!currentServer) {
      return NextResponse.json({ error: "Servidor no encontrado" }, { status: 404 });
    }

    const effectivePassword = rootPassword || currentServer.rootPassword;

    const server = await prisma.vpsServer.update({
      where: { id: params.id },
      data: {
        name: name !== undefined ? name.trim() : currentServer.name,
        ip: ip !== undefined ? ip.trim() : currentServer.ip,
        sshPort: sshPort !== undefined ? Number(sshPort) : currentServer.sshPort,
        rootUser: rootUser !== undefined ? rootUser.trim() : currentServer.rootUser,
        rootPassword: effectivePassword || null,
        location: location !== undefined ? location.trim() : currentServer.location,
        sslPort: sslPort !== undefined ? Number(sslPort) : currentServer.sslPort,
        openSshPort: openSshPort !== undefined ? Number(openSshPort) : currentServer.openSshPort,
        badvpnPort: badvpnPort !== undefined ? Number(badvpnPort) : currentServer.badvpnPort,
        dropbearPort: dropbearPort !== undefined ? Number(dropbearPort) : currentServer.dropbearPort,
        socksPort: socksPort !== undefined ? Number(socksPort) : 1080,
        status: status !== undefined ? status : currentServer.status,
      },
    });

    let setupResult: { success: boolean; output: string; error?: string } | null = null;

    // Si se solicitó autoSetup y tenemos password
    if (autoSetup && effectivePassword) {
      const script = generateVpsSetupScript({
        ssl: Number(sslPort || currentServer.sslPort),
        dropbear: Number(dropbearPort || currentServer.dropbearPort),
        badvpn: Number(badvpnPort || currentServer.badvpnPort),
        openssh: Number(openSshPort || currentServer.openSshPort),
        bannerText: bannerText || "TATO-VPN",
      });

      setupResult = await executeSshCommand(
        {
          host: (ip || currentServer.ip).trim(),
          port: Number(sshPort || currentServer.sshPort),
          username: (rootUser || currentServer.rootUser).trim(),
          password: effectivePassword,
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
          action: "AUTO_SETUP_ON_UPDATE",
          status: setupResult.success ? "SUCCESS" : "FAILED",
          output: (setupResult.output || "") + (setupResult.error ? `\nERROR: ${setupResult.error}` : ""),
        },
      });
    }

    return NextResponse.json({
      success: true,
      server,
      setupExecuted: !!setupResult,
      setupSuccess: setupResult?.success,
      setupOutput: setupResult?.output,
      setupError: setupResult?.error,
    });
  } catch (error: any) {
    console.error("Error updating VPS:", error);
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}

export async function DELETE(
  request: Request,
  { params }: { params: { id: string } }
) {
  const session = await getSession();
  if (!session) {
    return NextResponse.json({ error: "No autorizado" }, { status: 401 });
  }

  try {
    await prisma.vpsServer.delete({
      where: { id: params.id },
    });

    return NextResponse.json({ success: true, message: "VPS eliminada correctamente" });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}
