import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { LanguageService } from '../../../core/services/language.service';
import { DoctorDto, AppointmentResponse, OpdQueueItemDto, CheckInResponse } from '../../../core/models/appointment.models';

@Component({
  selector: 'app-opd-queue',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="queue-container animate-fade-in">
      <div class="header-section">
        <div>
          <h1 class="page-title">{{ t().navOpdQueue }} & Check-in Desk</h1>
          <p class="page-subtitle">Assign daily consultation tokens and track live patient queues</p>
        </div>
        <a routerLink="/appointments/book" class="btn-primary">
          📅 Book New Appointment
        </a>
      </div>

      <!-- Quick Metrics Header -->
      <div class="queue-stats">
        <div class="q-stat-card">
          <span class="q-stat-label">TODAY'S APPOINTMENTS</span>
          <span class="q-stat-val">{{ appointments().length }}</span>
        </div>
        <div class="q-stat-card text-teal">
          <span class="q-stat-label">CHECKED-IN (WAITING)</span>
          <span class="q-stat-val">{{ getCheckedInCount() }}</span>
        </div>
        <div class="q-stat-card text-emerald">
          <span class="q-stat-label">IN CONSULTATION</span>
          <span class="q-stat-val">{{ getInConsultCount() }}</span>
        </div>
        <div class="q-stat-card">
          <span class="q-stat-label">ACTIVE SPECIALISTS</span>
          <span class="q-stat-val">{{ doctors().length }}</span>
        </div>
      </div>

      <!-- Main Layout: Check-in Desk + Live Queue -->
      <div class="queue-grid">
        <!-- Left: Today's Booked Appointments (Reception Check-in Desk) -->
        <div class="glass-card desk-card">
          <div class="card-header">
            <h3 class="card-title">Reception Check-in</h3>
            <span class="badge-pill badge-teal">Daily Token Generator</span>
          </div>

          <div *ngIf="isLoadingAppointments()" class="loading-state">
            <div class="spinner"></div>
            <p>Loading appointments...</p>
          </div>

          <div *ngIf="!isLoadingAppointments() && appointments().length === 0" class="empty-state">
            <p>No appointments booked for today yet.</p>
            <a routerLink="/appointments/book" class="btn-secondary" style="margin-top: 0.75rem;">
              Book an Appointment
            </a>
          </div>

          <div *ngIf="!isLoadingAppointments() && appointments().length > 0" class="appointments-list">
            <div *ngFor="let a of appointments()" class="appointment-row">
              <div class="appt-details">
                <div class="appt-name-row">
                  <span class="appt-p-name">{{ a.patientName }}</span>
                  <span class="mrn-tag">{{ a.patientMrn }}</span>
                </div>
                <div class="appt-meta">
                  <span>👨‍⚕️ {{ a.doctorName }}</span> •
                  <span>⏰ {{ a.scheduledAt | date:'shortTime' }}</span> •
                  <span>📞 {{ a.patientPhone }}</span>
                </div>
              </div>

              <div class="appt-action">
                <button
                  *ngIf="a.status === 0"
                  class="btn-primary checkin-btn"
                  [disabled]="isCheckingIn() === a.id"
                  (click)="checkIn(a)"
                >
                  <span *ngIf="isCheckingIn() === a.id" class="spinner-small"></span>
                  <span>Check In →</span>
                </button>

                <div *ngIf="a.status !== 0" class="checkedin-badge">
                  <span class="token-lbl">TOKEN</span>
                  <span class="token-num">#{{ a.tokenNo }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Right: Live OPD Queue by Doctor -->
        <div class="glass-card queue-card">
          <div class="card-header">
            <h3 class="card-title">Live Doctor Queue</h3>
            <div class="doctor-filter">
              <select class="form-select doc-select" [(ngModel)]="selectedDoctorId" (ngModelChange)="loadDoctorQueue()">
                <option *ngFor="let d of doctors()" [value]="d.id">
                  {{ d.fullName }} ({{ d.specialty }})
                </option>
              </select>
            </div>
          </div>

          <div *ngIf="isLoadingQueue()" class="loading-state">
            <div class="spinner"></div>
            <p>Connecting to OPD Queue...</p>
          </div>

          <div *ngIf="!isLoadingQueue() && queueItems().length === 0" class="empty-state">
            <div class="empty-icon">🛋️</div>
            <h4>Queue is currently clear</h4>
            <p>Check in booked patients on the left to add them to this doctor's live queue.</p>
          </div>

          <div *ngIf="!isLoadingQueue() && queueItems().length > 0" class="queue-list">
            <div *ngFor="let q of queueItems(); let i = index" class="queue-item" [class.current]="i === 0">
              <div class="token-chip" [class.highlight]="i === 0">
                #{{ q.tokenNo }}
              </div>
              <div class="q-patient-info">
                <div class="q-name-row">
                  <span class="q-patient-name">{{ q.patientName }}</span>
                  <span *ngIf="i === 0" class="now-serving-badge">NOW SERVING</span>
                </div>
                <div class="q-meta">
                  <span>MRN: {{ q.patientMrn }}</span> •
                  <span>{{ q.ageYears ?? '-' }} yrs</span> •
                  <span>Checked-in: {{ q.scheduledAt | date:'shortTime' }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Generated Token Slip Modal -->
      <div *ngIf="generatedToken()" class="modal-backdrop">
        <div class="modal-card animate-fade-in">
          <div class="token-slip">
            <div class="slip-header">
              <h3 class="slip-h-name">AVELA HEALTHCARE</h3>
              <p class="slip-sub">Dhaka Central General Hospital</p>
              <div class="slip-divider"></div>
            </div>

            <div class="slip-token-box">
              <span class="slip-token-label">OPD QUEUE TOKEN</span>
              <span class="slip-token-number">#{{ generatedToken()?.tokenNo }}</span>
            </div>

            <div class="slip-info">
              <div class="slip-row">
                <span>Doctor:</span>
                <strong>{{ generatedToken()?.doctorName }}</strong>
              </div>
              <div class="slip-row">
                <span>Patient:</span>
                <strong>{{ generatedToken()?.patientName }}</strong>
              </div>
              <div class="slip-row">
                <span>MRN:</span>
                <strong>{{ generatedToken()?.patientMrn }}</strong>
              </div>
              <div class="slip-row">
                <span>Date:</span>
                <strong>{{ generatedToken()?.date | date:'mediumDate' }}</strong>
              </div>
            </div>

            <div class="slip-footer">
              <p>Please wait in the OPD lounge until Token #{{ generatedToken()?.tokenNo }} is called on the screen.</p>
            </div>
          </div>

          <div class="modal-actions">
            <button class="btn-primary" (click)="printToken()">
              🖨️ Print Token Slip
            </button>
            <button class="btn-secondary" (click)="generatedToken.set(null)">
              Close
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .queue-container {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    .header-section {
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    .page-title {
      font-size: 1.75rem;
      font-weight: 800;
      color: #0f172a;
    }

    .page-subtitle {
      font-size: 0.875rem;
      color: #64748b;
    }

    .queue-stats {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 1rem;
    }

    .q-stat-card {
      background: #ffffff;
      padding: 1.25rem;
      border-radius: 12px;
      border: 1px solid #e2e8f0;
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .q-stat-label {
      font-size: 0.6875rem;
      font-weight: 700;
      color: #64748b;
      letter-spacing: 0.05em;
    }

    .q-stat-val {
      font-size: 1.75rem;
      font-weight: 800;
      color: #0f172a;
    }

    .text-teal .q-stat-val { color: #0891b2; }
    .text-emerald .q-stat-val { color: #059669; }

    .queue-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1.5rem;
    }

    .desk-card, .queue-card {
      background: #ffffff;
      padding: 1.5rem;
    }

    .card-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 1.25rem;
      border-bottom: 1px solid #f1f5f9;
      padding-bottom: 0.75rem;
    }

    .card-title {
      font-size: 1.125rem;
      font-weight: 700;
      color: #0f172a;
    }

    .doc-select {
      max-width: 250px;
      padding: 0.4rem 0.75rem;
    }

    .appointments-list, .queue-list {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .appointment-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0.875rem 1rem;
      border: 1px solid #e2e8f0;
      border-radius: 10px;
      transition: all 0.2s;
    }

    .appointment-row:hover {
      background: #f8fafc;
      border-color: #cbd5e1;
    }

    .appt-details {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .appt-name-row {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .appt-p-name {
      font-weight: 700;
      color: #0f172a;
      font-size: 0.9375rem;
    }

    .mrn-tag {
      font-family: monospace;
      font-size: 0.6875rem;
      background: #ecfeff;
      color: #0891b2;
      padding: 0.1rem 0.4rem;
      border-radius: 4px;
      font-weight: 700;
    }

    .appt-meta {
      font-size: 0.75rem;
      color: #64748b;
    }

    .checkin-btn {
      padding: 0.4rem 0.85rem;
      font-size: 0.8125rem;
    }

    .checkedin-badge {
      display: flex;
      flex-direction: column;
      align-items: center;
      background: #ecfdf5;
      border: 1px solid #a7f3d0;
      padding: 0.35rem 0.75rem;
      border-radius: 8px;
    }

    .token-lbl {
      font-size: 0.5625rem;
      font-weight: 800;
      color: #059669;
      letter-spacing: 0.05em;
    }

    .token-num {
      font-size: 1.125rem;
      font-weight: 800;
      color: #047857;
      font-family: monospace;
    }

    .queue-item {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 0.875rem 1rem;
      border: 1px solid #e2e8f0;
      border-radius: 10px;
      transition: all 0.2s;
    }

    .queue-item.current {
      border-color: #06b6d4;
      background: #f0fdfa;
      box-shadow: 0 0 12px rgba(6, 182, 212, 0.15);
    }

    .token-chip {
      width: 44px;
      height: 44px;
      border-radius: 10px;
      background: #f1f5f9;
      color: #334155;
      font-size: 1.125rem;
      font-weight: 800;
      display: flex;
      align-items: center;
      justify-content: center;
      font-family: monospace;
    }

    .token-chip.highlight {
      background: linear-gradient(135deg, #0891b2, #06b6d4);
      color: #ffffff;
      box-shadow: 0 4px 10px rgba(8, 145, 178, 0.4);
    }

    .q-patient-info {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 0.2rem;
    }

    .q-name-row {
      display: flex;
      align-items: center;
      gap: 0.625rem;
    }

    .q-patient-name {
      font-weight: 700;
      color: #0f172a;
      font-size: 0.9375rem;
    }

    .now-serving-badge {
      font-size: 0.625rem;
      font-weight: 800;
      background: #10b981;
      color: #ffffff;
      padding: 0.15rem 0.5rem;
      border-radius: 20px;
      letter-spacing: 0.05em;
    }

    .q-meta {
      font-size: 0.75rem;
      color: #64748b;
    }

    .modal-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(15, 23, 42, 0.7);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 100;
    }

    .modal-card {
      background: #ffffff;
      border-radius: 16px;
      padding: 2rem;
      max-width: 420px;
      width: 90%;
      text-align: center;
    }

    .token-slip {
      border: 2px dashed #cbd5e1;
      border-radius: 12px;
      padding: 1.5rem;
      background: #f8fafc;
      margin-bottom: 1.5rem;
    }

    .slip-h-name {
      font-size: 1.125rem;
      font-weight: 800;
      color: #0f172a;
      letter-spacing: 0.05em;
    }

    .slip-sub {
      font-size: 0.75rem;
      color: #64748b;
      margin-top: 0.2rem;
    }

    .slip-divider {
      height: 1px;
      background: #e2e8f0;
      margin: 0.875rem 0;
    }

    .slip-token-box {
      margin: 1rem 0;
    }

    .slip-token-label {
      font-size: 0.6875rem;
      font-weight: 700;
      color: #0891b2;
      letter-spacing: 0.08em;
      display: block;
    }

    .slip-token-number {
      font-size: 3rem;
      font-weight: 900;
      color: #0891b2;
      font-family: monospace;
      line-height: 1.1;
    }

    .slip-info {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      font-size: 0.8125rem;
      text-align: left;
      margin-bottom: 1rem;
    }

    .slip-row {
      display: flex;
      justify-content: space-between;
    }

    .slip-row span { color: #64748b; }
    .slip-row strong { color: #0f172a; }

    .slip-footer p {
      font-size: 0.6875rem;
      color: #64748b;
      line-height: 1.4;
    }

    .modal-actions {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }
  `]
})
export class OpdQueueComponent implements OnInit {
  private readonly apiService = inject(ApiService);
  readonly langService = inject(LanguageService);
  readonly t = this.langService.t;

  readonly doctors = signal<DoctorDto[]>([]);
  selectedDoctorId: string = '';

  readonly appointments = signal<AppointmentResponse[]>([]);
  readonly queueItems = signal<OpdQueueItemDto[]>([]);
  readonly isLoadingAppointments = signal<boolean>(false);
  readonly isLoadingQueue = signal<boolean>(false);
  readonly isCheckingIn = signal<string | null>(null);
  readonly generatedToken = signal<CheckInResponse | null>(null);

  ngOnInit(): void {
    this.loadDoctors();
    this.loadTodayAppointments();
  }

  loadDoctors(): void {
    this.apiService.getDoctors().subscribe(docs => {
      this.doctors.set(docs);
      if (docs.length > 0) {
        this.selectedDoctorId = docs[0].id;
        this.loadDoctorQueue();
      }
    });
  }

  loadTodayAppointments(): void {
    this.isLoadingAppointments.set(true);
    const today = new Date().toISOString().split('T')[0];
    this.apiService.getAppointments(today).subscribe({
      next: appts => {
        this.appointments.set(appts);
        this.isLoadingAppointments.set(false);
      },
      error: err => {
        console.error('Failed to load appointments', err);
        this.isLoadingAppointments.set(false);
      }
    });
  }

  loadDoctorQueue(): void {
    if (!this.selectedDoctorId) return;
    this.isLoadingQueue.set(true);
    const today = new Date().toISOString().split('T')[0];
    this.apiService.getDoctorQueue(this.selectedDoctorId, today).subscribe({
      next: q => {
        this.queueItems.set(q);
        this.isLoadingQueue.set(false);
      },
      error: err => {
        console.error('Failed to load doctor queue', err);
        this.isLoadingQueue.set(false);
      }
    });
  }

  checkIn(appointment: AppointmentResponse): void {
    this.isCheckingIn.set(appointment.id);
    this.apiService.checkInAppointment(appointment.id).subscribe({
      next: res => {
        this.isCheckingIn.set(null);
        this.generatedToken.set(res);
        this.loadTodayAppointments(); // Update list
        if (this.selectedDoctorId === appointment.doctorId) {
          this.loadDoctorQueue(); // Update queue
        }
      },
      error: err => {
        this.isCheckingIn.set(null);
        alert(err.error?.detail || err.error?.title || 'Check-in failed');
      }
    });
  }

  getCheckedInCount(): number {
    return this.appointments().filter(a => a.status === 1).length;
  }

  getInConsultCount(): number {
    return this.appointments().filter(a => a.status === 2).length;
  }

  printToken(): void {
    window.print();
  }
}
