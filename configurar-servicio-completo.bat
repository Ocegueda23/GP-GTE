@echo off
setlocal

if "%~1"=="" goto :uso
if "%~2"=="" goto :uso
if "%~3"=="" goto :uso

set SERVIDOR_SQL=%~1
set PASSWORD_SQL=%~2
set RUTA_ALMACEN=%~3
set NOMBRE_SERVICIO=%~4
if "%NOMBRE_SERVICIO%"=="" set NOMBRE_SERVICIO=GTE
set USUARIO_SQL=%~5
if "%USUARIO_SQL%"=="" set USUARIO_SQL=svc_gte

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0configurar-servicio-completo.ps1" -ServidorSql "%SERVIDOR_SQL%" -PasswordSql "%PASSWORD_SQL%" -RutaAlmacen "%RUTA_ALMACEN%" -NombreServicio "%NOMBRE_SERVICIO%" -UsuarioSql "%USUARIO_SQL%"

echo.
pause
exit /b 0

:uso
echo Uso: configurar-servicio-completo.bat SERVIDOR_SQL PASSWORD_SQL RUTA_ALMACEN [NOMBRE_SERVICIO] [USUARIO_SQL]
echo.
echo Registra de una sola vez las CUATRO variables que el sistema necesita
echo (ASPNETCORE_ENVIRONMENT, ConnectionStrings__bdsGTE, una Jwt__ClaveFirma nueva y
echo AlmacenArchivos__Ruta) y reinicia el servicio al final.
echo.
echo Ejemplo:
echo   configurar-servicio-completo.bat SRVPROD\NASA LA-CONTRASENA-DEL-LOGIN-SQL C:\GTE\Archivos
echo.
echo RUTA_ALMACEN es donde se guardan los archivos subidos: adjuntos, imagenes pegadas
echo en descripciones y comentarios, articulos de la base de conocimiento y fotos de
echo perfil. Carpeta local del servidor (C:\GTE\Archivos) o share de red
echo (\\servidor\GTE\Archivos). Es OBLIGATORIA: el valor que trae appsettings.json es
echo D:\GTE\Archivos, la ruta de la maquina de desarrollo, y si el servidor no tiene esa
echo unidad TODA subida de archivos falla con INTERNAL_ERROR. Para una carpeta local, el
echo script la crea y comprueba que se puede escribir en ella antes de tocar el registro.
echo.
echo OJO, EL ORDEN CAMBIO: RUTA_ALMACEN es el TERCER parametro. Antes el tercero era
echo NOMBRE_SERVICIO, que ahora es el cuarto (default GTE); USUARIO_SQL es el quinto
echo (default svc_gte).
echo.
echo Requiere consola de Administrador.
echo IMPORTANTE si corres esto desde PowerShell (no CMD): si la contrasena tiene un
echo simbolo $, usa comillas SIMPLES en PowerShell, no dobles -- si no, PowerShell la
echo puede dejar vacia SIN ningun error visible.
pause
exit /b 1
