#!/usr/bin/env bash
# Aplica los scripts .sql numerados de esta carpeta sobre el SQL Server de docker-compose.
#
# Usa el sqlcmd que trae el contenedor, así que no hace falta instalarlo en la máquina.
# 000_base_de_datos.sql se ejecuta siempre (es idempotente). Los demás scripts se registran
# en dbo.VersionEsquema al aplicarse y no se vuelven a ejecutar. Los scripts 9xx son datos
# de prueba y solo se aplican con --con-datos-prueba.
#
# Uso: ./scripts/aplicar.sh [--con-datos-prueba] [--recrear]
set -euo pipefail

# En Git Bash para Windows, evita que las rutas del contenedor (/scripts/...) se conviertan
# en rutas de Windows.
export MSYS_NO_PATHCONV=1

con_datos_prueba=false
recrear=false
for argumento in "$@"; do
    case "$argumento" in
        --con-datos-prueba) con_datos_prueba=true ;;
        --recrear) recrear=true ;;
        *) echo "Opción desconocida: $argumento" >&2; exit 1 ;;
    esac
done

dir_scripts="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
raiz="$(dirname "$dir_scripts")"
# docker compose encuentra docker-compose.yml en el directorio actual. No se le pasa la ruta
# porque en Git Bash, con MSYS_NO_PATHCONV, llegaría a docker sin convertir y no la encontraría.
cd "$raiz"

if [[ ! -f "$raiz/.env" ]]; then
    echo "No existe el archivo .env. Copia .env.example como .env (ver README)." >&2
    exit 1
fi

clave="$(grep -E '^[[:space:]]*MSSQL_SA_PASSWORD=' "$raiz/.env" | head -n 1 | sed -E 's/^[[:space:]]*MSSQL_SA_PASSWORD=//' | tr -d '\r')"
if [[ -z "$clave" ]]; then
    echo "El archivo .env no define MSSQL_SA_PASSWORD." >&2
    exit 1
fi

# Ejecuta sqlcmd dentro del contenedor. La contraseña viaja como variable de entorno
# (SQLCMDPASSWORD) y no como argumento, para que no aparezca en la lista de procesos.
sqlcmd_contenedor() {
    docker compose exec -T -e "SQLCMDPASSWORD=$clave" sqlserver \
        /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -b -f 65001 "$@"
}

echo "Esperando a que SQL Server acepte conexiones..."
listo=false
for _ in $(seq 1 30); do
    if sqlcmd_contenedor -Q "SELECT 1" > /dev/null 2>&1; then
        listo=true
        break
    fi
    sleep 2
done
if [[ "$listo" != true ]]; then
    echo 'SQL Server no respondió en 60 segundos. ¿Ejecutaste "docker compose up -d"?' >&2
    exit 1
fi

if [[ "$recrear" == true ]]; then
    echo "Borrando la base de datos Seguimiento..."
    sqlcmd_contenedor -Q "IF DB_ID(N'Seguimiento') IS NOT NULL BEGIN ALTER DATABASE Seguimiento SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE Seguimiento; END"
fi

for ruta in $(ls "$dir_scripts"/[0-9][0-9][0-9]_*.sql | sort); do
    nombre="$(basename "$ruta")"

    if [[ "$nombre" == 9* && "$con_datos_prueba" != true ]]; then
        echo "  omitido   $nombre (datos de prueba; usa --con-datos-prueba)"
        continue
    fi

    if [[ "$nombre" != 000_* ]]; then
        conteo="$(sqlcmd_contenedor -d Seguimiento -h -1 -W \
            -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.VersionEsquema WHERE Script = '$nombre';" | tr -d '[:space:]')"
        if [[ "$conteo" != "0" ]]; then
            echo "  ya estaba $nombre"
            continue
        fi
    fi

    # 000 crea la base de datos, así que se ejecuta contra master; el resto, contra Seguimiento.
    base_datos="Seguimiento"
    [[ "$nombre" == 000_* ]] && base_datos="master"

    if ! sqlcmd_contenedor -d "$base_datos" -i "/scripts/$nombre"; then
        echo "Falló $nombre. La transacción del script se revirtió; corrige el error y vuelve a ejecutar." >&2
        exit 1
    fi
    echo "  aplicado  $nombre"
done

echo "Esquema al día."
