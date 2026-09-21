<#
.SYNOPSIS
    Aplica los scripts .sql numerados de esta carpeta sobre el SQL Server de docker-compose.

.DESCRIPTION
    Usa el sqlcmd que trae el contenedor, así que no hace falta instalarlo en la máquina.
    000_base_de_datos.sql se ejecuta siempre (es idempotente). Los demás scripts se registran
    en dbo.VersionEsquema al aplicarse y no se vuelven a ejecutar. Los scripts 9xx son datos
    de prueba y solo se aplican con -ConDatosPrueba.

.PARAMETER ConDatosPrueba
    También aplica los scripts 9xx (datos de prueba).

.PARAMETER Recrear
    Borra la base de datos Seguimiento antes de aplicar. Se pierden todos los datos.

.EXAMPLE
    ./scripts/aplicar.ps1 -ConDatosPrueba
#>
[CmdletBinding()]
param(
    [switch]$ConDatosPrueba,
    [switch]$Recrear
)

$raiz = Split-Path -Parent $PSScriptRoot
$archivoEnv = Join-Path $raiz '.env'
if (-not (Test-Path $archivoEnv)) {
    Write-Error 'No existe el archivo .env. Copia .env.example como .env (ver README).'
    exit 1
}

$lineaClave = Get-Content $archivoEnv | Where-Object { $_ -match '^\s*MSSQL_SA_PASSWORD=' } | Select-Object -First 1
if (-not $lineaClave) {
    Write-Error 'El archivo .env no define MSSQL_SA_PASSWORD.'
    exit 1
}
$clave = ($lineaClave -replace '^\s*MSSQL_SA_PASSWORD=', '').Trim()

# Ejecuta sqlcmd dentro del contenedor. La contraseña viaja como variable de entorno
# (SQLCMDPASSWORD) y no como argumento, para que no aparezca en la lista de procesos.
function Invoke-SqlcmdContenedor {
    param([string[]]$Argumentos)
    & docker compose --project-directory $raiz exec -T -e "SQLCMDPASSWORD=$clave" sqlserver `
        /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -b -f 65001 @Argumentos
}

Write-Host 'Esperando a que SQL Server acepte conexiones...'
$listo = $false
for ($intento = 1; $intento -le 30; $intento++) {
    Invoke-SqlcmdContenedor -Argumentos @('-Q', 'SELECT 1') *> $null
    if ($LASTEXITCODE -eq 0) { $listo = $true; break }
    Start-Sleep -Seconds 2
}
if (-not $listo) {
    Write-Error 'SQL Server no respondió en 60 segundos. ¿Ejecutaste "docker compose up -d"?'
    exit 1
}

if ($Recrear) {
    Write-Host 'Borrando la base de datos Seguimiento...'
    Invoke-SqlcmdContenedor -Argumentos @('-Q', "IF DB_ID(N'Seguimiento') IS NOT NULL BEGIN ALTER DATABASE Seguimiento SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE Seguimiento; END")
    if ($LASTEXITCODE -ne 0) { Write-Error 'No se pudo borrar la base de datos.'; exit 1 }
}

$scripts = Get-ChildItem -Path $PSScriptRoot -Filter '*.sql' |
    Where-Object { $_.Name -match '^\d{3}_' } |
    Sort-Object Name

foreach ($script in $scripts) {
    $nombre = $script.Name

    if ($nombre -like '9*' -and -not $ConDatosPrueba) {
        Write-Host "  omitido   $nombre (datos de prueba; usa -ConDatosPrueba)"
        continue
    }

    if ($nombre -notlike '000_*') {
        $conteo = Invoke-SqlcmdContenedor -Argumentos @('-d', 'Seguimiento', '-h', '-1', '-W', '-Q',
            "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.VersionEsquema WHERE Script = '$nombre';")
        if ($LASTEXITCODE -ne 0) { Write-Error "No se pudo consultar VersionEsquema antes de $nombre."; exit 1 }
        if (($conteo | Out-String).Trim() -ne '0') {
            Write-Host "  ya estaba $nombre"
            continue
        }
    }

    # 000 crea la base de datos, así que se ejecuta contra master; el resto, contra Seguimiento.
    $baseDatos = if ($nombre -like '000_*') { 'master' } else { 'Seguimiento' }
    Invoke-SqlcmdContenedor -Argumentos @('-d', $baseDatos, '-i', "/scripts/$nombre")
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falló $nombre. La transacción del script se revirtió; corrige el error y vuelve a ejecutar."
        exit 1
    }
    Write-Host "  aplicado  $nombre"
}

Write-Host 'Esquema al día.'
