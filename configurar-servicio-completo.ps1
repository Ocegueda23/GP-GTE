param(
    [Parameter(Mandatory = $true)][string]$ServidorSql,
    [Parameter(Mandatory = $true)][string]$PasswordSql,
    [string]$NombreServicio = "GTE",
    [string]$UsuarioSql = "svc_gte"
)

$ErrorActionPreference = "Stop"

$ruta = "HKLM:\SYSTEM\CurrentControlSet\Services\$NombreServicio"

if (-not (Test-Path $ruta)) {
    Write-Host "ERROR: no existe el servicio '$NombreServicio' en esta maquina." -ForegroundColor Red
    Write-Host "Crealo primero con 'sc create' (paso 3 del manual de instalacion)." -ForegroundColor Red
    exit 1
}

function ActualizarVariable {
    param($Ruta, $Clave, $Valor)
    $actuales = @()
    try {
        $actuales = (Get-ItemProperty -Path $Ruta -Name Environment -ErrorAction Stop).Environment
    } catch {
        $actuales = @()
    }
    $prefijo = "$Clave="
    $sinClave = @($actuales | Where-Object { $_ -notlike "$prefijo*" })
    $nuevas = $sinClave + "$Clave=$Valor"
    New-ItemProperty -Path $Ruta -Name Environment -PropertyType MultiString -Value $nuevas -Force | Out-Null
    return $nuevas
}

try {
    Write-Host "==================================================="
    Write-Host "  Configurando el servicio '$NombreServicio' completo"
    Write-Host "==================================================="
    Write-Host ""

    Write-Host "1/3: ASPNETCORE_ENVIRONMENT=Production"
    ActualizarVariable -Ruta $ruta -Clave "ASPNETCORE_ENVIRONMENT" -Valor "Production" | Out-Null

    $cadenaConexion = "Server=$ServidorSql;Database=bdsGTE;User Id=$UsuarioSql;Password=$PasswordSql;TrustServerCertificate=True;Application Name=GTE.WebApi"
    Write-Host "2/3: ConnectionStrings__bdsGTE (servidor: $ServidorSql, usuario: $UsuarioSql)"
    ActualizarVariable -Ruta $ruta -Clave "ConnectionStrings__bdsGTE" -Valor $cadenaConexion | Out-Null

    Write-Host "3/3: Jwt__ClaveFirma (clave nueva, aleatoria)"
    $bytes = New-Object byte[] 48
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($bytes)
    $clave = [Convert]::ToBase64String($bytes)
    $nuevas = ActualizarVariable -Ruta $ruta -Clave "Jwt__ClaveFirma" -Valor $clave
    Set-Clipboard -Value $clave
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
Write-Host "La clave Jwt__ClaveFirma tambien quedo copiada al portapapeles -- guardala en"
Write-Host "el gestor de contrasenas del equipo."
Write-Host ""

Write-Host "==================================================="
Write-Host "  Reiniciando el servicio '$NombreServicio'..."
Write-Host "==================================================="
try {
    Restart-Service -Name $NombreServicio -Force -ErrorAction Stop
    Start-Sleep -Seconds 2
    $estado = (Get-Service -Name $NombreServicio).Status
    if ($estado -eq "Running") {
        Write-Host "OK: el servicio quedo '$estado'." -ForegroundColor Green
    } else {
        Write-Host "AVISO: el servicio quedo en estado '$estado' (no 'Running')." -ForegroundColor Yellow
        Write-Host "Revisa el Visor de Eventos de Windows (Application) para el detalle del error." -ForegroundColor Yellow
    }
} catch {
    Write-Host "ERROR al reiniciar el servicio: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Reinicialo a mano: sc stop $NombreServicio  /  sc start $NombreServicio" -ForegroundColor Red
}
