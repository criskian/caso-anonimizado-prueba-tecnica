import { aFechaPrograma, aValorLocal, ahoraLocal, formatearFecha, mesActual } from './fechas';

describe('Fechas en hora del programa (UTC−5)', () => {
  it('envía el valor del formulario con desfase −05:00', () => {
    expect(aFechaPrograma('2026-09-21T10:30')).toBe('2026-09-21T10:30:00-05:00');
  });

  it('muestra en hora del programa una fecha que llega en UTC', () => {
    // 03:00 UTC del 1 de octubre son las 22:00 del 30 de septiembre en UTC−5.
    expect(aValorLocal('2026-10-01T03:00:00Z')).toBe('2026-09-30T22:00');
    expect(formatearFecha('2026-10-01T03:00:00Z')).toBe('30/09/2026 22:00');
  });

  it('respeta una fecha que ya viene en −05:00', () => {
    expect(aValorLocal('2026-09-21T10:30:00-05:00')).toBe('2026-09-21T10:30');
  });

  it('calcula el mes en curso en hora del programa, no en UTC', () => {
    const primeraHoraUtcDeOctubre = new Date('2026-10-01T02:00:00Z');

    expect(mesActual(primeraHoraUtcDeOctubre)).toBe('2026-09');
    expect(ahoraLocal(primeraHoraUtcDeOctubre)).toBe('2026-09-30T21:00');
  });
});
