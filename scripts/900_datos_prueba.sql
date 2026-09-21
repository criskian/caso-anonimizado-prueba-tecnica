-- 900 · Datos de prueba. Solo se aplica con aplicar -ConDatosPrueba / --con-datos-prueba.
--
-- Todos los datos son ficticios (H-09). Las fechas se calculan a partir del momento en que
-- se ejecuta el script, en hora local UTC−5 (H-13), para que la vista del mes en curso
-- tenga datos sin importar el día en que se revise. La generación es determinista: los
-- valores «aleatorios» salen de CHECKSUM sobre el número de paciente y del contacto.
--
-- Contenido:
--   · 6 gestores: 5 activos y 1 inactivo que conserva contactos del mes anterior.
--   · 40 pacientes repartidos en los tres países. Los múltiplos de 8 están INACTIVOS.
--     Los pacientes 37 y 38 ingresan este mes y el 39 a mitad del mes anterior (CA-6).
--   · Contactos en el mes anterior y en el mes en curso, nunca en el futuro ni antes del
--     ingreso del paciente. Unos 10 tienen correcciones (versión 2, y algunos versión 3).
--   · Casos para la regla de ilocalizable (CA-5), con contactos escritos a mano:
--       paciente 11 → tres días seguidos sin respuesta (se marcaría);
--       paciente 12 → tres NO_CONTESTA el mismo día (no se marcaría);
--       paciente 13 → un EFECTIVO corta la racha (no se marcaría).

SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRANSACTION;

DECLARE @ahora              DATETIMEOFFSET(0) = SWITCHOFFSET(SYSDATETIMEOFFSET(), '-05:00');
DECLARE @hoy                DATE = CAST(@ahora AS DATE);
DECLARE @inicioMes          DATE = DATEFROMPARTS(YEAR(@hoy), MONTH(@hoy), 1);
DECLARE @inicioMesAnterior  DATE = DATEADD(MONTH, -1, @inicioMes);
DECLARE @ahoraUtc           DATETIME2(3) = SYSUTCDATETIME();

------------------------------------------------------------------------------------------
-- Gestores
------------------------------------------------------------------------------------------
INSERT INTO dbo.Gestor (Nombre, Email, Activo) VALUES
    (N'Laura Méndez',    N'laura.mendez@ejemplo.com',    1),
    (N'Andrés Quintero', N'andres.quintero@ejemplo.com', 1),
    (N'Rosa Huamán',     N'rosa.huaman@ejemplo.com',     1),
    (N'Diego Salazar',   N'diego.salazar@ejemplo.com',   1),
    (N'Paola Andrade',   N'paola.andrade@ejemplo.com',   1),
    (N'Carlos Ríos',     N'carlos.rios@ejemplo.com',     0);

-- Orden 1 a 5: gestores activos; orden 6: gestor inactivo.
CREATE TABLE #Gestor (Orden INT PRIMARY KEY, Id INT NOT NULL);
INSERT INTO #Gestor (Orden, Id)
SELECT ROW_NUMBER() OVER (ORDER BY Id), Id FROM dbo.Gestor;

------------------------------------------------------------------------------------------
-- Pacientes
------------------------------------------------------------------------------------------
-- Las tablas temporales viven en tempdb, cuya intercalación puede ser distinta de la de
-- esta base; COLLATE DATABASE_DEFAULT evita conflictos al compararlas con dbo.*.
CREATE TABLE #Paciente (
    N               INT PRIMARY KEY,
    PaisCodigo      CHAR(2) COLLATE DATABASE_DEFAULT,
    TipoDocumento   VARCHAR(10) COLLATE DATABASE_DEFAULT,
    NumeroDocumento VARCHAR(20) COLLATE DATABASE_DEFAULT,
    Nombre          NVARCHAR(200) COLLATE DATABASE_DEFAULT,
    Telefono        VARCHAR(16) COLLATE DATABASE_DEFAULT,
    Email           NVARCHAR(254) COLLATE DATABASE_DEFAULT,
    Ciudad          NVARCHAR(100) COLLATE DATABASE_DEFAULT,
    FechaIngreso    DATE,
    Estado          VARCHAR(10) COLLATE DATABASE_DEFAULT,
    GestorOrden     INT,
    PacienteId      INT NULL -- se completa después de insertar en dbo.Paciente
);

WITH Numeros AS (
    SELECT TOP (40) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS N
    FROM sys.all_objects
),
Base AS (
    SELECT
        N,
        CHOOSE(N % 3 + 1, 'CO', 'PE', 'EC') AS PaisCodigo,
        CHOOSE(N % 8 + 1, N'María', N'José', N'Ana', N'Luis', N'Carmen', N'Jorge', N'Lucía', N'Pedro') AS NombrePila,
        CHOOSE((N / 8) % 5 + 1, N'García', N'Rodríguez', N'Torres', N'Vargas', N'Castillo') AS Apellido,
        CHOOSE((N / 8) % 5 + 1, 'garcia', 'rodriguez', 'torres', 'vargas', 'castillo') AS ApellidoCorreo,
        (N / 3) % 3 + 1 AS CiudadOrden
    FROM Numeros
)
INSERT INTO #Paciente
SELECT
    N,
    PaisCodigo,
    CASE PaisCodigo WHEN 'CO' THEN 'CC' WHEN 'PE' THEN 'DNI' ELSE 'CI' END,
    CASE PaisCodigo
        WHEN 'CO' THEN '10' + RIGHT('00000000' + CAST(N * 7919 AS VARCHAR(10)), 8)
        WHEN 'PE' THEN RIGHT('00000000' + CAST(40000000 + N * 7919 AS VARCHAR(10)), 8)
        ELSE '17' + RIGHT('00000000' + CAST(N * 7919 AS VARCHAR(10)), 8)
    END,
    NombrePila + N' ' + Apellido,
    -- E.164: Colombia +57 y 10 dígitos; Perú +51 y 9 dígitos; Ecuador +593 y 9 dígitos.
    CASE PaisCodigo
        WHEN 'CO' THEN '+57300' + RIGHT('0000000' + CAST(N * 104729 AS VARCHAR(10)), 7)
        WHEN 'PE' THEN '+519'   + RIGHT('00000000' + CAST(N * 104729 AS VARCHAR(10)), 8)
        ELSE           '+5939'  + RIGHT('00000000' + CAST(N * 104729 AS VARCHAR(10)), 8)
    END,
    -- Uno de cada cinco pacientes no tiene correo: el campo es opcional.
    CASE WHEN N % 5 = 0 THEN NULL
         ELSE LOWER(REPLACE(REPLACE(REPLACE(NombrePila, N'í', N'i'), N'é', N'e'), N'ú', N'u'))
              + N'.' + ApellidoCorreo + CAST(N AS NVARCHAR(3)) + N'@ejemplo.com'
    END,
    CASE PaisCodigo
        WHEN 'CO' THEN CHOOSE(CiudadOrden, N'Bogotá', N'Medellín', N'Cali')
        WHEN 'PE' THEN CHOOSE(CiudadOrden, N'Lima', N'Arequipa', N'Trujillo')
        ELSE           CHOOSE(CiudadOrden, N'Quito', N'Guayaquil', N'Cuenca')
    END,
    CASE
        WHEN N IN (37, 38) THEN DATEFROMPARTS(YEAR(@hoy), MONTH(@hoy), IIF(DAY(@hoy) < 10, DAY(@hoy), 10))
        WHEN N = 39 THEN DATEADD(DAY, 14, @inicioMesAnterior)
        ELSE DATEADD(DAY, -(N * 3), DATEADD(MONTH, -3, @inicioMes))
    END,
    CASE WHEN N % 8 = 0 THEN 'INACTIVO' ELSE 'ACTIVO' END,
    (N - 1) % 5 + 1,
    NULL
FROM Base;

INSERT INTO dbo.Paciente (PaisCodigo, TipoDocumentoCodigo, NumeroDocumento, Nombre, Telefono, Email,
                          CiudadId, FechaInicioTratamiento, FechaIngresoPrograma, Estado)
SELECT p.PaisCodigo, p.TipoDocumento, p.NumeroDocumento, p.Nombre, p.Telefono, p.Email,
       c.Id,
       -- Algunos pacientes empiezan el tratamiento días después de ingresar al programa.
       DATEADD(DAY, p.N % 10, p.FechaIngreso),
       p.FechaIngreso, p.Estado
FROM #Paciente AS p
JOIN dbo.Ciudad AS c ON c.PaisCodigo = p.PaisCodigo AND c.Nombre = p.Ciudad
ORDER BY p.N;

UPDATE p
SET p.PacienteId = pa.Id
FROM #Paciente AS p
JOIN dbo.Paciente AS pa ON pa.PaisCodigo = p.PaisCodigo AND pa.NumeroDocumento = p.NumeroDocumento;

------------------------------------------------------------------------------------------
-- Plan de contactos (versión original de cada uno)
------------------------------------------------------------------------------------------
CREATE TABLE #Plan (
    PlanId          INT IDENTITY(1, 1) PRIMARY KEY,
    PacienteId      INT,
    GestorId        INT,
    FechaContacto   DATETIMEOFFSET(0),
    CanalCodigo     VARCHAR(20) COLLATE DATABASE_DEFAULT,
    ResultadoCodigo VARCHAR(20) COLLATE DATABASE_DEFAULT,
    Observacion     NVARCHAR(500) COLLATE DATABASE_DEFAULT
);

-- Contactos generados: de 2 a 4 en el mes anterior y de 1 a 3 en el mes en curso por paciente,
-- salvo los pacientes 11, 12 y 13, cuyos contactos se escriben a mano más abajo.
WITH Ventanas AS (
    SELECT
        p.N, p.PacienteId, p.GestorOrden, m.Mes,
        -- La ventana empieza al inicio del mes o el día de ingreso, lo que sea posterior.
        TODATETIMEOFFSET(CAST(IIF(p.FechaIngreso > m.Inicio, p.FechaIngreso, m.Inicio) AS DATETIME2(0)), '-05:00') AS Desde,
        -- El mes en curso termina ahora: nunca se generan contactos en el futuro.
        IIF(m.Mes = 'ACTUAL', @ahora, TODATETIMEOFFSET(CAST(@inicioMes AS DATETIME2(0)), '-05:00')) AS Hasta,
        IIF(m.Mes = 'ACTUAL', 1 + ABS(CHECKSUM(p.N, 'actual')) % 3, 2 + ABS(CHECKSUM(p.N, 'anterior')) % 3) AS Cantidad
    FROM #Paciente AS p
    CROSS JOIN (VALUES ('ANTERIOR', @inicioMesAnterior), ('ACTUAL', @inicioMes)) AS m (Mes, Inicio)
    WHERE p.N NOT IN (11, 12, 13)
),
Intentos AS (
    SELECT
        v.*, i.I,
        ABS(CHECKSUM(v.N, v.Mes, i.I, 'hash')) AS H,
        DATEDIFF(DAY, v.Desde, v.Hasta) + IIF(v.Mes = 'ACTUAL', 1, 0) AS DiasVentana
    FROM Ventanas AS v
    JOIN (VALUES (1), (2), (3), (4)) AS i (I) ON i.I <= v.Cantidad
    WHERE v.Hasta > v.Desde
),
Candidatos AS (
    SELECT
        t.*,
        -- Día al azar dentro de la ventana, en horario laboral (8:00 a 17:59).
        DATEADD(MINUTE, 480 + (t.H / 7) % 600, DATEADD(DAY, t.H % IIF(t.DiasVentana < 1, 1, t.DiasVentana), t.Desde)) AS Fecha
    FROM Intentos AS t
)
INSERT INTO #Plan (PacienteId, GestorId, FechaContacto, CanalCodigo, ResultadoCodigo, Observacion)
SELECT
    c.PacienteId,
    -- En el mes anterior, uno de cada cinco pacientes lo contactó el gestor que hoy está inactivo.
    g.Id,
    -- Si el horario elegido cae después del final de la ventana (por ejemplo, hoy más tarde),
    -- se usa un momento anterior al final, sin salirse del inicio de la ventana.
    IIF(c.Fecha < c.Hasta, c.Fecha,
        IIF(DATEADD(MINUTE, -1 - c.H % 60, c.Hasta) > c.Desde, DATEADD(MINUTE, -1 - c.H % 60, c.Hasta), c.Desde)),
    CASE WHEN c.H % 20 < 12 THEN 'LLAMADA' WHEN c.H % 20 < 17 THEN 'WHATSAPP' ELSE 'CORREO' END,
    CASE WHEN c.H % 10 < 6 THEN 'EFECTIVO'
         WHEN c.H % 10 < 9 THEN 'NO_CONTESTA'
         WHEN c.H % 20 = 9 THEN 'NUMERO_EQUIVOCADO'
         ELSE 'RECHAZA_PROGRAMA' END,
    CASE WHEN c.H % 3 <> 0 THEN NULL
         WHEN c.H % 10 < 6 THEN N'Confirma la fecha de su próxima dispensación.'
         WHEN c.H % 10 < 9 THEN N'Se deja mensaje de voz.'
         WHEN c.H % 20 = 9 THEN N'El número corresponde a otra persona.'
         ELSE N'Indica que no desea recibir más llamadas del programa.' END
FROM Candidatos AS c
JOIN #Gestor AS g ON g.Orden = IIF(c.Mes = 'ANTERIOR' AND c.N % 5 = 0, 6, c.GestorOrden);

-- Casos escritos a mano para la regla de ilocalizable (CA-5), todos en el mes anterior.
DECLARE @base DATETIMEOFFSET(0) = TODATETIMEOFFSET(CAST(@inicioMesAnterior AS DATETIME2(0)), '-05:00');

INSERT INTO #Plan (PacienteId, GestorId, FechaContacto, CanalCodigo, ResultadoCodigo, Observacion)
SELECT p.PacienteId, g.Id, DATEADD(HOUR, x.Hora, DATEADD(DAY, x.Dia - 1, @base)), x.Canal, x.Resultado, x.Observacion
FROM (VALUES
    -- Paciente 11: un contacto efectivo y luego tres días distintos sin respuesta.
    (11,  2, 10, 'LLAMADA',  'EFECTIVO',    N'Confirma la fecha de su próxima dispensación.'),
    (11,  5, 10, 'LLAMADA',  'NO_CONTESTA', NULL),
    (11, 12, 16, 'WHATSAPP', 'NO_CONTESTA', N'Mensaje entregado, sin respuesta.'),
    (11, 20,  9, 'LLAMADA',  'NO_CONTESTA', N'Se deja mensaje de voz.'),
    -- Paciente 12: tres intentos sin respuesta el mismo día cuentan como un solo día.
    (12, 10, 11, 'LLAMADA',  'EFECTIVO',    NULL),
    (12, 20,  9, 'LLAMADA',  'NO_CONTESTA', NULL),
    (12, 20, 12, 'LLAMADA',  'NO_CONTESTA', NULL),
    (12, 20, 16, 'WHATSAPP', 'NO_CONTESTA', NULL),
    -- Paciente 13: un contacto efectivo entre dos días sin respuesta corta la racha.
    (13,  5, 10, 'LLAMADA',  'NO_CONTESTA', NULL),
    (13, 12, 10, 'LLAMADA',  'EFECTIVO',    NULL),
    (13, 20, 10, 'LLAMADA',  'NO_CONTESTA', NULL)
) AS x (N, Dia, Hora, Canal, Resultado, Observacion)
JOIN #Paciente AS p ON p.N = x.N
JOIN #Gestor AS g ON g.Orden = p.GestorOrden;

------------------------------------------------------------------------------------------
-- Contactos y su versión 1
------------------------------------------------------------------------------------------
-- MERGE permite devolver en OUTPUT la columna PlanId del origen junto al Id generado,
-- cosa que INSERT … OUTPUT no permite.
CREATE TABLE #Mapa (PlanId INT PRIMARY KEY, ContactoId BIGINT NOT NULL);

MERGE dbo.Contacto AS destino
USING (SELECT * FROM #Plan) AS origen
ON 1 = 0
WHEN NOT MATCHED THEN
    INSERT (PacienteId, GestorId, FechaContacto, CanalCodigo, ResultadoCodigo, Observacion,
            VersionActual, CreadoEnUtc, ActualizadoEnUtc)
    VALUES (origen.PacienteId, origen.GestorId, origen.FechaContacto, origen.CanalCodigo,
            origen.ResultadoCodigo, origen.Observacion, 1,
            -- Registro contemporáneo: 5 minutos después del contacto, nunca después de ahora.
            IIF(DATEADD(MINUTE, 5, CAST(SWITCHOFFSET(origen.FechaContacto, '+00:00') AS DATETIME2(3))) < @ahoraUtc,
                DATEADD(MINUTE, 5, CAST(SWITCHOFFSET(origen.FechaContacto, '+00:00') AS DATETIME2(3))), @ahoraUtc),
            IIF(DATEADD(MINUTE, 5, CAST(SWITCHOFFSET(origen.FechaContacto, '+00:00') AS DATETIME2(3))) < @ahoraUtc,
                DATEADD(MINUTE, 5, CAST(SWITCHOFFSET(origen.FechaContacto, '+00:00') AS DATETIME2(3))), @ahoraUtc))
OUTPUT origen.PlanId, inserted.Id INTO #Mapa (PlanId, ContactoId);

INSERT INTO dbo.ContactoVersion (ContactoId, NumeroVersion, FechaContacto, CanalCodigo, ResultadoCodigo,
                                 Observacion, MotivoCorreccion, RegistradoPorGestorId, RegistradoEnUtc)
SELECT c.Id, 1, c.FechaContacto, c.CanalCodigo, c.ResultadoCodigo, c.Observacion, NULL, c.GestorId, c.CreadoEnUtc
FROM dbo.Contacto AS c
JOIN #Mapa AS m ON m.ContactoId = c.Id;

------------------------------------------------------------------------------------------
-- Correcciones (CA-3)
------------------------------------------------------------------------------------------
-- Uno de cada quince contactos con más de un día de antigüedad recibe una corrección del
-- resultado (versión 2); la mitad de ellos, además, una corrección del canal (versión 3)
-- hecha por otro gestor. Se excluyen los pacientes 11 a 13 para no alterar los casos de CA-5.
CREATE TABLE #Correccion (
    ContactoId            BIGINT,
    NumeroVersion         INT,
    FechaContacto         DATETIMEOFFSET(0),
    CanalCodigo           VARCHAR(20) COLLATE DATABASE_DEFAULT,
    ResultadoCodigo       VARCHAR(20) COLLATE DATABASE_DEFAULT,
    Observacion           NVARCHAR(500) COLLATE DATABASE_DEFAULT,
    MotivoCorreccion      NVARCHAR(500) COLLATE DATABASE_DEFAULT,
    RegistradoPorGestorId INT,
    RegistradoEnUtc       DATETIME2(3)
);

WITH Elegidos AS (
    SELECT c.*, ROW_NUMBER() OVER (ORDER BY c.Id) AS Fila
    FROM dbo.Contacto AS c
    WHERE c.PacienteId NOT IN (SELECT PacienteId FROM #Paciente WHERE N IN (11, 12, 13))
      AND c.FechaContacto < DATEADD(DAY, -1, @ahora)
      AND c.ResultadoCodigo IN ('EFECTIVO', 'NO_CONTESTA')
),
Version2 AS (
    SELECT
        e.Id AS ContactoId, 2 AS NumeroVersion, e.FechaContacto, e.CanalCodigo,
        IIF(e.ResultadoCodigo = 'EFECTIVO', 'NO_CONTESTA', 'EFECTIVO') AS ResultadoCodigo,
        e.Observacion,
        IIF(e.ResultadoCodigo = 'EFECTIVO',
            N'El paciente no contestó; el resultado se registró como efectivo por error.',
            N'El paciente sí contestó al segundo intento; se había registrado como no contesta.') AS MotivoCorreccion,
        e.GestorId AS RegistradoPorGestorId,
        DATEADD(HOUR, 2, e.CreadoEnUtc) AS RegistradoEnUtc,
        e.Fila
    FROM Elegidos AS e
    WHERE e.Fila % 15 = 0
)
INSERT INTO #Correccion
SELECT ContactoId, NumeroVersion, FechaContacto, CanalCodigo, ResultadoCodigo, Observacion,
       MotivoCorreccion, RegistradoPorGestorId, RegistradoEnUtc
FROM Version2
UNION ALL
SELECT v.ContactoId, 3, v.FechaContacto,
       IIF(v.CanalCodigo = 'LLAMADA', 'WHATSAPP', 'LLAMADA'),
       v.ResultadoCodigo, v.Observacion,
       N'El contacto se hizo por otro canal; se corrige el canal registrado.',
       -- Otro gestor activo revisa y corrige.
       (SELECT Id FROM #Gestor WHERE Orden = (SELECT Orden FROM #Gestor WHERE Id = v.RegistradoPorGestorId) % 5 + 1),
       DATEADD(HOUR, 18, v.RegistradoEnUtc)
FROM Version2 AS v
WHERE v.Fila % 30 = 0;

INSERT INTO dbo.ContactoVersion (ContactoId, NumeroVersion, FechaContacto, CanalCodigo, ResultadoCodigo,
                                 Observacion, MotivoCorreccion, RegistradoPorGestorId, RegistradoEnUtc)
SELECT ContactoId, NumeroVersion, FechaContacto, CanalCodigo, ResultadoCodigo,
       Observacion, MotivoCorreccion, RegistradoPorGestorId, RegistradoEnUtc
FROM #Correccion;

-- La proyección refleja la última versión de cada contacto corregido.
UPDATE c
SET c.FechaContacto    = v.FechaContacto,
    c.CanalCodigo      = v.CanalCodigo,
    c.ResultadoCodigo  = v.ResultadoCodigo,
    c.Observacion      = v.Observacion,
    c.VersionActual    = v.NumeroVersion,
    c.ActualizadoEnUtc = v.RegistradoEnUtc
FROM dbo.Contacto AS c
JOIN #Correccion AS v ON v.ContactoId = c.Id
WHERE v.NumeroVersion = (SELECT MAX(NumeroVersion) FROM #Correccion WHERE ContactoId = c.Id);

INSERT INTO dbo.VersionEsquema (Script) VALUES ('900_datos_prueba.sql');

COMMIT TRANSACTION;
GO
