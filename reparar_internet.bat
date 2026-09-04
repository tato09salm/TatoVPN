@echo off
chcp 65001 >nul
:: Solicitar permisos de administrador automáticamente si no los tiene
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Solicitando permisos de administrador...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

title Reparador de Red y DNS - TatoVPN
cls
echo ================================================================
echo         REPARANDO CONEXION WI-FI Y SERVIDORES DNS
echo ================================================================
echo.
echo [1/5] Restaurando configuracion automatica (DHCP) en Wi-Fi...
netsh interface ipv4 set dnsservers name="Wi-Fi" source=dhcp
netsh interface ipv6 set dnsservers name="Wi-Fi" source=dhcp

echo [2/5] Restaurando adaptadores Ethernet secundarios...
netsh interface ipv4 set dnsservers name="Ethernet" source=dhcp >nul 2>&1
netsh interface ipv4 set dnsservers name="Ethernet 2" source=dhcp >nul 2>&1

echo [3/5] Limpiando cache DNS de Windows...
ipconfig /flushdns

echo [4/5] Liberando y renovando direccion IP en Wi-Fi...
ipconfig /renew "Wi-Fi" >nul 2>&1

echo [5/5] Eliminando archivos de respaldo huerfanos de DNS...
if exist "%TEMP%\tatovpn_dns_backup.json" del /f /q "%TEMP%\tatovpn_dns_backup.json"

echo.
echo ================================================================
echo   EXITO: El Wi-Fi y sus DNS han sido restaurados a automatico.
echo   Ya puedes navegar por internet en tu red Wi-Fi.
echo ================================================================
echo.
pause
