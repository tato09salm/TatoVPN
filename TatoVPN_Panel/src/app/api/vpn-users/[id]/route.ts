import { NextResponse } from "next/server";
import { prisma } from "@/lib/prisma";
import { getSession } from "@/lib/auth";
import { createLinuxSshUser, deleteLinuxSshUser } from "@/lib/ssh";

export async function GET(
  request: Request,
  { params }: { params: { id: string } }
) {
  const session = await getSession();
  if (!session) {
    return NextResponse.json({ error: "No autorizado" }, { status: 401 });
  }

  try {
    const account = await prisma.vpnAccount.findUnique({
      where: { id: params.id },
      include: { vps: true },
    });

    if (!account) {
      return NextResponse.json({ error: "Cuenta no encontrada" }, { status: 404 });
    }

    return NextResponse.json({ account });
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
    const { password, expirationDate, connectionLimit, isActive, notes, syncToLinux = true } = body;

    const currentAccount = await prisma.vpnAccount.findUnique({
      where: { id: params.id },
      include: { vps: true },
    });

    if (!currentAccount) {
      return NextResponse.json({ error: "Cuenta no encontrada" }, { status: 404 });
    }

    let sshResult = null;
    if (syncToLinux && currentAccount.vps.rootPassword && password) {
      sshResult = await createLinuxSshUser(
        {
          host: currentAccount.vps.ip,
          port: currentAccount.vps.sshPort,
          username: currentAccount.vps.rootUser,
          password: currentAccount.vps.rootPassword,
        },
        currentAccount.username,
        password,
        expirationDate ? new Date(expirationDate) : currentAccount.expirationDate
      );
    }

    const updatedAccount = await prisma.vpnAccount.update({
      where: { id: params.id },
      data: {
        password: password || undefined,
        expirationDate: expirationDate ? new Date(expirationDate) : undefined,
        connectionLimit: connectionLimit !== undefined ? Number(connectionLimit) : undefined,
        isActive: isActive !== undefined ? Boolean(isActive) : undefined,
        notes: notes !== undefined ? notes : undefined,
      },
      include: { vps: true },
    });

    return NextResponse.json({
      success: true,
      account: updatedAccount,
      sshSynced: sshResult?.success ?? false,
    });
  } catch (error: any) {
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
    const currentAccount = await prisma.vpnAccount.findUnique({
      where: { id: params.id },
      include: { vps: true },
    });

    if (!currentAccount) {
      return NextResponse.json({ error: "Cuenta no encontrada" }, { status: 404 });
    }

    // Intentar borrar el usuario en Linux si tenemos contraseña de la VPS
    if (currentAccount.vps.rootPassword) {
      await deleteLinuxSshUser(
        {
          host: currentAccount.vps.ip,
          port: currentAccount.vps.sshPort,
          username: currentAccount.vps.rootUser,
          password: currentAccount.vps.rootPassword,
        },
        currentAccount.username
      );
    }

    await prisma.vpnAccount.delete({
      where: { id: params.id },
    });

    return NextResponse.json({ success: true, message: "Cuenta VPN eliminada" });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}
