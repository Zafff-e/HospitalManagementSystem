import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  PatientResponse,
  CreatePatientRequest,
  UpdatePatientRequest,
  PagedResult,
  DivisionDto,
  DistrictDto,
  UpazilaDto,
  DuplicateWarningDto
} from '../models/patient.models';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly http = inject(HttpClient);
  // Default to backend port 5000 or relative /api/v1
  private readonly baseUrl = 'http://localhost:5000/api/v1';

  // Auth token helper (seeded receptionist token for full permission access)
  private authToken: string | null = null;

  setAuthToken(token: string): void {
    this.authToken = token;
  }

  private getHeaders(): { [header: string]: string } {
    const headers: { [header: string]: string } = {};
    if (this.authToken) {
      headers['Authorization'] = `Bearer ${this.authToken}`;
    }
    return headers;
  }

  searchPatients(params: {
    query?: string;
    phone?: string;
    nidLast4?: string;
    mrn?: string;
    page?: number;
    pageSize?: number;
  }): Observable<PagedResult<PatientResponse>> {
    let httpParams = new HttpParams();
    if (params.query) httpParams = httpParams.set('query', params.query);
    if (params.phone) httpParams = httpParams.set('phone', params.phone);
    if (params.nidLast4) httpParams = httpParams.set('nidLast4', params.nidLast4);
    if (params.mrn) httpParams = httpParams.set('mrn', params.mrn);
    if (params.page) httpParams = httpParams.set('page', params.page.toString());
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());

    return this.http.get<PagedResult<PatientResponse>>(`${this.baseUrl}/patients`, {
      params: httpParams,
      headers: this.getHeaders()
    });
  }

  getPatientById(id: string): Observable<PatientResponse> {
    return this.http.get<PatientResponse>(`${this.baseUrl}/patients/${id}`, {
      headers: this.getHeaders()
    });
  }

  createPatient(patient: CreatePatientRequest): Observable<PatientResponse> {
    return this.http.post<PatientResponse>(`${this.baseUrl}/patients`, patient, {
      headers: this.getHeaders()
    });
  }

  updatePatient(id: string, patient: UpdatePatientRequest): Observable<PatientResponse> {
    return this.http.put<PatientResponse>(`${this.baseUrl}/patients/${id}`, patient, {
      headers: this.getHeaders()
    });
  }

  checkDuplicate(phone: string, nidLast4?: string, dob?: string): Observable<DuplicateWarningDto> {
    return this.http.post<DuplicateWarningDto>(`${this.baseUrl}/patients/check-duplicate`, {
      phone,
      nidLast4,
      dateOfBirth: dob
    }, {
      headers: this.getHeaders()
    });
  }

  // Geo Hierarchy
  getDivisions(): Observable<DivisionDto[]> {
    return this.http.get<DivisionDto[]>(`${this.baseUrl}/geo/divisions`);
  }

  getDistricts(divisionId?: number): Observable<DistrictDto[]> {
    let params = new HttpParams();
    if (divisionId) params = params.set('divisionId', divisionId.toString());
    return this.http.get<DistrictDto[]>(`${this.baseUrl}/geo/districts`, { params });
  }

  getUpazilas(districtId?: number): Observable<UpazilaDto[]> {
    let params = new HttpParams();
    if (districtId) params = params.set('districtId', districtId.toString());
    return this.http.get<UpazilaDto[]>(`${this.baseUrl}/geo/upazilas`, { params });
  }

  // Appointments & OPD Queue
  getDoctors(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/appointments/doctors`, {
      headers: this.getHeaders()
    });
  }

  getAvailableSlots(doctorId: string, date: string): Observable<any[]> {
    const params = new HttpParams()
      .set('doctorId', doctorId)
      .set('date', date);
    return this.http.get<any[]>(`${this.baseUrl}/appointments/slots`, {
      params,
      headers: this.getHeaders()
    });
  }

  bookAppointment(request: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/appointments`, request, {
      headers: this.getHeaders()
    });
  }

  checkInAppointment(appointmentId: string): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/appointments/${appointmentId}/check-in`, {}, {
      headers: this.getHeaders()
    });
  }

  getDoctorQueue(doctorId: string, date: string): Observable<any[]> {
    const params = new HttpParams()
      .set('doctorId', doctorId)
      .set('date', date);
    return this.http.get<any[]>(`${this.baseUrl}/appointments/queue`, {
      params,
      headers: this.getHeaders()
    });
  }

  getAppointments(date?: string, doctorId?: string): Observable<any[]> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    if (doctorId) params = params.set('doctorId', doctorId);
    return this.http.get<any[]>(`${this.baseUrl}/appointments`, {
      params,
      headers: this.getHeaders()
    });
  }
}
