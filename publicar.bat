@echo off
setlocal

set CONFIGURACION=Release

if "%~1"=="" (
    set CARPETA_DESTINO=C:\publicado\GTE
) else (
    set CARPETA_DESTINO=%~1
)

cd /d "%~dp0"

rem La version la manda Directory.Build.props (esquema Proyecto.Mejora.Defecto.Reenvio,
rem ver CLAUDE.md seccion "Versionado"): se sube A MANO al liberar, contra el informe de
rem liberacion. Aqui solo se lee, para estampar la MISMA en el ensamblado de la API
rem (-p:Version, que devuelve /api/v1/version) y en el bundle del frontend (VITE_VERSION,
rem que la barra superior muestra debajo de "GTE"): asi se ve de un golpe de vista si el
rem servidor quedo al dia y si las dos mitades del despliegue coinciden.
for /f %%v in ('powershell -NoProfile -Command "([xml](Get-Content Directory.Build.props)).Project.PropertyGroup.Version.Trim()"') do set VERSION=%%v
if not defined VERSION (
    echo ===== ERROR: no se pudo leer la version de Directory.Build.props =====
    pause
    exit /b 1
)

echo ===================================================
echo   Publicando GTE (%CONFIGURACION%)
echo   Version: %VERSION%
echo   Destino: %CARPETA_DESTINO%
echo ===================================================

echo.
echo === Paso 1/4: limpiar la carpeta de destino (evita mezclar publishes viejos) ===
if exist "%CARPETA_DESTINO%" (
    rmdir /s /q "%CARPETA_DESTINO%"
)

echo.
echo === Paso 2/4: build del frontend (npm run build) ===
pushd frontend\gte-web
set VITE_VERSION=%VERSION%
call npm run build
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ===== ERROR: el build del frontend fallo =====
    popd
    pause
    exit /b %ERRORLEVEL%
)
popd

echo.
echo === Paso 3/4: dotnet publish de GTE.WebApi ===
dotnet publish "src\GTE.WebApi\GTE.WebApi.csproj" -c %CONFIGURACION% -p:Version=%VERSION% -o "%CARPETA_DESTINO%"
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ===== ERROR: la publicacion de la API fallo =====
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo === Paso 4/4: copiar el build del frontend a wwwroot del publish ===
if exist "%CARPETA_DESTINO%\wwwroot" (
    rmdir /s /q "%CARPETA_DESTINO%\wwwroot"
)
xcopy /e /i /y "frontend\gte-web\dist" "%CARPETA_DESTINO%\wwwroot" >nul
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ===== ERROR: no se pudo copiar el build del frontend a wwwroot =====
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ===== Publicado correctamente en %CARPETA_DESTINO% (version %VERSION%) =====
echo Confirma en el servidor que la barra superior diga %VERSION% debajo de "GTE"
echo (o revisa GET /api/v1/version); si dice otra cosa, el despliegue no quedo.
echo Copia esa carpeta completa al servidor de destino y sigue el Paso 3
echo del manual de instalacion (Doctos\MANUAL_INSTALACION_GTE.md).
pause
