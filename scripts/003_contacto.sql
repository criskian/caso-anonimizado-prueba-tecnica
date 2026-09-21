-- 003 · Contacto y su historial de versiones (H-01).
--
-- ContactoVersion es la fuente de verdad: cada registro y cada corrección insertan una fila,
-- y ninguna se modifica ni se borra (ver 004_inmutabilidad.sql).
-- Contacto es la proyección de la versión vigente, para que la consulta del mes (CA-4)
-- no tenga que buscar la última versión de cada contacto.
-- Invariante: los valores de Contacto coinciden con los de su ContactoVersion número VersionActual.
-- Ambas escrituras ocurren siempre en la misma transacción.

SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRANSACTION;

CREATE TABLE dbo.Contacto (
    -- BIGINT porque la tabla solo crece: no hay borrado físico (H-14).
    Id               BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Contacto PRIMARY KEY,
    PacienteId       INT                   NOT NULL CONSTRAINT FK_Contacto_Paciente REFERENCES dbo.Paciente (Id),
    -- Autor del contacto: quien lo realizó. No cambia con las correcciones.
    GestorId         INT                   NOT NULL CONSTRAINT FK_Contacto_Gestor REFERENCES dbo.Gestor (Id),
    -- Momento en que ocurrió el contacto, con su desfase horario (H-13).
    FechaContacto    DATETIMEOFFSET(0)     NOT NULL,
    CanalCodigo      VARCHAR(20)           NOT NULL CONSTRAINT FK_Contacto_Canal REFERENCES dbo.CanalContacto (Codigo),
    ResultadoCodigo  VARCHAR(20)           NOT NULL CONSTRAINT FK_Contacto_Resultado REFERENCES dbo.ResultadoContacto (Codigo),
    Observacion      NVARCHAR(500)         NULL,
    -- Número de la versión vigente. Es también el testigo de concurrencia optimista:
    -- la corrección hace UPDATE … WHERE VersionActual = @versionEsperada.
    VersionActual    INT                   NOT NULL CONSTRAINT DF_Contacto_VersionActual DEFAULT 1,
    CreadoEnUtc      DATETIME2(3)          NOT NULL CONSTRAINT DF_Contacto_CreadoEnUtc DEFAULT SYSUTCDATETIME(),
    ActualizadoEnUtc DATETIME2(3)          NOT NULL CONSTRAINT DF_Contacto_ActualizadoEnUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Contacto_VersionActual CHECK (VersionActual >= 1)
);

CREATE TABLE dbo.ContactoVersion (
    Id                    BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_ContactoVersion PRIMARY KEY,
    ContactoId            BIGINT                NOT NULL CONSTRAINT FK_ContactoVersion_Contacto REFERENCES dbo.Contacto (Id),
    NumeroVersion         INT                   NOT NULL,
    -- Copia completa de los valores de esta versión.
    FechaContacto         DATETIMEOFFSET(0)     NOT NULL,
    CanalCodigo           VARCHAR(20)           NOT NULL CONSTRAINT FK_ContactoVersion_Canal REFERENCES dbo.CanalContacto (Codigo),
    ResultadoCodigo       VARCHAR(20)           NOT NULL CONSTRAINT FK_ContactoVersion_Resultado REFERENCES dbo.ResultadoContacto (Codigo),
    Observacion           NVARCHAR(500)         NULL,
    MotivoCorreccion      NVARCHAR(500)         NULL,
    -- Quien registró esta versión: el autor en la versión 1, quien corrigió en las demás.
    RegistradoPorGestorId INT                   NOT NULL CONSTRAINT FK_ContactoVersion_Gestor REFERENCES dbo.Gestor (Id),
    RegistradoEnUtc       DATETIME2(3)          NOT NULL CONSTRAINT DF_ContactoVersion_RegistradoEnUtc DEFAULT SYSUTCDATETIME(),

    -- Una sola versión con cada número por contacto: segunda red de seguridad ante dos
    -- correcciones simultáneas, además del UPDATE condicionado sobre Contacto.
    CONSTRAINT UQ_ContactoVersion_Contacto_Numero UNIQUE (ContactoId, NumeroVersion),
    CONSTRAINT CK_ContactoVersion_NumeroVersion CHECK (NumeroVersion >= 1),
    -- La versión 1 es el registro original y no lleva motivo; toda corrección exige un motivo
    -- de al menos 10 caracteres sin contar los espacios de los extremos.
    CONSTRAINT CK_ContactoVersion_Motivo CHECK (
        (NumeroVersion = 1 AND MotivoCorreccion IS NULL)
        OR (NumeroVersion > 1 AND MotivoCorreccion IS NOT NULL AND LEN(TRIM(MotivoCorreccion)) >= 10)
    )
);

INSERT INTO dbo.VersionEsquema (Script) VALUES ('003_contacto.sql');

COMMIT TRANSACTION;
GO
