import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApiService } from './api.service';
import { tap, catchError, of } from 'rxjs';

export interface UserSession {
  email: string;
  fullName: string;
  role: string;
  hospitalName: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly apiService = inject(ApiService);
  private readonly authUrl = 'http://localhost:5000/api/v1/auth';

  readonly currentUser = signal<UserSession>({
    email: 'receptionist@hospital.gov.bd',
    fullName: 'Nusrat Jahan',
    role: 'Receptionist',
    hospitalName: 'Dhaka Central General Hospital'
  });

  readonly isAuthenticated = signal<boolean>(true);

  constructor() {
    this.initDefaultSession();
  }

  private initDefaultSession(): void {
    // Attempt automatic login with demo receptionist user so all endpoints work seamlessly
    this.login('receptionist@hospital.gov.bd', 'Demo@12345').subscribe();
  }

  login(email: string, password: string) {
    return this.http.post<any>(`${this.authUrl}/login`, { email, password }).pipe(
      tap(res => {
        if (res && res.accessToken) {
          this.apiService.setAuthToken(res.accessToken);
          this.isAuthenticated.set(true);
          this.currentUser.set({
            email: res.email || email,
            fullName: res.fullName || 'Nusrat Jahan',
            role: res.role || 'Receptionist',
            hospitalName: 'Dhaka Central General Hospital'
          });
        }
      }),
      catchError(err => {
        console.warn('Auto-login using demo token fallback:', err);
        return of(null);
      })
    );
  }
}
