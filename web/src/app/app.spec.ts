import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { erroresApiInterceptor } from './core/errores/errores-api.interceptor';
import { Catalogos } from './core/modelos/api';
import { GestorActualService } from './core/sesion/gestor-actual.service';

const CATALOGOS: Catalogos = {
  gestores: [
    { id: 1, nombre: 'Laura Méndez' },
    { id: 2, nombre: 'Andrés Quintero' },
  ],
  ciudades: [],
  canales: [],
  resultados: [],
};

describe('App («Actuando como»)', () => {
  let backend: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        provideHttpClient(withInterceptors([erroresApiInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    backend = TestBed.inject(HttpTestingController);
  });

  /** Crea el componente, responde la petición de catálogos y espera a que se pinte. */
  async function crear(
    respuesta: (p: ReturnType<HttpTestingController['expectOne']>) => void,
  ): Promise<ComponentFixture<App>> {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    respuesta(backend.expectOne('/api/catalogos'));
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture;
  }

  it('lista los gestores activos del catálogo', async () => {
    const fixture = await crear((p) => p.flush(CATALOGOS));
    const elemento = fixture.nativeElement as HTMLElement;

    const opciones = Array.from(elemento.querySelectorAll('#gestor-actual option')).map((o) =>
      o.textContent?.trim(),
    );
    expect(opciones).toEqual(['Elige un gestor', 'Laura Méndez', 'Andrés Quintero']);
  });

  it('guarda el gestor que elige el usuario', async () => {
    const fixture = await crear((p) => p.flush(CATALOGOS));
    const selector = (fixture.nativeElement as HTMLElement).querySelector<HTMLSelectElement>(
      '#gestor-actual',
    )!;

    selector.value = '2';
    selector.dispatchEvent(new Event('change'));

    expect(TestBed.inject(GestorActualService).gestorId()).toBe(2);
  });

  it('descarta un gestor recordado que ya no está activo', async () => {
    TestBed.inject(GestorActualService).seleccionar(99);

    await crear((p) => p.flush(CATALOGOS));

    expect(TestBed.inject(GestorActualService).gestorId()).toBeNull();
  });

  it('muestra un aviso si no se pueden cargar los catálogos', async () => {
    const fixture = await crear((p) => p.error(new ProgressEvent('error'), { status: 0 }));
    const alerta = (fixture.nativeElement as HTMLElement).querySelector('[role="alert"]');

    expect(alerta?.textContent).toContain('No se pudo conectar con el servidor');
  });
});
