USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      01_2026-09-09_SCRIPT_bdsGTE_CategoriaIncidente.sql
   Autor:       Equipo GTE
   Descripcion: Crea el catalogo dbo.tblCategoriaIncidente y lo engancha
                a dbo.tblIncidente (IdCategoriaIncidente).

                POR QUE UN CATALOGO PROPIO Y NO REUSAR tblCategoriaTicket:
                son dos taxonomias distintas. La de tickets clasifica lo
                que PIDE un usuario (Duda, Acceso, Mejora); la de
                incidentes clasifica lo que SE CAYO o hay que atender en
                operacion (fallo de servidor, enlace de proveedor, VPN,
                respaldos, despliegue de codigo). Mezclarlas ensucia los
                dos combos y rompe los reportes de ambos modulos.

                COLUMNA Nivel: cada categoria nace ubicada en el nivel que
                la atiende -- 'Soporte N1-N2' o 'Desarrollo' -- tal como
                se definio la lista con el equipo. Es texto y NO tiene
                CHECK a proposito: el catalogo se administra desde
                Administracion > Catalogos (motor de catalogo generico) y
                un CHECK impediria dar de alta un nivel nuevo sin script.
                El frontend lo usa para agrupar el combo de Categoria; no
                se muestra como columna en la bandeja.

                IdCategoriaIncidente en tblIncidente queda NULL-able: los
                incidentes ya registrados antes de este script no tienen
                categoria y no se les puede inventar una. Las altas nuevas
                si la exigen, pero eso lo valida el backend
                (CrearIncidenteValidator), no la BD, para que el mensaje
                de error sea el del dominio y no un 547 de SQL Server.

                Sin indice sobre IdCategoriaIncidente: hoy no hay filtro
                ni orden por categoria (mismo criterio que
                tblTicket.IdCategoriaTicket, que tampoco lo tiene). Si
                despues se agrega el filtro a la bandeja, ahi va el indice.
   Idempotente: CREATE/ALTER/FK bajo IF NOT EXISTS y el sembrado inserta
                solo las categorias que falten por Nombre; correrlo de
                nuevo no duplica ni reactiva las que se dieron de baja a
                mano.
   Requiere:    06 de la tanda 01 (tblIncidente).
   ===================================================================== */
BEGIN TRY

    /* ---------------------------------------------------------------
       1. Catalogo. Mismas columnas de auditoria y mismos tipos que
          dbo.tblCategoriaTicket, para que las dos categorias se
          administren igual desde el catalogo generico.
       --------------------------------------------------------------- */
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblCategoriaIncidente')
    BEGIN
        CREATE TABLE dbo.tblCategoriaIncidente
        (
            IdCategoriaIncidente INT           NOT NULL IDENTITY(1,1),
            Nombre               NVARCHAR(200) NOT NULL,
            Nivel                NVARCHAR(50)  NOT NULL,
            FechaRegistro        DATETIME2     NOT NULL CONSTRAINT DF_tblCategoriaIncidente_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro      NVARCHAR(400) NOT NULL,
            UsuarioMovto         NVARCHAR(100) NULL,
            FechaMovto           DATETIME      NULL,
            Activo               BIT           NOT NULL CONSTRAINT DF_tblCategoriaIncidente_Activo DEFAULT (1),
            CONSTRAINT PK_tblCategoriaIncidente PRIMARY KEY (IdCategoriaIncidente),
            CONSTRAINT UQ_tblCategoriaIncidente_Nombre UNIQUE (Nombre)
        );
        PRINT 'OK: tblCategoriaIncidente creada'
    END
    ELSE
        PRINT 'SKIP: tblCategoriaIncidente ya existe'

    /* ---------------------------------------------------------------
       2. Sembrado de la lista acordada con el equipo. Los IDs son
          IDENTITY y NO son contrato: el backend nunca referencia una
          categoria por numero, siempre por lo que eligio el usuario.
          Se inserta por Nombre para poder correr el script de nuevo
          cuando la lista crezca.
       --------------------------------------------------------------- */
    DECLARE @Semilla TABLE (Nombre NVARCHAR(200) NOT NULL PRIMARY KEY, Nivel NVARCHAR(50) NOT NULL)

    INSERT INTO @Semilla (Nombre, Nivel)
    VALUES
        (N'Fallo servidor',                                        N'Soporte N1-N2'),
        (N'Adquisicion Licencias',                                 N'Soporte N1-N2'),
        (N'Baja Empleado',                                         N'Soporte N1-N2'),
        (N'Proceso Baja empleado',                                 N'Soporte N1-N2'),
        (N'Compra de Recursos',                                    N'Soporte N1-N2'),
        (N'Amenaza Detectada',                                     N'Soporte N1-N2'),
        (N'Spam',                                                  N'Soporte N1-N2'),
        (N'Caida Servidor Correo',                                 N'Soporte N1-N2'),
        (N'Falla IP PUBLICA',                                      N'Soporte N1-N2'),
        (N'Asistencia Direccion',                                  N'Soporte N1-N2'),
        (N'Solicitudes Direccion',                                 N'Soporte N1-N2'),
        (N'Solicitudes Generales',                                 N'Soporte N1-N2'),
        (N'Ip Bloqueada',                                          N'Soporte N1-N2'),
        (N'Respaldos',                                             N'Soporte N1-N2'),
        (N'Mantenimiento Servidores',                              N'Soporte N1-N2'),
        (N'Preparacion Equipo Nuevo Ingreso',                      N'Soporte N1-N2'),
        (N'Cambio de equipo de computo',                           N'Soporte N1-N2'),
        (N'Falla Camaras',                                         N'Soporte N1-N2'),
        (N'Instalacion Camaras',                                   N'Soporte N1-N2'),
        (N'Configuracion Camaras',                                 N'Soporte N1-N2'),
        (N'Mantenimiento Equipos de Computo',                      N'Soporte N1-N2'),
        (N'Fallo Respaldos',                                       N'Soporte N1-N2'),
        (N'Problema red Interna',                                  N'Soporte N1-N2'),
        (N'Caida enlace proveedor',                                N'Soporte N1-N2'),
        (N'Falla VPN (General)',                                   N'Soporte N1-N2'),
        (N'Falla Electrica',                                       N'Soporte N1-N2'),
        (N'Cambios Infraestructura',                               N'Soporte N1-N2'),
        (N'Reconfiguracion Redes',                                 N'Soporte N1-N2'),
        (N'Mantenimiento WIFI',                                    N'Soporte N1-N2'),
        (N'Falla General WIFI',                                    N'Soporte N1-N2'),
        (N'Cambio Cableado Red',                                   N'Soporte N1-N2'),
        (N'Instalacion Cableado Red',                              N'Soporte N1-N2'),
        (N'Remplazo de Nodo Red',                                  N'Soporte N1-N2'),
        (N'Depuracion de errores criticos',                        N'Desarrollo'),
        (N'Release',                                               N'Desarrollo'),
        (N'Resolucion de problemas en despliegues de codigo',      N'Desarrollo'),
        (N'Ajustes en esquemas de bases de datos',                 N'Desarrollo'),
        (N'Problemas de integracion de APIs o servicios externos', N'Desarrollo'),
        (N'Mantenimientos Urgentes Sistemas',                      N'Desarrollo'),
        (N'Mantenimientos Programados Sistemas',                   N'Desarrollo'),
        (N'Optimizacion de Rendimiento',                           N'Desarrollo')

    INSERT INTO dbo.tblCategoriaIncidente (Nombre, Nivel, UsuarioRegistro, Activo)
    SELECT s.Nombre, s.Nivel, N'script-despliegue', 1
    FROM @Semilla s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblCategoriaIncidente c WHERE c.Nombre = s.Nombre)
    PRINT 'OK: categorias de incidente sembradas -> ' + CAST(@@ROWCOUNT AS NVARCHAR(10))

    /* ---------------------------------------------------------------
       3. La columna en el incidente.
       --------------------------------------------------------------- */
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblIncidente'
                     AND COLUMN_NAME = 'IdCategoriaIncidente')
    BEGIN
        ALTER TABLE dbo.tblIncidente ADD IdCategoriaIncidente INT NULL
        PRINT 'OK: tblIncidente.IdCategoriaIncidente agregada'
    END
    ELSE
        PRINT 'SKIP: tblIncidente.IdCategoriaIncidente ya existe'

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                   WHERE name = 'FK_tblIncidente_tblCategoriaIncidente')
    BEGIN
        ALTER TABLE dbo.tblIncidente ADD CONSTRAINT FK_tblIncidente_tblCategoriaIncidente
            FOREIGN KEY (IdCategoriaIncidente) REFERENCES dbo.tblCategoriaIncidente (IdCategoriaIncidente)
        PRINT 'OK: FK_tblIncidente_tblCategoriaIncidente creada'
    END
    ELSE
        PRINT 'SKIP: FK_tblIncidente_tblCategoriaIncidente ya existe'

    COMMIT TRANSACTION
    PRINT '===== Script ejecutado correctamente ====='

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION
    PRINT '===== ERROR - Se hizo ROLLBACK ====='
    PRINT 'Mensaje : ' + ERROR_MESSAGE()
    PRINT 'Linea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10))
    PRINT 'Numero  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
END CATCH
GO
