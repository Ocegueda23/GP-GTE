param(
    [Parameter(Mandatory = $true)][string]$Ruta,
    [string]$NombreServicio = "GTE",
    [switch]$NoReiniciar
)

# Fija AlmacenArchivos__Ruta en el servicio de GTE, crea la carpeta, comprueba que se puede
# escribir ahi y reinicia el servicio.
#
# Existe porque el valor que trae appsettings.json es D:\GTE\Archivos, la ruta de la maquina
# de desarrollo: si el servidor no tiene esa unidad, TODA subida de archivos falla con
# INTERNAL_ERROR (imagenes pegadas en descripciones y comentarios, adjuntos de WorkItem,
# base de conocimiento, fotos de perfil). Sin la variable de entorno, ese valor gana.
#
# El registro de la variable es el mismo mecanismo de configurar-variable-servicio.ps1
# (conserva las demas variables); lo que agrega este script es la carpeta, la sonda de
# escritura y el reinicio, que son los pasos que se olvidaban.

$ErrorActionPreference = "Stop"

$rutaRegistro = "HKLM:\SYSTEM\CurrentControlSet\Services\$NombreServicio"

Write-Host "==================================================="
Write-Host "  Almacen de archivos del servicio '$NombreServicio'"
Write-Host "  Ruta: $Ruta"
Write-Host "==================================================="
Write-Host ""

$identidad = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $identidad.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: esta consola no es de Administrador." -ForegroundColor Red
    Write-Host "Cierrala y abre PowerShell con 'Ejecutar como administrador'." -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $rutaRegistro)) {
    Write-Host "ERROR: no existe el servicio '$NombreServicio' en esta maquina." -ForegroundColor Red
    Write-Host "Crealo primero con 'sc create' (paso 3 del manual de instalacion)." -ForegroundColor Red
    exit 1
}

# ---------------------------------------------------------------- 1/3: carpeta y escritura
Write-Host "1/3: comprobando que se puede escribir en la ruta"

$esShareDeRed = $Ruta -like "\\*"

if ($esShareDeRed) {
    # La sonda se corre con TU cuenta, no con la del servicio (LocalSystem por defecto, que
    # en la red se presenta como la cuenta de equipo DOMINIO\NOMBREEQUIPO$). Que tu escribas
    # en el share no garantiza que el servicio pueda: por eso aqui solo se avisa.
    Write-Host "     Es un share de red: no se puede comprobar por ti." -ForegroundColor Yellow
    Write-Host "     La sonda correria con TU cuenta, no con la del servicio." -ForegroundColor Yellow
    Write-Host "     Confirma que la cuenta bajo la que corre '$NombreServicio' (por defecto" -ForegroundColor Yellow
    Write-Host "     LocalSystem, que en la red es la cuenta de equipo) tenga lectura y" -ForegroundColor Yellow
    Write-Host "     escritura sobre $Ruta." -ForegroundColor Yellow
} else {
    try {
        if (-not (Test-Path $Ruta)) {
            New-Item -ItemType Directory -Path $Ruta -Force -ErrorAction Stop | Out-Null
            Write-Host "     Carpeta creada." -ForegroundColor Green
        } else {
            Write-Host "     La carpeta ya existia." -ForegroundColor DarkGray
        }

        # [IO.Path]::Combine y no Join-Path: Join-Path valida la unidad y tapa el error real
        # ("no existe la unidad Z") con un "la ruta no puede ser nula" que no dice nada.
        $sonda = [IO.Path]::Combine($Ruta, ".sonda-" + [Guid]::NewGuid().ToString("N"))
        [IO.File]::WriteAllBytes($sonda, [byte[]]@(0))
        Remove-Item $sonda -Force
        Write-Host "     OK: la ruta acepta escritura." -ForegroundColor Green
    } catch {
        Write-Host ""
        Write-Host "ERROR: no se pudo escribir en $Ruta" -ForegroundColor Red
        Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        Write-Host "No se cambio nada. Corrige la ruta o los permisos y vuelve a correr esto:" -ForegroundColor Red
        Write-Host "  - Si la unidad no existe en este servidor, usa otra (ej. C:\GTE\Archivos)." -ForegroundColor Red
        Write-Host "  - Si es de permisos, da control total a la cuenta del servicio sobre esa carpeta." -ForegroundColor Red
        exit 1
    }
}
Write-Host ""

# ---------------------------------------------------------------- 2/3: variable de entorno
Write-Host "2/3: registrando AlmacenArchivos__Ruta en el servicio"

try {
    $actuales = @()
    try {
        $actuales = (Get-ItemProperty -Path $rutaRegistro -Name Environment -ErrorAction Stop).Environment
    } catch {
        $actuales = @()
    }

    $anterior = @($actuales | Where-Object { $_ -like "AlmacenArchivos__Ruta=*" })

    # Conserva cualquier otra variable ya puesta -- solo reemplaza esta linea.
    $sinClave = @($actuales | Where-Object { $_ -notlike "AlmacenArchivos__Ruta=*" })
    $nuevas = $sinClave + "AlmacenArchivos__Ruta=$Ruta"
    New-ItemProperty -Path $rutaRegistro -Name Environment -PropertyType MultiString `
        -Value $nuevas -Force -ErrorAction Stop | Out-Null
} catch {
    Write-Host ""
    Write-Host "ERROR: no se pudo escribir en el registro ($($_.Exception.Message))." -ForegroundColor Red
    exit 1
}

if ($anterior.Count -gt 0) {
    Write-Host "     Valor anterior: $($anterior[0])" -ForegroundColor DarkGray
} else {
    Write-Host "     No estaba puesta: el servicio venia usando la ruta de appsettings.json." -ForegroundColor DarkGray
}

Write-Host ""
Write-Host "     Variables de entorno del servicio '$NombreServicio':" -ForegroundColor Green
foreach ($linea in $nuevas) {
    # La cadena de conexion trae la contrasena del login de SQL: no se imprime completa.
    if ($linea -like "ConnectionStrings__*") {
        Write-Host "       $(($linea -split '=')[0])=(sin mostrar, contiene la contrasena)"
    } elseif ($linea -like "Jwt__ClaveFirma=*") {
        Write-Host "       Jwt__ClaveFirma=(sin mostrar)"
    } else {
        Write-Host "       $linea"
    }
}
Write-Host ""

# ---------------------------------------------------------------- 3/3: reinicio
if ($NoReiniciar) {
    Write-Host "3/3: reinicio omitido (-NoReiniciar)." -ForegroundColor Yellow
    Write-Host "     El cambio NO aplica hasta que reinicies:"
    Write-Host "       sc stop $NombreServicio"
    Write-Host "       sc start $NombreServicio"
    exit 0
}

Write-Host "3/3: reiniciando el servicio para que tome la ruta nueva"
try {
    Restart-Service -Name $NombreServicio -Force -ErrorAction Stop
    Start-Sleep -Seconds 3
    $estado = (Get-Service -Name $NombreServicio).Status
    if ($estado -ne "Running") {
        Write-Host "AVISO: el servicio quedo en estado '$estado' (no 'Running')." -ForegroundColor Yellow
        Write-Host "Revisa el Visor de Eventos (Application) y los logs del servicio." -ForegroundColor Yellow
        exit 1
    }
    Write-Host "     OK: el servicio quedo '$estado'." -ForegroundColor Green
} catch {
    Write-Host "ERROR al reiniciar: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Reinicialo a mano: sc stop $NombreServicio  /  sc start $NombreServicio" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "==================================================="
Write-Host "  Listo"
Write-Host "==================================================="
Write-Host ""
Write-Host "Compruebalo contra la propia API, con sesion iniciada en GTE:"
Write-Host "  GET /api/v1/version/almacen"
Write-Host "Debe responder sePuedeEscribir: true y la ruta $Ruta"
Write-Host ""
Write-Host "El arranque tambien lo deja escrito en el log del servicio, como"
Write-Host "'Almacen de archivos listo en $Ruta' o 'ALMACEN DE ARCHIVOS NO DISPONIBLE'."
Write-Host ""
Write-Host "Nota: los archivos que se subieron ANTES de este cambio siguen en la ruta"
Write-Host "vieja. Si habia adjuntos o imagenes que ahora salen rotos, muevelos a $Ruta"
Write-Host "conservando la subcarpeta de 2 caracteres (ej. 8d\8d1f1f51...)."
