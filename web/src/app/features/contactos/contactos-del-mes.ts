import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { EMPTY, Subject, catchError, of, switchMap, tap } from 'rxjs';
import { CatalogosService } from '../../core/api/catalogos.service';
import { ContactosService } from '../../core/api/contactos.service';
import { esErrorApi } from '../../core/errores/error-api';
import { formatearFecha, mesActual } from '../../core/fechas';
import { ContactosDelMes, FiltroContactosDelMes } from '../../core/modelos/api';

/** CA-4: vista de contactos del mes, con filtros por gestor y por ciudad. */
@Component({
  selector: 'app-contactos-del-mes',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './contactos-del-mes.html',
})
export class ContactosDelMesPagina {
  private readonly contactos = inject(ContactosService);
  private readonly consultas = new Subject<FiltroContactosDelMes>();

  protected readonly tamano = 50;
  protected readonly formatearFecha = formatearFecha;
  protected readonly catalogos = toSignal(
    inject(CatalogosService)
      .obtener()
      .pipe(catchError(() => of(null))),
    { initialValue: null },
  );

  protected readonly filtros = new FormGroup({
    mes: new FormControl(mesActual(), { nonNullable: true }),
    gestorId: new FormControl<number | null>(null),
    ciudadId: new FormControl<number | null>(null),
  });

  protected readonly pagina = signal(1);
  protected readonly resultado = signal<ContactosDelMes | null>(null);
  protected readonly cargando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly totalPaginas = computed(() =>
    Math.max(1, Math.ceil((this.resultado()?.total ?? 0) / this.tamano)),
  );

  constructor() {
    // switchMap descarta la respuesta de una consulta anterior si el usuario ya cambió el filtro.
    this.consultas
      .pipe(
        tap(() => {
          this.cargando.set(true);
          this.error.set(null);
        }),
        switchMap((filtro) =>
          this.contactos.consultarMes(filtro).pipe(
            catchError((error: unknown) => {
              this.error.set(esErrorApi(error) ? error.detalle : 'No se pudo consultar el mes.');
              this.cargando.set(false);
              return EMPTY;
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((resultado) => {
        this.resultado.set(resultado);
        this.cargando.set(false);
      });

    this.filtros.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => this.irAPagina(1));
    this.irAPagina(1);
  }

  protected irAPagina(pagina: number): void {
    this.pagina.set(pagina);
    const { mes, gestorId, ciudadId } = this.filtros.getRawValue();
    this.consultas.next({ mes, gestorId, ciudadId, pagina, tamano: this.tamano });
  }
}
