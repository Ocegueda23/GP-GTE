param(
    [Parameter(Mandatory = $true)][string]$NombreServicio,
    [Parameter(Mandatory = $true)][string]$Clave,
    [Parameter(Mandatory = $true)][string]$Valor
)

$ErrorActionPreference = "Stop"

$ruta = "HKLM:\SYSTEM\CurrentControlSet\Services\$NombreServicio"

if (-not (Test-Path $ruta)) {
    Write-Host "ERROR: no existe el servicio '$NombreServicio' en esta maquina." -ForegroundColor Red
    Write-Host "Crealo primero con 'sc create' (paso 3 del manual de instalacion)." -ForegroundColor Red
    exit 1
}

$actuales = @()
try {
    $actuales = (Get-ItemProperty -Path $ruta -Name Environment -ErrorAction Stop).Environment
} catch {
    $actuales = @()
}

# Conserva cualquier otra variable ya puesta -- solo reemplaza la linea de esta Clave.
$prefijo = "$Clave="
$sinClave = @($actuales | Where-Object { $_ -notlike "$prefijo*" })
$nuevas = $sinClave + "$Clave=$Valor"

try {
    New-ItemProperty -Path $ruta -Name Environment -PropertyType MultiString -Value $nuevas -Force -ErrorAction Stop | Out-Null
} catch {
    Write-Host ""
    Write-Host "ERROR: no se pudo escribir en el registro ($($_.Exception.Message))." -ForegroundColor Red
    Write-Host "Corre esta consola como Administrador e intenta de nuevo." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "OK. Variables de entorno actuales del servicio '$NombreServicio':" -ForegroundColor Green
foreach ($linea in $nuevas) {
    Write-Host "  $linea"
}
Write-Host ""
Write-Host "Falta reiniciar el servicio para que tome el cambio:"
Write-Host "  sc stop $NombreServicio"
Write-Host "  sc start $NombreServicio"
