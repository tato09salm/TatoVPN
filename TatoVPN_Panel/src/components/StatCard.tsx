import { LucideIcon } from "lucide-react";

interface StatCardProps {
  title: string;
  value: number | string;
  subtitle?: string;
  icon: LucideIcon;
  variant?: "orange" | "green" | "yellow" | "blue";
}

export default function StatCard({
  title,
  value,
  subtitle,
  icon: Icon,
  variant = "orange",
}: StatCardProps) {
  const styles = {
    orange: {
      border: "border-tato-orange/30 hover:border-tato-orange/60",
      bgGlow: "group-hover:shadow-neon-orange",
      iconBg: "bg-tato-orange/10 text-tato-orange border-tato-orange/30",
      badge: "text-tato-orange bg-tato-orange/10 border-tato-orange/20",
    },
    green: {
      border: "border-tato-green-dark/40 hover:border-tato-green-neon/60",
      bgGlow: "group-hover:shadow-neon-green",
      iconBg: "bg-tato-green-dark/20 text-tato-green-neon border-tato-green-dark/40",
      badge: "text-tato-green-neon bg-tato-green-dark/20 border-tato-green-dark/30",
    },
    yellow: {
      border: "border-tato-yellow/30 hover:border-tato-yellow/60",
      bgGlow: "group-hover:shadow-neon-yellow",
      iconBg: "bg-tato-yellow/10 text-tato-yellow border-tato-yellow/30",
      badge: "text-tato-yellow bg-tato-yellow/10 border-tato-yellow/20",
    },
    blue: {
      border: "border-blue-500/30 hover:border-blue-400/60",
      bgGlow: "",
      iconBg: "bg-blue-500/10 text-blue-400 border-blue-500/30",
      badge: "text-blue-400 bg-blue-500/10 border-blue-500/20",
    },
  };

  const cur = styles[variant] || styles.orange;

  return (
    <div
      className={`group relative overflow-hidden p-5 rounded-2xl bg-tato-900/90 border ${cur.border} transition-all duration-300 ${cur.bgGlow}`}
    >
      <div className="flex items-center justify-between">
        <div>
          <p className="text-xs font-semibold uppercase tracking-wider text-gray-400">
            {title}
          </p>
          <p className="text-3xl font-extrabold text-white mt-1 font-mono tracking-tight">
            {value}
          </p>
          {subtitle && (
            <p className="text-[12px] text-gray-400 mt-1 font-medium">
              {subtitle}
            </p>
          )}
        </div>
        <div
          className={`w-12 h-12 rounded-xl border flex items-center justify-center ${cur.iconBg}`}
        >
          <Icon className="w-6 h-6" />
        </div>
      </div>
    </div>
  );
}
