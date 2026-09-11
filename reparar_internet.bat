@echo off
chcp 65001 >nul
:: Solicitar permisos de administrador automáticamente si no los tiene
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Solicitando permisos de administrador...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

title Reparador de Red, Rutas y DNS - TatoVPN
cls
echo ================================================================
echo         REPARANDO CONEXION, RUTAS Y SERVIDORES DNS
echo ================================================================
echo.

echo [1/6] Cerrando procesos huerfanos de tun2socks...
taskkill /f /im tun2socks.exe >nul 2>&1

echo [2/6] Eliminando rutas de enrutamiento huerfanas de la VPN...
route delete 0.0.0.0 mask 128.0.0.0 >nul 2>&1
route delete 128.0.0.0 mask 128.0.0.0 >nul 2>&1

echo [3/6] Desactivando interfaz virtual TatoVPN...
netsh interface ipv4 delete address name="TatoVPN" gateway=all >nul 2>&1
netsh interface set interface name="TatoVPN" admin=disabled >nul 2>&1

echo [4/6] Restaurando configuracion automatica (DHCP) en adaptadores...
netsh interface ipv4 set dnsservers name="Wi-Fi" source=dhcp >nul 2>&1
netsh interface ipv6 set dnsservers name="Wi-Fi" source=dhcp >nul 2>&1
netsh interface ipv4 set dnsservers name="Ethernet" source=dhcp >nul 2>&1
netsh interface ipv4 set dnsservers name="Ethernet 2" source=dhcp >nul 2>&1

echo [5/6] Limpiando cache DNS de Windows...
ipconfig /flushdns

echo [6/6] Liberando y renovando direccion IP en Wi-Fi...
ipconfig /renew "Wi-Fi" >nul 2>&1

echo [Extra] Eliminando archivos de respaldo huerfanos de DNS...
if exist "%TEMP%\tatovpn_dns_backup.json" del /f /q "%TEMP%\tatovpn_dns_backup.json"

echo.
echo ================================================================
echo   EXITO: Rutas liberadas, adaptadores y DNS restablecidos.
echo   Ya puedes navegar normalmente por internet.
echo ================================================================
echo.
pause
