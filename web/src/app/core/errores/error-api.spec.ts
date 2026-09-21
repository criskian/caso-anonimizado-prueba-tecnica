import { HttpErrorResponse } from '@angular/common/http';
import { aErrorApi, esErrorApi } from './error-api';

describe('aErrorApi', () => {
  it('convierte un ProblemDetails de validación con sus errores por campo', () => {
    const error = aErrorApi(
      new HttpErrorResponse({
        status: 400,
        error: {
          title: 'Datos inválidos',
          detail: 'Uno o más datos no son válidos.',
          codigo: 'VALIDACION',
          errors: { motivo: ['Explica el motivo de la corrección.'] },
        },
      }),
    );

    expect(error).toEqual({
      estado: 400,
      codigo: 'VALIDACION',
      titulo: 'Datos inválidos',
      detalle: 'Uno o más datos no son válidos.',
      errores: { motivo: ['Explica el motivo de la corrección.'] },
    });
    expect(esErrorApi(error)).toBe(true);
  });

  it('conserva el código de un conflicto de versión', () => {
    const error = aErrorApi(
      new HttpErrorResponse({
        status: 409,
        error: { codigo: 'VERSION_DESACTUALIZADA', detail: 'Otro usuario corrigió.' },
      }),
    );

    expect(error.codigo).toBe('VERSION_DESACTUALIZADA');
    expect(error.errores).toEqual({});
  });

  it('distingue la falta de conexión con el servidor', () => {
    expect(aErrorApi(new HttpErrorResponse({ status: 0 })).codigo).toBe('SIN_CONEXION');
  });

  it('usa un mensaje genérico si la respuesta no es ProblemDetails', () => {
    const error = aErrorApi(
      new HttpErrorResponse({ status: 502, error: '<html>Bad Gateway</html>' }),
    );

    expect(error.estado).toBe(502);
    expect(error.codigo).toBe('ERROR_INESPERADO');
  });
});
