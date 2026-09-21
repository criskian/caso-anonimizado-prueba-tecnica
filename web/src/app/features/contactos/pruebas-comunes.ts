import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { erroresApiInterceptor } from '../../core/errores/errores-api.interceptor';
import { Catalogos, ContactoDetalle } from '../../core/modelos/api';
import { gestorActualInterceptor } from '../../core/sesion/gestor-actual.interceptor';

// Utilidades compartidas por las pruebas de las pantallas de contactos.

export const CATALOGOS: Catalogos = {
  gestores: [
    { id: 1, nombre: 'Laura Méndez' },
    { id: 2, nombre: 'Andrés Quintero' },
  ],
  ciudades: [
    { id: 1, nombre: 'Bogotá', paisCodigo: 'CO' },
    { id: 4, nombre: 'Lima', paisCodigo: 'PE' },
  ],
  canales: [
    { codigo: 'LLAMADA', nombre: 'Llamada' },
    { codigo: 'WHATSAPP', nombre: 'WhatsApp' },
  ],
  resultados: [
    { codigo: 'EFECTIVO', nombre: 'Contacto efectivo' },
    { codigo: 'NO_CONTESTA', nombre: 'No contesta' },
  ],
};

export function contactoDetalle(version = 1): ContactoDetalle {
  const original = {
    numeroVersion: 1,
    fechaContacto: '2026-09-20T10:00:00-05:00',
    canal: { codigo: 'LLAMADA', nombre: 'Llamada' },
    resultado: { codigo: 'NO_CONTESTA', nombre: 'No contesta' },
    observacion: null,
    motivoCorreccion: null,
    registradoPor: { id: 1, nombre: 'Laura Méndez' },
    registradoEnUtc: '2026-09-20T15:05:00Z',
  };
  const historial = [original];
  if (version > 1) {
    historial.push({
      ...original,
      numeroVersion: 2,
      resultado: { codigo: 'EFECTIVO', nombre: 'Contacto efectivo' },
      motivoCorreccion: 'El paciente sí contestó.' as never,
      registradoPor: { id: 2, nombre: 'Andrés Quintero' },
      registradoEnUtc: '2026-09-21T15:00:00Z',
    });
  }

  const vigente = historial[historial.length - 1];
  return {
    id: 7,
    paciente: { id: 3, nombre: 'Ana García', ciudad: 'Quito' },
    gestor: { id: 1, nombre: 'Laura Méndez' },
    fechaContacto: vigente.fechaContacto,
    canal: vigente.canal,
    resultado: vigente.resultado,
    observacion: vigente.observacion,
    versionActual: version,
    historial,
  };
}

export function configurar(): HttpTestingController {
  localStorage.clear();
  TestBed.configureTestingModule({
    providers: [
      provideRouter([]),
      provideHttpClient(withInterceptors([gestorActualInterceptor, erroresApiInterceptor])),
      provideHttpClientTesting(),
    ],
  });
  return TestBed.inject(HttpTestingController);
}

/** Deja que el componente procese las respuestas simuladas y vuelva a pintarse. */
export async function estabilizar<T>(fixture: ComponentFixture<T>): Promise<void> {
  await fixture.whenStable();
  fixture.detectChanges();
}

export function escribir(elemento: HTMLElement, selector: string, valor: string): void {
  const campo = elemento.querySelector<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>(
    selector,
  )!;
  campo.value = valor;
  campo.dispatchEvent(new Event(campo instanceof HTMLSelectElement ? 'change' : 'input'));
}

/** Elige en un <select> la opción cuyo texto visible contiene «texto», como haría un usuario. */
export function elegirOpcion(elemento: HTMLElement, selector: string, texto: string): void {
  const lista = elemento.querySelector<HTMLSelectElement>(selector)!;
  const indice = Array.from(lista.options).findIndex((o) => o.textContent?.includes(texto));
  if (indice < 0) {
    throw new Error(`No hay una opción «${texto}» en ${selector}.`);
  }
  lista.selectedIndex = indice;
  lista.dispatchEvent(new Event('change'));
}
