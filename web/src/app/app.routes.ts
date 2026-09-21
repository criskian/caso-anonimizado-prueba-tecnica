import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'contactos' },
  {
    path: 'contactos',
    title: 'Contactos del mes',
    loadComponent: () =>
      import('./features/contactos/contactos-del-mes').then((m) => m.ContactosDelMesPagina),
  },
  {
    path: 'contactos/nuevo',
    title: 'Registrar contacto',
    loadComponent: () =>
      import('./features/contactos/nuevo-contacto').then((m) => m.NuevoContactoPagina),
  },
  {
    path: 'contactos/:id',
    title: 'Detalle del contacto',
    loadComponent: () =>
      import('./features/contactos/detalle-contacto').then((m) => m.DetalleContactoPagina),
  },
  { path: '**', redirectTo: 'contactos' },
];
