import { NextResponse } from "next/server";
import { prisma } from "@/lib/prisma";
import { getSession } from "@/lib/auth";
import { createLinuxSshUser } from "@/lib/ssh";

export async function GET(request: Request) {
  const session = await getSession();
  if (!session) {
    return NextResponse.json({ error: "No autorizado" }, { status: 401 });
  }

  const { searchParams } = new URL(request.url);
  const vpsId = searchParams.get("vpsId");

  try {
    const where: any = {};
    if (vpsId) where.vpsId = vpsId;

    const accounts = await prisma.vpnAccount.findMany({
      where,
      include: {
        vps: {
          select: {
            id: true,
            name: true,
            ip: true,
            sshPort: true,
            sslPort: true,
            dropbearPort: true,
            badvpnPort: true,
            status: true,
            location: true,
          },
        },
      },
      orderBy: { createdAt: "desc" },
    });

    return NextResponse.json({ accounts });
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
      username,
      password,
      vpsId,
      mode = "vps", // 'vps' | 'external'
      externalIp,
      externalPort = 443,
      externalType = "SSL", // 'SSL' | 'SSH' | 'DROPBEAR'
      serverName,
      expirationDays = 30,
      connectionLimit = 999,
      notes,
      syncToLinux = true,
    } = body;

    if (!username || !password) {
      return NextResponse.json(
        { error: "El usuario y la contraseña son obligatorios" },
        { status: 400 }
      );
    }

    let targetVpsId = vpsId;

    // Si es un servidor externo / manual (sin VPS gestionada previa)
    if (mode === "external" || (!vpsId && externalIp)) {
      if (!externalIp) {
        return NextResponse.json(
          { error: "Debes ingresar la IP o Host del servidor externo" },
          { status: 400 }
        );
      }

      const cleanIp = externalIp.trim();
      const portNum = Number(externalPort) || (externalType === "SSH" ? 22 : 443);
      const sName = serverName?.trim() || `Ext-${cleanIp}`;

      // Buscar o crear la entrada de servidor externo en la base de datos
      const vpsRecord = await prisma.vpsServer.upsert({
        where: { ip: cleanIp },
        update: {
          name: serverName?.trim() || undefined,
          sslPort: externalType === "SSL" || portNum === 443 ? portNum : 443,
          sshPort: externalType === "SSH" || portNum === 22 ? portNum : 22,
          dropbearPort: externalType === "DROPBEAR" || portNum === 80 ? portNum : 80,
        },
        create: {
          name: sName,
          ip: cleanIp,
          sslPort: externalType === "SSL" || portNum === 443 ? portNum : 443,
          sshPort: externalType === "SSH" || portNum === 22 ? portNum : 22,
          dropbearPort: externalType === "DROPBEAR" || portNum === 80 ? portNum : 80,
          location: "Servidor Externo / Manual",
          status: "ONLINE",
          isConfigured: true,
          rootUser: "none",
        },
      });

      targetVpsId = vpsRecord.id;
    }

    if (!targetVpsId) {
      return NextResponse.json(
        { error: "Debes seleccionar una VPS o ingresar los datos de un servidor externo" },
        { status: 400 }
      );
    }

    const vps = await prisma.vpsServer.findUnique({
      where: { id: targetVpsId },
    });

    if (!vps) {
      return NextResponse.json({ error: "Servidor VPS no encontrado" }, { status: 404 });
    }

    // Calcular fecha de expiración
    const expirationDate = expirationDays
      ? new Date(Date.now() + Number(expirationDays) * 24 * 60 * 60 * 1000)
      : null;

    // Verificar si el usuario ya existe en esta VPS
    const existing = await prisma.vpnAccount.findUnique({
      where: {
        username_vpsId: {
          username: username.trim(),
          vpsId: targetVpsId,
        },
      },
    });

    if (existing) {
      return NextResponse.json(
        { error: `El usuario '${username}' ya existe en el servidor ${vps.name}` },
        { status: 400 }
      );
    }

    let sshResult = null;
    // Solo sincronizar vía SSH si está habilitado y la VPS tiene contraseña root real
    if (syncToLinux && vps.rootPassword && mode !== "external") {
      sshResult = await createLinuxSshUser(
        {
          host: vps.ip,
          port: vps.sshPort,
          username: vps.rootUser,
          password: vps.rootPassword,
        },
        username.trim(),
        password.trim(),
        expirationDate
      );
    }

    const account = await prisma.vpnAccount.create({
      data: {
        username: username.trim(),
        password: password.trim(),
        vpsId: targetVpsId,
        connectionLimit: Number(connectionLimit),
        expirationDate,
        notes: notes?.trim() || null,
        isActive: true,
      },
      include: {
        vps: true,
      },
    });

    return NextResponse.json({
      success: true,
      account,
      sshSynced: sshResult?.success ?? false,
      sshOutput: sshResult?.output,
      sshError: sshResult?.error,
    });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}
