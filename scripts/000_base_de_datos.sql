-- 000 · Base de datos y registro de versiones del esquema.
-- Se ejecuta siempre contra master y es idempotente: no se registra en VersionEsquema.
-- Cada script numerado (001, 002…) se registra a sí mismo al final de su transacción,
-- así el ejecutor sabe cuáles ya se aplicaron y no los repite.

IF DB_ID(N'Seguimiento') IS NULL
    CREATE DATABASE Seguimiento COLLATE Modern_Spanish_CI_AS;
GO

USE Seguimiento;
GO

IF OBJECT_ID(N'dbo.VersionEsquema', N'U') IS NULL
    CREATE TABLE dbo.VersionEsquema (
        Script        VARCHAR(100) NOT NULL CONSTRAINT PK_VersionEsquema PRIMARY KEY,
        AplicadoEnUtc DATETIME2(3) NOT NULL CONSTRAINT DF_VersionEsquema_AplicadoEnUtc DEFAULT SYSUTCDATETIME()
    );
GO
