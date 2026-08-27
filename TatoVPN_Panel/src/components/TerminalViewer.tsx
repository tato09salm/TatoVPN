"use client";

import { useEffect, useRef } from "react";
import { Terminal, Copy, Check } from "lucide-react";
import { useState } from "react";

interface TerminalViewerProps {
  logs: string;
  title?: string;
  isExecuting?: boolean;
}

export default function TerminalViewer({
  logs,
  title = "SSH Terminal Output",
  isExecuting = false,
}: TerminalViewerProps) {
  const bottomRef = useRef<HTMLDivElement>(null);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [logs]);

  const copyToClipboard = () => {
    navigator.clipboard.writeText(logs);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="rounded-xl overflow-hidden border border-tato-700 bg-tato-950 shadow-2xl font-mono text-xs">
      {/* Header */}
      <div className="bg-tato-900 border-b border-tato-800 px-4 py-2.5 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <div className="flex items-center gap-1.5 mr-2">
            <div className="w-3 h-3 rounded-full bg-red-500/80"></div>
            <div className="w-3 h-3 rounded-full bg-yellow-500/80"></div>
            <div className="w-3 h-3 rounded-full bg-green-500/80"></div>
          </div>
          <Terminal className="w-3.5 h-3.5 text-tato-green-neon" />
          <span className="text-gray-300 font-semibold">{title}</span>
          {isExecuting && (
            <span className="inline-flex items-center gap-1 text-[10px] px-2 py-0.5 rounded-full bg-tato-orange/20 text-tato-orange border border-tato-orange/30 animate-pulse font-sans">
              Ejecutando SSH...
            </span>
          )}
        </div>

        <button
          onClick={copyToClipboard}
          className="flex items-center gap-1 text-gray-400 hover:text-white px-2 py-1 rounded bg-tato-850 hover:bg-tato-800 text-[11px] transition"
        >
          {copied ? (
            <>
              <Check className="w-3 h-3 text-tato-green-neon" />
              <span className="text-tato-green-neon">Copiado</span>
            </>
          ) : (
            <>
              <Copy className="w-3 h-3" />
              <span>Copiar logs</span>
            </>
          )}
        </button>
      </div>

      {/* Terminal Body */}
      <div className="p-4 max-h-96 min-h-48 overflow-y-auto font-mono text-tato-green-neon/90 whitespace-pre-wrap leading-relaxed select-text bg-[#07090c]">
        {logs || (
          <span className="text-gray-600 italic">
            Esperando ejecución de comandos SSH...
          </span>
        )}
        <div ref={bottomRef} />
      </div>
    </div>
  );
}
