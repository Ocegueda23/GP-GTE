@echo off
setlocal

if "%~1"=="" goto :uso

if "%~2"=="" (
    set NOMBRE_SERVICIO=GTE
) else (
    set NOMBRE_SERVICIO=%~2
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0configurar-almacen-archivos.ps1" -Ruta "%~1" -NombreServicio "%NOMBRE_SERVICIO%"

echo.
pause
exit /b 0

:uso
echo Uso: configurar-almacen-archivos.bat RUTA_DEL_ALMACEN [NOMBRE_SERVICIO]
echo.
echo Fija donde guarda GTE los archivos subidos (adjuntos, imagenes pegadas en
echo descripciones y comentarios, articulos de la base de conocimiento, fotos de
echo perfil), crea la carpeta, comprueba que se puede escribir ahi y reinicia el
echo servicio.
echo.
echo Ejemplos:
echo   configurar-almacen-archivos.bat C:\GTE\Archivos
echo   configurar-almacen-archivos.bat \\servidor\GTE\Archivos
echo   configurar-almacen-archivos.bat D:\GTE\Archivos GTE_PRUEBAS
echo.
echo NOMBRE_SERVICIO es opcional, por defecto GTE.
echo.
echo Requiere consola de Administrador. No borra las demas variables de entorno del
echo servicio: solo reemplaza AlmacenArchivos__Ruta.
echo.
echo POR QUE HACE FALTA: el valor que trae appsettings.json es D:\GTE\Archivos, la
echo ruta de la maquina de desarrollo. Si el servidor no tiene esa unidad, TODA
echo subida de archivos falla con INTERNAL_ERROR, y sin esta variable de entorno ese
echo valor es el que gana.
pause
exit /b 1
