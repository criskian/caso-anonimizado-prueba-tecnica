// Tipos del contrato de la API (02-plan §4). Reflejan los DTO de Seguimiento.Api.
// Las fechas-hora viajan como texto ISO 8601 con desfase, por ejemplo 2026-09-21T10:30:00-05:00.

export interface Referencia {
  id: number;
  nombre: string;
}

export interface CodigoNombre {
  codigo: string;
  nombre: string;
}

export interface Ciudad extends Referencia {
  paisCodigo: string;
}

export interface Catalogos {
  gestores: Referencia[];
  ciudades: Ciudad[];
  canales: CodigoNombre[];
  resultados: CodigoNombre[];
}

export interface PacienteResumen {
  id: number;
  nombre: string;
  documento: string;
  ciudad: string;
}

export interface VersionContacto {
  numeroVersion: number;
  fechaContacto: string;
  canal: CodigoNombre;
  resultado: CodigoNombre;
  observacion: string | null;
  motivoCorreccion: string | null;
  registradoPor: Referencia;
  registradoEnUtc: string;
}

export interface ContactoDetalle {
  id: number;
  paciente: Referencia & { ciudad: string };
  gestor: Referencia;
  fechaContacto: string;
  canal: CodigoNombre;
  resultado: CodigoNombre;
  observacion: string | null;
  versionActual: number;
  historial: VersionContacto[];
}

export interface ContactoMesItem {
  id: number;
  fechaContacto: string;
  paciente: Referencia;
  ciudad: Referencia;
  gestor: Referencia;
  canal: CodigoNombre;
  resultado: CodigoNombre;
  versionActual: number;
  corregido: boolean;
}

export interface ContactosDelMes {
  mes: string;
  pagina: number;
  tamano: number;
  total: number;
  items: ContactoMesItem[];
}

export interface FiltroContactosDelMes {
  mes?: string | null;
  gestorId?: number | null;
  ciudadId?: number | null;
  pagina?: number | null;
  tamano?: number | null;
}

export interface RegistrarContacto {
  pacienteId: number;
  fechaContacto: string;
  canal: string;
  resultado: string;
  observacion: string | null;
}

export interface CorregirContacto {
  versionEsperada: number;
  fechaContacto: string;
  canal: string;
  resultado: string;
  observacion: string | null;
  motivo: string;
}
