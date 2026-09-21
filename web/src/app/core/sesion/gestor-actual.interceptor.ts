import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { GestorActualService } from './gestor-actual.service';

export const CABECERA_GESTOR = 'X-Gestor-Id';

/** Agrega a las llamadas a la API el gestor elegido en «Actuando como». */
export const gestorActualInterceptor: HttpInterceptorFn = (peticion, siguiente) => {
  const gestorId = inject(GestorActualService).gestorId();
  if (gestorId === null || !peticion.url.startsWith('/api/')) {
    return siguiente(peticion);
  }

  return siguiente(peticion.clone({ setHeaders: { [CABECERA_GESTOR]: String(gestorId) } }));
};
