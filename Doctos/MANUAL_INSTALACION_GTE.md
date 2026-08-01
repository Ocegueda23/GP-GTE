# Manual de instalacion - GTE (Gestor Tecnologico Empresarial)

> Sigue el mismo patron real que ya usa `Interflo.ServiceHealth` en produccion: Kestrel
> corriendo directo como Windows Service, sin IIS, sin reverse proxy, sin Docker, sin
> pipeline de CI/CD. La API sirve tambien la SPA compilada (React) en el mismo proceso
> (topologia minima fase 1 del Documento Maestro, seccion 1.1). Publicacion y despliegue
> son manuales, igual que el resto del ecosistema Interflo.

## 1. Requisitos previos

- Windows Server (o Windows 10/11) donde vaya a quedar corriendo permanentemente, con
  **.NET 9 Runtime** instalado (ASP.NET Core Runtime 9.0; no hace falta el SDK completo en
  el servidor de destino, solo en la maquina donde se compila/publica).
- Acceso de red desde esa maquina hacia el SQL Server donde vive `bdsGTE` (puerto 1433 o
  el que corresponda). GTE es totalmente independiente (ADR-03): no necesita acceso a
  ninguna otra base de datos del ecosistema.
- **Una cuenta de servicio de Windows dedicada** (de dominio, ej. `INTERFLO\svc.gte`, o
  local al servidor si no hay dominio, ej. `SERVIDOR\svc.gte`) bajo la cual va a correr el
  Windows Service. GTE usa `Trusted_Connection=True` (autenticacion integrada de Windows)
  para conectar a `bdsGTE` -- **la identidad que importa es la del propio proceso**, no un
  usuario/password dentro de la cadena de conexion. Solicitar esta cuenta a quien
  administre el dominio/el servidor si todavia no existe.
- Permisos de administrador en la maquina destino para crear un Windows Service y, si se
  usa una cuenta de dominio nueva, coordinar con el administrador de AD para que tenga el
  derecho "Iniciar sesion como servicio" en ese servidor (normalmente se otorga solo al
  crear el servicio con `sc create ... obj= ... password= ...`, ver Paso 4).
- Si `AlmacenArchivos:Ruta` va a apuntar a un share de red (recomendado en produccion en
  vez de una carpeta local junto al ejecutable), la cuenta de servicio necesita permiso de
  lectura/escritura sobre ese share.

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
3. **`01_2026-08-01_SCRIPT_bdsGTE_UsuarioServicio.sql`** (usuario de BD de minimo
   privilegio para la cuenta de servicio del Paso 1). **Antes de correrlo**, editar la
   variable `@NombreLogin` en los DOS bloques del script (login a nivel servidor +
   usuario/permisos en bdsGTE) con el nombre real de la cuenta de servicio (formato
   `DOMINIO\cuenta` o `SERVIDOR\cuenta`). Crea el login de Windows si no existe y le
   concede en `bdsGTE`: `db_datareader` + `db_datawriter` + `EXECUTE` sobre
   `spCambiarEstatus`, `spGenerarFolio`, `spRegistrarBitacora` y `spSnapshotKpi`. Nunca
   `db_owner`. Nota: es la unica excepcion de la carpeta que toca `master` (el login vive
   ahi por definicion) ademas de `bdsGTE` -- ver el encabezado del propio script.
4. Ejecutar `10_2026-07-30_SCRIPT_bdsGTE_Verificacion.sql` (o el mas reciente con
   "Verificacion" en el nombre) para confirmar que los ~100 objetos esperados existen.

## 3. Paso 2: publicar la aplicacion

Desde la maquina de desarrollo (con el SDK de .NET 9 y Node.js instalados), desde la raiz
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
sc create GTE binPath= "C:\Servicios\GTE\GTE.WebApi.exe" start=auto DisplayName= "Interflo GTE" obj= "DOMINIO\svc.gte" password= "la-contrasena-real"
```

Importante: dejar el espacio despues de `binPath=`, `start=`, `DisplayName=`, `obj=` y
`password=` (sintaxis de `sc.exe`, falla en silencio si se pega el `=` al valor). Si el
servidor no tiene dominio, usar `.\svc.gte` como cuenta local.

Definir las variables de entorno del servicio -- **todo lo sensible va aqui, nunca en
`appsettings.json`** (`REG_MULTI_SZ` acepta varias lineas, una variable por linea; `\0`
separa cada linea dentro del mismo valor al usar `reg add` desde una sola linea de
comando):

```bash
reg add "HKLM\SYSTEM\CurrentControlSet\Services\GTE" /v Environment /t REG_MULTI_SZ /d "ASPNETCORE_ENVIRONMENT=Production\0ConnectionStrings__bdsGTE=Server=NOMBRE_SERVIDOR_SQL;Database=bdsGTE;Trusted_Connection=True;TrustServerCertificate=True;Application Name=GTE.WebApi\0Jwt__ClaveFirma=LA-CLAVE-REAL-DE-AL-MENOS-32-CARACTERES" /f
```

Notas sobre estas variables:

- `ASPNETCORE_ENVIRONMENT=Production`: obligatoria -- sin ella, la API no toma el puerto
  de `appsettings.Production.json` y ademas exige `Jwt:ClaveFirma` (ver siguiente punto),
  asi que un olvido se nota de inmediato al arrancar, no queda "abierta" por accidente.
- `Jwt__ClaveFirma`: **obligatoria** fuera de Development (32+ caracteres). La API no
  arranca sin ella -- generar una clave real (no reusar la de otro ambiente) y guardarla
  en el gestor de contrasenas del equipo, no solo en el registro de este servidor.
- `ConnectionStrings__bdsGTE`: mismo formato que en desarrollo pero apuntando al SQL
  Server real. `Trusted_Connection=True` funciona porque el servicio corre como la cuenta
  del Paso 1, que ya tiene permisos minimos en `bdsGTE` (Paso 1.3 de este manual).
- Si `AlmacenArchivos:Ruta` va a ser un share de red, agregar tambien
  `AlmacenArchivos__Ruta=\\servidor\GTE\Archivos` (o la ruta real) a la misma lista.
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
(`Jwt:Desarrollo:Habilitado` solo es `true` en `appsettings.Development.json`) y los
usuarios migrados nacen con `PasswordHash = NULL` -- nadie puede entrar todavia por
`/auth/login`. Esto es una limitacion conocida y deliberadamente fuera de alcance de esta
entrega (no hay flujo de "olvide mi contrasena" por correo ni bootstrap de UI para el
primer Administrador). Dos formas de resolverlo la primera vez:

- **UPDATE directo a la BD**: generar un hash BCrypt real (por ejemplo con
  `dotnet fsi`/un script corto usando `BCrypt.Net-Next`, el mismo paquete que usa la API) y
  escribirlo a mano en `tblUsuario.PasswordHash` para la cuenta de Administrador, junto con
  `RequiereCambioPassword = 1` para forzar que lo cambie al entrar.
- **Arrancar ese primer login en Development**: correr la API localmente contra la BD de
  produccion (con cuidado, solo para este paso puntual) con
  `Jwt:Desarrollo:Habilitado=true`, entrar con el atajo, usar "Restablecer contrasena" en
  Administracion > Usuarios para poner una contrasena real, y despues operar siempre en
  Production.

## 6. Actualizar una version ya instalada

```bash
sc stop GTE
```

Reemplazar los archivos en `C:\Servicios\GTE` con el nuevo `publicar.bat` (cuidando no
pisar ningun `appsettings.*.Local.json` especifico de ese servidor que no este en el
repo -- no deberia existir ninguno si los secretos se manejan por variable de entorno,
como se documenta en el Paso 3).

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
| El servicio no arranca (`sc query` no llega a RUNNING) | Cuenta de servicio sin el derecho "Iniciar sesion como servicio", o password incorrecto en `sc create` | Visor de Eventos > Application (busca "Logon failure"); recrear el servicio con `sc delete` + `sc create` verificando el password |
| El servicio arranca pero se detiene solo / error de conexion a BD | `ConnectionStrings__bdsGTE` mal escrita, firewall de SQL, o la cuenta de servicio no corrio el script `01_..._UsuarioServicio.sql` | `logs/gte-*.log` junto al ejecutable; confirmar que el login de la cuenta existe en `sys.server_principals` y tiene usuario en `bdsGTE` |
| Falla al arrancar con "Falta Jwt:ClaveFirma" | No se configuro `Jwt__ClaveFirma` en las variables de entorno del servicio (Paso 3) | `reg query "HKLM\SYSTEM\CurrentControlSet\Services\GTE" /v Environment` |
| El puerto configurado no responde desde otras maquinas | Firewall de Windows bloqueando, o Kestrel escuchando solo en localhost | Revisar `appsettings.Production.json` (debe ser `0.0.0.0`, no `127.0.0.1`) y la regla de firewall del Paso 5 |
| La pantalla de login carga pero un archivo `.js`/`.css` da 404 | El `publicar.bat` no llego a copiar `dist` a `wwwroot`, o se publico sin correr antes `npm run build` | Confirmar que `wwwroot/assets` tiene archivos en el servidor; volver a correr `publicar.bat` completo |
| Nadie puede iniciar sesion en un ambiente recien instalado | Bootstrap de la primera contrasena, ver seccion 5 de este manual | `tblUsuario.PasswordHash` de la cuenta de Administrador sigue en `NULL` |
| `GRANT EXECUTE` u otro paso del script `01_..._UsuarioServicio.sql` falla con "ya existe" en una corrida limpia | Normal si se corre dos veces -- el script es idempotente, la segunda vez todo es `SKIP` | Revisar la salida completa, no solo la ultima linea |
