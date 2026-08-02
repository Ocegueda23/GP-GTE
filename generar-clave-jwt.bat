@echo off
setlocal

rem Nombre del Windows Service a configurar (default GTE). Si el servicio no
rem existe todavia en esta maquina, el script solo genera y muestra la clave.
if "%~1"=="" (
    set NOMBRE_SERVICIO=GTE
) else (
    set NOMBRE_SERVICIO=%~1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0generar-clave-jwt.ps1" -NombreServicio "%NOMBRE_SERVICIO%"

echo.
pause
