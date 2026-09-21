import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { ErrorApi } from './errores/error-api';
import { erroresApiInterceptor } from './errores/errores-api.interceptor';
import { CABECERA_GESTOR, gestorActualInterceptor } from './sesion/gestor-actual.interceptor';
import { GestorActualService } from './sesion/gestor-actual.service';

describe('Interceptores', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let gestorActual: GestorActualService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([gestorActualInterceptor, erroresApiInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    gestorActual = TestBed.inject(GestorActualService);
  });

  afterEach(() => backend.verify());

  it('envía el gestor elegido en la cabecera X-Gestor-Id', () => {
    gestorActual.seleccionar(3);

    http.post('/api/contactos', {}).subscribe();

    expect(backend.expectOne('/api/contactos').request.headers.get(CABECERA_GESTOR)).toBe('3');
  });

  it('no envía la cabecera si no hay gestor elegido', () => {
    http.get('/api/catalogos').subscribe();

    expect(backend.expectOne('/api/catalogos').request.headers.has(CABECERA_GESTOR)).toBe(false);
  });

  it('no envía la cabecera a direcciones que no son de la API', () => {
    gestorActual.seleccionar(3);

    http.get('https://otro.sitio/recurso').subscribe();

    expect(
      backend.expectOne('https://otro.sitio/recurso').request.headers.has(CABECERA_GESTOR),
    ).toBe(false);
  });

  it('entrega a quien llama un ErrorApi en lugar de la respuesta HTTP cruda', async () => {
    const respuesta = firstValueFrom(http.get('/api/contactos/1'));
    backend
      .expectOne('/api/contactos/1')
      .flush(
        { codigo: 'CONTACTO_NO_ENCONTRADO', detail: 'No existe el contacto 1.' },
        { status: 404, statusText: 'Not Found' },
      );

    const error = (await respuesta.catch((e: unknown) => e)) as ErrorApi;
    expect(error.estado).toBe(404);
    expect(error.codigo).toBe('CONTACTO_NO_ENCONTRADO');
    expect(error.detalle).toBe('No existe el contacto 1.');
  });
});

describe('GestorActualService', () => {
  beforeEach(() => localStorage.clear());

  it('recuerda el gestor elegido entre recargas', () => {
    TestBed.inject(GestorActualService).seleccionar(4);
    TestBed.resetTestingModule();

    expect(TestBed.inject(GestorActualService).gestorId()).toBe(4);
  });

  it('ignora un valor guardado que no es un identificador válido', () => {
    localStorage.setItem('seguimiento.gestorActual', 'abc');

    expect(TestBed.inject(GestorActualService).gestorId()).toBeNull();
  });
});
