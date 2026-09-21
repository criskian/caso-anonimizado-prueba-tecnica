import { HttpTestingController, TestRequest } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { mesActual } from '../../core/fechas';
import { ContactosDelMes } from '../../core/modelos/api';
import { ContactosDelMesPagina } from './contactos-del-mes';
import { CATALOGOS, configurar, elegirOpcion, estabilizar } from './pruebas-comunes';

const PAGINA: ContactosDelMes = {
  mes: '2026-09',
  pagina: 1,
  tamano: 50,
  total: 1,
  items: [
    {
      id: 7,
      fechaContacto: '2026-09-20T10:00:00-05:00',
      paciente: { id: 3, nombre: 'Ana García' },
      ciudad: { id: 1, nombre: 'Bogotá' },
      gestor: { id: 2, nombre: 'Andrés Quintero' },
      canal: { codigo: 'LLAMADA', nombre: 'Llamada' },
      resultado: { codigo: 'EFECTIVO', nombre: 'Contacto efectivo' },
      versionActual: 2,
      corregido: true,
    },
  ],
};

describe('ContactosDelMesPagina (CA-4)', () => {
  let backend: HttpTestingController;
  let fixture: ComponentFixture<ContactosDelMesPagina>;
  let elemento: HTMLElement;

  const consultaDelMes = (): TestRequest => backend.expectOne((p) => p.url === '/api/contactos');

  beforeEach(() => {
    backend = configurar();
    fixture = TestBed.createComponent(ContactosDelMesPagina);
    fixture.detectChanges();
    backend.expectOne('/api/catalogos').flush(CATALOGOS);
    elemento = fixture.nativeElement as HTMLElement;
  });

  afterEach(() => backend.verify());

  it('CA4_AlAbrir_ConsultaElMesEnCursoSinFiltros', async () => {
    const peticion = consultaDelMes();
    expect(peticion.request.params.get('mes')).toBe(mesActual());
    expect(peticion.request.params.has('gestorId')).toBe(false);
    expect(peticion.request.params.has('ciudadId')).toBe(false);

    peticion.flush(PAGINA);
    await estabilizar(fixture);

    const fila = elemento.querySelector('tbody tr')!;
    expect(fila.textContent).toContain('20/09/2026 10:00');
    expect(fila.textContent).toContain('corregido v2');
    expect(elemento.textContent).toContain('1 contacto en 2026-09');
  });

  it('CA4_FiltrarPorGestorYCiudad_EnviaAmbosFiltros', async () => {
    consultaDelMes().flush(PAGINA);
    await estabilizar(fixture);

    elegirOpcion(elemento, '#filtro-gestor', 'Andrés Quintero');
    consultaDelMes().flush(PAGINA);
    elegirOpcion(elemento, '#filtro-ciudad', 'Bogotá');

    const peticion = consultaDelMes();
    expect(peticion.request.params.get('gestorId')).toBe('2');
    expect(peticion.request.params.get('ciudadId')).toBe('1');
    expect(peticion.request.params.get('pagina')).toBe('1');
    peticion.flush({ ...PAGINA, items: [], total: 0 });
    await estabilizar(fixture);

    expect(elemento.textContent).toContain('No hay contactos que cumplan los filtros.');
  });

  it('CA4_ErrorDelServidor_SeMuestraComoAviso', async () => {
    consultaDelMes().flush(
      { status: 400, codigo: 'MES_INVALIDO', detail: 'El mes debe tener formato AAAA-MM.' },
      { status: 400, statusText: 'Bad Request' },
    );
    await estabilizar(fixture);

    expect(elemento.querySelector('[role="alert"]')?.textContent).toContain('formato AAAA-MM');
  });
});
