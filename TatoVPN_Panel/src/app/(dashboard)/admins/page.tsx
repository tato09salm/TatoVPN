"use client";

import { useState, useEffect } from "react";
import {
  ShieldCheck,
  UserPlus,
  Trash2,
  Edit3,
  RefreshCw,
  Eye,
  EyeOff,
  Copy,
  Check,
  Search,
  KeyRound,
  Mail,
  UserCheck,
  ShieldAlert,
  Sparkles,
  Lock,
  Calendar,
  AlertCircle,
  CheckCircle2,
  Users2,
  Crown,
  UserCog,
  Headphones,
} from "lucide-react";
import Modal from "@/components/Modal";

interface AdminUser {
  id: string;
  name: string;
  email: string;
  role: string;
  createdAt: string;
  updatedAt: string;
}

interface CurrentUser {
  id?: string;
  userId?: string;
  email?: string;
  name?: string;
  role?: string;
}

export default function AdminsPage() {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [roleFilter, setRoleFilter] = useState("ALL");
  const [copiedEmail, setCopiedEmail] = useState<string | null>(null);

  // Modals state
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [showDeleteModal, setShowDeleteModal] = useState(false);
  const [userToEdit, setUserToEdit] = useState<AdminUser | null>(null);
  const [userToDelete, setUserToDelete] = useState<AdminUser | null>(null);

  // Form states
  const [formName, setFormName] = useState("");
  const [formEmail, setFormEmail] = useState("");
  const [formPassword, setFormPassword] = useState("");
  const [formRole, setFormRole] = useState("ADMIN");
  const [showPassword, setShowPassword] = useState(false);

  // Action status feedback
  const [actionLoading, setActionLoading] = useState(false);
  const [alertMsg, setAlertMsg] = useState<{
    type: "success" | "error";
    text: string;
  } | null>(null);

  const fetchUsers = async () => {
    setLoading(true);
    try {
      const [resUsers, resMe] = await Promise.all([
        fetch("/api/admin-users"),
        fetch("/api/auth/me"),
      ]);

      if (resUsers.ok) {
        const data = await resUsers.json();
        setUsers(data.users || []);
      }
      if (resMe.ok) {
        const meData = await resMe.json();
        setCurrentUser(meData.user || null);
      }
    } catch (err) {
      console.error("Error al cargar usuarios:", err);
      showAlert("error", "Error al cargar la lista de administradores");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchUsers();
  }, []);

  const showAlert = (type: "success" | "error", text: string) => {
    setAlertMsg({ type, text });
    setTimeout(() => {
      setAlertMsg(null);
    }, 4500);
  };

  const handleCopyEmail = (email: string) => {
    navigator.clipboard.writeText(email);
    setCopiedEmail(email);
    setTimeout(() => setCopiedEmail(null), 2000);
  };

  const generateRandomPassword = () => {
    const chars =
      "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%&*";
    let pwd = "";
    for (let i = 0; i < 12; i++) {
      pwd += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    setFormPassword(pwd);
    setShowPassword(true);
  };

  // Open Create Modal
  const openCreateModal = () => {
    setFormName("");
    setFormEmail("");
    setFormPassword("");
    setFormRole("ADMIN");
    setShowPassword(false);
    setShowCreateModal(true);
  };

  // Open Edit Modal
  const openEditModal = (user: AdminUser) => {
    setUserToEdit(user);
    setFormName(user.name);
    setFormEmail(user.email);
    setFormPassword("");
    setFormRole(user.role || "ADMIN");
    setShowPassword(false);
    setShowEditModal(true);
  };

  // Open Delete Modal
  const openDeleteModal = (user: AdminUser) => {
    setUserToDelete(user);
    setShowDeleteModal(true);
  };

  // Submit Create User
  const handleCreateUser = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formName.trim() || !formEmail.trim() || !formPassword.trim()) {
      showAlert("error", "Por favor completa todos los campos requeridos");
      return;
    }

    setActionLoading(true);
    try {
      const res = await fetch("/api/admin-users", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: formName,
          email: formEmail,
          password: formPassword,
          role: formRole,
        }),
      });

      const data = await res.json();
      if (!res.ok) throw new Error(data.error || "Error al crear usuario");

      showAlert("success", `Usuario "${data.user.email}" creado exitosamente`);
      setShowCreateModal(false);
      fetchUsers();
    } catch (err: any) {
      showAlert("error", err.message);
    } finally {
      setActionLoading(false);
    }
  };

  // Submit Edit User
  const handleUpdateUser = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!userToEdit) return;

    setActionLoading(true);
    try {
      const res = await fetch("/api/admin-users", {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          id: userToEdit.id,
          name: formName,
          email: formEmail,
          role: formRole,
          password: formPassword.trim() || undefined,
        }),
      });

      const data = await res.json();
      if (!res.ok) throw new Error(data.error || "Error al actualizar usuario");

      showAlert("success", "Usuario actualizado exitosamente");
      setShowEditModal(false);
      fetchUsers();
    } catch (err: any) {
      showAlert("error", err.message);
    } finally {
      setActionLoading(false);
    }
  };

  // Submit Delete User
  const handleDeleteUser = async () => {
    if (!userToDelete) return;

    setActionLoading(true);
    try {
      const res = await fetch(`/api/admin-users?id=${userToDelete.id}`, {
        method: "DELETE",
      });

      const data = await res.json();
      if (!res.ok) throw new Error(data.error || "Error al eliminar usuario");

      showAlert("success", "Usuario eliminado del panel correctamente");
      setShowDeleteModal(false);
      fetchUsers();
    } catch (err: any) {
      showAlert("error", err.message);
    } finally {
      setActionLoading(false);
    }
  };

  // Filtered list
  const filteredUsers = users.filter((u) => {
    const matchesSearch =
      u.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      u.email.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesRole = roleFilter === "ALL" || u.role === roleFilter;
    return matchesSearch && matchesRole;
  });

  const getRoleBadge = (role: string) => {
    switch (role?.toUpperCase()) {
      case "SUPERADMIN":
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-tato-orange/15 text-tato-orange border border-tato-orange/40 shadow-neon-orange">
            <Crown className="w-3 h-3" />
            SUPERADMIN
          </span>
        );
      case "ADMIN":
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-tato-green-dark/20 text-tato-green-neon border border-tato-green-neon/40 shadow-neon-green">
            <ShieldCheck className="w-3 h-3" />
            ADMINISTRADOR
          </span>
        );
      case "OPERATOR":
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-blue-500/15 text-blue-400 border border-blue-500/30">
            <UserCog className="w-3 h-3" />
            OPERADOR
          </span>
        );
      case "SOPORTE":
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-purple-500/15 text-purple-400 border border-purple-500/30">
            <Headphones className="w-3 h-3" />
            SOPORTE
          </span>
        );
      default:
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-medium bg-gray-800 text-gray-300 border border-gray-700">
            {role}
          </span>
        );
    }
  };

  const currentLoggedId = currentUser?.id || currentUser?.userId;

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Toast Alert Notification */}
      {alertMsg && (
        <div
          className={`fixed top-5 right-5 z-50 p-4 rounded-xl border flex items-center gap-3 shadow-2xl backdrop-blur-md transition-all animate-bounce duration-300 ${
            alertMsg.type === "success"
              ? "bg-tato-900/95 border-tato-green-neon/50 text-tato-green-neon shadow-neon-green"
              : "bg-tato-900/95 border-red-500/50 text-red-400 shadow-red-500/20"
          }`}
        >
          {alertMsg.type === "success" ? (
            <CheckCircle2 className="w-5 h-5 shrink-0" />
          ) : (
            <AlertCircle className="w-5 h-5 shrink-0" />
          )}
          <span className="text-xs font-semibold text-white">
            {alertMsg.text}
          </span>
        </div>
      )}

      {/* Header Banner */}
      <div className="relative overflow-hidden rounded-3xl bg-gradient-to-r from-tato-900 via-tato-850 to-tato-900 border border-tato-800 p-6 md:p-8 shadow-xl">
        <div className="absolute right-0 top-0 bottom-0 w-1/3 bg-gradient-to-l from-tato-orange/10 to-transparent pointer-events-none" />
        <div className="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-6">
          <div>
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-tato-orange/10 border border-tato-orange/30 text-tato-orange text-xs font-semibold mb-3">
              <KeyRound className="w-3.5 h-3.5" />
              <span>Control de Acceso Web al Panel</span>
            </div>
            <h2 className="text-2xl md:text-3xl font-black text-white tracking-wide flex items-center gap-3">
              <span>Usuarios del Panel</span>
              <span className="text-xs font-bold px-2.5 py-1 rounded-full bg-tato-800 border border-tato-700 text-gray-300">
                {users.length} Registrados
              </span>
            </h2>
            <p className="text-xs md:text-sm text-gray-400 mt-1 max-w-2xl">
              Gestiona las cuentas con las que se puede iniciar sesión en{" "}
              <code className="text-tato-yellow bg-tato-950 px-1.5 py-0.5 rounded border border-tato-800">
                http://localhost:3000/login
              </code>{" "}
              mediante correo electrónico y contraseña.
            </p>
          </div>

          <div className="flex items-center gap-3">
            <button
              onClick={fetchUsers}
              disabled={loading}
              title="Recargar lista"
              className="p-2.5 rounded-xl bg-tato-850 hover:bg-tato-800 border border-tato-700 text-gray-300 hover:text-white transition"
            >
              <RefreshCw
                className={`w-4 h-4 ${loading ? "animate-spin text-tato-orange" : ""}`}
              />
            </button>
            <button
              onClick={openCreateModal}
              className="px-4 py-2.5 rounded-xl bg-tato-orange hover:bg-tato-orange-light text-black font-bold text-xs transition flex items-center gap-2 shadow-lg shadow-tato-orange/20"
            >
              <UserPlus className="w-4 h-4" />
              <span>Nuevo Usuario de Panel</span>
            </button>
          </div>
        </div>
      </div>

      {/* Metrics Row */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        <div className="p-4 rounded-2xl bg-tato-900 border border-tato-800 flex items-center justify-between">
          <div>
            <p className="text-[11px] text-gray-400 font-medium">Total Accesos</p>
            <p className="text-xl font-black text-white mt-1">{users.length}</p>
          </div>
          <div className="w-10 h-10 rounded-xl bg-tato-800 border border-tato-700 flex items-center justify-center text-tato-orange">
            <Users2 className="w-5 h-5" />
          </div>
        </div>

        <div className="p-4 rounded-2xl bg-tato-900 border border-tato-800 flex items-center justify-between">
          <div>
            <p className="text-[11px] text-gray-400 font-medium">Superadmins</p>
            <p className="text-xl font-black text-tato-orange mt-1">
              {users.filter((u) => u.role === "SUPERADMIN").length}
            </p>
          </div>
          <div className="w-10 h-10 rounded-xl bg-tato-orange/10 border border-tato-orange/30 flex items-center justify-center text-tato-orange">
            <Crown className="w-5 h-5" />
          </div>
        </div>

        <div className="p-4 rounded-2xl bg-tato-900 border border-tato-800 flex items-center justify-between">
          <div>
            <p className="text-[11px] text-gray-400 font-medium">Administradores</p>
            <p className="text-xl font-black text-tato-green-neon mt-1">
              {users.filter((u) => u.role === "ADMIN").length}
            </p>
          </div>
          <div className="w-10 h-10 rounded-xl bg-tato-green-dark/20 border border-tato-green-neon/30 flex items-center justify-center text-tato-green-neon">
            <ShieldCheck className="w-5 h-5" />
          </div>
        </div>

        <div className="p-4 rounded-2xl bg-tato-900 border border-tato-800 flex items-center justify-between">
          <div>
            <p className="text-[11px] text-gray-400 font-medium">Operadores / Soporte</p>
            <p className="text-xl font-black text-blue-400 mt-1">
              {
                users.filter(
                  (u) => u.role === "OPERATOR" || u.role === "SOPORTE"
                ).length
              }
            </p>
          </div>
          <div className="w-10 h-10 rounded-xl bg-blue-500/10 border border-blue-500/30 flex items-center justify-center text-blue-400">
            <UserCog className="w-5 h-5" />
          </div>
        </div>
      </div>

      {/* Search & Filter Bar */}
      <div className="p-4 rounded-2xl bg-tato-900 border border-tato-800 flex flex-col md:flex-row gap-4 items-center justify-between">
        <div className="relative w-full md:w-80">
          <Search className="w-4 h-4 text-gray-400 absolute left-3 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            placeholder="Buscar por nombre o correo..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-9 pr-4 py-2 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white placeholder-gray-500 focus:outline-none focus:border-tato-orange"
          />
        </div>

        <div className="flex items-center gap-2 w-full md:w-auto overflow-x-auto pb-1 md:pb-0">
          <span className="text-[11px] text-gray-400 font-medium whitespace-nowrap">
            Filtrar rol:
          </span>
          {["ALL", "SUPERADMIN", "ADMIN", "OPERATOR", "SOPORTE"].map((r) => (
            <button
              key={r}
              onClick={() => setRoleFilter(r)}
              className={`px-3 py-1 rounded-lg text-xs font-semibold transition whitespace-nowrap ${
                roleFilter === r
                  ? "bg-tato-800 text-tato-orange border border-tato-orange/40 shadow-sm"
                  : "bg-tato-850 text-gray-400 hover:text-white border border-transparent"
              }`}
            >
              {r === "ALL"
                ? "Todos"
                : r === "SUPERADMIN"
                ? "Superadmin"
                : r === "ADMIN"
                ? "Admin"
                : r === "OPERATOR"
                ? "Operador"
                : "Soporte"}
            </button>
          ))}
        </div>
      </div>

      {/* Users Table */}
      <div className="rounded-2xl bg-tato-900 border border-tato-800 shadow-xl overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead className="bg-tato-850/80 text-gray-400 border-b border-tato-800 text-[11px] uppercase tracking-wider font-semibold">
              <tr>
                <th className="py-3.5 px-6">Usuario & Nombre</th>
                <th className="py-3.5 px-6">Correo de Acceso (Login)</th>
                <th className="py-3.5 px-6">Rol de Sistema</th>
                <th className="py-3.5 px-6">Fecha Registro</th>
                <th className="py-3.5 px-6 text-right">Acciones</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-tato-800/60 text-gray-300">
              {loading ? (
                <tr>
                  <td colSpan={5} className="py-12 text-center text-gray-400">
                    <RefreshCw className="w-6 h-6 animate-spin mx-auto text-tato-orange mb-2" />
                    <p>Cargando usuarios del panel...</p>
                  </td>
                </tr>
              ) : filteredUsers.length === 0 ? (
                <tr>
                  <td colSpan={5} className="py-12 text-center text-gray-400">
                    <ShieldAlert className="w-8 h-8 mx-auto text-gray-500 mb-2" />
                    <p className="font-semibold text-gray-300">
                      No se encontraron usuarios
                    </p>
                    <p className="text-[11px] text-gray-500 mt-1">
                      {searchTerm
                        ? "Intenta con otro término de búsqueda"
                        : "Haz clic en 'Nuevo Usuario de Panel' para agregar el primero"}
                    </p>
                  </td>
                </tr>
              ) : (
                filteredUsers.map((user) => {
                  const isCurrent = currentLoggedId === user.id;

                  return (
                    <tr
                      key={user.id}
                      className="hover:bg-tato-850/50 transition-colors"
                    >
                      {/* Name & Avatar */}
                      <td className="py-4 px-6">
                        <div className="flex items-center gap-3">
                          <div className="w-9 h-9 rounded-xl bg-gradient-to-br from-tato-800 to-tato-850 border border-tato-700 flex items-center justify-center font-black text-sm text-tato-orange shrink-0 shadow-inner">
                            {user.name.charAt(0).toUpperCase()}
                          </div>
                          <div>
                            <div className="flex items-center gap-2">
                              <span className="font-bold text-white text-xs">
                                {user.name}
                              </span>
                              {isCurrent && (
                                <span className="text-[10px] font-bold px-1.5 py-0.2 bg-tato-green-dark/30 text-tato-green-neon border border-tato-green-neon/40 rounded">
                                  Tú
                                </span>
                              )}
                            </div>
                            <p className="text-[10px] text-gray-400 font-mono mt-0.5">
                              ID: {user.id.substring(0, 10)}...
                            </p>
                          </div>
                        </div>
                      </td>

                      {/* Email with copy */}
                      <td className="py-4 px-6">
                        <div className="flex items-center gap-2 font-mono text-gray-200">
                          <Mail className="w-3.5 h-3.5 text-tato-orange" />
                          <span>{user.email}</span>
                          <button
                            onClick={() => handleCopyEmail(user.email)}
                            title="Copiar correo"
                            className="p-1 hover:bg-tato-800 rounded text-gray-400 hover:text-white transition"
                          >
                            {copiedEmail === user.email ? (
                              <Check className="w-3.5 h-3.5 text-tato-green-neon" />
                            ) : (
                              <Copy className="w-3.5 h-3.5" />
                            )}
                          </button>
                        </div>
                      </td>

                      {/* Role */}
                      <td className="py-4 px-6">{getRoleBadge(user.role)}</td>

                      {/* Date */}
                      <td className="py-4 px-6 text-gray-400 text-[11px] font-mono">
                        <div className="flex items-center gap-1.5">
                          <Calendar className="w-3.5 h-3.5 text-gray-500" />
                          <span>
                            {new Date(user.createdAt).toLocaleDateString(
                              "es-ES",
                              {
                                day: "2-digit",
                                month: "short",
                                year: "numeric",
                              }
                            )}
                          </span>
                        </div>
                      </td>

                      {/* Actions */}
                      <td className="py-4 px-6 text-right">
                        <div className="flex items-center justify-end gap-2">
                          <button
                            onClick={() => openEditModal(user)}
                            className="p-1.5 rounded-lg bg-tato-850 hover:bg-tato-800 text-gray-300 hover:text-tato-orange border border-tato-700/60 hover:border-tato-orange/40 transition"
                            title="Editar usuario y contraseña"
                          >
                            <Edit3 className="w-4 h-4" />
                          </button>
                          <button
                            onClick={() => openDeleteModal(user)}
                            disabled={isCurrent}
                            className={`p-1.5 rounded-lg transition border ${
                              isCurrent
                                ? "bg-tato-950 text-gray-600 border-tato-800 cursor-not-allowed"
                                : "bg-tato-850 hover:bg-red-500/10 text-gray-400 hover:text-red-400 border-tato-700/60 hover:border-red-500/30"
                            }`}
                            title={
                              isCurrent
                                ? "No puedes eliminar tu propia sesión"
                                : "Eliminar usuario del panel"
                            }
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal: CREAR NUEVO USUARIO */}
      <Modal
        isOpen={showCreateModal}
        onClose={() => !actionLoading && setShowCreateModal(false)}
        title="Crear Nuevo Usuario del Panel"
      >
        <form onSubmit={handleCreateUser} className="space-y-4">
          <div className="p-3 rounded-xl bg-tato-850 border border-tato-700/60 text-xs text-gray-300 flex items-start gap-2.5">
            <Sparkles className="w-4 h-4 text-tato-orange shrink-0 mt-0.5" />
            <p>
              Este usuario podrá iniciar sesión en la URL web del panel (
              <span className="text-tato-orange font-mono">http://localhost:3000/</span>
              ) con el correo y contraseña especificados.
            </p>
          </div>

          <div>
            <label className="block text-xs font-semibold text-gray-300 mb-1">
              Nombre Completo *
            </label>
            <input
              type="text"
              required
              placeholder="Ej: Juan Pérez / Soporte Técnico"
              value={formName}
              onChange={(e) => setFormName(e.target.value)}
              className="w-full px-3.5 py-2.5 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white placeholder-gray-500 focus:outline-none focus:border-tato-orange"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-gray-300 mb-1">
              Correo Electrónico (Login) *
            </label>
            <div className="relative">
              <Mail className="w-4 h-4 text-gray-500 absolute left-3 top-1/2 -translate-y-1/2" />
              <input
                type="email"
                required
                placeholder="usuario@tatovpn.com"
                value={formEmail}
                onChange={(e) => setFormEmail(e.target.value)}
                className="w-full pl-9 pr-3.5 py-2.5 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white placeholder-gray-500 focus:outline-none focus:border-tato-orange font-mono"
              />
            </div>
          </div>

          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="block text-xs font-semibold text-gray-300">
                Contraseña de Acceso *
              </label>
              <button
                type="button"
                onClick={generateRandomPassword}
                className="text-[11px] text-tato-orange hover:text-tato-orange-light font-semibold flex items-center gap-1"
              >
                <Sparkles className="w-3 h-3" />
                Generar Segura
              </button>
            </div>
            <div className="relative">
              <Lock className="w-4 h-4 text-gray-500 absolute left-3 top-1/2 -translate-y-1/2" />
              <input
                type={showPassword ? "text" : "password"}
                required
                minLength={6}
                placeholder="Mínimo 6 caracteres"
                value={formPassword}
                onChange={(e) => setFormPassword(e.target.value)}
                className="w-full pl-9 pr-10 py-2.5 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white placeholder-gray-500 focus:outline-none focus:border-tato-orange font-mono"
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-white"
              >
                {showPassword ? (
                  <EyeOff className="w-4 h-4" />
                ) : (
                  <Eye className="w-4 h-4" />
                )}
              </button>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-gray-300 mb-1">
              Rol de Permisos *
            </label>
            <select
              value={formRole}
              onChange={(e) => setFormRole(e.target.value)}
              className="w-full px-3.5 py-2.5 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white focus:outline-none focus:border-tato-orange"
            >
              <option value="SUPERADMIN">
                SUPERADMIN (Control Total del Sistema & Usuarios)
              </option>
              <option value="ADMIN">
                ADMINISTRADOR (Gestión de VPS & Cuentas VPN)
              </option>
              <option value="OPERATOR">
                OPERADOR (Solo aprovisionamiento y monitoreo)
              </option>
              <option value="SOPORTE">
                SOPORTE (Visualización y soporte a clientes)
              </option>
            </select>
          </div>

          <div className="flex justify-end gap-3 pt-4 border-t border-tato-800">
            <button
              type="button"
              disabled={actionLoading}
              onClick={() => setShowCreateModal(false)}
              className="px-4 py-2 rounded-xl bg-tato-850 hover:bg-tato-800 text-gray-300 text-xs font-semibold transition"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={actionLoading}
              className="px-5 py-2 rounded-xl bg-tato-orange hover:bg-tato-orange-light text-black text-xs font-bold transition flex items-center gap-2 shadow-lg shadow-tato-orange/20"
            >
              {actionLoading ? (
                <>
                  <RefreshCw className="w-4 h-4 animate-spin" />
                  <span>Creando usuario...</span>
                </>
              ) : (
                <>
                  <UserCheck className="w-4 h-4" />
                  <span>Crear Usuario</span>
                </>
              )}
            </button>
          </div>
        </form>
      </Modal>

      {/* Modal: EDITAR USUARIO */}
      <Modal
        isOpen={showEditModal}
        onClose={() => !actionLoading && setShowEditModal(false)}
        title="Editar Usuario del Panel"
      >
        <form onSubmit={handleUpdateUser} className="space-y-4">
          <div>
            <label className="block text-xs font-semibold text-gray-300 mb-1">
              Nombre Completo *
            </label>
            <input
              type="text"
              required
              value={formName}
              onChange={(e) => setFormName(e.target.value)}
              className="w-full px-3.5 py-2.5 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white focus:outline-none focus:border-tato-orange"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-gray-300 mb-1">
              Correo Electrónico (Login) *
            </label>
            <div className="relative">
              <Mail className="w-4 h-4 text-gray-500 absolute left-3 top-1/2 -translate-y-1/2" />
              <input
                type="email"
                required
                value={formEmail}
                onChange={(e) => setFormEmail(e.target.value)}
                className="w-full pl-9 pr-3.5 py-2.5 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white focus:outline-none focus:border-tato-orange font-mono"
              />
            </div>
          </div>

          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="block text-xs font-semibold text-gray-300">
                Cambiar Contraseña (Opcional)
              </label>
              <button
                type="button"
                onClick={generateRandomPassword}
                className="text-[11px] text-tato-orange hover:text-tato-orange-light font-semibold flex items-center gap-1"
              >
                <Sparkles className="w-3 h-3" />
                Generar Segura
              </button>
            </div>
            <div className="relative">
              <Lock className="w-4 h-4 text-gray-500 absolute left-3 top-1/2 -translate-y-1/2" />
              <input
                type={showPassword ? "text" : "password"}
                placeholder="Dejar en blanco para mantener la actual"
                value={formPassword}
                onChange={(e) => setFormPassword(e.target.value)}
                className="w-full pl-9 pr-10 py-2.5 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white placeholder-gray-500 focus:outline-none focus:border-tato-orange font-mono"
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-white"
              >
                {showPassword ? (
                  <EyeOff className="w-4 h-4" />
                ) : (
                  <Eye className="w-4 h-4" />
                )}
              </button>
            </div>
            <p className="text-[10px] text-gray-500 mt-1">
              Si no deseas cambiar la contraseña de este usuario, déjalo vacío.
            </p>
          </div>

          <div>
            <label className="block text-xs font-semibold text-gray-300 mb-1">
              Rol de Permisos *
            </label>
            <select
              value={formRole}
              onChange={(e) => setFormRole(e.target.value)}
              className="w-full px-3.5 py-2.5 bg-tato-850 border border-tato-700 rounded-xl text-xs text-white focus:outline-none focus:border-tato-orange"
            >
              <option value="SUPERADMIN">
                SUPERADMIN (Control Total del Sistema & Usuarios)
              </option>
              <option value="ADMIN">
                ADMINISTRADOR (Gestión de VPS & Cuentas VPN)
              </option>
              <option value="OPERATOR">
                OPERADOR (Solo aprovisionamiento y monitoreo)
              </option>
              <option value="SOPORTE">
                SOPORTE (Visualización y soporte a clientes)
              </option>
            </select>
          </div>

          <div className="flex justify-end gap-3 pt-4 border-t border-tato-800">
            <button
              type="button"
              disabled={actionLoading}
              onClick={() => setShowEditModal(false)}
              className="px-4 py-2 rounded-xl bg-tato-850 hover:bg-tato-800 text-gray-300 text-xs font-semibold transition"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={actionLoading}
              className="px-5 py-2 rounded-xl bg-tato-orange hover:bg-tato-orange-light text-black text-xs font-bold transition flex items-center gap-2 shadow-lg shadow-tato-orange/20"
            >
              {actionLoading ? (
                <>
                  <RefreshCw className="w-4 h-4 animate-spin" />
                  <span>Guardando...</span>
                </>
              ) : (
                <>
                  <Check className="w-4 h-4" />
                  <span>Guardar Cambios</span>
                </>
              )}
            </button>
          </div>
        </form>
      </Modal>

      {/* Modal: ELIMINAR USUARIO */}
      <Modal
        isOpen={showDeleteModal}
        onClose={() => !actionLoading && setShowDeleteModal(false)}
        title="Eliminar Usuario del Panel"
      >
        <div className="space-y-4">
          <div className="p-4 rounded-xl bg-red-500/10 border border-red-500/30 text-red-400 text-xs flex items-start gap-3">
            <AlertCircle className="w-5 h-5 shrink-0" />
            <div>
              <p className="font-bold text-white">¿Estás seguro de continuar?</p>
              <p className="mt-1">
                El usuario{" "}
                <span className="font-bold text-red-300 font-mono">
                  {userToDelete?.email}
                </span>{" "}
                perderá acceso de forma inmediata al panel web. Esta acción no se puede
                deshacer.
              </p>
            </div>
          </div>

          <div className="flex justify-end gap-3 pt-2">
            <button
              type="button"
              disabled={actionLoading}
              onClick={() => setShowDeleteModal(false)}
              className="px-4 py-2 rounded-xl bg-tato-850 hover:bg-tato-800 text-gray-300 text-xs font-semibold transition"
            >
              Cancelar
            </button>
            <button
              type="button"
              disabled={actionLoading}
              onClick={handleDeleteUser}
              className="px-5 py-2 rounded-xl bg-red-600 hover:bg-red-500 text-white text-xs font-bold transition flex items-center gap-2 shadow-lg shadow-red-600/30"
            >
              {actionLoading ? (
                <>
                  <RefreshCw className="w-4 h-4 animate-spin" />
                  <span>Eliminando...</span>
                </>
              ) : (
                <>
                  <Trash2 className="w-4 h-4" />
                  <span>Sí, Eliminar Usuario</span>
                </>
              )}
            </button>
          </div>
        </div>
      </Modal>
    </div>
  );
}
