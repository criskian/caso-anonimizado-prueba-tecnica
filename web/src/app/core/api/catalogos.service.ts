import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';
import { Catalogos } from '../modelos/api';

@Injectable({ providedIn: 'root' })
export class CatalogosService {
  private readonly http = inject(HttpClient);

  // Los catálogos cambian muy poco: se piden una vez y se comparten entre pantallas.
  // Si la petición falla, la próxima suscripción vuelve a intentarlo.
  private readonly catalogos$ = this.http.get<Catalogos>('/api/catalogos').pipe(shareReplay(1));

  obtener(): Observable<Catalogos> {
    return this.catalogos$;
  }
}
