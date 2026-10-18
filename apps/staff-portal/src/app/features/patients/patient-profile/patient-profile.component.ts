import { Component, inject, signal, OnInit, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { LanguageService } from '../../../core/services/language.service';
import { PatientResponse, Gender, BloodGroup } from '../../../core/models/patient.models';

@Component({
  selector: 'app-patient-profile',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="profile-container animate-fade-in" *ngIf="patient() as p">
      <!-- Back Nav & Actions -->
      <div class="top-nav">
        <a routerLink="/patients" class="back-link">
          ← Back to Patient List
        </a>
        <div class="profile-actions">
          <button class="btn-secondary" (click)="printCard()">
            🖨️ Print Health Card
          </button>
          <a routerLink="/appointments" class="btn-primary">
            📅 Book Appointment
          </a>
        </div>
      </div>

      <!-- Main Profile Banner -->
      <div class="glass-card hero-card">
        <div class="hero-left">
          <div class="avatar-large" [style.background]="getAvatarColor(p.mrn)">
            {{ getInitials(p.firstNameEn || p.displayName) }}
          </div>
          <div class="hero-info">
            <div class="name-row">
              <h1 class="hero-name">{{ p.displayName }}</h1>
              <span class="status-pill active">Registered</span>
            </div>
            <p *ngIf="p.firstNameBn" class="hero-bengali font-bn">
              {{ p.firstNameBn }} {{ p.lastNameBn }}
            </p>

            <div class="meta-row">
              <span class="meta-item">
                <strong>{{ p.ageYears ?? '-' }} {{ t().years }}</strong> • {{ getGenderLabel(p.gender) }}
              </span>
              <span class="meta-divider">•</span>
              <span class="blood-pill">
                🩸 {{ getBloodGroupLabel(p.bloodGroup) }}
              </span>
              <span class="meta-divider">•</span>
              <span class="meta-item">
                📞 {{ p.phonePrimary }}
              </span>
            </div>
          </div>
        </div>

        <div class="hero-right">
          <div class="mrn-badge-card">
            <span class="mrn-label">MEDICAL RECORD NUMBER</span>
            <span class="mrn-value">{{ p.mrn }}</span>
            <div class="barcode-visual">
              <div class="bar" style="width: 2px"></div>
              <div class="bar" style="width: 4px"></div>
              <div class="bar" style="width: 1px"></div>
              <div class="bar" style="width: 3px"></div>
              <div class="bar" style="width: 2px"></div>
              <div class="bar" style="width: 5px"></div>
              <div class="bar" style="width: 2px"></div>
              <div class="bar" style="width: 1px"></div>
              <div class="bar" style="width: 3px"></div>
              <div class="bar" style="width: 4px"></div>
              <div class="bar" style="width: 2px"></div>
            </div>
          </div>
        </div>
      </div>

      <!-- Grid Details -->
      <div class="details-grid">
        <!-- Personal & Identity Details -->
        <div class="glass-card detail-card">
          <h3 class="card-heading">
            <span>Identity & Demographics</span>
          </h3>
          <div class="info-list">
            <div class="info-item">
              <span class="info-label">Date of Birth</span>
              <span class="info-val">{{ p.dateOfBirth ? (p.dateOfBirth | date:'mediumDate') : 'Not specified' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">National ID (Masked)</span>
              <span class="info-val font-mono">{{ p.nidLast4 ? '•••• •••• ' + p.nidLast4 : 'Not Provided' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">Birth Registration No</span>
              <span class="info-val font-mono">{{ p.birthRegNo || 'Not Provided' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">Occupation</span>
              <span class="info-val">{{ p.occupation || 'Not Specified' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">Registered Since</span>
              <span class="info-val">{{ p.createdAt | date:'medium' }}</span>
            </div>
          </div>
        </div>

        <!-- Address & Geo-Hierarchy -->
        <div class="glass-card detail-card">
          <h3 class="card-heading">
            <span>Location & Address</span>
          </h3>
          <div class="info-list">
            <div class="info-item">
              <span class="info-label">Division</span>
              <span class="info-val">{{ p.divisionName || 'Dhaka' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">District</span>
              <span class="info-val">{{ p.districtName || 'Dhaka' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">Upazila / Thana</span>
              <span class="info-val">{{ p.upazilaName || 'Dhanmondi' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">Street / House Address</span>
              <span class="info-val">{{ p.addressLine || 'Not Provided' }}</span>
            </div>
            <div class="info-item">
              <span class="info-label">Secondary Mobile</span>
              <span class="info-val">{{ p.phoneSecondary || 'None' }}</span>
            </div>
          </div>
        </div>

        <!-- Emergency Contact Card -->
        <div class="glass-card detail-card">
          <h3 class="card-heading">
            <span>Emergency Contact</span>
          </h3>
          <div class="emergency-box">
            <div class="ec-icon">🚨</div>
            <div class="ec-details">
              <span class="ec-name">Next-of-Kin Primary Contact</span>
              <span class="ec-phone">{{ p.phonePrimary }}</span>
              <span class="ec-rel">Hospital Verified</span>
            </div>
          </div>
          <div class="emergency-notes">
            <p>In case of critical clinical emergency, automated SMS notifications will be dispatched to this number via Bangladesh SMS Gateway.</p>
          </div>
        </div>
      </div>

      <!-- Clinical Tabs Placeholder -->
      <div class="glass-card history-card">
        <div class="tabs-header">
          <button class="tab-btn active">Appointments & OPD Visits (0)</button>
          <button class="tab-btn">Prescriptions & EMR</button>
          <button class="tab-btn">Lab Investigations</button>
          <button class="tab-btn">Billing & Receipts</button>
        </div>
        <div class="tab-content">
          <div class="empty-timeline">
            <div class="empty-icon">🩺</div>
            <h4>No active appointments or clinical visits yet</h4>
            <p>Click "Book Appointment" to assign a doctor schedule or generate an OPD token.</p>
          </div>
        </div>
      </div>
    </div>

    <div *ngIf="isLoading()" class="loading-container">
      <div class="spinner"></div>
      <p>Loading patient record...</p>
    </div>
  `,
  styles: [`
    .profile-container {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
      max-width: 1100px;
      margin: 0 auto;
    }

    .top-nav {
      display: flex;
      align-items: center;
      justify-content: space-between;
    }

    .back-link {
      color: #0891b2;
      font-weight: 600;
      font-size: 0.875rem;
      text-decoration: none;
    }

    .profile-actions {
      display: flex;
      gap: 0.75rem;
    }

    .hero-card {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1.75rem 2rem;
      background: #ffffff;
    }

    .hero-left {
      display: flex;
      align-items: center;
      gap: 1.5rem;
    }

    .avatar-large {
      width: 72px;
      height: 72px;
      border-radius: 50%;
      color: #ffffff;
      font-size: 1.5rem;
      font-weight: 800;
      display: flex;
      align-items: center;
      justify-content: center;
      box-shadow: 0 4px 14px rgba(0, 0, 0, 0.1);
    }

    .hero-info {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .name-row {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .hero-name {
      font-size: 1.5rem;
      font-weight: 800;
      color: #0f172a;
    }

    .status-pill {
      font-size: 0.6875rem;
      font-weight: 700;
      background: #ecfdf5;
      color: #059669;
      padding: 0.2rem 0.6rem;
      border-radius: 20px;
      border: 1px solid #a7f3d0;
    }

    .hero-bengali {
      font-size: 0.9375rem;
      color: #64748b;
    }

    .meta-row {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      font-size: 0.875rem;
      color: #475569;
      margin-top: 0.25rem;
    }

    .meta-divider {
      color: #cbd5e1;
    }

    .blood-pill {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
      background: #fff1f2;
      color: #e11d48;
      border: 1px solid #fecdd3;
      padding: 0.15rem 0.5rem;
      border-radius: 20px;
      font-size: 0.75rem;
      font-weight: 700;
    }

    .mrn-badge-card {
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      padding: 1rem 1.5rem;
      text-align: right;
    }

    .mrn-label {
      font-size: 0.625rem;
      font-weight: 700;
      color: #64748b;
      letter-spacing: 0.08em;
      display: block;
    }

    .mrn-value {
      font-size: 1.5rem;
      font-weight: 800;
      color: #0891b2;
      font-family: monospace;
    }

    .barcode-visual {
      display: flex;
      gap: 2px;
      justify-content: flex-end;
      height: 20px;
      margin-top: 0.5rem;
    }

    .bar {
      background: #334155;
      height: 100%;
    }

    .details-grid {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 1.25rem;
    }

    .detail-card {
      background: #ffffff;
      padding: 1.5rem;
    }

    .card-heading {
      font-size: 0.9375rem;
      font-weight: 700;
      color: #0f172a;
      margin-bottom: 1rem;
      border-bottom: 1px solid #f1f5f9;
      padding-bottom: 0.5rem;
    }

    .info-list {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .info-item {
      display: flex;
      flex-direction: column;
      gap: 0.2rem;
    }

    .info-label {
      font-size: 0.75rem;
      color: #64748b;
      font-weight: 600;
    }

    .info-val {
      font-size: 0.875rem;
      color: #1e293b;
      font-weight: 500;
    }

    .font-mono {
      font-family: monospace;
    }

    .emergency-box {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      background: #fffbeb;
      border: 1px solid #fde68a;
      border-radius: 10px;
      padding: 1rem;
      margin-bottom: 1rem;
    }

    .ec-icon {
      font-size: 1.5rem;
    }

    .ec-details {
      display: flex;
      flex-direction: column;
    }

    .ec-name {
      font-size: 0.875rem;
      font-weight: 700;
      color: #92400e;
    }

    .ec-phone {
      font-size: 0.8125rem;
      font-weight: 600;
      color: #78350f;
      font-family: monospace;
    }

    .ec-rel {
      font-size: 0.6875rem;
      color: #b45309;
    }

    .emergency-notes {
      font-size: 0.75rem;
      color: #64748b;
      line-height: 1.4;
    }

    .history-card {
      background: #ffffff;
      padding: 0;
      overflow: hidden;
    }

    .tabs-header {
      display: flex;
      border-bottom: 1px solid #e2e8f0;
      background: #f8fafc;
    }

    .tab-btn {
      padding: 0.875rem 1.5rem;
      background: none;
      border: none;
      font-size: 0.875rem;
      font-weight: 600;
      color: #64748b;
      cursor: pointer;
      border-bottom: 2px solid transparent;
    }

    .tab-btn.active {
      color: #0891b2;
      border-bottom-color: #0891b2;
      background: #ffffff;
    }

    .tab-content {
      padding: 2.5rem;
      text-align: center;
    }

    .empty-timeline .empty-icon {
      font-size: 2.5rem;
      margin-bottom: 0.5rem;
    }

    .empty-timeline h4 {
      font-size: 1rem;
      color: #0f172a;
      font-weight: 700;
    }

    .empty-timeline p {
      font-size: 0.8125rem;
      color: #64748b;
      margin-top: 0.25rem;
    }

    .loading-container {
      padding: 4rem;
      text-align: center;
      color: #64748b;
    }

    .spinner {
      width: 36px;
      height: 36px;
      border: 3px solid #e2e8f0;
      border-top-color: #0891b2;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
      margin: 0 auto 1rem;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }
  `]
})
export class PatientProfileComponent implements OnInit {
  id = input.required<string>();

  private readonly apiService = inject(ApiService);
  readonly langService = inject(LanguageService);
  readonly t = this.langService.t;

  readonly patient = signal<PatientResponse | null>(null);
  readonly isLoading = signal<boolean>(true);

  ngOnInit(): void {
    const patientId = this.id();
    if (patientId) {
      this.apiService.getPatientById(patientId).subscribe({
        next: p => {
          this.patient.set(p);
          this.isLoading.set(false);
        },
        error: err => {
          console.error('Failed to load patient', err);
          this.isLoading.set(false);
        }
      });
    }
  }

  printCard(): void {
    window.print();
  }

  getGenderLabel(gender: Gender): string {
    switch (gender) {
      case Gender.Male: return this.t().male;
      case Gender.Female: return this.t().female;
      default: return this.t().other;
    }
  }

  getBloodGroupLabel(bg: BloodGroup): string {
    switch (bg) {
      case BloodGroup.APositive: return 'A+';
      case BloodGroup.ANegative: return 'A-';
      case BloodGroup.BPositive: return 'B+';
      case BloodGroup.BNegative: return 'B-';
      case BloodGroup.ABPositive: return 'AB+';
      case BloodGroup.ABNegative: return 'AB-';
      case BloodGroup.OPositive: return 'O+';
      case BloodGroup.ONegative: return 'O-';
      default: return 'Unknown';
    }
  }

  getInitials(name: string): string {
    if (!name) return 'PT';
    const parts = name.trim().split(' ');
    if (parts.length >= 2) return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
    return name.slice(0, 2).toUpperCase();
  }

  getAvatarColor(mrn: string): string {
    return 'linear-gradient(135deg, #06b6d4, #0891b2)';
  }
}
