@echo off
setlocal

if "%~1"=="" goto :uso
if "%~2"=="" goto :uso

set SERVIDOR_SQL=%~1
set PASSWORD_SQL=%~2
set NOMBRE_SERVICIO=%~3
if "%NOMBRE_SERVICIO%"=="" set NOMBRE_SERVICIO=GTE
set USUARIO_SQL=%~4
if "%USUARIO_SQL%"=="" set USUARIO_SQL=svc_gte

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0configurar-servicio-completo.ps1" -ServidorSql "%SERVIDOR_SQL%" -PasswordSql "%PASSWORD_SQL%" -NombreServicio "%NOMBRE_SERVICIO%" -UsuarioSql "%USUARIO_SQL%"

echo.
pause
exit /b 0

:uso
echo Uso: configurar-servicio-completo.bat SERVIDOR_SQL PASSWORD_SQL [NOMBRE_SERVICIO] [USUARIO_SQL]
echo.
echo Registra de una sola vez ASPNETCORE_ENVIRONMENT, ConnectionStrings__bdsGTE y una
echo Jwt__ClaveFirma nueva en el servicio, y lo reinicia al final.
echo.
echo Ejemplo:
echo   configurar-servicio-completo.bat SRVPROD\NASA LA-CONTRASENA-DEL-LOGIN-SQL
echo.
echo NOMBRE_SERVICIO por default es GTE; USUARIO_SQL por default es svc_gte.
echo.
echo Requiere consola de Administrador.
echo IMPORTANTE si corres esto desde PowerShell (no CMD): si la contrasena tiene un
echo simbolo $, usa comillas SIMPLES en PowerShell, no dobles -- si no, PowerShell la
echo puede dejar vacia SIN ningun error visible.
pause
exit /b 1
