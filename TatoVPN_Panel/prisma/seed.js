const { PrismaClient } = require("@prisma/client");
const bcrypt = require("bcryptjs");

const prisma = new PrismaClient();

async function main() {
  const email = "admin@tatovpn.com";
  const existingUser = await prisma.adminUser.findUnique({
    where: { email },
  });

  if (!existingUser) {
    const passwordHash = await bcrypt.hash("admin123", 10);
    const admin = await prisma.adminUser.create({
      data: {
        email,
        name: "TatoVPN Administrator",
        passwordHash,
        role: "SUPERADMIN",
      },
    });
    console.log("✅ Admin user created:", admin.email, "(Password: admin123)");
  } else {
    console.log("ℹ️ Admin user already exists:", existingUser.email);
  }
}

main()
  .catch((e) => {
    console.error(e);
    process.exit(1);
  })
  .finally(async () => {
    await prisma.$disconnect();
  });
