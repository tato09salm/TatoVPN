import { Client } from "ssh2";

export interface SshConfig {
  host: string;
  port?: number;
  username?: string;
  password?: string;
  privateKey?: string;
  timeout?: number;
}

export interface SshExecutionResult {
  success: boolean;
  output: string;
  error?: string;
  exitCode?: number;
}

export async function executeSshCommand(
  config: SshConfig,
  command: string,
  onData?: (chunk: string) => void
): Promise<SshExecutionResult> {
  return new Promise((resolve) => {
    const conn = new Client();
    let stdoutBuffer = "";
    let stderrBuffer = "";

    const timeout = setTimeout(() => {
      conn.end();
      resolve({
        success: false,
        output: stdoutBuffer,
        error: "SSH Connection timed out after 30 seconds",
      });
    }, config.timeout || 30000);

    conn
      .on("ready", () => {
        conn.exec(command, (err, stream) => {
          if (err) {
            clearTimeout(timeout);
            conn.end();
            return resolve({
              success: false,
              output: stdoutBuffer,
              error: `Exec error: ${err.message}`,
            });
          }

          stream
            .on("close", (code: number) => {
              clearTimeout(timeout);
              conn.end();
              resolve({
                success: code === 0,
                output: stdoutBuffer,
                error: code !== 0 ? stderrBuffer || `Command exited with code ${code}` : undefined,
                exitCode: code,
              });
            })
            .on("data", (data: Buffer) => {
              const str = data.toString();
              stdoutBuffer += str;
              if (onData) onData(str);
            })
            .stderr.on("data", (data: Buffer) => {
              const str = data.toString();
              stderrBuffer += str;
              if (onData) onData(str);
            });
        });
      })
      .on("error", (err) => {
        clearTimeout(timeout);
        resolve({
          success: false,
          output: stdoutBuffer,
          error: `SSH connection error: ${err.message}`,
        });
      })
      .connect({
        host: config.host,
        port: config.port || 22,
        username: config.username || "root",
        password: config.password,
        privateKey: config.privateKey,
        readyTimeout: config.timeout || 20000,
        algorithms: {
          serverHostKey: [
            "ssh-rsa",
            "ssh-dss",
            "ecdsa-sha2-nistp256",
            "ecdsa-sha2-nistp384",
            "ecdsa-sha2-nistp521",
            "rsa-sha2-512",
            "rsa-sha2-256",
            "ssh-ed25519",
          ],
        },
      });
  });
}

/**
 * Automatiza la creación/actualización de un usuario SSH en la VPS remota
 */
export async function createLinuxSshUser(
  config: SshConfig,
  username: string,
  password: string,
  expirationDate?: Date | null
): Promise<SshExecutionResult> {
  // Genera fecha formateada para useradd/chage (YYYY-MM-DD)
  const expParam = expirationDate
    ? ` -e $(date -d "${expirationDate.toISOString().split("T")[0]}" +%Y-%m-%d)`
    : "";

  const cmd = `
    if id "${username}" &>/dev/null; then
      echo "Usuario existe, actualizando contraseña...";
      echo "${username}:${password}" | chpasswd;
    else
      useradd -M -s /bin/false${expParam} "${username}" || useradd -M -s /bin/bash${expParam} "${username}";
      echo "${username}:${password}" | chpasswd;
    fi
    echo "SUCCESS: Usuario ${username} configurado correctamente."
  `;

  return executeSshCommand(config, cmd);
}

/**
 * Automatiza la eliminación de un usuario SSH en la VPS remota
 */
export async function deleteLinuxSshUser(
  config: SshConfig,
  username: string
): Promise<SshExecutionResult> {
  const cmd = `userdel -f "${username}" || true; echo "Usuario ${username} removido."`;
  return executeSshCommand(config, cmd);
}
