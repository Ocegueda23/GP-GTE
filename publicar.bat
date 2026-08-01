@echo off
setlocal

set CONFIGURACION=Release

if "%~1"=="" (
    set CARPETA_DESTINO=C:\publicado\GTE
) else (
    set CARPETA_DESTINO=%~1
)

echo ===================================================
echo   Publicando GTE (%CONFIGURACION%)
echo   Destino: %CARPETA_DESTINO%
echo ===================================================

cd /d "%~dp0"

echo.
echo === Paso 1/3: build del frontend (npm run build) ===
pushd frontend\gte-web
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
echo === Paso 2/3: dotnet publish de GTE.WebApi ===
dotnet publish "src\GTE.WebApi\GTE.WebApi.csproj" -c %CONFIGURACION% -o "%CARPETA_DESTINO%"
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ===== ERROR: la publicacion de la API fallo =====
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo === Paso 3/3: copiar el build del frontend a wwwroot del publish ===
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
echo ===== Publicado correctamente en %CARPETA_DESTINO% =====
echo Copia esa carpeta completa al servidor de destino y sigue el Paso 3
echo del manual de instalacion (Doctos\MANUAL_INSTALACION_GTE.md).
pause
