// Fechas en hora del programa (H-13): Colombia, Perú y Ecuador continental están en UTC−5
// todo el año. La interfaz muestra y captura siempre en esa hora, sin importar la zona
// horaria del navegador, y envía a la API el desfase explícito.

const DESFASE_MINUTOS = -5 * 60;
const DESFASE_TEXTO = '-05:00';

/** Convierte el valor de un <input type="datetime-local"> ("2026-09-21T10:30") a ISO con desfase. */
export function aFechaPrograma(valorLocal: string): string {
  return `${valorLocal.slice(0, 16)}:00${DESFASE_TEXTO}`;
}

/** Convierte una fecha ISO de la API al formato de <input type="datetime-local">, en hora del programa. */
export function aValorLocal(iso: string): string {
  return enHoraPrograma(new Date(iso)).toISOString().slice(0, 16);
}

/** Momento actual en hora del programa, en formato de <input type="datetime-local">. */
export function ahoraLocal(ahora: Date = new Date()): string {
  return enHoraPrograma(ahora).toISOString().slice(0, 16);
}

/** Mes en curso en hora del programa, en formato AAAA-MM. */
export function mesActual(ahora: Date = new Date()): string {
  return enHoraPrograma(ahora).toISOString().slice(0, 7);
}

/** Texto para mostrar: "21/09/2026 10:30", en hora del programa. */
export function formatearFecha(iso: string): string {
  const [fecha, hora] = aValorLocal(iso).split('T');
  const [anio, mes, dia] = fecha.split('-');
  return `${dia}/${mes}/${anio} ${hora}`;
}

// Desplaza el instante para que sus campos UTC muestren la hora del programa.
function enHoraPrograma(instante: Date): Date {
  return new Date(instante.getTime() + DESFASE_MINUTOS * 60_000);
}
