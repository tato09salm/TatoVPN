import { NextResponse } from "next/server";
import { prisma } from "@/lib/prisma";
import { getSession, comparePassword, hashPassword } from "@/lib/auth";

export async function POST(request: Request) {
  const session = await getSession();
  if (!session) {
    return NextResponse.json({ error: "No autorizado" }, { status: 401 });
  }

  try {
    const { currentPassword, newPassword } = await request.json();

    if (!currentPassword || !newPassword) {
      return NextResponse.json(
        { error: "La contraseña actual y la nueva contraseña son requeridas" },
        { status: 400 }
      );
    }

    if (newPassword.length < 6) {
      return NextResponse.json(
        { error: "La nueva contraseña debe tener al menos 6 caracteres" },
        { status: 400 }
      );
    }

    const user = await prisma.adminUser.findUnique({
      where: { id: session.userId },
    });

    if (!user) {
      return NextResponse.json(
        { error: "Usuario no encontrado" },
        { status: 404 }
      );
    }

    const isMatch = await comparePassword(currentPassword, user.passwordHash);
    if (!isMatch) {
      return NextResponse.json(
        { error: "La contraseña actual es incorrecta" },
        { status: 400 }
      );
    }

    const newHash = await hashPassword(newPassword);

    await prisma.adminUser.update({
      where: { id: user.id },
      data: { passwordHash: newHash },
    });

    return NextResponse.json({
      success: true,
      message: "Contraseña actualizada exitosamente",
    });
  } catch (error: any) {
    console.error("Change password error:", error);
    return NextResponse.json(
      { error: error.message || "Error al cambiar la contraseña" },
      { status: 500 }
    );
  }
}
