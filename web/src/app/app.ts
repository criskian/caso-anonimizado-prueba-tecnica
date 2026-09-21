import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';
import { CatalogosService } from './core/api/catalogos.service';
import { esErrorApi } from './core/errores/error-api';
import { Referencia } from './core/modelos/api';
import { GestorActualService } from './core/sesion/gestor-actual.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  protected readonly gestorActual = inject(GestorActualService);
  protected readonly gestores = signal<Referencia[]>([]);
  protected readonly errorCatalogos = signal<string | null>(null);

  constructor() {
    inject(CatalogosService)
      .obtener()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (catalogos) => {
          this.gestores.set(catalogos.gestores);
          // Si el gestor recordado ya no está activo, se descarta la selección.
          const actual = this.gestorActual.gestorId();
          if (actual !== null && !catalogos.gestores.some((g) => g.id === actual)) {
            this.gestorActual.seleccionar(null);
          }
        },
        error: (error: unknown) =>
          this.errorCatalogos.set(
            esErrorApi(error) ? error.detalle : 'No se pudieron cargar los catálogos.',
          ),
      });
  }

  protected alCambiarGestor(evento: Event): void {
    const valor = (evento.target as HTMLSelectElement).value;
    this.gestorActual.seleccionar(valor ? Number(valor) : null);
  }
}
