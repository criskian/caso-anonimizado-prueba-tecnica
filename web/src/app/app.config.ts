import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import { routes } from './app.routes';
import { erroresApiInterceptor } from './core/errores/errores-api.interceptor';
import { gestorActualInterceptor } from './core/sesion/gestor-actual.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // Los parámetros de ruta y de consulta llegan a los componentes como input().
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([gestorActualInterceptor, erroresApiInterceptor])),
  ],
};
