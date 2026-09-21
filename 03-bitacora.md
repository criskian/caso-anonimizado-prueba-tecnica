# 03 · Bitácora

## Matriz de trazabilidad

| Criterio | Commits y archivos principales | Pruebas que lo verifican | Estado |
|---|---|---|---|
| **CA-2** Registrar un contacto con fecha, canal y resultado | `b20a9fb`, `59ba837`. `ContactoService.RegistrarAsync`, `ContactoRepositorio.RegistrarAsync`, `ContactosController.Registrar`, pantalla `nuevo-contacto` | `CA2_RegistroDeContactoTests` y `CA2_BusquedaDePacientesTests` (21 de servicio), `CA2_RegistrarContacto_CreaVersionUnoSinMotivo` (integración), `nuevo-contacto.spec.ts` (4 de interfaz) | Cubierto |
| **CA-3** Corregir un contacto y que el reporte lo refleje | `ef02cc8` (historial y triggers), `e4012f1`, `59ba837`. `ContactoService.CorregirAsync`, `ContactoRepositorio.CorregirAsync`, `scripts/003` y `004`, pantalla `detalle-contacto` | `CA3_CorreccionDeContactoTests` (15 de servicio), `CA3_CorreccionIntegracionTests` (7 de integración, entre ellas `CA3_VistaDelMes_MuestraLosValoresCorregidos`), `detalle-contacto.spec.ts` (4 de interfaz, entre ellas `CA3_FormularioCorreccion_Conflicto409_MuestraAviso`) | Cubierto |
| **CA-4** Filtrar los contactos del mes por gestor y ciudad | `ef02cc8` (índices), `76fd900`, `59ba837`. `ContactosDelMesService`, `ContactosDelMesRepositorio`, pantalla `contactos-del-mes` | `CA4_ContactosDelMesTests` (11 de servicio), `CA4_ContactosDelMesIntegracionTests` (5 de integración, entre ellas `CA4_FiltroGestorYCiudad_SoloDevuelveContactosQueCumplenAmbos`), `contactos-del-mes.spec.ts` (3 de interfaz) | Cubierto |
| CA-1 Registrar un paciente | Diseño en `02-plan.md` §8.1 | — | Fuera de alcance |
| CA-5 Paciente ilocalizable | Diseño en `02-plan.md` §8.2. El índice que necesita ya existe | — | Fuera de alcance |
| CA-6 Reporte de adherencia | Diseño en `02-plan.md` §8.3 | — | Fuera de alcance |

En CA-3 tomé el reporte como la vista de contactos del mes, porque el reporte de adherencia (CA-6) no entra en esta entrega. Eso lo verifica `CA3_VistaDelMes_MuestraLosValoresCorregidos`.

Todas las pruebas pasan: 60 en la API (47 de servicio y 13 de integración contra SQL Server real) y 31 en la interfaz. Además recorrí las tres pantallas en Chrome contra la API y la base reales, y seguí el README en un clon limpio del repositorio para comprobar que arranca.

## Uso de IA

Usé solo **Claude Code**, con los modelos **Opus** y **Sonnet**.

Primero hice mi propio análisis del enunciado y del PRD: saqué mis hallazgos y armé la idea general de la solución. Después usé la IA para:

- **Contrastar y redactar mejor mis hallazgos.** También me señaló algunos hallazgos menores que yo no había visto y que decidí incluir.
- **Redactar los documentos completos** (hallazgos, plan, README y esta bitácora) a partir de mis ideas y decisiones. Los revisé y ajusté.
- **Ejecutar el plan:** escribir el código, las pruebas y los scripts, y correr las verificaciones.

Cada decisión de abajo dice si salió de mí o si fue una propuesta de la IA que acepté, corregí o rechacé.

## Registro de decisiones

| # | Decisión | Por qué | Origen |
|---|---|---|---|
| 1 | Leer el PRD y armar mis hallazgos antes de usar la IA. | Quería que el análisis fuera mío y usar la IA para contrastarlo. | Mía |
| 2 | Priorizar pocos hallazgos que bloquean, y sumar los menores que me señaló la IA. | La prueba valora más cuatro que bloquean que quince cosméticos. | Mía; los hallazgos menores los propuso la IA |
| 3 | Implementar solo CA-2, CA-3 y CA-4, y dejar CA-1, CA-5 y CA-6 diseñados. | Al principio quería hacer los seis. CA-5 y CA-6 dependen de reglas sin confirmar, y CA-3 es el más delicado en farma. | Mía|
| 4 | Ilocalizable: tres días distintos sin respuesta, calculado y no guardado. | Así una corrección lo revierte sola. | La IA me dio opciones; elegí yo |
| 5 | Llamar "cobertura de seguimiento" al indicador de CA-6, con numerador dentro del denominador. | Así nunca pasa del 100 % y el laboratorio no lo confunde con adherencia al tratamiento. | elegí yo |
| 6 | Corregir es crear una versión nueva; nunca se sobrescribe ni se borra. | Es un entorno regulado: tiene que quedar quién cambió qué y por qué. | Idea mía. La forma de implementarlo (historial más triggers en la base) la propuso la IA y la acepté |
| 7 | Usar Dapper con scripts `.sql` en lugar de EF Core. | El SQL queda a la vista y puedo explicarlo línea por línea. | La IA me dio las dos opciones; elegí yo |
| 8 | **Corregí** los mensajes de commit: nada de «agrega…»; formato `feat`, `fix`, `docs` en español. | «Agrega» suena a que lo hizo otra persona. | Corrección mía a una propuesta de la IA |
| 9 | **Saqué del `02-plan.md`** el detalle de commits y archivos de la secuencia de trabajo. | Es una métrica interna. La IA me advirtió que la secuencia es obligatoria, así que quedó una versión corta con tareas y tiempos. | Mía, ajustada con la IA |
| 10 | **Simplifiqué** la sección «Cambios al plan», que la IA había llenado con demasiado detalle. | Solo deben quedar los cambios que importan para entender el plan. | Corrección mía |
| 11 | Probar todo lo que se implementa y no agregar nada fuera del plan. | Prefiero menos funcionalidad bien probada. | Mía |
| 12 | **Corregido:** la primera versión de los ejecutores de scripts creaba las tablas en `master` y rompía los acentos en PowerShell. | Lo detectaron las pruebas del esquema. La transacción se revirtió y no quedó nada creado. | Error de la IA, corregido |
| 13 | **Corregido:** cada error inesperado quedaba registrado dos veces en el log, y los errores de negocio salían como errores graves. | Ahora hay un solo punto que registra, y solo lo inesperado. | Error de la IA, corregido |
| 14 | **Corregido:** una validación mal puesta en el DTO hacía fallar todos los registros con un 500. | Las pruebas unitarias no lo veían; lo encontró la prueba de punta a punta. | Error de la IA, corregido |
| 15 | **Corregido:** las claves de los errores de validación no tenían el mismo formato en todos los casos (commit `fix`). | La interfaz necesita una sola convención para mostrar cada error junto a su campo. | Error de la IA, corregido |
| 16 | Usar Angular 21 y no 22. | Angular 22 exige una versión de Node más nueva de la que tengo; 21 funciona con más versiones, y es más probable que el README arranque en otra máquina. | Propuesta de la IA, aceptada |
| 17 | Documentar `npm ci` en el README. | `npm install` falla con npm 10.9.0 por un error de npm; `npm ci` instala desde el archivo de versiones y funciona. | Propuesta de la IA, aceptada |

## La consulta del mes (CA-4) y sus índices

La vista del mes combina contacto, paciente, ciudad, gestor, canal y resultado. Está en `ContactosDelMesRepositorio`. Tomé tres decisiones al escribirla:

- **Filtra por un rango de fechas** («desde el día 1 hasta antes del día 1 del mes siguiente», en hora UTC−5) y no con funciones sobre la fecha. Así la base puede usar el índice por fecha.
- **Primero filtra y corta la página** solo con contacto y paciente. Después busca los nombres de ciudad, gestor, canal y resultado solo para las 50 filas que se muestran.
- **Los filtros son opcionales**, así que la consulta se recompila en cada ejecución (`OPTION (RECOMPILE)`). Así la base arma un plan para los filtros que realmente llegaron, en lugar de reutilizar uno pensado para otra combinación.

Además, lee los valores vigentes de cada contacto, así que una corrección se ve de inmediato en la vista (CA-3).

Para no justificar los índices solo en teoría, los medí en una base aparte con el volumen que supuse en H-14: 5.000 pacientes y 240.000 contactos en un año (unos 20.000 por mes). Estas son las páginas leídas de la tabla de contactos para contar un mes:

| Filtro | Índice que usa | Páginas leídas |
|---|---|---|
| Sin índice (recorriendo toda la tabla) | — | 2.214 |
| Sin filtros | `IX_Contacto_FechaContacto` | 155 |
| Por gestor | `IX_Contacto_GestorId_FechaContacto` | 18 |
| Por gestor y ciudad | `IX_Contacto_FechaContacto` | 155 |
| Por gestor y ciudad, con el índice que recomiendo | `IX_Contacto_GestorId_FechaContacto` ampliado | 35 |

Con los dos filtros juntos, la base no usa el índice del gestor. Ese índice no incluye el paciente, y lo necesita para unir con la ciudad. **Con volumen real, ampliaría ese índice** para que incluya el paciente, el canal, el resultado y la versión (`INCLUDE (PacienteId, CanalCodigo, ResultadoCodigo, VersionActual)`). Así la base resuelve el filtro combinado leyendo casi cinco veces menos. No lo apliqué en esta entrega porque, con los 400 pacientes con los que arranca el programa, no hay diferencia.
