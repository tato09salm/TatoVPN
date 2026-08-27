import { NextResponse } from "next/server";
import { prisma } from "@/lib/prisma";
import { getSession } from "@/lib/auth";
import { executeSshCommand } from "@/lib/ssh";
import { generateVpsSetupScript } from "@/lib/scripts/vpsSetupScript";

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
      sslPort = 443,
      dropbearPort = 80,
      badvpnPort = 7300,
      openSshPort = 22,
      bannerText = "TATO-VPN",
    } = body;

    if (!ip || !rootPassword) {
      return NextResponse.json(
        { error: "La IP y la Contraseña de root son obligatorias para el aprovisionamiento" },
        { status: 400 }
      );
    }

    const vpsName = name?.trim() || `VPS-${ip.split(".").slice(-2).join(".")}`;

    // 1. Probar conectividad SSH
    const testResult = await executeSshCommand(
      {
        host: ip.trim(),
        port: Number(sshPort),
        username: rootUser.trim(),
        password: rootPassword,
        timeout: 15000,
      },
      "uname -a"
    );

    if (!testResult.success) {
      return NextResponse.json(
        {
          error: `Fallo al conectar por SSH a ${ip}:${sshPort}. Verifica que la IP y la contraseña sean correctas.`,
          details: testResult.error,
        },
        { status: 400 }
      );
    }

    // 2. Crear o actualizar registro en base de datos
    const server = await prisma.vpsServer.upsert({
      where: { ip: ip.trim() },
      update: {
        name: vpsName,
        sshPort: Number(sshPort),
        rootUser: rootUser.trim(),
        rootPassword,
        sslPort: Number(sslPort),
        dropbearPort: Number(dropbearPort),
        badvpnPort: Number(badvpnPort),
        openSshPort: Number(openSshPort),
        status: "CONFIGURING",
        osInfo: testResult.output.trim(),
        lastChecked: new Date(),
      },
      create: {
        name: vpsName,
        ip: ip.trim(),
        sshPort: Number(sshPort),
        rootUser: rootUser.trim(),
        rootPassword,
        sslPort: Number(sslPort),
        dropbearPort: Number(dropbearPort),
        badvpnPort: Number(badvpnPort),
        openSshPort: Number(openSshPort),
        status: "CONFIGURING",
        osInfo: testResult.output.trim(),
        lastChecked: new Date(),
      },
    });

    // 3. Ejecutar script de configuración de puertos y banner
    const script = generateVpsSetupScript({
      ssl: Number(sslPort),
      dropbear: Number(dropbearPort),
      badvpn: Number(badvpnPort),
      openssh: Number(openSshPort),
      bannerText: bannerText || "TATO-VPN",
    });

    const setupResult = await executeSshCommand(
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
      },
    });

    await prisma.vpsTaskLog.create({
      data: {
        vpsId: server.id,
        action: "AUTO_PROVISION",
        status: setupResult.success ? "SUCCESS" : "FAILED",
        output: (setupResult.output || "") + (setupResult.error ? `\nERROR: ${setupResult.error}` : ""),
      },
    });

    return NextResponse.json({
      success: setupResult.success,
      server,
      output: setupResult.output,
      error: setupResult.error,
    });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}
