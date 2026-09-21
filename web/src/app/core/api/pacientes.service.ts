import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PacienteResumen } from '../modelos/api';

@Injectable({ providedIn: 'root' })
export class PacientesService {
  private readonly http = inject(HttpClient);

  buscar(texto: string): Observable<PacienteResumen[]> {
    return this.http.get<PacienteResumen[]>('/api/pacientes', {
      params: new HttpParams().set('buscar', texto),
    });
  }
}
