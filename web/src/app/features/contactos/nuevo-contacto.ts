import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, map, of, switchMap } from 'rxjs';
import { CatalogosService } from '../../core/api/catalogos.service';
import { ContactosService } from '../../core/api/contactos.service';
import { PacientesService } from '../../core/api/pacientes.service';
import { esErrorApi } from '../../core/errores/error-api';
import { aplicarErroresDelServidor } from '../../core/errores/errores-formulario';
import { aFechaPrograma, ahoraLocal } from '../../core/fechas';
import { PacienteResumen } from '../../core/modelos/api';
import { GestorActualService } from '../../core/sesion/gestor-actual.service';

/** CA-2: el gestor registra un contacto con un paciente, con su fecha, canal y resultado. */
@Component({
  selector: 'app-nuevo-contacto',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './nuevo-contacto.html',
})
export class NuevoContactoPagina {
  private readonly contactos = inject(ContactosService);
  private readonly router = inject(Router);
  protected readonly gestorActual = inject(GestorActualService);
  protected readonly catalogos = toSignal(
    inject(CatalogosService)
      .obtener()
      .pipe(catchError(() => of(null))),
    { initialValue: null },
  );

  // La búsqueda no se envía: solo sirve para elegir el paciente.
  protected readonly busqueda = new FormControl('', { nonNullable: true });
  protected readonly formulario = new FormGroup({
    pacienteId: new FormControl<number | null>(null, Validators.required),
    fechaContacto: new FormControl(ahoraLocal(), {
      nonNullable: true,
      validators: Validators.required,
    }),
    canal: new FormControl('', { nonNullable: true, validators: Validators.required }),
    resultado: new FormControl('', { nonNullable: true, validators: Validators.required }),
    observacion: new FormControl('', { nonNullable: true, validators: Validators.maxLength(500) }),
  });

  protected readonly pacientes = signal<PacienteResumen[]>([]);
  protected readonly pacienteElegido = signal<PacienteResumen | null>(null);
  protected readonly errorGeneral = signal<string | null>(null);
  protected readonly enviando = signal(false);
  protected readonly ahora = ahoraLocal();

  constructor() {
    const pacientesService = inject(PacientesService);
    this.busqueda.valueChanges
      .pipe(
        debounceTime(300),
        map((texto) => texto.trim()),
        distinctUntilChanged(),
        switchMap((texto) =>
          texto.length < 2
            ? of([])
            : pacientesService.buscar(texto).pipe(catchError(() => of([] as PacienteResumen[]))),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((pacientes) => this.pacientes.set(pacientes));
  }

  protected elegirPaciente(paciente: PacienteResumen): void {
    this.pacienteElegido.set(paciente);
    this.formulario.controls.pacienteId.setValue(paciente.id);
    this.pacientes.set([]);
  }

  protected cambiarPaciente(): void {
    this.pacienteElegido.set(null);
    this.formulario.controls.pacienteId.setValue(null);
    this.busqueda.setValue('');
  }

  protected registrar(): void {
    this.errorGeneral.set(null);
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    const valor = this.formulario.getRawValue();
    this.enviando.set(true);
    this.contactos
      .registrar({
        pacienteId: valor.pacienteId!,
        fechaContacto: aFechaPrograma(valor.fechaContacto),
        canal: valor.canal,
        resultado: valor.resultado,
        observacion: valor.observacion || null,
      })
      .subscribe({
        next: (contacto) =>
          this.router.navigate(['/contactos', contacto.id], { queryParams: { registrado: 1 } }),
        error: (error: unknown) => {
          this.enviando.set(false);
          if (!esErrorApi(error)) {
            this.errorGeneral.set('No se pudo registrar el contacto.');
          } else if (!aplicarErroresDelServidor(this.formulario, error)) {
            this.errorGeneral.set(error.detalle);
          }
        },
      });
  }
}
