param(
    [Parameter(Mandatory = $true)][string]$ServidorSql,
    [Parameter(Mandatory = $true)][string]$PasswordSql,
    [Parameter(Mandatory = $true)][string]$RutaAlmacen,
    [string]$NombreServicio = "GTE",
    [string]$UsuarioSql = "svc_gte"
)

$ErrorActionPreference = "Stop"

# RutaAlmacen es obligatoria y quedo en la TERCERA posicion. Sin ella el servicio se queda
# con la ruta de appsettings.json (D:\GTE\Archivos, la de la maquina de desarrollo) y TODA
# subida de archivos falla con INTERNAL_ERROR: adjuntos, imagenes pegadas en descripciones y
# comentarios, base de conocimiento y fotos de perfil. Este guard atrapa una llamada con el
# orden viejo, donde el tercer argumento era el nombre del servicio.
# En -like el backslash NO escapa: '\\*' son dos barras literales, el prefijo de un UNC.
$pareceRuta = ($RutaAlmacen -match '^[A-Za-z]:\\') -or ($RutaAlmacen -like '\\*')
if (-not $pareceRuta) {
    Write-Host "ERROR: -RutaAlmacen no parece una ruta: '$RutaAlmacen'" -ForegroundColor Red
    Write-Host "Se espera una carpeta local del servidor o un share de red." -ForegroundColor Red
    Write-Host "El orden de los parametros CAMBIO -- ahora es:" -ForegroundColor Red
    Write-Host "  SERVIDOR_SQL PASSWORD_SQL RUTA_ALMACEN [NOMBRE_SERVICIO] [USUARIO_SQL]" -ForegroundColor Red
    exit 1
}

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

# Se comprueba ANTES de tocar el registro: si la ruta no sirve, mas vale no cambiar nada que
# dejar el servicio apuntando a una ruta igual de mala.
$esShareDeRed = $RutaAlmacen -like '\\*'
if (-not $esShareDeRed) {
    try {
        if (-not (Test-Path $RutaAlmacen)) {
            New-Item -ItemType Directory -Path $RutaAlmacen -Force -ErrorAction Stop | Out-Null
        }
        # [IO.Path]::Combine y no Join-Path: Join-Path valida la unidad y tapa el error real
        # ("no existe la unidad Z") con un "la ruta no puede ser nula" que no dice nada.
        $sonda = [IO.Path]::Combine($RutaAlmacen, ".sonda-" + [Guid]::NewGuid().ToString("N"))
        [IO.File]::WriteAllBytes($sonda, [byte[]]@(0))
        Remove-Item $sonda -Force
    } catch {
        Write-Host "ERROR: no se pudo escribir en el almacen de archivos $RutaAlmacen" -ForegroundColor Red
        Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        Write-Host "No se cambio nada. Corrige la ruta o los permisos y vuelve a correr esto:" -ForegroundColor Red
        Write-Host "  - Si la unidad no existe en este servidor, usa otra (ej. C:\GTE\Archivos)." -ForegroundColor Red
        Write-Host "  - Si es de permisos, da control total a la cuenta del servicio sobre esa carpeta." -ForegroundColor Red
        exit 1
    }
}

try {
    Write-Host "==================================================="
    Write-Host "  Configurando el servicio '$NombreServicio' completo"
    Write-Host "==================================================="
    Write-Host ""

    Write-Host "1/4: ASPNETCORE_ENVIRONMENT=Production"
    ActualizarVariable -Ruta $ruta -Clave "ASPNETCORE_ENVIRONMENT" -Valor "Production" | Out-Null

    $cadenaConexion = "Server=$ServidorSql;Database=bdsGTE;User Id=$UsuarioSql;Password=$PasswordSql;TrustServerCertificate=True;Application Name=GTE.WebApi"
    Write-Host "2/4: ConnectionStrings__bdsGTE (servidor: $ServidorSql, usuario: $UsuarioSql)"
    ActualizarVariable -Ruta $ruta -Clave "ConnectionStrings__bdsGTE" -Valor $cadenaConexion | Out-Null

    Write-Host "3/4: Jwt__ClaveFirma (clave nueva, aleatoria)"
    $bytes = New-Object byte[] 48
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($bytes)
    $clave = [Convert]::ToBase64String($bytes)
    ActualizarVariable -Ruta $ruta -Clave "Jwt__ClaveFirma" -Valor $clave | Out-Null
    Set-Clipboard -Value $clave

    Write-Host "4/4: AlmacenArchivos__Ruta ($RutaAlmacen)"
    if ($esShareDeRed) {
        Write-Host "     Es un share de red: no se comprueba por ti, la sonda correria con TU" -ForegroundColor Yellow
        Write-Host "     cuenta y no con la del servicio. Confirma que la cuenta bajo la que" -ForegroundColor Yellow
        Write-Host "     corre '$NombreServicio' (por defecto LocalSystem, que en la red es la" -ForegroundColor Yellow
        Write-Host "     cuenta de equipo) tenga lectura y escritura ahi." -ForegroundColor Yellow
    } else {
        Write-Host "     Comprobado: la carpeta existe y acepta escritura." -ForegroundColor Green
    }
    $nuevas = ActualizarVariable -Ruta $ruta -Clave "AlmacenArchivos__Ruta" -Valor $RutaAlmacen
} catch {
    Write-Host ""
    Write-Host "ERROR: no se pudo escribir en el registro ($($_.Exception.Message))." -ForegroundColor Red
    Write-Host "Corre esta consola como Administrador e intenta de nuevo." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "OK. Variables de entorno actuales del servicio '$NombreServicio':" -ForegroundColor Green
foreach ($linea in $nuevas) {
    # La cadena de conexion trae la contrasena del login de SQL, y la clave JWT es un
    # secreto: no se imprimen completas para que no queden en el historial de la consola.
    if ($linea -like "ConnectionStrings__*") {
        Write-Host "  $(($linea -split '=')[0])=(sin mostrar, contiene la contrasena)"
    } elseif ($linea -like "Jwt__ClaveFirma=*") {
        Write-Host "  Jwt__ClaveFirma=(sin mostrar)"
    } else {
        Write-Host "  $linea"
    }
}
Write-Host ""
Write-Host "La clave Jwt__ClaveFirma quedo copiada al portapapeles -- guardala en"
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

Write-Host ""
Write-Host "Comprueba el almacen contra la propia API, con sesion iniciada en GTE:"
Write-Host "  GET /api/v1/version/almacen"
Write-Host "Debe responder sePuedeEscribir: true y la ruta $RutaAlmacen"
