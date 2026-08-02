@echo off
setlocal

if "%~1"=="" goto :uso
if "%~2"=="" goto :uso
if "%~3"=="" goto :uso

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0configurar-variable-servicio.ps1" -NombreServicio "%~1" -Clave "%~2" -Valor "%~3"

echo.
pause
exit /b 0

:uso
echo Uso: configurar-variable-servicio.bat NOMBRE_SERVICIO CLAVE VALOR
echo.
echo Ejemplos:
echo   configurar-variable-servicio.bat GTE ASPNETCORE_ENVIRONMENT Production
echo   configurar-variable-servicio.bat GTE ConnectionStrings__bdsGTE "Server=SRVPROD\NASA;Database=bdsGTE;User Id=svc_gte;Password=LA-CONTRASENA-REAL;TrustServerCertificate=True;Application Name=GTE.WebApi"
echo.
echo Requiere consola de Administrador. No borra otras variables ya puestas en el
echo servicio -- solo reemplaza la linea de la Clave indicada.
echo.
echo IMPORTANTE si corres esto desde una consola de PowerShell (no CMD): si el VALOR
echo tiene un simbolo $ (ej. una contrasena), usa comillas SIMPLES en PowerShell, no
echo dobles -- PowerShell interpola $algo como variable dentro de comillas dobles y
echo lo puede dejar vacio SIN ningun error visible.
pause
exit /b 1
