import { Injectable, signal } from '@angular/core';

const CLAVE = 'seguimiento.gestorActual';

/**
 * Gestor con el que se está trabajando («Actuando como»). Es una simulación declarada, no
 * autenticación (H-11): cualquiera puede elegir cualquier gestor. Se recuerda en el navegador
 * solo por comodidad; si el almacenamiento no está disponible, la aplicación sigue funcionando.
 */
@Injectable({ providedIn: 'root' })
export class GestorActualService {
  private readonly id = signal<number | null>(leer());

  readonly gestorId = this.id.asReadonly();

  seleccionar(gestorId: number | null): void {
    this.id.set(gestorId);
    try {
      if (gestorId === null) {
        localStorage.removeItem(CLAVE);
      } else {
        localStorage.setItem(CLAVE, String(gestorId));
      }
    } catch {
      // Navegación privada o almacenamiento bloqueado: la selección dura solo esta sesión.
    }
  }
}

function leer(): number | null {
  try {
    const valor = Number(localStorage.getItem(CLAVE));
    return Number.isInteger(valor) && valor > 0 ? valor : null;
  } catch {
    return null;
  }
}
