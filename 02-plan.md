# 02 · Plan y cierre de alcance 

(Cambios al plan y aclaraciones de uso de la IA en la sección 10)

## 1. Alcance cerrado

Construyo **una sola funcionalidad de punta a punta: la bitácora de contactos del mes.** El gestor registra un contacto, lo corrige sin perder el rastro de lo que había, y la coordinadora consulta el mes filtrando por gestor y ciudad.

| CA | Qué cubre | Por qué entra |
|---|---|---|
| **CA-2** | Registrar un contacto asociado a un paciente, con fecha, canal y resultado | Es la base: sin contactos no hay nada que corregir ni que consultar. |
| **CA-3** | Corregir un contacto y que la consulta refleje la corrección | Es el criterio más delicado en un entorno regulado (H-01). Muestra el versionado inmutable, la autoría, el motivo obligatorio y la concurrencia. |
| **CA-4** | Vista de contactos del mes con filtros por gestor y ciudad | Cierra el ciclo: es donde se ve que la corrección se refleja. Además resuelve la consulta que combina cuatro tablas, con índice justificado. |

**Cómo interpreto CA-3.** "El reporte refleja la información corregida" se verifica sobre la vista de contactos del mes (CA-4), que siempre muestra la versión vigente de cada contacto. El reporte de adherencia (CA-6) no está en esta entrega, pero su diseño (sección 8.3) usa las mismas versiones vigentes, así que también reflejará las correcciones.

**Supuestos que adopto**, tomados de mis hallazgos:

| Hallazgo | Decisión | Dónde se refleja |
|---|---|---|
| H-01 | La corrección crea una versión nueva, que no se puede modificar, con autor, fecha-hora y motivo obligatorio. No hay borrado físico. | Tablas `ContactoVersion` y trigger (sección 3); endpoint de correcciones (sección 4) |
| H-02 | Las consultas usan la versión vigente. El historial permite reconstruir cualquier fecha pasada. El cierre de mes queda fuera. | Sección 3.3; sección 8.3 (fotos inmutables del reporte) |
| H-07 | Catálogos cerrados de canal y de resultado. | Tablas `CanalContacto` y `ResultadoContacto` |
| H-11 | El gestor es una entidad. El filtro es por el gestor que hizo el contacto. La identidad se simula con la cabecera `X-Gestor-Id`. | Contrato (sección 4); pantalla «Actuando como» |
| H-12 | Catálogos de país, ciudad y tipo de documento. El filtro usa la ciudad actual del paciente. | Sección 3 |
| H-13 | El mes llega como parámetro. Lo decide la fecha en que ocurrió el contacto, en hora local UTC−5. No se admiten fechas futuras. | `FechaContacto DATETIMEOFFSET`; reglas de negocio en la sección 4.4 |
| H-14 | Se diseña para unos 240.000 contactos al año. | Índices (sección 3.4); paginación |

---

## 2. Fuera de alcance

| Qué dejo fuera | Por qué | Qué queda preparado |
|---|---|---|
| **CA-1** Registro de pacientes | Tope de tres criterios. Además es el de menor riesgo técnico: es un formulario y un INSERT. Los pacientes los carga el script de datos de prueba. | Diseño completo en la sección 8.1. El modelo actual ya tiene las restricciones de país, documento y teléfono. |
| **CA-5** Paciente ilocalizable | Tope de tres criterios. Su regla (H-07, H-08) es un supuesto mío y debería confirmarla el PO antes de marcar pacientes. | Regla, consulta y pruebas definidas en la sección 8.2. El índice que necesita ya se crea en esta entrega. |
| **CA-6** Reporte de adherencia | Tope de tres criterios. Es el único número que sale hacia el laboratorio, y su definición (H-04, H-05, H-06) depende de un calendario médico que está pendiente. No produzco un indicador para el patrocinador sobre una regla que no está confirmada. | Fórmula, modelo de historial de estados, foto inmutable del reporte, consulta y pruebas en la sección 8.3. |
| Autorregistro del paciente | Contradice el teléfono obligatorio y no tiene verificación de identidad (H-03). | Diseño como «solicitud de inscripción» en la sección 8.1. |
| Autenticación y roles | No está definida en el PRD (H-11), y hacerla bien no cabe en el tiempo disponible. | La cabecera `X-Gestor-Id` se declara como **simulación que no es segura**. Reemplazarla por un token solo cambia cómo se obtiene el gestor actual. |
| Consentimiento y anonimización | Es un requisito legal que necesita una definición jurídica (H-09). | Diseño de la tabla de consentimiento en la sección 8.1. Todos los datos de prueba son ficticios. |
| Farmacovigilancia | Necesita un procedimiento con plazos y destinatarios (H-10). Un campo sin ese flujo da una falsa sensación de cumplimiento. | Riesgo abierto, declarado. |
| Cierre formal de mes y bloqueo de correcciones | Necesita la respuesta del PO sobre H-02. | El historial de versiones ya permite reconstruir cualquier fecha. |
| Anular un contacto | No lo pide ningún criterio. | Se resuelve con el mismo patrón que la corrección: una versión con estado anulado. |
| Agendamiento y calendario de seguimiento | No está descrito en el PRD, y el calendario está pendiente (H-05). | — |
| Exportar al laboratorio, despliegue, internacionalización, diseño visual | No aportan a los criterios en alcance. El diseño de pantallas lo resuelve el equipo de diseño, según el propio PRD. | — |
| Aplicación móvil | Se pide sin código. | Criterio en la sección 7. |

---

## 3. Modelo de datos

SQL Server 2022. El esquema se crea con los scripts numerados de `scripts/`. Los nombres van en español, igual que en el dominio del PRD.

### 3.1 Catálogos

| Tabla | Campo | Tipo | Restricciones |
|---|---|---|---|
| `Pais` | `Codigo` | `CHAR(2)` | PK. Código ISO 3166-1: `CO`, `PE`, `EC` |
| | `Nombre` | `NVARCHAR(60)` | NOT NULL |
| `Ciudad` | `Id` | `INT IDENTITY` | PK |
| | `PaisCodigo` | `CHAR(2)` | NOT NULL, FK a `Pais` |
| | `Nombre` | `NVARCHAR(100)` | NOT NULL. `UNIQUE (PaisCodigo, Nombre)`. `UNIQUE (Id, PaisCodigo)` como destino de una FK compuesta |
| `TipoDocumento` | `PaisCodigo` + `Codigo` | `CHAR(2)` + `VARCHAR(10)` | PK compuesta (`CO-CC`, `CO-CE`, `PE-DNI`, `EC-CI`, `PAS`…) |
| | `Nombre` | `NVARCHAR(60)` | NOT NULL |
| `Gestor` | `Id` | `INT IDENTITY` | PK |
| | `Nombre` | `NVARCHAR(150)` | NOT NULL |
| | `Email` | `NVARCHAR(254)` | NOT NULL, UNIQUE |
| | `Activo` | `BIT` | NOT NULL, DEFAULT 1 |
| `CanalContacto` | `Codigo` | `VARCHAR(20)` | PK: `LLAMADA`, `WHATSAPP`, `CORREO` |
| | `Nombre`, `Activo` | `NVARCHAR(60)`, `BIT` | NOT NULL |
| `ResultadoContacto` | `Codigo` | `VARCHAR(20)` | PK: `EFECTIVO`, `NO_CONTESTA`, `NUMERO_EQUIVOCADO`, `RECHAZA_PROGRAMA` |
| | `Nombre`, `Activo` | `NVARCHAR(60)`, `BIT` | NOT NULL |

Uso tablas de catálogo en lugar de restricciones CHECK o enums porque agregar un canal o un resultado se convierte en un INSERT versionado, sin cambiar el esquema ni el código. La columna `Activo` permite retirar un valor sin romper los contactos históricos que lo usan.

### 3.2 Paciente

| Campo | Tipo | Restricciones |
|---|---|---|
| `Id` | `INT IDENTITY` | PK |
| `PaisCodigo` | `CHAR(2)` | NOT NULL |
| `TipoDocumentoCodigo` | `VARCHAR(10)` | NOT NULL. FK compuesta `(PaisCodigo, TipoDocumentoCodigo)` a `TipoDocumento`, así el tipo de documento tiene que existir en ese país |
| `NumeroDocumento` | `VARCHAR(20)` | NOT NULL. `UNIQUE (PaisCodigo, TipoDocumentoCodigo, NumeroDocumento)` evita duplicados (H-12) |
| `Nombre` | `NVARCHAR(200)` | NOT NULL |
| `Telefono` | `VARCHAR(16)` | **NOT NULL** (PRD §1), en formato E.164, con `CHECK (Telefono LIKE '+[0-9]%')` |
| `Email` | `NVARCHAR(254)` | NULL |
| `CiudadId` | `INT` | NOT NULL. FK compuesta `(CiudadId, PaisCodigo)` a `Ciudad(Id, PaisCodigo)`, así la ciudad pertenece al país del paciente |
| `FechaInicioTratamiento` | `DATE` | NOT NULL |
| `FechaIngresoPrograma` | `DATE` | NOT NULL. Un contacto no puede ser anterior a esta fecha (H-04) |
| `Estado` | `VARCHAR(10)` | NOT NULL, `CHECK IN ('ACTIVO','INACTIVO')` |
| `CreadoEnUtc` | `DATETIME2(3)` | NOT NULL, DEFAULT `SYSUTCDATETIME()` |

En esta entrega la API **solo lee** pacientes. Los carga el script de datos de prueba.

### 3.3 Contacto: una proyección vigente y un historial que no se modifica

| Tabla | Campo | Tipo | Restricciones |
|---|---|---|---|
| `Contacto` | `Id` | `BIGINT IDENTITY` | PK. BIGINT porque la tabla crece sin borrado (H-14) |
| | `PacienteId` | `INT` | NOT NULL, FK. **No cambia después de crearse** |
| | `GestorId` | `INT` | NOT NULL, FK. Es el autor del contacto y **no cambia** |
| | `FechaContacto` | `DATETIMEOFFSET(0)` | NOT NULL. Es el momento en que ocurrió el contacto, con su desfase horario (H-13) |
| | `CanalCodigo` | `VARCHAR(20)` | NOT NULL, FK |
| | `ResultadoCodigo` | `VARCHAR(20)` | NOT NULL, FK |
| | `Observacion` | `NVARCHAR(500)` | NULL |
| | `VersionActual` | `INT` | NOT NULL, `CHECK (VersionActual >= 1)`. Es también el **testigo de concurrencia** |
| | `CreadoEnUtc`, `ActualizadoEnUtc` | `DATETIME2(3)` | NOT NULL |
| `ContactoVersion` | `Id` | `BIGINT IDENTITY` | PK |
| | `ContactoId` | `BIGINT` | NOT NULL, FK |
| | `NumeroVersion` | `INT` | NOT NULL. `UNIQUE (ContactoId, NumeroVersion)` |
| | `FechaContacto`, `CanalCodigo`, `ResultadoCodigo`, `Observacion` | mismos tipos | Copia completa de los valores de esa versión |
| | `MotivoCorreccion` | `NVARCHAR(500)` | NULL en la versión 1. En las demás es obligatorio y tiene al menos 10 caracteres sin espacios en los extremos (restricción CHECK) |
| | `RegistradoPorGestorId` | `INT` | NOT NULL, FK. Es quien hizo el registro o la corrección |
| | `RegistradoEnUtc` | `DATETIME2(3)` | NOT NULL, DEFAULT `SYSUTCDATETIME()` |

**Cómo trato la corrección de un registro ya guardado**

- **`ContactoVersion` es la fuente de verdad y solo admite inserciones.** Un trigger `INSTEAD OF UPDATE, DELETE` lanza un error (`THROW`). La inmutabilidad la garantiza la base de datos, no solo la aplicación: ni un script manual puede reescribir la historia sin desactivar el trigger, y eso ya deja rastro. Otro trigger impide borrar filas de `Contacto` y cambiar su `PacienteId` o su `GestorId`.
- **`Contacto` es la proyección de la versión vigente.** Existe para que las consultas del mes no tengan que buscar la última versión de cada contacto.
- **Registrar un contacto** inserta la fila en `Contacto` (versión 1) y su `ContactoVersion` número 1, en una sola transacción.
- **Corregir un contacto** hace dos cosas en una sola transacción:
  1. `UPDATE Contacto SET …, VersionActual = VersionActual + 1 WHERE Id = @id AND VersionActual = @versionEsperada`.
  2. Si ese UPDATE afectó una fila, `INSERT` en `ContactoVersion` con el número siguiente.

  Si el UPDATE afectó 0 filas, alguien corrigió antes: la API responde **409** y no se escribe nada. El UPDATE comprueba la versión y bloquea la fila en un solo paso, así que `READ COMMITTED` basta. La restricción única `(ContactoId, NumeroVersion)` es la segunda red de seguridad.
- **Invariante:** los valores de `Contacto` coinciden con los de su `ContactoVersion` número `VersionActual`. Lo comprueba una prueba de integración.
- **Descarté las tablas temporales de SQL Server.** No registran quién corrigió ni por qué como campos obligatorios, y un administrador puede apagar el versionado.
- **El historial permite reconstruir el pasado.** Los datos tal como estaban en la fecha X son, para cada contacto, la versión con el mayor `NumeroVersion` cuyo `RegistradoEnUtc` sea menor o igual a X (H-02).

### 3.4 Índices

| Índice | Para qué |
|---|---|
| `IX_Contacto_FechaContacto` sobre `(FechaContacto)` INCLUDE `(PacienteId, GestorId, CanalCodigo, ResultadoCodigo, VersionActual)` | Rango del mes sin filtros: la consulta se resuelve con un rango sobre el índice, sin ir a la tabla base. |
| `IX_Contacto_GestorId_FechaContacto` sobre `(GestorId, FechaContacto)` | Filtro por gestor: primero la columna de igualdad, después la de rango. |
| `IX_Contacto_PacienteId_FechaContacto` sobre `(PacienteId, FechaContacto)` | Ficha del paciente, y la regla de CA-5 en la siguiente iteración. |
| `IX_Paciente_CiudadId` | Filtro por ciudad; sirve de lado interno de la unión con `Contacto`. |
| `UQ_ContactoVersion` sobre `(ContactoId, NumeroVersion)` | Historial de un contacto y protección ante concurrencia. |

Con 400 pacientes ningún índice se nota. La justificación es para el volumen supuesto en H-14: unos 20.000 contactos al mes.

---

## 4. Contrato de la interfaz

### 4.1 Convenciones

- **API REST en `/api`.** Las fechas van en ISO 8601 con desfase (`2026-09-21T15:30:00-05:00`).
- **Identidad simulada.** Toda escritura exige la cabecera `X-Gestor-Id`. Si falta, o si el gestor no existe o está inactivo, la API responde **401**.
- **Errores en formato ProblemDetails (RFC 7807).** Incluyen `status`, `title`, `detail`, una extensión `codigo` estable para la interfaz y, en los 400, un diccionario `errors` por campo.
- **Separación de responsabilidades.** El controlador solo traduce el DTO en un comando y delega. Todas las reglas viven en `ContactoService`. Un `IExceptionHandler` único traduce las excepciones de dominio a HTTP.

### 4.2 Endpoints

| Método y ruta | CA | Entrada | Salida correcta | Errores |
|---|---|---|---|---|
| `GET /api/catalogos` | 2, 4 | — | 200 `{ gestores[], ciudades[], canales[], resultados[] }`, solo los activos | — |
| `GET /api/pacientes?buscar=texto` | 2 | Texto de 2 o más caracteres; busca por nombre o documento | 200 `PacienteResumenDto[]`: hasta 20 pacientes, solo `ACTIVO` | 400 `BUSQUEDA_CORTA` |
| `POST /api/contactos` | 2 | `{ pacienteId, fechaContacto, canal, resultado, observacion? }` | 201 `ContactoDetalleDto` con cabecera `Location` | 400 validación · 401 · 404 `PACIENTE_NO_ENCONTRADO` · 422 `FECHA_FUTURA`, `FECHA_ANTERIOR_AL_INGRESO`, `PACIENTE_INACTIVO`, `CATALOGO_INACTIVO` |
| `GET /api/contactos/{id}` | 3 | — | 200 `ContactoDetalleDto` con `historial[]` | 404 `CONTACTO_NO_ENCONTRADO` |
| `POST /api/contactos/{id}/correcciones` | 3 | `{ versionEsperada, fechaContacto, canal, resultado, observacion?, motivo }` | 200 `ContactoDetalleDto` en la versión n+1 | 400 validación (motivo vacío o corto) · 401 · 404 · **409 `VERSION_DESACTUALIZADA`** · 422 `SIN_CAMBIOS`, `FECHA_FUTURA`, `FECHA_ANTERIOR_AL_INGRESO` |
| `GET /api/contactos?mes=AAAA-MM&gestorId=&ciudadId=&pagina=1&tamano=50` | 4 | `mes` es opcional y por defecto es el mes actual en UTC−5. Los filtros son opcionales y se combinan con Y. `tamano` va de 1 a 200 | 200 `{ mes, pagina, tamano, total, items: ContactoMesItemDto[] }` | 400 `MES_INVALIDO`, `PAGINACION_INVALIDA` |

La corrección es un `POST` a un subrecurso y no un `PUT`: no reemplaza el contacto, **crea** una corrección.

### 4.3 Objetos de transferencia

- `ContactoDetalleDto`: `id`, `paciente {id, nombre, ciudad}`, `gestor {id, nombre}`, `fechaContacto`, `canal {codigo, nombre}`, `resultado {codigo, nombre}`, `observacion`, `versionActual` e `historial[]`. Cada elemento del historial tiene `numeroVersion`, `fechaContacto`, `canal`, `resultado`, `observacion`, `motivoCorreccion`, `registradoPor {id, nombre}` y `registradoEnUtc`.
- `ContactoMesItemDto`: `id`, `fechaContacto`, `paciente`, `ciudad`, `gestor`, `canal`, `resultado`, `versionActual` y `corregido` (verdadero si `versionActual` es mayor que 1).

### 4.4 Reglas de negocio (en `ContactoService`, con pruebas unitarias)

1. El paciente tiene que existir (404) y estar `ACTIVO` para registrarle un contacto nuevo (422). Un contacto de un paciente que ya está inactivo sí se puede corregir: se está corrigiendo historia.
2. `fechaContacto` no puede estar en el futuro. Se tolera un desfase de 5 minutos entre relojes, y la hora actual se obtiene de un `TimeProvider` inyectado, lo que permite probarla. Tampoco puede ser anterior a la `FechaIngresoPrograma` del paciente (422).
3. El canal y el resultado tienen que existir en el catálogo (400) y estar activos (422).
4. Una corrección exige un motivo de 10 a 500 caracteres (400) y al menos un cambio respecto a la versión vigente (422 `SIN_CAMBIOS`).
5. Una corrección no puede cambiar el paciente ni el autor original. Quien corrige queda en `RegistradoPorGestorId`.
6. El mes de un contacto se calcula con la fecha del contacto en hora local UTC−5.

### 4.5 Pantallas (Angular)

| Ruta | Pantalla | Qué muestra y qué hace |
|---|---|---|
| (barra superior) | «Actuando como» | Selector de gestor. Se guarda en el navegador, y un interceptor lo envía en `X-Gestor-Id`. Aparece rotulado como simulación. |
| `/contactos` | Contactos del mes (CA-4) | Selector de mes, filtros de gestor y ciudad, tabla paginada y una etiqueta «corregido vN». |
| `/contactos/nuevo` | Registrar contacto (CA-2) | Búsqueda de paciente, fecha y hora, canal, resultado y observación. |
| `/contactos/:id` | Detalle y corrección (CA-3) | Valores vigentes, historial de versiones (quién, cuándo y por qué) y formulario de corrección con motivo obligatorio. |

**Errores tratados de punta a punta.** Elegí dos casos que la interfaz **no puede prevenir por sí sola**, para que el recorrido completo sea real:

- **409 `VERSION_DESACTUALIZADA`.** Dos pestañas corrigen el mismo contacto. La segunda recibe el aviso «Otro usuario corrigió este contacto; recarga para ver la versión vigente», con un botón para recargar.
- **422 `FECHA_ANTERIOR_AL_INGRESO`.** El mensaje aparece junto al campo de fecha.

Los errores 400 que devuelve el servidor también se muestran junto al campo que corresponde, gracias al diccionario `errors`.

### 4.6 Arquitectura

```
api/
  src/Seguimiento.Api          controladores, DTO, mapeo, IExceptionHandler, configuración
  src/Seguimiento.Servicios    ContactoService, reglas, excepciones de dominio, interfaces de repositorios
  src/Seguimiento.Datos        repositorios con Dapper y Microsoft.Data.SqlClient, fábrica de conexiones
  tests/Seguimiento.Servicios.Tests      xUnit con repositorios falsos y FakeTimeProvider
  tests/Seguimiento.Integracion.Tests    xUnit con SQL Server real (Testcontainers) y los scripts de scripts/
web/                           Angular: core/ (servicios de API, interceptores, modelos), features/contactos/
scripts/                       scripts .sql numerados, aplicar.ps1 y aplicar.sh
```

- **Dapper** en lugar de EF Core: el SQL queda explícito y se puede defender línea por línea, y la transacción de la corrección se ve completa.
- **Configuración.** La cadena de conexión viene de la variable de entorno `ConnectionStrings__Seguimiento`. El repositorio incluye `.env.example`, y no se sube ningún secreto.
- **Scripts.** Se aplican con el `sqlcmd` que trae el contenedor de SQL Server y se registran en una tabla `VersionEsquema`, así que un script ya aplicado no se vuelve a ejecutar.

### 4.7 Pruebas atadas a los criterios

| Prueba | Nivel | CA |
|---|---|---|
| `CA2_RegistrarContacto_PacienteActivo_QuedaAsociadoConFechaCanalYResultado` | Unidad | CA-2 |
| `CA2_RegistrarContacto_CreaVersionUnoSinMotivo` | Unidad | CA-2 |
| `CA2_RegistrarContacto_FechaFutura_RechazaPorReglaDeNegocio` | Unidad | CA-2 |
| `CA2_RegistrarContacto_FechaAnteriorAlIngreso_RechazaPorReglaDeNegocio` | Unidad | CA-2 |
| `CA2_RegistrarContacto_PacienteInexistente_LanzaNoEncontrado` | Unidad | CA-2 |
| `CA2_RegistrarContacto_PacienteInactivo_RechazaPorReglaDeNegocio` | Unidad | CA-2 |
| `CA3_Corregir_CreaVersionNuevaYConservaLaAnterior` | Unidad | CA-3 |
| `CA3_Corregir_SinMotivo_LanzaValidacion` | Unidad | CA-3 |
| `CA3_Corregir_VersionDesactualizada_LanzaConflicto` | Unidad | CA-3 |
| `CA3_Corregir_SinCambios_RechazaPorReglaDeNegocio` | Unidad | CA-3 |
| `CA3_Corregir_RegistraQuienCorrigeSinCambiarElAutorOriginal` | Unidad | CA-3 |
| `CA3_VistaDelMes_MuestraLosValoresCorregidos` | Integración | CA-3 |
| `CA3_BaseDeDatos_RechazaUpdateYDeleteSobreVersiones` | Integración | CA-3 |
| `CA3_ProyeccionCoincideConLaVersionVigente` | Integración | CA-3 |
| `CA4_FiltroGestorYCiudad_SoloDevuelveContactosQueCumplenAmbos` | Integración | CA-4 |
| `CA4_SinFiltros_DevuelveTodosLosContactosDelMes` | Integración | CA-4 |
| `CA4_ContactoDeOtroMes_NoAparece` | Integración | CA-4 |
| `CA4_ContactoUltimaNocheDelMesEnHoraLocal_QuedaEnEseMes` | Integración | CA-4 (H-13) |
| `CA4_MesInvalido_LanzaValidacion` | Unidad | CA-4 |
| `CA3_FormularioCorreccion_Conflicto409_MuestraAviso` (opcional) | Interfaz | CA-3 |

El filtro de CA-4 vive en SQL. Una prueba de CA-4 con un repositorio falso probaría el repositorio falso, no el filtro. Por eso las pruebas de CA-4 son de integración contra SQL Server real.

---

## 5. Secuencia de trabajo

| # | Tarea | Tiempo |
|---|---|---|
| 1 | Hallazgos | 40 min |
| 2 | Este plan | 40 min |
| 3 | Base del repositorio y SQL Server en Docker | 15 min |
| 4 | Esquema versionado: catálogos, paciente, contacto con historial, triggers, índices y ejecutores de scripts | 30 min |
| 5 | Datos de prueba con **fechas relativas a hoy** (detalle debajo de la tabla) | 20 min |
| 6 | API base: capas, catálogos y manejo de errores | 25 min |
| 7 | CA-2: registro de contacto, primero las pruebas | 30 min |
| 8 | CA-3: corrección versionada con concurrencia optimista | 35 min |
| 9 | CA-4: consulta del mes, paginación y pruebas de integración | 35 min |
| 10 | Web base: servicios de API, interceptores y «Actuando como» | 20 min |
| 11 | Web: las tres pantallas y los errores de punta a punta | 45 min |
| 12 | README y prueba de un clon limpio en otra carpeta | 15 min |
| 13 | Bitácora: matriz de trazabilidad y decisiones | 15 min |
| | **Total** | **≈ 6 h** |

Los datos de prueba (tarea 5) incluyen:

- 3 países, 9 ciudades y 5 gestores, más 1 gestor inactivo.
- 40 pacientes, de los cuales 5 están inactivos.
- Unos 200 contactos repartidos entre el mes actual y el anterior, 10 de ellos corregidos una o dos veces.
- Casos preparados para la siguiente iteración: rachas de `NO_CONTESTA` (CA-5) y pacientes que ingresan o se retiran a mitad de mes (CA-6).

Las fechas son relativas a hoy para que la vista del mes en curso nunca aparezca vacía, sin importar el día en que se revise.

**Recorte si falta tiempo.** Si al terminar la tarea 9 llevo más de 4 horas 30 minutos, la interfaz de CA-4 se reduce a la lista con los dos filtros, sin paginación visible. Si aun así no alcanza, CA-4 sale del alcance y se entrega CA-2 y CA-3, con la lista del mes como superficie de CA-3. El recorte se anota en la sección 10.

---

## 6. Riesgos

| Riesgo | Probabilidad | Mitigación |
|---|---|---|
| El contenedor de SQL Server tarda en arrancar y los scripts fallan | Media | `aplicar.*` reintenta `SELECT 1` hasta 60 segundos antes de ejecutar los scripts. `docker-compose.yml` tiene una verificación de salud. |
| Testcontainers falla o va lento en la máquina del evaluador | Media | Las pruebas de integración aceptan una cadena de conexión por variable de entorno, para usar la base de Docker Compose en su lugar. Las pruebas unitarias no necesitan Docker. El README explica los dos caminos. |
| El evaluador no tiene exactamente el SDK de .NET 8 | Media | `global.json` con `rollForward: latestMajor` y proyectos con `net8.0`. El README pide «SDK .NET 8 o superior». |
| Diferencias de fecha por zona horaria en las pruebas | Media | `TimeProvider` inyectado en el servicio, y una prueba explícita del borde de fin de mes (H-13). |
| CORS o puertos entre Angular y la API | Baja | `proxy.conf.json` en Angular y puertos fijos en `launchSettings.json`. |
| Exceso de alcance: la tentación de implementar CA-5 o CA-6 | Media | Pasar de tres criterios resta. Se quedan como diseño (sección 8), sin excepciones. |
| Aceptar código de un asistente sin entenderlo | Media | Reviso cada paso antes de su commit. Lo que no puedo explicar se reescribe o se elimina, y los rechazos se anotan en la bitácora. |
| Me paso de tiempo | Media | Recorte definido en la sección 5. |

---

## 7. Extensión móvil

**Qué guardo en el dispositivo.** Solo lo que el gestor necesita para trabajar sin señal:

- Los catálogos.
- Una ficha mínima de **sus** pacientes activos: nombre, teléfono, ciudad y últimos contactos.
- Una bandeja de salida con los contactos y las correcciones que todavía no se han enviado.

Son datos de salud, así que van cifrados en reposo, con el almacenamiento seguro del sistema operativo o SQLite cifrado. Caducan si el dispositivo no se sincroniza en un plazo configurable y se pueden borrar a distancia. Nunca se guardan los pacientes de otros gestores.

Cada operación de la bandeja lleva un identificador único generado en el teléfono, que funciona como clave de idempotencia. Las correcciones llevan además la versión sobre la que se hicieron.

**Cuándo sincronizo.**

- Al recuperar la conexión, al abrir la aplicación y cuando el gestor lo pide.
- Primero se **envía** la bandeja, en orden. Después se **reciben** los cambios desde la última marca de sincronización.
- Un envío que se repite por una red inestable no duplica nada, gracias al identificador único.
- La fecha del contacto es la hora en que ocurrió en el teléfono, con su desfase horario, no la hora en que se sincronizó (H-13). El servidor guarda además la hora en que lo recibió, y así se ven los registros tardíos.

**Qué hago si el mismo registro cambió en el servidor.**

- **Un contacto nuevo nunca genera conflicto:** solo se agrega.
- **Una corrección sí puede generarlo.** Si se hizo sobre la versión 2 y en el servidor ya existe la versión 3, el servidor responde 409, igual que en la web. En ese caso:
  - No aplico «el último que escribe gana» en silencio, porque en un entorno regulado eso es sobrescribir sin rastro.
  - El teléfono marca la corrección como «en conflicto» y muestra las dos versiones.
  - El gestor decide si la reaplica sobre la versión vigente, con su motivo, o si la descarta.
- **Nada se pierde:** lo que ya estaba en el servidor está en el historial, y lo que se descarta en el teléfono queda registrado como descartado.

---

## 8. Diseño de los criterios fuera de alcance

Todo lo que sigue está resuelto a nivel de diseño, para implementarlo en la siguiente iteración. No se implementa en esta entrega.

### 8.1 CA-1: registro de pacientes (unas 1,5 horas)

- **Endpoint `POST /api/pacientes`.** Recibe `{ paisCodigo, tipoDocumento, numeroDocumento, nombre, telefono, email?, ciudadId, fechaInicioTratamiento, fechaIngresoPrograma?, consentimiento: { aceptado, versionTexto } }` y responde 201.
- **Reglas:**
  - El teléfono es obligatorio, en formato E.164, y su prefijo tiene que coincidir con el país (+57, +51 o +593).
  - La ciudad y el tipo de documento tienen que pertenecer al país. Ya lo garantizan las FK compuestas.
  - El consentimiento tiene que estar aceptado (422). Se guarda en `PacienteConsentimiento` (paciente, versión del texto, canal, fecha de otorgamiento, fecha de revocación y quién lo registró) (H-09).
  - Un documento duplicado devuelve **409 `PACIENTE_YA_REGISTRADO`**, con el identificador del paciente que ya existe.
- **«Disponible para agendar»:** el paciente se crea como `ACTIVO` y aparece en `GET /api/pacientes?buscar=`.
- **Corrección de datos del paciente:** mismo patrón que los contactos, con una tabla `PacienteVersion` y motivo obligatorio.
- **Autorregistro (H-03):** una tabla `SolicitudInscripcion` (nombre, correo, estado `PENDIENTE`, `CONVERTIDA` o `DESCARTADA`, y el paciente creado, si lo hay). El endpoint es público, con límite de peticiones y captcha. El gestor la convierte en paciente completando los datos que faltan. No cuenta en ningún indicador mientras esté pendiente.
- **Pruebas:**
  - `CA1_RegistrarPaciente_QuedaActivoYDisponibleParaContactos`
  - `CA1_RegistrarPaciente_SinTelefono_LanzaValidacion`
  - `CA1_RegistrarPaciente_DocumentoDuplicado_LanzaConflicto`
  - `CA1_RegistrarPaciente_CiudadDeOtroPais_LanzaValidacion`

### 8.2 CA-5: paciente ilocalizable (unas 1,5 horas)

- **Regla (H-08).** Al cierre del mes, un paciente es ilocalizable si **los tres días más recientes en que se le intentó contactar son días sin respuesta**. Un día sin respuesta es uno en que todos los intentos, en cualquier canal, fueron `NO_CONTESTA`. Tres llamadas el mismo día cuentan como un solo día. Cualquier otro resultado corta la racha.
- **Se calcula, no se guarda.** Así, una corrección de `NO_CONTESTA` a `EFECTIVO` quita la marca sola. El paciente **sigue activo**.
- **Consulta:**

```sql
WITH dias AS (
    SELECT c.PacienteId,
           CAST(SWITCHOFFSET(c.FechaContacto, '-05:00') AS DATE) AS Dia,
           MIN(CASE WHEN c.ResultadoCodigo = 'NO_CONTESTA' THEN 1 ELSE 0 END) AS SinRespuesta
    FROM Contacto c
    WHERE c.FechaContacto < @finDeMesExclusivo
    GROUP BY c.PacienteId, CAST(SWITCHOFFSET(c.FechaContacto, '-05:00') AS DATE)
), ultimos AS (
    SELECT PacienteId, SinRespuesta,
           ROW_NUMBER() OVER (PARTITION BY PacienteId ORDER BY Dia DESC) AS n
    FROM dias
)
SELECT PacienteId
FROM ultimos
WHERE n <= 3
GROUP BY PacienteId
HAVING COUNT(*) = 3 AND MIN(SinRespuesta) = 1;
```

  El índice `IX_Contacto_PacienteId_FechaContacto` ya existe en esta entrega.
- **Dónde se muestra:** la marca aparece en el reporte mensual (sección 8.3), en `GET /api/reportes/mensual?mes=`.
- **Pregunta abierta al PO:** hoy `NUMERO_EQUIVOCADO` corta la racha, aunque probablemente merece un aviso propio de «dato de contacto inválido».
- **Pruebas:**
  - `CA5_TresDiasSinRespuesta_MarcaIlocalizable`
  - `CA5_TresNoContestaElMismoDia_NoMarca`
  - `CA5_EfectivoEntreMedio_CortaLaRacha`
  - `CA5_CorreccionDeNoContestaAEfectivo_QuitaLaMarca`
  - `CA5_IntentosPosterioresAlCierre_NoCuentan`

### 8.3 CA-6: cobertura de seguimiento mensual (unas 2,5 horas)

- **Fórmula (H-06).**
  - Denominador: pacientes activos al último día del mes.
  - Numerador: los de ese mismo grupo que tuvieron al menos un contacto `EFECTIVO` en el mes, en hora local UTC−5.
  - Como el numerador sale del denominador, **el porcentaje nunca pasa del 100 %**.
  - Si no hay pacientes activos, el porcentaje es nulo, no se divide por cero.
  - El reporte muestra aparte los ingresos del mes y los ilocalizables.
  - Se rotula como «cobertura de seguimiento», no como adherencia terapéutica.
- **Plazo previsto (H-05).** Mientras no llegue el calendario, es el mes calendario. La regla vive en una única clase, para cambiarla cuando llegue el calendario real.
- **Historial de estados (H-04).** Una tabla `PacienteEstadoHistorial` con paciente, estado, `VigenteDesde`, `VigenteHasta` (nulo si está vigente), motivo, quién lo registró y cuándo.
  - Un índice único filtrado (`WHERE VigenteHasta IS NULL`) garantiza un solo estado vigente por paciente.
  - `Paciente.Estado` se mantiene como proyección, con el mismo patrón que los contactos.
  - El script de migración carga el historial inicial a partir de `Estado` y `FechaIngresoPrograma`.
  - Activo al corte: `Estado = 'ACTIVO' AND VigenteDesde <= @corte AND (VigenteHasta IS NULL OR VigenteHasta > @corte)`.
- **Foto inmutable del reporte (H-02).** Generar el reporte guarda una fila en `ReporteMensual` con el mes, cuándo se generó, quién lo generó, numerador, denominador, porcentaje, ingresos del mes, ilocalizables y versión de la regla. El detalle por paciente va en `ReporteMensualDetalle`.
  - Volver a generar el reporte **crea otra foto**, nunca sobrescribe la anterior.
  - Así se conserva lo que se entregó al laboratorio y se ve qué cambió después por las correcciones.
- **Endpoints:**
  - `POST /api/reportes/mensual { mes }` responde 201 con la foto. Si el mes todavía no ha terminado, responde 422 `MES_NO_CERRADO`.
  - `GET /api/reportes/mensual?mes=` devuelve las fotos del mes.
- **Pruebas:**
  - `CA6_PacienteRetiradoAntesDelCierre_NoCuentaEnNingunLado`
  - `CA6_ActivoSinContactoEfectivo_SoloCuentaEnDenominador`
  - `CA6_NoContesta_NoCuentaComoContactado`
  - `CA6_PorcentajeNuncaSuperaCien`
  - `CA6_SinActivos_PorcentajeNulo`
  - `CA6_RegenerarReporte_ConservaLaFotoAnterior`
  - `CA6_CorreccionPosterior_SeReflejaEnLaNuevaFoto` (CA-3 aplicado al reporte)

---

## 9. Trazabilidad prevista

| CA | Endpoints | Pantalla | Tablas | Pruebas |
|---|---|---|---|---|
| CA-2 | `POST /api/contactos`, `GET /api/pacientes` | `/contactos/nuevo` | `Contacto`, `ContactoVersion`, `Paciente` | `CA2_*` |
| CA-3 | `POST /api/contactos/{id}/correcciones`, `GET /api/contactos/{id}` | `/contactos/:id` | `Contacto`, `ContactoVersion` | `CA3_*` |
| CA-4 | `GET /api/contactos?mes=` | `/contactos` | `Contacto`, `Paciente`, `Ciudad`, `Gestor` | `CA4_*` |

La matriz definitiva, con commits y el estado de cada criterio, va en `03-bitacora.md`.

---

## 10. Cambios al plan

Cada desvío posterior al commit del plan se registra aquí con la fecha, qué cambió y por qué.

| Fecha | Qué cambió | Por qué |
|---|---|---|
| 2026-09-21 | La sección 5 conserva el orden de las tareas y el tiempo de cada una, pero ya no lista el mensaje de commit ni los archivos de cada paso. | Ese detalle es una métrica interna de ejecución, no una decisión de plan. La evidencia de cómo se ejecutó está en el historial de commits. |
| 2026-09-21 | Además de los scripts `001` a `006`, se agregó `000_base_de_datos.sql`, que crea la base de datos y la tabla `VersionEsquema`. | El ejecutor necesita la tabla de registro antes de decidir qué scripts aplicar. `000` es idempotente, se ejecuta siempre y no se registra. |
| 2026-09-21 | La restricción del teléfono es más estricta que la de la sección 3.2: «+» seguido de 7 a 15 dígitos, sin espacios ni otros caracteres, y el primer dígito no puede ser 0. | `LIKE '+[0-9]%'` aceptaba valores como «+57 300 123». La regla nueva es la de E.164. |

