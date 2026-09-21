import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ContactosService } from './contactos.service';

describe('ContactosService', () => {
  let servicio: ContactosService;
  let backend: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    servicio = TestBed.inject(ContactosService);
    backend = TestBed.inject(HttpTestingController);
  });

  afterEach(() => backend.verify());

  it('CA-4: envía solo los filtros que tienen valor', () => {
    servicio
      .consultarMes({ mes: '2026-09', gestorId: 2, ciudadId: null, pagina: undefined })
      .subscribe();

    const peticion = backend.expectOne((p) => p.url === '/api/contactos');
    expect(peticion.request.params.keys().sort()).toEqual(['gestorId', 'mes']);
    expect(peticion.request.params.get('gestorId')).toBe('2');
  });

  it('CA-3: publica la corrección en el subrecurso del contacto', () => {
    servicio
      .corregir(7, {
        versionEsperada: 1,
        fechaContacto: '2026-09-20T10:00:00-05:00',
        canal: 'LLAMADA',
        resultado: 'EFECTIVO',
        observacion: null,
        motivo: 'Se registró mal el resultado.',
      })
      .subscribe();

    const peticion = backend.expectOne('/api/contactos/7/correcciones');
    expect(peticion.request.method).toBe('POST');
    expect(peticion.request.body.versionEsperada).toBe(1);
  });
});
