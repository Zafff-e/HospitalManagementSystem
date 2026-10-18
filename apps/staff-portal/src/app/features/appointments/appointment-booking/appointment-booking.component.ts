import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { LanguageService } from '../../../core/services/language.service';
import { DoctorDto, TimeSlotDto, AppointmentResponse } from '../../../core/models/appointment.models';
import { PatientResponse } from '../../../core/models/patient.models';

@Component({
  selector: 'app-appointment-booking',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="booking-container animate-fade-in">
      <div class="header-section">
        <div>
          <h1 class="page-title">{{ t().navAppointments }} & Scheduling</h1>
          <p class="page-subtitle">Schedule OPD slots with doctors and manage consultation rosters</p>
        </div>
        <a routerLink="/opd-queue" class="btn-secondary">
          🏥 View Live OPD Queue
        </a>
      </div>

      <div class="booking-grid">
        <!-- Left Panel: Doctor & Date & Slot Selection -->
        <div class="selection-panel glass-card">
          <!-- Step 1: Select Doctor -->
          <div class="panel-section">
            <h3 class="section-title">
              <span class="step-num">1</span> Select Specialist Doctor
            </h3>
            <div class="doctors-list">
              <div
                *ngFor="let doc of doctors()"
                class="doctor-card"
                [class.selected]="selectedDoctor()?.id === doc.id"
                (click)="selectDoctor(doc)"
              >
                <div class="doc-avatar">👨‍⚕️</div>
                <div class="doc-info">
                  <div class="doc-header">
                    <span class="doc-name">{{ doc.fullName }}</span>
                    <span class="doc-fee">৳ {{ doc.consultationFee }}</span>
                  </div>
                  <span class="doc-specialty">{{ doc.specialty }}</span>
                  <span class="doc-meta">BMDC: {{ doc.bmdcRegNo }} • {{ doc.qualification }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Step 2: Select Date -->
          <div class="panel-section" *ngIf="selectedDoctor()">
            <h3 class="section-title">
              <span class="step-num">2</span> Select Date
            </h3>
            <div class="date-picker-row">
              <input
                type="date"
                class="form-input date-input"
                [ngModel]="selectedDate()"
                (ngModelChange)="onDateChange($event)"
              />
              <span class="date-day-badge">{{ getDayName(selectedDate()) }}</span>
            </div>
          </div>

          <!-- Step 3: Time Slot Grid -->
          <div class="panel-section" *ngIf="selectedDoctor()">
            <div class="slots-header">
              <h3 class="section-title">
                <span class="step-num">3</span> Available Consultation Slots
              </h3>
              <div class="slots-legend">
                <span class="legend-item"><span class="legend-dot available"></span> Available</span>
                <span class="legend-item"><span class="legend-dot booked"></span> Booked</span>
              </div>
            </div>

            <div *ngIf="isLoadingSlots()" class="slots-loading">
              <div class="spinner"></div>
              <span>Generating consultation schedule...</span>
            </div>

            <div *ngIf="!isLoadingSlots() && slots().length === 0" class="slots-empty">
              <p>No available consultation slots for this doctor on {{ selectedDate() }}.</p>
              <span class="sub-hint">Check doctor schedule or choose another date (Sat–Thu).</span>
            </div>

            <div *ngIf="!isLoadingSlots() && slots().length > 0" class="slots-grid">
              <button
                *ngFor="let slot of slots()"
                class="slot-tile"
                [class.disabled]="!slot.isAvailable"
                [class.selected]="selectedSlot()?.slotTime === slot.slotTime"
                [disabled]="!slot.isAvailable"
                (click)="selectSlot(slot)"
              >
                <span class="slot-time">{{ slot.slotTime | date:'shortTime' }}</span>
                <span class="slot-status">{{ slot.isAvailable ? 'Available' : 'Booked' }}</span>
              </button>
            </div>
          </div>
        </div>

        <!-- Right Panel: Patient Assignment & Booking Review -->
        <div class="summary-panel glass-card">
          <h3 class="summary-title">Appointment Summary</h3>

          <!-- Patient Search / Selector -->
          <div class="patient-assign-section">
            <label class="form-label">Assign Patient (MRN or Name)</label>
            <div class="patient-search-box">
              <input
                type="text"
                class="form-input"
                placeholder="Type to search registered patient..."
                [(ngModel)]="patientQuery"
                (ngModelChange)="searchPatients($event)"
              />
            </div>

            <!-- Patient Suggestions -->
            <div *ngIf="suggestedPatients().length > 0" class="patient-dropdown">
              <div
                *ngFor="let p of suggestedPatients()"
                class="patient-option"
                (click)="selectPatient(p)"
              >
                <div class="p-option-name">{{ p.displayName }}</div>
                <div class="p-option-sub">MRN: {{ p.mrn }} • 📞 {{ p.phonePrimary }}</div>
              </div>
            </div>

            <!-- Selected Patient Card -->
            <div *ngIf="selectedPatient()" class="selected-patient-card animate-fade-in">
              <div class="sp-avatar">PT</div>
              <div class="sp-details">
                <span class="sp-name">{{ selectedPatient()?.displayName }}</span>
                <span class="sp-meta">MRN: {{ selectedPatient()?.mrn }} • Phone: {{ selectedPatient()?.phonePrimary }}</span>
              </div>
              <button class="remove-p-btn" (click)="selectedPatient.set(null)">✕</button>
            </div>
          </div>

          <!-- Reason for Visit -->
          <div class="form-group" style="margin-top: 1rem;">
            <label class="form-label">Chief Complaint / Visit Reason</label>
            <textarea
              class="form-textarea"
              rows="3"
              placeholder="e.g. Chest pain, Routine cardiology follow-up"
              [(ngModel)]="visitReason"
            ></textarea>
          </div>

          <!-- Booking Summary Details -->
          <div class="booking-receipt">
            <div class="receipt-row">
              <span class="r-label">Doctor:</span>
              <span class="r-val">{{ selectedDoctor()?.fullName || 'Not Selected' }}</span>
            </div>
            <div class="receipt-row">
              <span class="r-label">Department:</span>
              <span class="r-val">{{ selectedDoctor()?.specialty || '-' }}</span>
            </div>
            <div class="receipt-row">
              <span class="r-label">Slot:</span>
              <span class="r-val">{{ selectedSlot() ? (selectedSlot()!.slotTime | date:'medium') : 'Not Selected' }}</span>
            </div>
            <div class="receipt-row">
              <span class="r-label">Consultation Fee:</span>
              <span class="r-val fee-val">৳ {{ selectedDoctor()?.consultationFee || 0 }}</span>
            </div>
          </div>

          <!-- Confirm Booking CTA -->
          <button
            class="btn-primary confirm-btn"
            [disabled]="!canConfirm() || isBooking()"
            (click)="confirmBooking()"
          >
            <span *ngIf="isBooking()" class="spinner-small"></span>
            <span>{{ isBooking() ? 'Booking...' : 'Confirm Appointment' }}</span>
          </button>
        </div>
      </div>

      <!-- Success Confirmation Modal -->
      <div *ngIf="confirmedBooking()" class="modal-backdrop">
        <div class="modal-card animate-fade-in">
          <div class="success-icon">✓</div>
          <h2 class="modal-title">Appointment Confirmed!</h2>
          <p class="modal-desc">Consultation slot successfully booked.</p>

          <div class="ticket-box">
            <div class="ticket-row">
              <span class="t-label">Doctor:</span>
              <span class="t-val">{{ confirmedBooking()?.doctorName }}</span>
            </div>
            <div class="ticket-row">
              <span class="t-label">Patient:</span>
              <span class="t-val">{{ confirmedBooking()?.patientName }} ({{ confirmedBooking()?.patientMrn }})</span>
            </div>
            <div class="ticket-row">
              <span class="t-label">Date & Time:</span>
              <span class="t-val">{{ confirmedBooking()?.scheduledAt | date:'medium' }}</span>
            </div>
          </div>

          <div class="modal-actions">
            <button class="btn-primary" (click)="confirmedBooking.set(null)">
              Book Another Appointment
            </button>
            <a routerLink="/opd-queue" class="btn-secondary">
              Go to OPD Check-in
            </a>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .booking-container {
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

    .booking-grid {
      display: grid;
      grid-template-columns: 1.4fr 1fr;
      gap: 1.5rem;
    }

    .selection-panel, .summary-panel {
      background: #ffffff;
      padding: 1.75rem;
    }

    .panel-section {
      margin-bottom: 2rem;
    }

    .section-title {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      font-size: 1.0625rem;
      font-weight: 700;
      color: #0f172a;
      margin-bottom: 1rem;
    }

    .step-num {
      width: 24px;
      height: 24px;
      border-radius: 50%;
      background: #ecfeff;
      color: #0891b2;
      font-size: 0.75rem;
      font-weight: 800;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .doctors-list {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .doctor-card {
      display: flex;
      align-items: center;
      gap: 1rem;
      padding: 1rem;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      cursor: pointer;
      transition: all 0.2s;
    }

    .doctor-card:hover {
      border-color: #06b6d4;
      background: #f8fafc;
    }

    .doctor-card.selected {
      border-color: #0891b2;
      background: #ecfeff;
      box-shadow: 0 0 0 2px rgba(8, 145, 178, 0.2);
    }

    .doc-avatar {
      font-size: 2rem;
    }

    .doc-info {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 0.2rem;
    }

    .doc-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .doc-name {
      font-weight: 700;
      color: #0f172a;
      font-size: 0.9375rem;
    }

    .doc-fee {
      font-weight: 800;
      color: #0891b2;
      font-size: 0.875rem;
    }

    .doc-specialty {
      font-size: 0.8125rem;
      color: #0284c7;
      font-weight: 600;
    }

    .doc-meta {
      font-size: 0.6875rem;
      color: #64748b;
    }

    .date-picker-row {
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .date-input {
      max-width: 250px;
    }

    .date-day-badge {
      font-size: 0.8125rem;
      font-weight: 700;
      background: #f1f5f9;
      color: #475569;
      padding: 0.4rem 0.85rem;
      border-radius: 8px;
    }

    .slots-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
    }

    .slots-legend {
      display: flex;
      gap: 1rem;
      font-size: 0.75rem;
      color: #64748b;
    }

    .legend-item {
      display: flex;
      align-items: center;
      gap: 0.35rem;
    }

    .legend-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
    }

    .legend-dot.available { background: #10b981; }
    .legend-dot.booked { background: #ef4444; }

    .slots-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(95px, 1fr));
      gap: 0.625rem;
    }

    .slot-tile {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.2rem;
      padding: 0.625rem 0.5rem;
      border-radius: 8px;
      border: 1px solid #cbd5e1;
      background: #ffffff;
      cursor: pointer;
      transition: all 0.2s;
    }

    .slot-tile:hover:not(.disabled) {
      border-color: #0891b2;
      background: #f0fdfa;
    }

    .slot-tile.selected {
      background: #0891b2;
      border-color: #0891b2;
      color: #ffffff;
    }

    .slot-tile.selected .slot-time,
    .slot-tile.selected .slot-status {
      color: #ffffff;
    }

    .slot-tile.disabled {
      background: #f1f5f9;
      border-color: #e2e8f0;
      opacity: 0.6;
      cursor: not-allowed;
    }

    .slot-time {
      font-size: 0.8125rem;
      font-weight: 700;
      color: #0f172a;
    }

    .slot-status {
      font-size: 0.625rem;
      color: #059669;
      font-weight: 600;
    }

    .slot-tile.disabled .slot-status {
      color: #ef4444;
    }

    .slots-empty, .slots-loading {
      padding: 2rem;
      text-align: center;
      color: #64748b;
      font-size: 0.875rem;
    }

    .sub-hint {
      font-size: 0.75rem;
      color: #94a3b8;
    }

    .summary-title {
      font-size: 1.125rem;
      font-weight: 800;
      color: #0f172a;
      margin-bottom: 1.25rem;
      border-bottom: 1px solid #f1f5f9;
      padding-bottom: 0.75rem;
    }

    .patient-search-box {
      position: relative;
    }

    .patient-dropdown {
      background: #ffffff;
      border: 1px solid #cbd5e1;
      border-radius: 8px;
      max-height: 200px;
      overflow-y: auto;
      margin-top: 0.25rem;
      box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
    }

    .patient-option {
      padding: 0.625rem 0.875rem;
      cursor: pointer;
      border-bottom: 1px solid #f1f5f9;
    }

    .patient-option:hover {
      background: #ecfeff;
    }

    .p-option-name {
      font-weight: 600;
      color: #0f172a;
      font-size: 0.875rem;
    }

    .p-option-sub {
      font-size: 0.75rem;
      color: #64748b;
    }

    .selected-patient-card {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.75rem 1rem;
      background: #f0fdfa;
      border: 1px solid #99f6e4;
      border-radius: 10px;
      margin-top: 0.75rem;
    }

    .sp-avatar {
      width: 34px;
      height: 34px;
      border-radius: 50%;
      background: #0891b2;
      color: #ffffff;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 0.75rem;
      font-weight: 800;
    }

    .sp-details {
      flex: 1;
      display: flex;
      flex-direction: column;
    }

    .sp-name {
      font-weight: 700;
      color: #0f172a;
      font-size: 0.875rem;
    }

    .sp-meta {
      font-size: 0.6875rem;
      color: #0d9488;
    }

    .remove-p-btn {
      background: none;
      border: none;
      color: #94a3b8;
      cursor: pointer;
      font-size: 1rem;
    }

    .booking-receipt {
      background: #f8fafc;
      border-radius: 12px;
      padding: 1.25rem;
      margin: 1.5rem 0;
      display: flex;
      flex-direction: column;
      gap: 0.625rem;
    }

    .receipt-row {
      display: flex;
      justify-content: space-between;
      font-size: 0.875rem;
    }

    .r-label {
      color: #64748b;
      font-weight: 600;
    }

    .r-val {
      font-weight: 700;
      color: #0f172a;
    }

    .fee-val {
      color: #0891b2;
      font-size: 1.0625rem;
    }

    .confirm-btn {
      width: 100%;
      padding: 0.875rem;
      font-size: 1rem;
    }

    .confirm-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
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
      padding: 2.5rem;
      max-width: 480px;
      width: 90%;
      text-align: center;
    }

    .success-icon {
      width: 60px;
      height: 60px;
      border-radius: 50%;
      background: #ecfdf5;
      color: #10b981;
      font-size: 2rem;
      display: flex;
      align-items: center;
      justify-content: center;
      margin: 0 auto 1.25rem;
      border: 2px solid #a7f3d0;
    }

    .ticket-box {
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      padding: 1.25rem;
      margin: 1.5rem 0;
      text-align: left;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .ticket-row {
      display: flex;
      justify-content: space-between;
      font-size: 0.875rem;
    }

    .t-label {
      color: #64748b;
      font-weight: 600;
    }

    .t-val {
      font-weight: 700;
      color: #0f172a;
    }

    .modal-actions {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }
  `]
})
export class AppointmentBookingComponent implements OnInit {
  private readonly apiService = inject(ApiService);
  readonly langService = inject(LanguageService);
  readonly t = this.langService.t;

  readonly doctors = signal<DoctorDto[]>([]);
  readonly selectedDoctor = signal<DoctorDto | null>(null);
  readonly selectedDate = signal<string>(new Date().toISOString().split('T')[0]);
  readonly slots = signal<TimeSlotDto[]>([]);
  readonly selectedSlot = signal<TimeSlotDto | null>(null);
  readonly isLoadingSlots = signal<boolean>(false);

  patientQuery = '';
  readonly suggestedPatients = signal<PatientResponse[]>([]);
  readonly selectedPatient = signal<PatientResponse | null>(null);
  visitReason = '';

  readonly isBooking = signal<boolean>(false);
  readonly confirmedBooking = signal<AppointmentResponse | null>(null);

  ngOnInit(): void {
    this.loadDoctors();
  }

  loadDoctors(): void {
    this.apiService.getDoctors().subscribe({
      next: docs => {
        this.doctors.set(docs);
        if (docs.length > 0) {
          this.selectDoctor(docs[0]);
        }
      },
      error: err => console.error('Failed to load doctors', err)
    });
  }

  selectDoctor(doc: DoctorDto): void {
    this.selectedDoctor.set(doc);
    this.selectedSlot.set(null);
    this.loadSlots();
  }

  onDateChange(newDate: string): void {
    this.selectedDate.set(newDate);
    this.selectedSlot.set(null);
    this.loadSlots();
  }

  loadSlots(): void {
    const doc = this.selectedDoctor();
    const date = this.selectedDate();
    if (!doc || !date) return;

    this.isLoadingSlots.set(true);
    this.apiService.getAvailableSlots(doc.id, date).subscribe({
      next: s => {
        this.slots.set(s);
        this.isLoadingSlots.set(false);
      },
      error: err => {
        console.error('Failed to load slots', err);
        this.slots.set([]);
        this.isLoadingSlots.set(false);
      }
    });
  }

  selectSlot(slot: TimeSlotDto): void {
    if (slot.isAvailable) {
      this.selectedSlot.set(slot);
    }
  }

  searchPatients(q: string): void {
    if (!q || q.length < 2) {
      this.suggestedPatients.set([]);
      return;
    }
    this.apiService.searchPatients({ query: q, pageSize: 5 }).subscribe(res => {
      this.suggestedPatients.set(res.items);
    });
  }

  selectPatient(p: PatientResponse): void {
    this.selectedPatient.set(p);
    this.suggestedPatients.set([]);
    this.patientQuery = '';
  }

  canConfirm(): boolean {
    return !!(this.selectedDoctor() && this.selectedSlot() && this.selectedPatient());
  }

  confirmBooking(): void {
    if (!this.canConfirm()) return;

    this.isBooking.set(true);
    const req = {
      patientId: this.selectedPatient()!.id,
      doctorId: this.selectedDoctor()!.id,
      scheduledAt: this.selectedSlot()!.slotTime,
      reason: this.visitReason || undefined
    };

    this.apiService.bookAppointment(req).subscribe({
      next: res => {
        this.isBooking.set(false);
        this.confirmedBooking.set(res);
        this.loadSlots(); // Refresh slot status
      },
      error: err => {
        this.isBooking.set(false);
        alert(err.error?.detail || err.error?.title || 'Appointment booking failed');
      }
    });
  }

  getDayName(dateStr: string): string {
    const d = new Date(dateStr);
    return d.toLocaleDateString('en-US', { weekday: 'long' });
  }
}
