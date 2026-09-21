-- 005 · Índices.
-- Con 400 pacientes ninguno se nota; están pensados para el volumen supuesto en H-14
-- (unos 20.000 contactos al mes, 240.000 al año, sin borrado).

SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRANSACTION;

-- Consulta del mes sin filtros (CA-4): rango semiabierto sobre FechaContacto.
-- Las columnas incluidas cubren la consulta sin volver a la tabla base.
CREATE INDEX IX_Contacto_FechaContacto
    ON dbo.Contacto (FechaContacto)
    INCLUDE (PacienteId, GestorId, CanalCodigo, ResultadoCodigo, VersionActual);

-- Consulta del mes filtrada por gestor (CA-4): primero la columna de igualdad, después la de rango.
CREATE INDEX IX_Contacto_GestorId_FechaContacto
    ON dbo.Contacto (GestorId, FechaContacto);

-- Contactos de un paciente en orden de fecha: ficha del paciente y regla de ilocalizable (CA-5).
CREATE INDEX IX_Contacto_PacienteId_FechaContacto
    ON dbo.Contacto (PacienteId, FechaContacto);

-- Filtro por ciudad (CA-4) y unión con Contacto.
CREATE INDEX IX_Paciente_CiudadId
    ON dbo.Paciente (CiudadId);

-- El historial de un contacto ya está cubierto por UQ_ContactoVersion_Contacto_Numero.

INSERT INTO dbo.VersionEsquema (Script) VALUES ('005_indices.sql');

COMMIT TRANSACTION;
GO
