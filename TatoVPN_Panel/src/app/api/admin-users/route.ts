import { NextResponse } from "next/server";
import { prisma } from "@/lib/prisma";
import { getSession, hashPassword } from "@/lib/auth";

// GET: List all panel users (admins / operators)
export async function GET() {
  const session = await getSession();
  if (!session) {
    return NextResponse.json({ error: "No autorizado" }, { status: 401 });
  }

  try {
    const users = await prisma.adminUser.findMany({
      select: {
        id: true,
        email: true,
        name: true,
        role: true,
        createdAt: true,
        updatedAt: true,
      },
      orderBy: { createdAt: "desc" },
    });

    return NextResponse.json({ users });
  } catch (error: any) {
    console.error("Error fetching admin users:", error);
    return NextResponse.json(
      { error: "Error al obtener la lista de usuarios del panel" },
      { status: 500 }
    );
  }
}

// POST: Create a new panel user (with email, password, name, role)
export async function POST(request: Request) {
  const session = await getSession();
  if (!session) {
    return NextResponse.json({ error: "No autorizado" }, { status: 401 });
  }

  try {
    const body = await request.json();
    const { email, password, name, role = "ADMIN" } = body;

    if (!email || !password || !name) {
      return NextResponse.json(
        { error: "Correo electrónico, contraseña y nombre son obligatorios" },
        { status: 400 }
      );
    }

    const cleanEmail = email.trim().toLowerCase();

    // Check basic email format
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(cleanEmail)) {
      return NextResponse.json(
        { error: "El formato del correo electrónico es inválido" },
        { status: 400 }
      );
    }

    if (password.length < 6) {
      return NextResponse.json(
        { error: "La contraseña debe tener al menos 6 caracteres" },
        { status: 400 }
      );
    }

    // Check if email already exists
    const existing = await prisma.adminUser.findUnique({
      where: { email: cleanEmail },
    });

    if (existing) {
      return NextResponse.json(
        { error: "Ya existe un usuario con este correo electrónico" },
        { status: 409 }
      );
    }

    const passwordHash = await hashPassword(password);

    const newUser = await prisma.adminUser.create({
      data: {
        email: cleanEmail,
        name: name.trim(),
        passwordHash,
        role: role.toUpperCase(),
      },
      select: {
        id: true,
        email: true,
        name: true,
        role: true,
        createdAt: true,
        updatedAt: true,
      },
    });

    return NextResponse.json({
      success: true,
      message: "Usuario del panel creado correctamente",
      user: newUser,
    });
  } catch (error: any) {
    console.error("Error creating admin user:", error);
    return NextResponse.json(
      { error: error.message || "Error al crear el usuario" },
      { status: 500 }
    );
  }
}

// PUT: Update an existing panel user (name, email, role, optional password)
export async function PUT(request: Request) {
  const session = await getSession();
  if (!session) {
    return NextResponse.json({ error: "No autorizado" }, { status: 401 });
  }

  try {
    const body = await request.json();
    const { id, email, name, role, password } = body;

    if (!id) {
      return NextResponse.json(
        { error: "ID del usuario es requerido" },
        { status: 400 }
      );
    }

    const user = await prisma.adminUser.findUnique({
      where: { id },
    });

    if (!user) {
      return NextResponse.json(
        { error: "Usuario no encontrado" },
        { status: 404 }
      );
    }

    const dataToUpdate: any = {};

    if (name) {
      dataToUpdate.name = name.trim();
    }

    if (role) {
      dataToUpdate.role = role.toUpperCase();
    }

    if (email) {
      const cleanEmail = email.trim().toLowerCase();
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(cleanEmail)) {
        return NextResponse.json(
          { error: "El formato del correo electrónico es inválido" },
          { status: 400 }
        );
      }

      // Check if another user has this email
      if (cleanEmail !== user.email) {
        const existing = await prisma.adminUser.findUnique({
          where: { email: cleanEmail },
        });
        if (existing) {
          return NextResponse.json(
            { error: "El correo electrónico ya está en uso por otro usuario" },
            { status: 409 }
          );
        }
        dataToUpdate.email = cleanEmail;
      }
    }

    if (password && password.trim().length > 0) {
      if (password.length < 6) {
        return NextResponse.json(
          { error: "La nueva contraseña debe tener al menos 6 caracteres" },
          { status: 400 }
        );
      }
      dataToUpdate.passwordHash = await hashPassword(password);
    }

    const updatedUser = await prisma.adminUser.update({
      where: { id },
      data: dataToUpdate,
      select: {
        id: true,
        email: true,
        name: true,
        role: true,
        createdAt: true,
        updatedAt: true,
      },
    });

    return NextResponse.json({
      success: true,
      message: "Usuario actualizado correctamente",
      user: updatedUser,
    });
  } catch (error: any) {
    console.error("Error updating admin user:", error);
    return NextResponse.json(
      { error: error.message || "Error al actualizar el usuario" },
      { status: 500 }
    );
  }
}

// DELETE: Delete a panel user
export async function DELETE(request: Request) {
  const session = await getSession();
  if (!session) {
    return NextResponse.json({ error: "No autorizado" }, { status: 401 });
  }

  try {
    const { searchParams } = new URL(request.url);
    const id = searchParams.get("id");

    if (!id) {
      return NextResponse.json(
        { error: "ID de usuario requerido" },
        { status: 400 }
      );
    }

    // Protection: User cannot delete themselves
    if (session.userId === id) {
      return NextResponse.json(
        { error: "No puedes eliminar tu propia cuenta en sesión activa" },
        { status: 400 }
      );
    }

    // Check count of admins to avoid deleting the only admin
    const totalUsers = await prisma.adminUser.count();
    if (totalUsers <= 1) {
      return NextResponse.json(
        { error: "No puedes eliminar el único usuario del panel existente" },
        { status: 400 }
      );
    }

    await prisma.adminUser.delete({
      where: { id },
    });

    return NextResponse.json({
      success: true,
      message: "Usuario eliminado correctamente",
    });
  } catch (error: any) {
    console.error("Error deleting admin user:", error);
    return NextResponse.json(
      { error: error.message || "Error al eliminar el usuario" },
      { status: 500 }
    );
  }
}
