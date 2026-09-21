-- 006 · Datos de referencia de los catálogos.
-- Son datos del programa, no de prueba: también se cargarían en producción.
-- Los gestores, pacientes y contactos de ejemplo van en 900_datos_prueba.sql.

SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRANSACTION;

INSERT INTO dbo.Pais (Codigo, Nombre) VALUES
    ('CO', N'Colombia'),
    ('PE', N'Perú'),
    ('EC', N'Ecuador');

INSERT INTO dbo.Ciudad (PaisCodigo, Nombre) VALUES
    ('CO', N'Bogotá'),
    ('CO', N'Medellín'),
    ('CO', N'Cali'),
    ('PE', N'Lima'),
    ('PE', N'Arequipa'),
    ('PE', N'Trujillo'),
    ('EC', N'Quito'),
    ('EC', N'Guayaquil'),
    ('EC', N'Cuenca');

INSERT INTO dbo.TipoDocumento (PaisCodigo, Codigo, Nombre) VALUES
    ('CO', 'CC',  N'Cédula de ciudadanía'),
    ('CO', 'CE',  N'Cédula de extranjería'),
    ('CO', 'PPT', N'Permiso por protección temporal'),
    ('CO', 'PAS', N'Pasaporte'),
    ('PE', 'DNI', N'Documento nacional de identidad'),
    ('PE', 'CE',  N'Carné de extranjería'),
    ('PE', 'PAS', N'Pasaporte'),
    ('EC', 'CI',  N'Cédula de identidad'),
    ('EC', 'PAS', N'Pasaporte');

INSERT INTO dbo.CanalContacto (Codigo, Nombre) VALUES
    ('LLAMADA',  N'Llamada'),
    ('WHATSAPP', N'WhatsApp'),
    ('CORREO',   N'Correo electrónico');

-- Un mensaje de voz o un WhatsApp sin respuesta se registran como NO_CONTESTA (H-07).
INSERT INTO dbo.ResultadoContacto (Codigo, Nombre) VALUES
    ('EFECTIVO',          N'Contacto efectivo'),
    ('NO_CONTESTA',       N'No contesta'),
    ('NUMERO_EQUIVOCADO', N'Número equivocado'),
    ('RECHAZA_PROGRAMA',  N'Rechaza el programa');

INSERT INTO dbo.VersionEsquema (Script) VALUES ('006_datos_catalogos.sql');

COMMIT TRANSACTION;
GO
