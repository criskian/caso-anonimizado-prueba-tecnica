# Seguimiento de pacientes: bitácora de contactos del mes

Prueba técnica de TBTB Global. Permite registrar los contactos de los gestores con los pacientes (CA-2), corregirlos sin perder el registro original (CA-3) y consultar los contactos del mes filtrando por gestor y ciudad (CA-4).

| Documento | Contenido |
|---|---|
| [01-hallazgos.md](01-hallazgos.md) | Lo que le falta o está mal en el PRD, con preguntas y supuestos |
| [02-plan.md](02-plan.md) | Alcance, modelo de datos, contrato de la API, riesgos y extensión móvil |
| [03-bitacora.md](03-bitacora.md) | Trazabilidad de cada criterio hasta su prueba, y decisiones y uso de IA |

## Requisitos

- [Git](https://git-scm.com/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (o Docker Engine con Docker Compose), en marcha
- [SDK de .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) o superior
- [Node.js](https://nodejs.org/) 20.19 o superior, 22.12 o superior, o 24 o superior, con npm

No hace falta instalar SQL Server ni `sqlcmd`: la base corre en Docker y los scripts usan el `sqlcmd` del contenedor.

## Puesta en marcha

Los comandos se ejecutan desde la raíz del repositorio. Donde hay diferencia se muestran para PowerShell (Windows) y para bash (Linux, macOS o Git Bash).

### 1. Clonar y configurar

```bash
git clone https://github.com/criskian/caso-anonimizado-prueba-tecnica.git
cd caso-anonimizado-prueba-tecnica
```

Copia el archivo de ejemplo de configuración. Trae una contraseña de desarrollo para SQL Server que puedes dejar o cambiar.

```powershell
# PowerShell
Copy-Item .env.example .env
```

```bash
# bash
cp .env.example .env
```

### 2. Levantar SQL Server

```bash
docker compose up -d
```

La primera vez descarga la imagen de SQL Server 2022 (unos 1,5 GB). El contenedor se llama `seguimiento-sqlserver` y publica el puerto **14330**.

### 3. Crear la base de datos con datos de prueba

```powershell
# PowerShell
powershell -ExecutionPolicy Bypass -File scripts/aplicar.ps1 -ConDatosPrueba
```

```bash
# bash
bash scripts/aplicar.sh --con-datos-prueba
```

El script espera a que SQL Server esté listo, crea la base `Seguimiento`, aplica los scripts numerados de `scripts/` y carga los datos de prueba: 6 gestores, 40 pacientes y unos 190 contactos del mes anterior y del actual. Las fechas se calculan desde el día en que se ejecuta, así que siempre hay datos en el mes en curso.

Si lo ejecutas otra vez no repite nada. Para empezar de cero, agrega `-Recrear` (PowerShell) o `--recrear` (bash).

### 4. Levantar la API

La cadena de conexión no está en el repositorio. Guárdala con *user-secrets*, usando la misma contraseña que quedó en `.env`. Si no la cambiaste, el comando sirve tal cual:

```bash
dotnet user-secrets set "ConnectionStrings:Seguimiento" "Server=localhost,14330;Database=Seguimiento;User Id=sa;Password=Cambia_Esta_Clave_2026!;TrustServerCertificate=True" --project api/src/Seguimiento.Api
```

```bash
dotnet run --project api/src/Seguimiento.Api
```

La API queda en <http://localhost:5080>. La documentación interactiva está en <http://localhost:5080/swagger>.

### 5. Levantar la interfaz

En otra terminal:

```bash
cd web
npm ci
npm start
```

Abre <http://localhost:4200>. La interfaz envía las llamadas a `/api` a la API mediante el proxy de desarrollo de Angular.

### 6. Usarla

1. Arriba a la derecha, en **Actuando como**, elige un gestor. Es una simulación: no hay autenticación (ver H-11 en los hallazgos).
2. **Contactos del mes**: filtra por mes, gestor y ciudad. Los contactos corregidos muestran la etiqueta «corregido vN».
3. **Registrar contacto**: busca un paciente por nombre o documento (por ejemplo, «García»), completa el formulario y regístralo.
4. En el detalle del contacto, **corrígelo**: cambia un dato, escribe el motivo y guarda. El historial conserva la versión original.

Para ver los errores de punta a punta:

- **Conflicto (409):** abre el mismo contacto en dos pestañas y corrígelo en las dos. La segunda muestra el aviso «Otro usuario corrigió este contacto» con el botón para recargar.
- **Regla de negocio junto al campo (422):** registra un contacto para «Jorge Castillo», que ingresó al programa este mes, con una fecha del mes pasado.

## Pruebas

```bash
# API: pruebas de servicio y de integración contra SQL Server real
dotnet test api/Seguimiento.sln

# Interfaz: pruebas de componentes, linter y formato
cd web
npm test
npm run lint
npm run format:check
```

Cada prueba lleva en el nombre el criterio que verifica (`CA2_…`, `CA3_…`, `CA4_…`).

Las pruebas de integración levantan su propio SQL Server con Testcontainers, que necesita Docker; la primera vez tardan cerca de un minuto. Si Testcontainers no funciona en tu máquina, pueden usar el SQL Server del paso 2: crean una base temporal con otro nombre y la borran al terminar.

```powershell
# PowerShell
$env:SEGUIMIENTO_PRUEBAS_SERVIDOR = "Server=localhost,14330;Database=master;User Id=sa;Password=Cambia_Esta_Clave_2026!;TrustServerCertificate=True"
dotnet test api/Seguimiento.sln
```

```bash
# bash
SEGUIMIENTO_PRUEBAS_SERVIDOR="Server=localhost,14330;Database=master;User Id=sa;Password=Cambia_Esta_Clave_2026!;TrustServerCertificate=True" dotnet test api/Seguimiento.sln
```

## Estructura

```
scripts/   Scripts SQL numerados (000 a 006: esquema; 900: datos de prueba) y los ejecutores aplicar.ps1 / aplicar.sh
api/       .NET 8: Seguimiento.Api (controladores y DTO), Seguimiento.Servicios (reglas),
           Seguimiento.Datos (Dapper) y dos proyectos de pruebas (servicio e integración)
web/       Angular 21: core/ (servicios de API, interceptores, modelos) y features/contactos/ (pantallas)
```

## Si algo falla

| Síntoma | Solución |
|---|---|
| El puerto 14330 está ocupado | Cambia `MSSQL_PORT` en `.env` y usa ese puerto en la cadena de conexión del paso 4. |
| `aplicar` dice que SQL Server no respondió | Revisa que Docker esté en marcha y que `docker compose ps` muestre el contenedor como *healthy*. |
| La API no arranca y pide la cadena de conexión | Falta el paso 4 (`dotnet user-secrets set …`). |
| `npm install` falla con «Cannot read properties of null (reading 'edgesOut')» | Es un error de npm 10.9.0. Usa `npm ci`, como indica el paso 5: instala desde `package-lock.json`. |
| PowerShell no deja ejecutar el script | Usa el comando del paso 3 tal cual: incluye `-ExecutionPolicy Bypass`. |
