import { HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { vi } from 'vitest';
import { GestorActualService } from '../../core/sesion/gestor-actual.service';
import { NuevoContactoPagina } from './nuevo-contacto';
import {
  CATALOGOS,
  configurar,
  contactoDetalle,
  elegirOpcion,
  escribir,
  estabilizar,
} from './pruebas-comunes';

describe('NuevoContactoPagina (CA-2)', () => {
  let backend: HttpTestingController;
  let fixture: ComponentFixture<NuevoContactoPagina>;
  let elemento: HTMLElement;

  beforeEach(async () => {
    backend = configurar();
    TestBed.inject(GestorActualService).seleccionar(1);
    fixture = TestBed.createComponent(NuevoContactoPagina);
    fixture.detectChanges();
    backend.expectOne('/api/catalogos').flush(CATALOGOS);
    await estabilizar(fixture);
    elemento = fixture.nativeElement as HTMLElement;
  });

  afterEach(() => backend.verify());

  async function completarFormulario(): Promise<void> {
    vi.useFakeTimers();
    escribir(elemento, '#busqueda', 'Ana');
    await vi.advanceTimersByTimeAsync(300);
    vi.useRealTimers();
    backend
      .expectOne((p) => p.url === '/api/pacientes' && p.params.get('buscar') === 'Ana')
      .flush([{ id: 3, nombre: 'Ana García', documento: 'CI 1700015838', ciudad: 'Quito' }]);
    await estabilizar(fixture);

    elemento.querySelector<HTMLButtonElement>('.resultados-busqueda button')!.click();
    await estabilizar(fixture);
    escribir(elemento, '#fecha', '2026-09-20T10:30');
    elegirOpcion(elemento, '#canal', 'Llamada');
    elegirOpcion(elemento, '#resultado', 'Contacto efectivo');
  }

  function enviar(): Promise<void> {
    elemento.querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit'));
    return estabilizar(fixture);
  }

  it('CA2_RegistrarContacto_EnviaPacienteFechaCanalYResultadoYAbreElDetalle', async () => {
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    await completarFormulario();

    await enviar();

    const peticion = backend.expectOne('/api/contactos');
    expect(peticion.request.body).toEqual({
      pacienteId: 3,
      fechaContacto: '2026-09-20T10:30:00-05:00',
      canal: 'LLAMADA',
      resultado: 'EFECTIVO',
      observacion: null,
    });
    expect(peticion.request.headers.get('X-Gestor-Id')).toBe('1');

    peticion.flush(contactoDetalle(1), { status: 201, statusText: 'Created' });
    await estabilizar(fixture);
    expect(navegar).toHaveBeenCalledWith(['/contactos', 7], { queryParams: { registrado: 1 } });
  });

  it('CA2_RegistrarContacto_FechaAnteriorAlIngreso_SeMuestraJuntoAlCampoDeFecha', async () => {
    await completarFormulario();

    await enviar();
    backend.expectOne('/api/contactos').flush(
      {
        status: 422,
        codigo: 'FECHA_ANTERIOR_AL_INGRESO',
        detail:
          'La fecha del contacto es anterior al ingreso del paciente al programa (2026-09-10).',
      },
      { status: 422, statusText: 'Unprocessable Entity' },
    );
    await estabilizar(fixture);

    const errorFecha = elemento
      .querySelector('#fecha')!
      .parentElement!.querySelector('.error-campo');
    expect(errorFecha?.textContent).toContain('anterior al ingreso del paciente');
    expect(elemento.querySelector('.alerta-error')).toBeNull();
  });

  it('CA2_RegistrarContacto_ErrorDeReglaSinCampo_SeMuestraComoAvisoGeneral', async () => {
    await completarFormulario();

    await enviar();
    backend.expectOne('/api/contactos').flush(
      {
        status: 422,
        codigo: 'PACIENTE_INACTIVO',
        detail: 'El paciente está inactivo y no admite contactos nuevos.',
      },
      { status: 422, statusText: 'Unprocessable Entity' },
    );
    await estabilizar(fixture);

    expect(elemento.querySelector('.alerta-error')?.textContent).toContain(
      'El paciente está inactivo',
    );
  });

  it('CA2_RegistrarContacto_SinPaciente_NoEnviaNada', async () => {
    await enviar();

    backend.expectNone('/api/contactos');
    expect(elemento.textContent).toContain('Elige un paciente.');
  });
});
