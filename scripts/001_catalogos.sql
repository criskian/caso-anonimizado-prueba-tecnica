-- 001 · Catálogos: país, ciudad, tipo de documento, gestor, canal y resultado de contacto.
-- Catálogos en tablas (y no CHECK o enums) para que agregar un valor sea un INSERT versionado
-- sin tocar el esquema ni el código (H-07, H-12). La columna Activo retira un valor sin
-- romper los registros históricos que lo usan.

SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRANSACTION;

CREATE TABLE dbo.Pais (
    Codigo CHAR(2)      NOT NULL CONSTRAINT PK_Pais PRIMARY KEY, -- ISO 3166-1 alfa-2
    Nombre NVARCHAR(60) NOT NULL
);

CREATE TABLE dbo.Ciudad (
    Id         INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Ciudad PRIMARY KEY,
    PaisCodigo CHAR(2)            NOT NULL CONSTRAINT FK_Ciudad_Pais REFERENCES dbo.Pais (Codigo),
    Nombre     NVARCHAR(100)      NOT NULL,
    CONSTRAINT UQ_Ciudad_Pais_Nombre UNIQUE (PaisCodigo, Nombre),
    -- Destino de la FK compuesta de Paciente: garantiza que la ciudad sea del país del paciente.
    CONSTRAINT UQ_Ciudad_Id_Pais UNIQUE (Id, PaisCodigo)
);

CREATE TABLE dbo.TipoDocumento (
    PaisCodigo CHAR(2)      NOT NULL CONSTRAINT FK_TipoDocumento_Pais REFERENCES dbo.Pais (Codigo),
    Codigo     VARCHAR(10)  NOT NULL,
    Nombre     NVARCHAR(60) NOT NULL,
    CONSTRAINT PK_TipoDocumento PRIMARY KEY (PaisCodigo, Codigo)
);

CREATE TABLE dbo.Gestor (
    Id     INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Gestor PRIMARY KEY,
    Nombre NVARCHAR(150)      NOT NULL,
    Email  NVARCHAR(254)      NOT NULL CONSTRAINT UQ_Gestor_Email UNIQUE,
    Activo BIT                NOT NULL CONSTRAINT DF_Gestor_Activo DEFAULT 1
);

CREATE TABLE dbo.CanalContacto (
    Codigo VARCHAR(20)  NOT NULL CONSTRAINT PK_CanalContacto PRIMARY KEY,
    Nombre NVARCHAR(60) NOT NULL,
    Activo BIT          NOT NULL CONSTRAINT DF_CanalContacto_Activo DEFAULT 1
);

CREATE TABLE dbo.ResultadoContacto (
    Codigo VARCHAR(20)  NOT NULL CONSTRAINT PK_ResultadoContacto PRIMARY KEY,
    Nombre NVARCHAR(60) NOT NULL,
    Activo BIT          NOT NULL CONSTRAINT DF_ResultadoContacto_Activo DEFAULT 1
);

INSERT INTO dbo.VersionEsquema (Script) VALUES ('001_catalogos.sql');

COMMIT TRANSACTION;
GO
