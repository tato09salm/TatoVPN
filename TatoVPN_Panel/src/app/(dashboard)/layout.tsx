import Sidebar from "@/components/Sidebar";
import Navbar from "@/components/Navbar";
import { getSession } from "@/lib/auth";

export default async function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await getSession();

  return (
    <div className="min-h-screen flex bg-tato-950 text-gray-100">
      <Sidebar />
      <div className="flex-1 flex flex-col min-w-0">
        <Navbar
          userEmail={session?.email || "admin@tatovpn.com"}
          userName={session?.name || "Administrador"}
        />
        <main className="flex-1 p-6 md:p-8 overflow-y-auto bg-grid-pattern">
          {children}
        </main>
      </div>
    </div>
  );
}
