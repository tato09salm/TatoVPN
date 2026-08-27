import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  theme: {
    extend: {
      colors: {
        background: "var(--background)",
        foreground: "var(--foreground)",
        tato: {
          950: "#07090c",
          900: "#0d1117",
          850: "#131822",
          800: "#1a2230",
          700: "#273347",
          600: "#3b4d6b",
          green: {
            DEFAULT: "#10b981",
            neon: "#00ff88",
            light: "#34d399",
            dark: "#059669",
          },
          yellow: {
            DEFAULT: "#facc15",
            bright: "#ffe600",
            amber: "#f59e0b",
          },
          orange: {
            DEFAULT: "#f97316",
            bright: "#ff6b00",
            light: "#fb923c",
            dark: "#ea580c",
          },
        },
      },
      fontFamily: {
        mono: ["var(--font-mono)", "JetBrains Mono", "Courier New", "monospace"],
      },
      boxShadow: {
        "neon-green": "0 0 20px -5px rgba(0, 255, 136, 0.4)",
        "neon-orange": "0 0 20px -5px rgba(249, 115, 22, 0.4)",
        "neon-yellow": "0 0 20px -5px rgba(250, 204, 21, 0.4)",
      },
    },
  },
  plugins: [],
};
export default config;
