import { HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GestorActualService } from '../../core/sesion/gestor-actual.service';
import { DetalleContactoPagina } from './detalle-contacto';
import {
  CATALOGOS,
  configurar,
  contactoDetalle,
  elegirOpcion,
  escribir,
  estabilizar,
} from './pruebas-comunes';

describe('DetalleContactoPagina (CA-3)', () => {
  let backend: HttpTestingController;
  let fixture: ComponentFixture<DetalleContactoPagina>;
  let elemento: HTMLElement;

  beforeEach(async () => {
    backend = configurar();
    TestBed.inject(GestorActualService).seleccionar(2);
    fixture = TestBed.createComponent(DetalleContactoPagina);
    fixture.componentRef.setInput('id', '7');
    fixture.detectChanges();
    backend.expectOne('/api/catalogos').flush(CATALOGOS);
    backend.expectOne('/api/contactos/7').flush(contactoDetalle(1));
    await estabilizar(fixture);
    elemento = fixture.nativeElement as HTMLElement;
  });

  afterEach(() => backend.verify());

  async function enviarCorreccion(): Promise<void> {
    elegirOpcion(elemento, '#resultado', 'Contacto efectivo');
    escribir(elemento, '#motivo', 'El paciente sí contestó; se registró mal.');
    elemento.querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit'));
    await estabilizar(fixture);
  }

  it('CA3_Corregir_EnviaLaVersionVistaYMuestraElHistorialNuevo', async () => {
    await enviarCorreccion();

    const peticion = backend.expectOne('/api/contactos/7/correcciones');
    expect(peticion.request.body).toEqual({
      versionEsperada: 1,
      fechaContacto: '2026-09-20T10:00:00-05:00',
      canal: 'LLAMADA',
      resultado: 'EFECTIVO',
      observacion: null,
      motivo: 'El paciente sí contestó; se registró mal.',
    });
    expect(peticion.request.headers.get('X-Gestor-Id')).toBe('2');

    peticion.flush(contactoDetalle(2));
    await estabilizar(fixture);

    expect(elemento.querySelector('[role="status"]')?.textContent).toContain('versión 2');
    const filas = elemento.querySelectorAll('section:last-of-type tbody tr');
    expect(filas.length).toBe(2);
    expect(filas[0].textContent).toContain('Registro original');
    expect(filas[1].textContent).toContain('El paciente sí contestó.');
  });

  it('CA3_FormularioCorreccion_Conflicto409_MuestraAviso', async () => {
    await enviarCorreccion();

    backend.expectOne('/api/contactos/7/correcciones').flush(
      {
        status: 409,
        codigo: 'VERSION_DESACTUALIZADA',
        detail:
          'Otro usuario corrigió este contacto (versión vigente: 2). Recarga para ver los datos actuales.',
      },
      { status: 409, statusText: 'Conflict' },
    );
    await estabilizar(fixture);

    const aviso = elemento.querySelector('.alerta-aviso[role="alert"]');
    expect(aviso?.textContent).toContain('Otro usuario corrigió este contacto');

    // «Recargar» vuelve a pedir el contacto y la próxima corrección sale sobre la versión 2.
    aviso!.querySelector('button')!.click();
    backend.expectOne('/api/contactos/7').flush(contactoDetalle(2));
    await estabilizar(fixture);
    expect(elemento.querySelector('.alerta-aviso[role="alert"]')).toBeNull();

    await enviarCorreccion();
    expect(backend.expectOne('/api/contactos/7/correcciones').request.body.versionEsperada).toBe(2);
  });

  it('CA3_FormularioCorreccion_MotivoRechazadoPorElServidor_SeMuestraJuntoAlCampo', async () => {
    await enviarCorreccion();

    backend.expectOne('/api/contactos/7/correcciones').flush(
      {
        status: 400,
        codigo: 'VALIDACION',
        detail: 'Uno o más datos no son válidos.',
        errors: { motivo: ['Explica el motivo de la corrección en 10 a 500 caracteres.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await estabilizar(fixture);

    const errorMotivo = elemento
      .querySelector('#motivo')!
      .parentElement!.querySelector('.error-campo');
    expect(errorMotivo?.textContent).toContain('Explica el motivo de la corrección');
    expect(elemento.querySelector('.alerta-error')).toBeNull();
  });

  it('CA3_FormularioCorreccion_SinMotivo_NoEnviaNada', async () => {
    elegirOpcion(elemento, '#resultado', 'Contacto efectivo');
    elemento.querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit'));
    await estabilizar(fixture);

    backend.expectNone('/api/contactos/7/correcciones');
    expect(elemento.textContent).toContain('Explica el motivo en 10 a 500 caracteres.');
  });
});
