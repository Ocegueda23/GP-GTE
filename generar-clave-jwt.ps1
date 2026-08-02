param(
    [string]$NombreServicio = "GTE"
)

$ErrorActionPreference = "Stop"

Write-Host "==================================================="
Write-Host "  Generando clave de firma JWT (Jwt__ClaveFirma)"
Write-Host "==================================================="
Write-Host ""

$bytes = New-Object byte[] 48
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
$clave = [Convert]::ToBase64String($bytes)

Write-Host $clave
Set-Clipboard -Value $clave

Write-Host ""
Write-Host "La clave de arriba ya esta copiada al portapapeles."
Write-Host "Guardala tambien en el gestor de contrasenas del equipo."
Write-Host ""

$ruta = "HKLM:\SYSTEM\CurrentControlSet\Services\$NombreServicio"

if (Test-Path $ruta) {
    Write-Host "==================================================="
    Write-Host "  Escribiendo Jwt__ClaveFirma en el servicio '$NombreServicio'"
    Write-Host "==================================================="

    $actuales = @()
    try {
        $actuales = (Get-ItemProperty -Path $ruta -Name Environment -ErrorAction Stop).Environment
    } catch {
        $actuales = @()
    }

    # Conserva cualquier otra variable ya puesta (ConnectionStrings__bdsGTE,
    # ASPNETCORE_ENVIRONMENT, etc.) -- solo reemplaza la linea de Jwt__ClaveFirma.
    $sinJwt = @($actuales | Where-Object { $_ -notlike "Jwt__ClaveFirma=*" })
    $nuevas = $sinJwt + "Jwt__ClaveFirma=$clave"

    try {
        New-ItemProperty -Path $ruta -Name Environment -PropertyType MultiString -Value $nuevas -Force -ErrorAction Stop | Out-Null
    } catch {
        Write-Host ""
        Write-Host "ERROR: no se pudo escribir en el registro ($($_.Exception.Message))." -ForegroundColor Red
        Write-Host "Corre esta consola como Administrador e intenta de nuevo." -ForegroundColor Red
        exit 1
    }

    Write-Host ""
    Write-Host "OK. Variables de entorno actuales del servicio '$NombreServicio':"
    foreach ($linea in $nuevas) {
        Write-Host "  $linea"
    }
    Write-Host ""
    Write-Host "Falta reiniciar el servicio para que tome el cambio:"
    Write-Host "  sc stop $NombreServicio"
    Write-Host "  sc start $NombreServicio"
} else {
    Write-Host "==================================================="
    Write-Host "  No existe el servicio '$NombreServicio' en esta maquina todavia."
    Write-Host "  Solo se genero la clave (arriba) -- creala en el servicio real"
    Write-Host "  con 'sc create' (paso 3 del manual) y vuelve a correr este"
    Write-Host "  script ahi, o pasa el nombre real: generar-clave-jwt.bat NOMBRE"
    Write-Host "==================================================="
}
