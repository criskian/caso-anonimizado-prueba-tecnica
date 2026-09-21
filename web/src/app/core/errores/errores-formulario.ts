import { FormGroup } from '@angular/forms';
import { ErrorApi } from './error-api';

// Errores de regla de negocio (422) que pertenecen a un campo concreto del formulario.
const CAMPO_POR_CODIGO: Record<string, string> = {
  FECHA_FUTURA: 'fechaContacto',
  FECHA_ANTERIOR_AL_INGRESO: 'fechaContacto',
};

/**
 * Coloca los errores del servidor junto a su campo, con la clave «servidor». Devuelve true si
 * todos los errores quedaron asociados a un campo; si alguno no, la pantalla debe mostrar el
 * error general.
 */
export function aplicarErroresDelServidor(formulario: FormGroup, error: ErrorApi): boolean {
  const porCampo: Record<string, string[]> = { ...error.errores };
  const campoDelCodigo = CAMPO_POR_CODIGO[error.codigo];
  if (campoDelCodigo) {
    porCampo[campoDelCodigo] = [error.detalle];
  }

  const campos = Object.entries(porCampo);
  let todosAsociados = campos.length > 0;
  for (const [campo, mensajes] of campos) {
    const control = formulario.get(campo);
    if (control) {
      control.setErrors({ ...control.errors, servidor: mensajes.join(' ') });
      control.markAsTouched();
    } else {
      todosAsociados = false;
    }
  }

  return todosAsociados;
}
