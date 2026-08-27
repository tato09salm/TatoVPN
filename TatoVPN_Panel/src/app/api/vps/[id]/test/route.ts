import { NextResponse } from "next/server";
import { prisma } from "@/lib/prisma";
import { getSession } from "@/lib/auth";
import { executeSshCommand } from "@/lib/ssh";

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
        { error: "No hay contraseña root configurada para este servidor" },
        { status: 400 }
      );
    }

    const testResult = await executeSshCommand(
      {
        host: server.ip,
        port: server.sshPort,
        username: server.rootUser,
        password: server.rootPassword,
        timeout: 10000,
      },
      "uname -a && uptime"
    );

    await prisma.vpsServer.update({
      where: { id: server.id },
      data: {
        status: testResult.success ? "ONLINE" : "OFFLINE",
        lastChecked: new Date(),
        osInfo: testResult.success ? testResult.output.trim() : server.osInfo,
      },
    });

    return NextResponse.json({
      success: testResult.success,
      output: testResult.output,
      error: testResult.error,
    });
  } catch (error: any) {
    return NextResponse.json({ error: error.message }, { status: 500 });
  }
}
