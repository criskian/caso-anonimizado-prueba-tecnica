import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ContactoDetalle,
  ContactosDelMes,
  CorregirContacto,
  FiltroContactosDelMes,
  RegistrarContacto,
} from '../modelos/api';

@Injectable({ providedIn: 'root' })
export class ContactosService {
  private readonly http = inject(HttpClient);

  /** CA-4: contactos de un mes, con filtros opcionales por gestor y ciudad. */
  consultarMes(filtro: FiltroContactosDelMes): Observable<ContactosDelMes> {
    let params = new HttpParams();
    for (const [clave, valor] of Object.entries(filtro)) {
      if (valor !== null && valor !== undefined && valor !== '') {
        params = params.set(clave, String(valor));
      }
    }

    return this.http.get<ContactosDelMes>('/api/contactos', { params });
  }

  obtener(id: number): Observable<ContactoDetalle> {
    return this.http.get<ContactoDetalle>(`/api/contactos/${id}`);
  }

  /** CA-2 */
  registrar(contacto: RegistrarContacto): Observable<ContactoDetalle> {
    return this.http.post<ContactoDetalle>('/api/contactos', contacto);
  }

  /** CA-3 */
  corregir(id: number, correccion: CorregirContacto): Observable<ContactoDetalle> {
    return this.http.post<ContactoDetalle>(`/api/contactos/${id}/correcciones`, correccion);
  }
}
