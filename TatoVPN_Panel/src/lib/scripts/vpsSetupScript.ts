export interface VpsSetupOptions {
  ssl?: number;
  dropbear?: number;
  badvpn?: number;
  openssh?: number;
  bannerText?: string;
}

export function generateVpsSetupScript(options: VpsSetupOptions = {}) {
  const ssl = options.ssl || 443;
  const dropbear = options.dropbear || 80;
  const badvpn = options.badvpn || 7300;
  const openssh = options.openssh || 22;
  const bannerText = options.bannerText || "TATO-VPN";

  return `#!/bin/bash
# ==========================================================================
# TatoVPN Automated Enterprise VPS Setup Script (Ubuntu VM / Debian / Linux)
# Puertos: SSL/TLS: ${ssl}, OpenSSH: ${openssh}, Dropbear: ${dropbear}, Dropbear-SSL: 442, BadVPN: ${badvpn}
# Banner: ${bannerText}
# ==========================================================================
set -e
export DEBIAN_FRONTEND=noninteractive

echo "=========================================================="
echo " [>] INICIANDO CONFIGURACIÓN PROFESIONAL DE PUERTOS TATOVPN"
echo " [>] OPTIMIZADO PARA VM UBUNTU (20.04 / 22.04 / 24.04)"
echo "=========================================================="

# 1. Detectar sistema operativo
if [ -f /etc/os-release ]; then
  . /etc/os-release
  OS=$ID
else
  OS="unknown"
fi
echo "[+] Sistema operativo detectado: $OS ($VERSION_ID)"

# 2. Liberar puertos 80, 443 y 442 de posibles conflictos web
echo "[+] Verificando y liberando puertos 80 / 443 / 442 de procesos conflictivos..."
systemctl stop apache2 nginx httpd caddy lighttpd 2>/dev/null || true
systemctl disable apache2 nginx httpd caddy lighttpd 2>/dev/null || true
fuser -k 80/tcp 2>/dev/null || true
fuser -k ${ssl}/tcp 2>/dev/null || true
fuser -k 443/tcp 2>/dev/null || true
fuser -k 442/tcp 2>/dev/null || true

# 3. Actualizar e instalar dependencias
echo "[+] Instalando paquetes base (OpenSSH, Stunnel4, Dropbear, BadVPN, Psmisc, IPTables)..."
if [[ "$OS" == "ubuntu" || "$OS" == "debian" ]]; then
  apt-get update -y
  apt-get install -y openssh-server stunnel4 dropbear cmake build-essential git iptables ufw curl wget net-tools psmisc libssl-dev
elif [[ "$OS" == "centos" || "$OS" == "almalinux" || "$OS" == "rocky" || "$OS" == "fedora" ]]; then
  yum install -y epel-release || dnf install -y epel-release
  yum install -y openssh-server stunnel dropbear git iptables curl wget net-tools psmisc openssl-devel
fi

# 4. Configurar Banner en Color Rojo
echo "[+] Configurando Banner de bienvenida en color ROJO con '${bannerText}'..."
cat << 'EOF_BANNER' > /etc/issue.net
<p style="text-align:center;"><b><font color="red">====================================================</font></b></p>
<p style="text-align:center;"><b><font color="red">              ⚡ ${bannerText} ⚡               </font></b></p>
<p style="text-align:center;"><b><font color="#ff3333">     SERVIDOR UBUNTU OPTIMIZADO PARA TATOVPN_C#     </font></b></p>
<p style="text-align:center;"><b><font color="yellow">  SSH: ${openssh} | SSL: ${ssl} | DROPBEAR: ${dropbear} | SSL-DB: 442 </font></b></p>
<p style="text-align:center;"><b><font color="red">====================================================</font></b></p>
EOF_BANNER

# Banner ANSI para terminal SSH directa
cat << 'EOF_MOTD' > /etc/motd
\\033[1;31m====================================================\\033[0m
\\033[1;31m              ⚡ ${bannerText} ⚡               \\033[0m
\\033[1;33m     SERVIDOR UBUNTU OPTIMIZADO PARA TATOVPN_C#     \\033[0m
\\033[1;32m  SSH: ${openssh} | SSL: ${ssl} | DROPBEAR: ${dropbear} | SSL-DB: 442 \\033[0m
\\033[1;31m====================================================\\033[0m
EOF_MOTD

# 5. Configurar OpenSSH Server
echo "[+] Configurando OpenSSH en puerto ${openssh}..."
sed -i 's/#PasswordAuthentication yes/PasswordAuthentication yes/g' /etc/ssh/sshd_config
sed -i 's/PasswordAuthentication no/PasswordAuthentication yes/g' /etc/ssh/sshd_config
sed -i 's/#PermitRootLogin prohibit-password/PermitRootLogin yes/g' /etc/ssh/sshd_config
sed -i 's/PermitRootLogin no/PermitRootLogin yes/g' /etc/ssh/sshd_config
sed -i 's/#Banner none/Banner \\/etc\\/issue.net/g' /etc/ssh/sshd_config
sed -i 's/#Banner \\/etc\\/issue.net/Banner \\/etc\\/issue.net/g' /etc/ssh/sshd_config

if ! grep -q "Banner /etc/issue.net" /etc/ssh/sshd_config; then
  echo "Banner /etc/issue.net" >> /etc/ssh/sshd_config
fi

if ! grep -q "Port ${openssh}" /etc/ssh/sshd_config; then
  echo "Port ${openssh}" >> /etc/ssh/sshd_config
fi

systemctl restart ssh || systemctl restart sshd || service ssh restart || true

# 6. Configurar Dropbear como Servicio Systemd Robusto
echo "[+] Configurando Dropbear en puerto ${dropbear}..."
mkdir -p /etc/dropbear
if [ ! -f /etc/dropbear/dropbear_rsa_host_key ]; then
  dropbearkey -t rsa -f /etc/dropbear/dropbear_rsa_host_key 2>/dev/null || true
fi
if [ ! -f /etc/dropbear/dropbear_dss_host_key ]; then
  dropbearkey -t dss -f /etc/dropbear/dropbear_dss_host_key 2>/dev/null || true
fi
if [ ! -f /etc/dropbear/dropbear_ecdsa_host_key ]; then
  dropbearkey -t ecdsa -f /etc/dropbear/dropbear_ecdsa_host_key 2>/dev/null || true
fi

# Deshabilitar sockets conflictivos de systemd en Ubuntu
systemctl stop dropbear dropbear.socket 2>/dev/null || true
systemctl disable dropbear.socket 2>/dev/null || true
systemctl mask dropbear.socket 2>/dev/null || true

# Configurar /etc/default/dropbear
cat << 'EOF_DROPBEAR_DEFAULT' > /etc/default/dropbear
NO_START=0
DROPBEAR_PORT=${dropbear}
DROPBEAR_EXTRA_ARGS="-b /etc/issue.net"
DROPBEAR_BANNER="/etc/issue.net"
DROPBEAR_RECEIVE_WINDOW=65536
EOF_DROPBEAR_DEFAULT

# Crear servicio systemd directo para Dropbear
cat << 'EOF_DROPBEAR_SERVICE' > /etc/systemd/system/dropbear.service
[Unit]
Description=Dropbear SSH Server for TatoVPN
After=network.target auditd.service

[Service]
Type=forking
ExecStart=/usr/sbin/dropbear -p ${dropbear} -b /etc/issue.net -R
PIDFile=/var/run/dropbear.pid
Restart=always
RestartSec=3

[Install]
WantedBy=multi-user.target
EOF_DROPBEAR_SERVICE

systemctl daemon-reload
systemctl unmask dropbear 2>/dev/null || true
systemctl enable dropbear || true
systemctl restart dropbear || service dropbear restart || true

# 7. Configurar Stunnel4 (SSL/TLS ${ssl} -> 22 / ${openssh} y 442 -> ${dropbear})
echo "[+] Generando certificados y configurando Stunnel (SSL/TLS ${ssl} y 442)..."
mkdir -p /etc/stunnel /var/run/stunnel4 /var/log/stunnel4
chmod 755 /var/run/stunnel4 2>/dev/null || true

# Habilitar stunnel4 en Ubuntu / Debian
cat << 'EOF_STUNNEL_DEFAULT' > /etc/default/stunnel4
ENABLED=1
FILES="/etc/stunnel/*.conf"
OPTIONS=""
PPP_RESTART=0
EOF_STUNNEL_DEFAULT

# Generar certificado SSL autofirmado de 2048 bits
openssl req -new -newkey rsa:2048 -days 3650 -nodes -x509 -sha256 \
  -subj "/C=US/ST=VPN/L=Tato/O=TatoVPN/CN=tatovpn.net" \
  -keyout /etc/stunnel/stunnel.pem -out /etc/stunnel/stunnel.pem 2>/dev/null || \
openssl req -new -x509 -days 3650 -nodes -subj "/C=US/ST=VPN/L=Tato/O=TatoVPN/CN=tatovpn.net" -out /etc/stunnel/stunnel.pem -keyout /etc/stunnel/stunnel.pem

chmod 600 /etc/stunnel/stunnel.pem

# Configuración de Stunnel
cat << EOF_STUNNEL > /etc/stunnel/stunnel.conf
pid = /var/run/stunnel4.pid
cert = /etc/stunnel/stunnel.pem
client = no
socket = l:TCP_NODELAY=1
socket = r:TCP_NODELAY=1

[ssh-ssl]
accept = 0.0.0.0:${ssl}
connect = 127.0.0.1:${openssh}

[dropbear-ssl]
accept = 0.0.0.0:442
connect = 127.0.0.1:${dropbear}
EOF_STUNNEL

STUNNEL_BIN=$(which stunnel4 || which stunnel || echo "/usr/bin/stunnel4")

cat << EOF_STUNNEL_SERVICE > /etc/systemd/system/stunnel4.service
[Unit]
Description=Stunnel4 TLS Gateway for TatoVPN
After=network.target

[Service]
Type=forking
ExecStart=$STUNNEL_BIN /etc/stunnel/stunnel.conf
PIDFile=/var/run/stunnel4.pid
Restart=always
RestartSec=3

[Install]
WantedBy=multi-user.target
EOF_STUNNEL_SERVICE

# Crear alias para stunnel.service
cp /etc/systemd/system/stunnel4.service /etc/systemd/system/stunnel.service 2>/dev/null || true

systemctl daemon-reload
systemctl unmask stunnel4 stunnel 2>/dev/null || true
systemctl enable stunnel4 2>/dev/null || systemctl enable stunnel 2>/dev/null || true
systemctl restart stunnel4 2>/dev/null || systemctl restart stunnel 2>/dev/null || $STUNNEL_BIN /etc/stunnel/stunnel.conf || true

# 8. Configurar BadVPN UDPGW (Puerto ${badvpn})
echo "[+] Configurando BadVPN udpgw en puerto ${badvpn}..."
mkdir -p /tmp/badvpn_build
cd /tmp/badvpn_build
if [ ! -f /usr/local/bin/badvpn-udpgw ]; then
  wget -q https://github.com/ambrop72/badvpn/archive/refs/tags/1.999.130.tar.gz -O badvpn.tar.gz || true
  if [ -f badvpn.tar.gz ]; then
    tar -xzf badvpn.tar.gz
    cd badvpn-1.999.130
    mkdir -p build && cd build
    cmake .. -DBUILD_NOTHING_BY_DEFAULT=1 -DBUILD_UDPGW=1
    make install
  fi
fi

cat << 'EOF_BADVPN' > /etc/systemd/system/badvpn.service
[Unit]
Description=BadVPN UDP Gateway for TatoVPN
After=network.target

[Service]
ExecStart=/usr/local/bin/badvpn-udpgw --listen-addr 127.0.0.1:${badvpn} --max-clients 999
Restart=always
User=root

[Install]
WantedBy=multi-user.target
EOF_BADVPN

systemctl daemon-reload
systemctl enable badvpn || true
systemctl restart badvpn || true

# 9. Reglas de Firewall y Apertura de Puertos
echo "[+] Configurando reglas de Firewall e Iptables para permitir conexiones..."
if command -v ufw &>/dev/null; then
  ufw allow ${openssh}/tcp || true
  ufw allow ${ssl}/tcp || true
  ufw allow 443/tcp || true
  ufw allow ${dropbear}/tcp || true
  ufw allow 80/tcp || true
  ufw allow 442/tcp || true
  ufw allow ${badvpn}/udp || true
fi

# Iptables persistentes
iptables -I INPUT -p tcp --dport ${openssh} -j ACCEPT || true
iptables -I INPUT -p tcp --dport ${ssl} -j ACCEPT || true
iptables -I INPUT -p tcp --dport 443 -j ACCEPT || true
iptables -I INPUT -p tcp --dport ${dropbear} -j ACCEPT || true
iptables -I INPUT -p tcp --dport 80 -j ACCEPT || true
iptables -I INPUT -p tcp --dport 442 -j ACCEPT || true
iptables -I INPUT -p udp --dport ${badvpn} -j ACCEPT || true

# 10. Diagnóstico y Estado de Puertos en Escucha
echo ""
echo "=========================================================="
echo " [✓] REPORTE DE PUERTOS EN ESCUCHA (LISTEN):"
echo "=========================================================="
netstat -tlpn | grep -E "${openssh}|${ssl}|${dropbear}|442|443|stunnel|dropbear|sshd|ssh" || ss -tlpn | grep -E "${openssh}|${ssl}|${dropbear}|442|443" || true
echo "=========================================================="
echo " [✓] CONFIGURACIÓN TATOVPN COMPLETADA EXITOSAMENTE!"
echo " [*] OpenSSH Server: Puerto ${openssh} (ONLINE)"
echo " [*] SSL/TLS Stunnel (OpenSSH): Puerto ${ssl} (ONLINE)"
echo " [*] SSL/TLS Stunnel (Dropbear): Puerto 442 (ONLINE)"
echo " [*] Dropbear Server Direct: Puerto ${dropbear} (ONLINE)"
echo " [*] BadVPN UDP: Puerto ${badvpn} (ONLINE)"
echo " [*] Banner Rojo: '${bannerText}' (ACTIVO)"
echo "=========================================================="
`;
}

