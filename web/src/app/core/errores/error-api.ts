import { HttpErrorResponse } from '@angular/common/http';

/**
 * Error de la API ya normalizado. Los componentes solo trabajan con este tipo: nunca con
 * HttpErrorResponse ni con el formato ProblemDetails (RFC 7807) que devuelve el servidor.
 */
export interface ErrorApi {
  estado: number;
  /** Código estable definido por la API, por ejemplo VERSION_DESACTUALIZADA. */
  codigo: string;
  titulo: string;
  detalle: string;
  /** Errores por campo (400), con la clave en camelCase igual que en el formulario. */
  errores: Record<string, string[]>;
}

interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  codigo?: string;
  errors?: Record<string, string[]>;
}

export function esErrorApi(valor: unknown): valor is ErrorApi {
  return typeof valor === 'object' && valor !== null && 'codigo' in valor && 'estado' in valor;
}

export function aErrorApi(respuesta: HttpErrorResponse): ErrorApi {
  if (respuesta.status === 0) {
    return {
      estado: 0,
      codigo: 'SIN_CONEXION',
      titulo: 'Sin conexión',
      detalle: 'No se pudo conectar con el servidor. Revisa que la API esté en marcha.',
      errores: {},
    };
  }

  const problema: ProblemDetails =
    typeof respuesta.error === 'object' && respuesta.error !== null ? respuesta.error : {};

  return {
    estado: respuesta.status,
    codigo: problema.codigo ?? 'ERROR_INESPERADO',
    titulo: problema.title ?? 'Error',
    detalle: problema.detail ?? 'Ocurrió un error inesperado. Intenta de nuevo más tarde.',
    errores: problema.errors ?? {},
  };
}
