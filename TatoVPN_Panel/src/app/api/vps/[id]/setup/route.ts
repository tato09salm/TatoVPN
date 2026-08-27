import { NextResponse } from "next/server";
import { prisma } from "@/lib/prisma";
import { getSession } from "@/lib/auth";
import { executeSshCommand } from "@/lib/ssh";
import { generateVpsSetupScript } from "@/lib/scripts/vpsSetupScript";

export async function POST(
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
    });

    if (!server) {
      return NextResponse.json({ error: "Servidor no encontrado" }, { status: 404 });
    }

    if (!server.rootPassword) {
      return NextResponse.json(
        { error: "Se requiere la contraseña de root para realizar el setup" },
        { status: 400 }
      );
    }

    let bannerText = "TATO-VPN";
    try {
      const body = await request.json();
      if (body.bannerText) bannerText = body.bannerText;
    } catch {
      // Ignorar si no hay body json
    }

    const script = generateVpsSetupScript({
      ssl: server.sslPort,
      dropbear: server.dropbearPort,
      badvpn: server.badvpnPort,
      openssh: server.openSshPort,
      bannerText,
    });

    // Guardar log inicial
    const taskLog = await prisma.vpsTaskLog.create({
      data: {
        vpsId: server.id,
        action: "SETUP_SERVICES_PORTS",
        status: "RUNNING",
        output: "Iniciando aprovisionamiento automatizado...\n",
      },
    });

    // Ejecutar script vía SSH
    const result = await executeSshCommand(
      {
        host: server.ip,
        port: server.sshPort,
        username: server.rootUser,
        password: server.rootPassword,
        timeout: 180000,
      },
      script
    );

    // Actualizar log y estado de la VPS
    await prisma.vpsTaskLog.update({
      where: { id: taskLog.id },
      data: {
        status: result.success ? "SUCCESS" : "FAILED",
        output: (result.output || "") + (result.error ? `\nERROR: ${result.error}` : ""),
      },
    });

    await prisma.vpsServer.update({
      where: { id: server.id },
      data: {
        isConfigured: result.success,
        status: result.success ? "ONLINE" : "ERROR",
        lastChecked: new Date(),
      },
    });

    return NextResponse.json({
      success: result.success,
      output: result.output,
      error: result.error,
      logId: taskLog.id,
    });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}
