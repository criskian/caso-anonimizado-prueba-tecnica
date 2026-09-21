import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { erroresApiInterceptor } from './core/errores/errores-api.interceptor';
import { gestorActualInterceptor } from './core/sesion/gestor-actual.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([gestorActualInterceptor, erroresApiInterceptor])),
  ],
};
