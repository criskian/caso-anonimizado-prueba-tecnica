-- 002 · Paciente.
-- En esta entrega la API solo lee pacientes; los carga el script de datos de prueba.

SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRANSACTION;

CREATE TABLE dbo.Paciente (
    Id                     INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Paciente PRIMARY KEY,
    PaisCodigo             CHAR(2)            NOT NULL,
    TipoDocumentoCodigo    VARCHAR(10)        NOT NULL,
    NumeroDocumento        VARCHAR(20)        NOT NULL,
    Nombre                 NVARCHAR(200)      NOT NULL,
    -- Obligatorio porque el contacto principal es telefónico (PRD §1). Formato E.164:
    -- «+» seguido de 7 a 15 dígitos, sin espacios ni guiones.
    Telefono               VARCHAR(16)        NOT NULL,
    Email                  NVARCHAR(254)      NULL,
    CiudadId               INT                NOT NULL,
    FechaInicioTratamiento DATE               NOT NULL,
    -- Un contacto no puede ser anterior al ingreso al programa (H-04).
    FechaIngresoPrograma   DATE               NOT NULL,
    Estado                 VARCHAR(10)        NOT NULL,
    CreadoEnUtc            DATETIME2(3)       NOT NULL CONSTRAINT DF_Paciente_CreadoEnUtc DEFAULT SYSUTCDATETIME(),

    -- Un paciente es único por país, tipo y número de documento (H-12).
    CONSTRAINT UQ_Paciente_Documento UNIQUE (PaisCodigo, TipoDocumentoCodigo, NumeroDocumento),
    -- El tipo de documento tiene que existir en el país del paciente.
    CONSTRAINT FK_Paciente_TipoDocumento FOREIGN KEY (PaisCodigo, TipoDocumentoCodigo)
        REFERENCES dbo.TipoDocumento (PaisCodigo, Codigo),
    -- La ciudad tiene que pertenecer al país del paciente.
    CONSTRAINT FK_Paciente_Ciudad FOREIGN KEY (CiudadId, PaisCodigo)
        REFERENCES dbo.Ciudad (Id, PaisCodigo),
    CONSTRAINT CK_Paciente_Telefono CHECK (
        Telefono LIKE '+[1-9]%'
        AND SUBSTRING(Telefono, 2, 15) NOT LIKE '%[^0-9]%'
        AND LEN(Telefono) BETWEEN 8 AND 16
    ),
    CONSTRAINT CK_Paciente_Estado CHECK (Estado IN ('ACTIVO', 'INACTIVO'))
);

INSERT INTO dbo.VersionEsquema (Script) VALUES ('002_paciente.sql');

COMMIT TRANSACTION;
GO
