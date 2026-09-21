import { Component, effect, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { catchError, of } from 'rxjs';
import { CatalogosService } from '../../core/api/catalogos.service';
import { ContactosService } from '../../core/api/contactos.service';
import { ErrorApi, esErrorApi } from '../../core/errores/error-api';
import { aplicarErroresDelServidor } from '../../core/errores/errores-formulario';
import { aFechaPrograma, aValorLocal, ahoraLocal, formatearFecha } from '../../core/fechas';
import { ContactoDetalle } from '../../core/modelos/api';
import { GestorActualService } from '../../core/sesion/gestor-actual.service';

/**
 * CA-3: detalle de un contacto, su historial de versiones y el formulario para corregirlo.
 * Corregir crea una versión nueva: la anterior se sigue viendo en el historial (H-01).
 */
@Component({
  selector: 'app-detalle-contacto',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './detalle-contacto.html',
})
export class DetalleContactoPagina {
  private readonly contactos = inject(ContactosService);
  protected readonly gestorActual = inject(GestorActualService);
  protected readonly formatearFecha = formatearFecha;
  protected readonly catalogos = toSignal(
    inject(CatalogosService)
      .obtener()
      .pipe(catchError(() => of(null))),
    { initialValue: null },
  );

  /** Id de la ruta /contactos/:id. */
  readonly id = input.required<string>();
  /** Llega en 1 cuando se viene de registrar el contacto. */
  readonly registrado = input<string>();

  protected readonly detalle = signal<ContactoDetalle | null>(null);
  protected readonly errorCarga = signal<string | null>(null);
  protected readonly errorGeneral = signal<string | null>(null);
  protected readonly conflicto = signal<string | null>(null);
  protected readonly exito = signal<string | null>(null);
  protected readonly enviando = signal(false);
  protected readonly ahora = ahoraLocal();

  // Versión que se estaba viendo al abrir el formulario: es la que se envía como versionEsperada.
  private versionBase = 0;

  protected readonly formulario = new FormGroup({
    fechaContacto: new FormControl('', { nonNullable: true, validators: Validators.required }),
    canal: new FormControl('', { nonNullable: true, validators: Validators.required }),
    resultado: new FormControl('', { nonNullable: true, validators: Validators.required }),
    observacion: new FormControl('', { nonNullable: true, validators: Validators.maxLength(500) }),
    motivo: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(10), Validators.maxLength(500)],
    }),
  });

  constructor() {
    effect(() => {
      this.exito.set(this.registrado() ? 'Contacto registrado.' : null);
      this.cargar(Number(this.id()));
    });
  }

  protected recargar(): void {
    this.exito.set(null);
    this.cargar(Number(this.id()));
  }

  protected corregir(): void {
    this.errorGeneral.set(null);
    this.conflicto.set(null);
    this.exito.set(null);
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    const valor = this.formulario.getRawValue();
    this.enviando.set(true);
    this.contactos
      .corregir(Number(this.id()), {
        versionEsperada: this.versionBase,
        fechaContacto: aFechaPrograma(valor.fechaContacto),
        canal: valor.canal,
        resultado: valor.resultado,
        observacion: valor.observacion || null,
        motivo: valor.motivo,
      })
      .subscribe({
        next: (detalle) => {
          this.enviando.set(false);
          this.mostrar(detalle);
          this.exito.set(
            `Corrección guardada: el contacto está en la versión ${detalle.versionActual}.`,
          );
        },
        error: (error: unknown) => {
          this.enviando.set(false);
          this.mostrarError(error);
        },
      });
  }

  private cargar(id: number): void {
    this.errorCarga.set(null);
    this.conflicto.set(null);
    this.errorGeneral.set(null);
    this.contactos.obtener(id).subscribe({
      next: (detalle) => this.mostrar(detalle),
      error: (error: unknown) =>
        this.errorCarga.set(esErrorApi(error) ? error.detalle : 'No se pudo cargar el contacto.'),
    });
  }

  private mostrar(detalle: ContactoDetalle): void {
    this.detalle.set(detalle);
    this.versionBase = detalle.versionActual;
    this.formulario.reset({
      fechaContacto: aValorLocal(detalle.fechaContacto),
      canal: detalle.canal.codigo,
      resultado: detalle.resultado.codigo,
      observacion: detalle.observacion ?? '',
      motivo: '',
    });
  }

  private mostrarError(error: unknown): void {
    if (!esErrorApi(error)) {
      this.errorGeneral.set('No se pudo guardar la corrección.');
      return;
    }

    if (esConflictoDeVersion(error)) {
      this.conflicto.set(error.detalle);
    } else if (!aplicarErroresDelServidor(this.formulario, error)) {
      this.errorGeneral.set(error.detalle);
    }
  }
}

function esConflictoDeVersion(error: ErrorApi): boolean {
  return error.estado === 409 && error.codigo === 'VERSION_DESACTUALIZADA';
}
