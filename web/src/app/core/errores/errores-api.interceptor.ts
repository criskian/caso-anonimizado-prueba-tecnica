import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { aErrorApi } from './error-api';

/** Convierte toda respuesta de error de la API en un ErrorApi antes de que llegue a los servicios. */
export const erroresApiInterceptor: HttpInterceptorFn = (peticion, siguiente) =>
  siguiente(peticion).pipe(
    catchError((error: unknown) =>
      throwError(() => (error instanceof HttpErrorResponse ? aErrorApi(error) : error)),
    ),
  );
