# Manual de instalacion - GTE (Gestor Tecnologico Empresarial)

> Sigue el mismo patron real que ya usa `Interflo.ServiceHealth` en produccion: Kestrel
> corriendo directo como Windows Service, sin IIS, sin reverse proxy, sin Docker, sin
> pipeline de CI/CD. La API sirve tambien la SPA compilada (React) en el mismo proceso
> (topologia minima fase 1 del Documento Maestro, seccion 1.1). Publicacion y despliegue
> son manuales, igual que el resto del ecosistema Interflo.

## 1. Requisitos previos

- Windows Server (o Windows 10/11) donde vaya a quedar corriendo permanentemente, con
  **.NET 8 Runtime** instalado (ASP.NET Core Runtime 8.0, LTS; no hace falta el SDK
  completo en el servidor de destino, solo en la maquina donde se compila/publica).
  Confirmar con `dotnet --list-runtimes` -- .NET no usa una version mayor distinta a la
  que pide el `.exe` aunque haya otras instaladas (pasó con `SRVPROD\NASA`: tenia 8.0 y
  10.0 pero no 9.0, que era lo que pedia el build viejo).
- Acceso de red desde esa maquina hacia el SQL Server donde vive `bdsGTE` (puerto 1433 o
  el que corresponda). GTE es totalmente independiente (ADR-03): no necesita acceso a
  ninguna otra base de datos del ecosistema.
- **El SQL Server destino debe tener habilitado el modo mixto** ("SQL Server and Windows
  Authentication mode"), no solo autenticacion de Windows. GTE se conecta a `bdsGTE` con un
  **login propio de SQL Server** (usuario + contrasena, ver Paso 1.3) -- decision
  deliberada: el sistema no depende de que el servidor de aplicaciones y el SQL Server
  compartan dominio/Active Directory, ni de coordinar una cuenta de Windows con un
  administrador de AD (coherente con que GTE tampoco depende de Entra ID para su propia
  autenticacion de aplicacion). Verificar/cambiar el modo en SSMS: click derecho al
  servidor > Propiedades > Seguridad > "SQL Server and Windows Authentication mode" --
  requiere reiniciar el servicio de SQL Server para que el cambio surta efecto.
- Permisos de administrador en la maquina destino para crear un Windows Service (puede
  correr con la cuenta local por default, `LocalSystem` -- no necesita una cuenta de
  dominio especial, ya que la identidad que importa para conectar a `bdsGTE` es el login
  de SQL Server del Paso 1.3, no la cuenta bajo la que corre el servicio de Windows).
- Si `AlmacenArchivos:Ruta` va a apuntar a un share de red (recomendado en produccion en
  vez de una carpeta local junto al ejecutable), la cuenta bajo la que corre el servicio
  (por default, la cuenta de equipo si se deja `LocalSystem`) necesita permiso de
  lectura/escritura sobre ese share -- si el share exige credenciales especificas, puede
  ser necesario correr el servicio con una cuenta de Windows dedicada solo para eso.

## 2. Paso 1: desplegar el esquema en bdsGTE

Todos los scripts estan en `DataBase/Scripts/`, nomenclatura
`<Secuencia>_<Fecha>_<Categoria>_<Objeto>.sql`, y son idempotentes (correrlos de nuevo
solo imprime `SKIP`). Ejecutar **en orden** contra el servidor de destino (SSMS, Azure
Data Studio, o `sqlcmd -S NOMBRE_SERVIDOR -I -i archivo.sql` -- el flag `-I` fija
`QUOTED_IDENTIFIER ON`, que los indices filtrados del esquema necesitan):

1. La tanda inicial `01` a `10` (crea `bdsGTE` si no existe, catalogos, motor de estatus,
   folios, los ~100 objetos del esquema). Ver `DataBase/Scripts/README.md` para el detalle
   de cada uno.
2. Las tandas siguientes fechadas (`01_2026-07-30_INSERT_...`,
   `01_2026-07-31_INSERT_...`, `01_2026-08-01_SCRIPT_bdsGTE_Autenticacion.sql`, etc.) --
   todo lo que exista con fecha posterior a la tanda inicial.
3. **`01_2026-08-01_SCRIPT_bdsGTE_UsuarioServicio.sql`** (login de SQL Server de minimo
   privilegio con el que la API se conecta a `bdsGTE`). **Antes de correrlo**, editar la
   variable `@Password` en el Bloque 1 con una contrasena real generada para este
   ambiente (guardarla en el gestor de contrasenas del equipo) -- el script se detiene con
   error si se deja el valor de ejemplo. El login se llama `svc_gte` por default (cambiar
   `@NombreLogin` en AMBOS bloques si se necesita otro nombre). Crea el login de SQL
   Server si no existe y le concede en `bdsGTE`: `db_datareader` + `db_datawriter` +
   `EXECUTE` sobre `spCambiarEstatus`, `spGenerarFolio`, `spRegistrarBitacora` y
   `spSnapshotKpi`. Nunca `db_owner`. Nota: es la unica excepcion de la carpeta que toca
   `master` (el login vive ahi por definicion) ademas de `bdsGTE` -- ver el encabezado del
   propio script.
4. Ejecutar `10_2026-07-30_SCRIPT_bdsGTE_Verificacion.sql` (o el mas reciente con
   "Verificacion" en el nombre) para confirmar que los ~100 objetos esperados existen.

## 3. Paso 2: publicar la aplicacion

Desde la maquina de desarrollo (con el SDK de .NET 8 y Node.js instalados), desde la raiz
del repositorio:

```bash
publicar.bat
```

Esto corre `npm run build` del frontend (`frontend/gte-web`), `dotnet publish` de
`GTE.WebApi` y copia el build de React a `wwwroot` del publish, todo en un solo paso.
Publica en `C:\publicado\GTE` por default; para publicar en otra ruta:

```bash
publicar.bat D:\OtraRuta\GTE
```

Copiar la carpeta publicada completa al servidor de destino (por ejemplo a
`C:\Servicios\GTE`).

## 4. Paso 3: configurar y instalar como Windows Service

En el servidor de destino, con una consola como administrador:

```bash
sc create GTE binPath= "C:\Servicios\GTE\GTE.WebApi.exe" start=auto DisplayName= "Interflo GTE"
```

Importante: dejar el espacio despues de `binPath=`, `start=` y `DisplayName=` (sintaxis de
`sc.exe`, falla en silencio si se pega el `=` al valor). Sin `obj=`, el servicio corre
como `LocalSystem` -- no hace falta una cuenta de Windows dedicada, porque la identidad
que usa la API para conectar a `bdsGTE` es el login de SQL Server del Paso 1.3, no la
cuenta del servicio de Windows.

Definir las variables de entorno del servicio -- **todo lo sensible va aqui, nunca en
`appsettings.json`**. Copiar las herramientas del repositorio al servidor (junto con lo
publicado, o aparte) y correr como Administrador.

**Opcion recomendada -- las tres variables de una vez, con reinicio del servicio
incluido** (`configurar-servicio-completo.bat`/`.ps1`):

```bash
configurar-servicio-completo.bat NOMBRE_SERVIDOR_SQL LA-CONTRASENA-DEL-PASO-1.3
```

Registra `ASPNETCORE_ENVIRONMENT=Production`, arma y registra
`ConnectionStrings__bdsGTE` (con usuario `svc_gte` por default), genera y registra una
`Jwt__ClaveFirma` nueva (y la copia al portapapeles), y reinicia el servicio al final --
avisa si no quedo en estado `Running`. Nombre de servicio y usuario de SQL son
parametros opcionales (`configurar-servicio-completo.bat SERVIDOR PASSWORD
[NOMBRE_SERVICIO] [USUARIO_SQL]`, default `GTE`/`svc_gte`).

**Opcion variable por variable** (`configurar-variable-servicio.bat`/`.ps1` +
`generar-clave-jwt.bat`/`.ps1`), util para tocar solo una sin reiniciar el servicio o sin
regenerar la clave JWT:

```bash
configurar-variable-servicio.bat GTE ASPNETCORE_ENVIRONMENT Production
configurar-variable-servicio.bat GTE ConnectionStrings__bdsGTE "Server=NOMBRE_SERVIDOR_SQL;Database=bdsGTE;User Id=svc_gte;Password=LA-CONTRASENA-DEL-PASO-1.3;TrustServerCertificate=True;Application Name=GTE.WebApi"
generar-clave-jwt.bat GTE
```

Cada uno agrega/reemplaza SOLO su propia variable -- lee lo que ya haya en el registro
del servicio y conserva el resto (no hay que preocuparse por el orden, ni por pisar lo
que ya se puso). Con esta opcion, reiniciar el servicio a mano despues (`sc stop GTE` /
`sc start GTE`).

**Cuidado si corres cualquiera de estas herramientas desde una consola de PowerShell**
(no `CMD`): si algun valor tiene un `$` (tipico en contrasenas), usa comillas SIMPLES en
PowerShell, no dobles -- PowerShell interpola `$algo` como variable dentro de comillas
dobles y lo puede dejar vacio SIN ningun error visible. Desde `CMD` (`cmd.exe` clasico)
esto no aplica.

Notas sobre estas variables:

- `ASPNETCORE_ENVIRONMENT=Production`: obligatoria -- sin ella, la API no toma el puerto
  de `appsettings.Production.json` y ademas exige `Jwt:ClaveFirma` (ver siguiente punto),
  asi que un olvido se nota de inmediato al arrancar, no queda "abierta" por accidente.
- `Jwt__ClaveFirma`: **obligatoria** fuera de Development (32+ caracteres). La API no
  arranca sin ella. `generar-clave-jwt.bat` la genera aleatoria, la copia al portapapeles
  (guardarla tambien en el gestor de contrasenas del equipo) y la escribe en el servicio --
  generar una clave **distinta por cada ambiente**, nunca reusar la misma.
- `ConnectionStrings__bdsGTE`: `User Id`/`Password` son el login de SQL Server
  `svc_gte` creado en el Paso 1.3 (autenticacion de SQL Server, no de Windows -- ver
  requisito del modo mixto en la seccion 1). Usar la misma contrasena que se puso en el
  script de ese paso, guardada en el gestor de contrasenas del equipo.
- Si `AlmacenArchivos:Ruta` va a ser un share de red, agregar tambien
  `AlmacenArchivos__Ruta=\\servidor\GTE\Archivos` (o la ruta real) a la misma lista del
  primer `reg add`.
- Si por alguna razon el SPA necesitara llamar a la API desde OTRO origen (no deberia,
  quedan en el mismo proceso/puerto), se agregaria `Cors__Origenes__0=https://...`.

Revisar tambien `appsettings.Production.json` (puerto donde va a escuchar Kestrel):

```json
{ "Kestrel": { "Endpoints": { "Http": { "Url": "http://0.0.0.0:5090" } } } }
```

Cambiar `5090` si ese puerto ya esta en uso en el servidor destino.

Iniciar el servicio:

```bash
sc start GTE
```

Verificar que quedo corriendo (`STATE : 4 RUNNING`):

```bash
sc query GTE
```

## 5. Paso 4: firewall y verificacion

1. Si el servidor tiene firewall de Windows activo, abrir el puerto configurado (5090 por
   default):
   ```bash
   netsh advfirewall firewall add rule name="Interflo GTE" dir=in action=allow protocol=TCP localport=5090
   ```
2. Desde un navegador (en el servidor o en la red interna), entrar a
   `http://NOMBRE_SERVIDOR:5090/health`. Debe responder
   `{"estado":"ok","fecha":"..."}`.
3. Entrar a `http://NOMBRE_SERVIDOR:5090/`. Debe verse la pantalla de login de GTE (el
   shell de la SPA se sirve sin sesion a proposito -- es lo unico que un usuario
   anonimo puede ver; todo lo demas exige token).
4. Revisar el Visor de Eventos de Windows (Application) o `logs/gte-*.log` junto al
   ejecutable si no arranca -- los errores de conexion a `bdsGTE` (login sin permisos,
   firewall de SQL, `ConnectionStrings__bdsGTE` mal escrita) quedan ahi.

### Primer login en un ambiente nuevo (bootstrap conocido, ver PENDIENTES.md 3.4)

En un ambiente de produccion real, el atajo de desarrollo sin contrasena esta deshabilitado
(`Jwt:Desarrollo:Habilitado` solo es `true` en `appsettings.Development.json`), y si la BD
es nueva (sin la migracion del GT todavia, B3) **`tblUsuario` nace completamente vacia** --
no hay ni siquiera un Administrador para entrar. Esto es una limitacion conocida y
deliberadamente fuera de alcance de esta entrega (no hay flujo de "olvide mi contrasena"
por correo ni bootstrap de UI para el primer Administrador). Dos formas de resolverlo:

**Opcion A -- crear el primer Administrador a mano (probada en `SRVPROD\NASA`):**

1. Confirmar que ya se corrio `01_2026-08-01_SCRIPT_bdsGTE_Autenticacion.sql` contra esa
   `bdsGTE` (agrega `PasswordHash`/`RequiereCambioPassword` a `tblUsuario`) -- si no, el
   `INSERT` de abajo falla con "Invalid column name".
2. Generar un hash BCrypt real con la MISMA version de `BCrypt.Net-Next` que usa la API
   (4.0.3) -- por ejemplo con un proyecto de consola desechable:
   ```bash
   dotnet new console -o hash-temporal
   cd hash-temporal
   dotnet add package BCrypt.Net-Next --version 4.0.3
   ```
   Y en `Program.cs`:
   ```csharp
   var password = "una-contrasena-temporal-fuerte";
   Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(password));
   ```
   `dotnet run`, copiar el hash impreso, y borrar la carpeta `hash-temporal` (nunca
   commitear un hash real, ni siquiera de una contrasena temporal).
3. Correr contra `bdsGTE` (idempotente, no truena si ya existe):
   ```sql
   IF NOT EXISTS (SELECT 1 FROM dbo.tblUsuario WHERE Dominio = N'NOMBRE_USUARIO')
   BEGIN
       DECLARE @IdUsuario INT;

       INSERT INTO dbo.tblUsuario (Dominio, Nombre, Activo, FechaRegistro, UsuarioRegistro, PasswordHash, RequiereCambioPassword)
       VALUES (N'NOMBRE_USUARIO', N'NOMBRE_USUARIO', 1, SYSDATETIME(), N'bootstrap-manual', N'EL_HASH_GENERADO', 1);

       SET @IdUsuario = SCOPE_IDENTITY();

       INSERT INTO dbo.tblUsuarioRol (IdUsuario, IdRol, FechaRegistro, UsuarioRegistro, Activo)
       SELECT @IdUsuario, IdRol, SYSDATETIME(), N'bootstrap-manual', 1
       FROM dbo.tblRol
       WHERE Nombre = N'Administrador';

       PRINT 'OK: usuario creado con IdUsuario = ' + CAST(@IdUsuario AS NVARCHAR(10));
   END
   ELSE
       PRINT 'SKIP: el usuario ya existe';
   ```
4. Entrar a `/` con `NOMBRE_USUARIO` + la contrasena temporal -- `RequiereCambioPassword = 1`
   fuerza a ponerle una contrasena real de inmediato (confirmado que este flujo funciona).

**Opcion B -- arrancar ese primer login en Development**: correr la API localmente contra
la BD real (con cuidado, solo para este paso puntual) con
`Jwt:Desarrollo:Habilitado=true`, entrar con el atajo, usar "Restablecer contrasena" en
Administracion > Usuarios para poner una contrasena real, y despues operar siempre en
Production. Mas simple si ya existe el usuario (solo le falta contrasena); si
`tblUsuario` esta vacia del todo, la Opcion A es mas directa porque tambien asigna el rol
de Administrador en el mismo paso.

## 6. Actualizar una version ya instalada

```bash
sc stop GTE
```

**Borrar el CONTENIDO COMPLETO** de la carpeta instalada (ej. `C:\Servicios\GTE`) antes de
copiar el nuevo publish -- no solo sobreescribir. `dotnet publish` no elimina archivos que
la version nueva ya no necesita, asi que dejar restos de una publicacion anterior puede
mezclar ensamblados de dos versiones distintas (paso justo lo que rompio la primera
instalacion real: quedaron archivos apuntando a una version de .NET distinta a la
instalada, `FileNotFoundException` de `System.Runtime`). No debería haber ningún
`appsettings.*.Local.json` especifico de ese servidor que preservar -- los secretos se
manejan por variable de entorno, como se documenta en el Paso 3.

```bash
sc start GTE
```

Si el nuevo despliegue incluye un script SQL nuevo (cambios de esquema), correrlo contra
`bdsGTE` **antes** de arrancar el servicio actualizado.

## 7. Desinstalar

```bash
sc stop GTE
sc delete GTE
```

Esto no borra los datos de `bdsGTE` ni los archivos publicados -- borrar ambos a mano si
en verdad ya no se va a usar mas GTE. El login/usuario de servicio (Paso 1.3) tampoco se
elimina automaticamente.

## 8. Solucion de problemas comunes

| Sintoma | Causa probable | Que revisar |
|---|---|---|
| `sc start GTE` falla con **ERROR 1053** ("El servicio no respondio a tiempo...") | El `.exe` publicado no tiene el paquete `Microsoft.Extensions.Hosting.WindowsServices` / `UseWindowsService()` en `Program.cs` -- sin esto, el proceso nunca le avisa al Service Control Manager que ya quedo `RUNNING`, sin importar que la API arranque bien. Ya viene resuelto en el código (ver `Program.cs`); si aparece este error es porque se publicó una versión vieja | Volver a publicar con `publicar.bat` (versión actual del repo) y reinstalar el servicio |
| El servicio no arranca (`sc query` no llega a RUNNING) | Ruta del `.exe` incorrecta en `binPath=`, o falta el .NET 8 Runtime en el servidor | Visor de Eventos > Application; confirmar `dotnet --list-runtimes` incluye `Microsoft.AspNetCore.App 8.x` |
| El Visor de Eventos dice "You must install or update .NET to run this application" / pide `Microsoft.AspNetCore.App` version 'X.0.0' | Falta exactamente esa version mayor en el servidor -- .NET no hace fallback a otra version mayor aunque este instalada | Instalar el ASP.NET Core Runtime 8.0 (x64) en el servidor, o confirmar que se publico con el `.csproj` actual (`net8.0`) y no una copia vieja |
| El proceso truena con `FileNotFoundException: Could not load file or assembly 'System.Runtime, Version=X.0.0.0...'` (version DISTINTA a la del runtime instalado, ej. corre bajo 8.0.29 pero pide 9.0.0.0) | Quedaron archivos de un publish VIEJO (de otra version de .NET) mezclados con el nuevo en la misma carpeta -- `dotnet publish -o carpeta` no borra lo que ya no necesita, solo agrega/sobreescribe | `publicar.bat` ya limpia su carpeta de destino local antes de publicar (paso 1/4). **Falta limpiar la carpeta INSTALADA en el servidor** (ej. `C:\Program Files (x86)\Interflo\ServiceGTE\`): `sc stop GTE`, borrar el contenido completo de esa carpeta, copiar de nuevo el publish fresco completo, `sc start GTE` |
| Error de login "Cannot open server ... requested by the login" o "Login failed for user 'svc_gte'" | El SQL Server destino no tiene habilitado el modo mixto (solo Windows Authentication), o no se corrio el script `01_..._UsuarioServicio.sql`, o la contrasena en `ConnectionStrings__bdsGTE` no coincide con la del script | Server Properties > Security en SSMS (modo mixto); confirmar que el login `svc_gte` existe en `sys.server_principals` y tiene usuario en `bdsGTE` (`sys.database_principals`) |
| El servicio arranca pero se detiene solo / error de conexion a BD | `ConnectionStrings__bdsGTE` mal escrita, firewall de SQL Server (puerto 1433), o permisos insuficientes del login `svc_gte` | `logs/gte-*.log` junto al ejecutable; probar la misma cadena de conexion con `sqlcmd -S servidor -d bdsGTE -U svc_gte -P la-contrasena` |
| Falla al arrancar con "Falta Jwt:ClaveFirma" | No se configuro `Jwt__ClaveFirma` en las variables de entorno del servicio (Paso 3) | `reg query "HKLM\SYSTEM\CurrentControlSet\Services\GTE" /v Environment` |
| El puerto configurado no responde desde otras maquinas | Firewall de Windows bloqueando, o Kestrel escuchando solo en localhost | Revisar `appsettings.Production.json` (debe ser `0.0.0.0`, no `127.0.0.1`) y la regla de firewall del Paso 5 |
| La pantalla de login carga pero un archivo `.js`/`.css` da 404 | El `publicar.bat` no llego a copiar `dist` a `wwwroot`, o se publico sin correr antes `npm run build` | Confirmar que `wwwroot/assets` tiene archivos en el servidor; volver a correr `publicar.bat` completo |
| Nadie puede iniciar sesion en un ambiente recien instalado | Bootstrap de la primera contrasena, ver seccion 5 de este manual | `tblUsuario.PasswordHash` de la cuenta de Administrador sigue en `NULL` |
| `GRANT EXECUTE` u otro paso del script `01_..._UsuarioServicio.sql` falla con "ya existe" en una corrida limpia | Normal si se corre dos veces -- el script es idempotente, la segunda vez todo es `SKIP` | Revisar la salida completa, no solo la ultima linea |
