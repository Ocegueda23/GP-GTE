USE bdsGTE; 
GO
IF NOT EXISTS (SELECT 1 FROM dbo.tblUsuario WHERE Dominio = N'Administrador')
BEGIN
    DECLARE @IdUsuario INT;
-- password: YouCanDoIt.2026
    INSERT INTO dbo.tblUsuario (Dominio, Nombre, Activo, FechaRegistro, UsuarioRegistro, PasswordHash, RequiereCambioPassword)
    VALUES (N'Administrador', N'Administrador', 1, SYSDATETIME(), N'bootstrap-manual', N'$2a$11$9n7CIpHbdKd6FcHBlX3gquevz1q88g8.p3sJSnKFu2i8EslvLgu/6', 1);

    SET @IdUsuario = SCOPE_IDENTITY();

    INSERT INTO dbo.tblUsuarioRol (IdUsuario, IdRol, FechaRegistro, UsuarioRegistro, Activo)
    SELECT @IdUsuario, IdRol, SYSDATETIME(), N'bootstrap-manual', 1
    FROM dbo.tblRol
    WHERE Nombre = N'Administrador';

    PRINT 'OK: usuario Administrador creado con IdUsuario = ' + CAST(@IdUsuario AS NVARCHAR(10));
END
ELSE
    PRINT 'SKIP: el usuario Administrador ya existe';