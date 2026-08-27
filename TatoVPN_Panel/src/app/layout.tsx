import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "TatoVPN Panel - Gestión de Servidores y Cuentas VPN",
  description: "Panel de control administrativo para servidores VPS y cuentas VPN de TatoVPN",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="es" className="dark">
      <body className="bg-tato-950 text-gray-100 min-h-screen antialiased selection:bg-tato-orange selection:text-black">
        {children}
      </body>
    </html>
  );
}
