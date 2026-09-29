USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      01_2026-09-28_UPDATE_bdsGTE_JefeUsuariosMigrados.sql
   Autor:       Equipo GTE
   Descripcion: Corrige la jefatura de los 4 usuarios migrados del GT.

                El script 02_Libera/02 (MigracionUsuarios) fija el jefe
                con el literal 'UPDATE dbo.tblUsuario SET IdJefe = 1',
                porque asumia que el IdUsuario 1 era 'aviramontes',
                sembrada por el bootstrap original. Sobre una base
                recien limpiada eso ya no se cumple: el bootstrap
                vigente (01_Libera/11) crea 'Administrador', y esa es la
                cuenta correcta de jefatura. El literal 1 puede entonces
                apuntar a quien no debe, o la migracion dejar IdJefe en
                NULL si se corrio antes que el bootstrap.

                Este script resuelve al jefe POR Dominio, no por Id, asi
                que queda correcto sin importar que IdUsuario le haya
                tocado a 'Administrador'.

                Alcance deliberadamente acotado: solo los 4 Dominios que
                siembra la migracion, y solo cuando el IdJefe esta en
                NULL o conserva la huella del literal 1 mal resuelto. Si
                alguien reasigno la jefatura a mano, NO se pisa -- por eso
                la condicion no es un UPDATE plano: el script tiene que
                poder correrse de nuevo sin deshacer decisiones
                posteriores.
   Requiere:    01_Libera/11 (usuario Administrador) y 02_Libera/02
                (usuarios migrados) aplicados.
   ===================================================================== */
BEGIN TRY

    DECLARE @IdAdministrador INT;
    SELECT @IdAdministrador = IdUsuario
    FROM dbo.tblUsuario
    WHERE Dominio = N'Administrador' AND Activo = 1;

    IF @IdAdministrador IS NULL
    BEGIN
        THROW 50001, 'No existe el usuario Administrador activo: correr antes 01_Libera/11_2026-07-30_Insert_usuarioAdministrador.sql', 1;
    END

    PRINT 'INFO: Administrador resuelto con IdUsuario = ' + CAST(@IdAdministrador AS NVARCHAR(10));

    DECLARE @Migrados TABLE (Dominio NVARCHAR(100));
    INSERT INTO @Migrados (Dominio) VALUES
        (N'Antonio.Ochoa'),
        (N'Roberto.Gonzalez'),
        (N'Roberto.Lopez'),
        (N'Jose.Hernandez');

    DECLARE @Corregidos INT;

    UPDATE u
    SET u.IdJefe = @IdAdministrador
    FROM dbo.tblUsuario u
    JOIN @Migrados m ON m.Dominio = u.Dominio
    WHERE u.IdUsuario <> @IdAdministrador
      AND (u.IdJefe IS NULL
           OR (u.IdJefe = 1 AND @IdAdministrador <> 1));
    -- @@ROWCOUNT se captura de inmediato: el IF de abajo lo sobrescribe.
    SET @Corregidos = @@ROWCOUNT;

    IF @Corregidos = 0
        PRINT 'SKIP: ningun usuario migrado requeria correccion de jefatura';
    ELSE
        PRINT 'OK: jefatura corregida a Administrador (' + CAST(@Corregidos AS NVARCHAR(10)) + ' filas)';

    DECLARE @Faltantes INT;
    SELECT @Faltantes = COUNT(*)
    FROM @Migrados m
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblUsuario u WHERE u.Dominio = m.Dominio);

    IF @Faltantes > 0
        PRINT 'AVISO: ' + CAST(@Faltantes AS NVARCHAR(10)) + ' de los 4 usuarios migrados no existen todavia; correr 02_Libera/02 y despues este script otra vez';

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
