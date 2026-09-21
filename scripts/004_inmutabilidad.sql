-- 004 · Inmutabilidad garantizada por la base de datos (H-01, ALCOA+).
--
-- La aplicación nunca modifica ni borra versiones, pero no basta con que la aplicación
-- se porte bien: estos triggers impiden reescribir la historia también desde un script
-- manual. Desactivarlos exige ALTER sobre la tabla, un permiso que el usuario de la
-- aplicación no debe tener en producción (lo mismo vale para TRUNCATE, que no dispara triggers).
--
-- CREATE TRIGGER tiene que ser la primera instrucción de su lote; por eso cada uno va
-- separado por GO. XACT_ABORT y la transacción abierta hacen el script atómico: si falla
-- un lote, sqlcmd (-b) corta la ejecución y la transacción se revierte al cerrar la conexión.

SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRANSACTION;
GO

-- ContactoVersion solo admite inserciones.
CREATE TRIGGER dbo.TR_ContactoVersion_SoloInsercion
ON dbo.ContactoVersion
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51001, N'ContactoVersion es de solo inserción: las versiones de un contacto no se modifican ni se borran.', 1;
END;
GO

-- Un contacto no se borra: se corrige con una versión nueva.
CREATE TRIGGER dbo.TR_Contacto_SinBorrado
ON dbo.Contacto
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51002, N'Los contactos no se borran: se corrigen creando una versión nueva.', 1;
END;
GO

-- El paciente y el autor de un contacto no cambian con una corrección.
CREATE TRIGGER dbo.TR_Contacto_PacienteYAutorInmutables
ON dbo.Contacto
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted AS i
        JOIN deleted AS d ON d.Id = i.Id
        WHERE i.PacienteId <> d.PacienteId OR i.GestorId <> d.GestorId
    )
        THROW 51003, N'El paciente y el autor de un contacto no se pueden cambiar.', 1;
END;
GO

INSERT INTO dbo.VersionEsquema (Script) VALUES ('004_inmutabilidad.sql');

COMMIT TRANSACTION;
GO
