import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { LanguageService } from '../../../core/services/language.service';
import {
  Gender,
  BloodGroup,
  DivisionDto,
  DistrictDto,
  UpazilaDto,
  DuplicateWarningDto,
  CreatePatientRequest
} from '../../../core/models/patient.models';

@Component({
  selector: 'app-patient-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="register-container animate-fade-in">
      <!-- Top Title & Navigation -->
      <div class="wizard-header">
        <div class="titles">
          <a routerLink="/patients" class="back-link">
            ← {{ t().navPatients }}
          </a>
          <h1 class="wizard-title">{{ t().registerNewPatient }}</h1>
          <p class="wizard-sub">Step-by-step registration with National ID & Geo-hierarchy validation</p>
        </div>
      </div>

      <!-- Stepper Indicator -->
      <div class="stepper-bar glass-card">
        <div class="step-item" [class.active]="currentStep() === 1" [class.completed]="currentStep() > 1" (click)="goToStep(1)">
          <div class="step-circle">1</div>
          <span class="step-title">{{ t().step1Title }}</span>
        </div>
        <div class="step-divider" [class.completed]="currentStep() > 1"></div>

        <div class="step-item" [class.active]="currentStep() === 2" [class.completed]="currentStep() > 2" (click)="goToStep(2)">
          <div class="step-circle">2</div>
          <span class="step-title">{{ t().step2Title }}</span>
        </div>
        <div class="step-divider" [class.completed]="currentStep() > 2"></div>

        <div class="step-item" [class.active]="currentStep() === 3" [class.completed]="currentStep() > 3" (click)="goToStep(3)">
          <div class="step-circle">3</div>
          <span class="step-title">{{ t().step3Title }}</span>
        </div>
        <div class="step-divider" [class.completed]="currentStep() > 3"></div>

        <div class="step-item" [class.active]="currentStep() === 4" (click)="goToStep(4)">
          <div class="step-circle">4</div>
          <span class="step-title">{{ t().step4Title }}</span>
        </div>
      </div>

      <!-- Main Form Area -->
      <form [formGroup]="patientForm" class="wizard-body glass-card">
        
        <!-- STEP 1: Personal Demographics -->
        <div *ngIf="currentStep() === 1" class="step-content">
          <h2 class="step-heading">
            <span class="step-num-pill">1</span> {{ t().step1Title }}
          </h2>
          <p class="step-desc">Enter the patient's personal details in English and/or Bengali.</p>

          <div class="form-grid">
            <div class="form-group">
              <label class="form-label">{{ t().firstNameEn }}</label>
              <input type="text" class="form-input" formControlName="firstNameEn" placeholder="e.g. Mohammad" />
            </div>

            <div class="form-group">
              <label class="form-label">{{ t().lastNameEn }}</label>
              <input type="text" class="form-input" formControlName="lastNameEn" placeholder="e.g. Rahman" />
            </div>

            <div class="form-group">
              <label class="form-label">{{ t().firstNameBn }}</label>
              <input type="text" class="form-input font-bn" formControlName="firstNameBn" placeholder="যেমন: মোহাম্মদ" />
            </div>

            <div class="form-group">
              <label class="form-label">{{ t().lastNameBn }}</label>
              <input type="text" class="form-input font-bn" formControlName="lastNameBn" placeholder="যেমন: রহমান" />
            </div>

            <div class="form-group">
              <label class="form-label">{{ t().dob }} <span class="required">*</span></label>
              <input type="date" class="form-input" formControlName="dateOfBirth" (change)="calculateAge()" />
              <span *ngIf="calculatedAge() !== null" class="age-hint">
                Calculated Age: <strong>{{ calculatedAge() }} {{ t().years }}</strong>
              </span>
            </div>

            <div class="form-group">
              <label class="form-label">{{ t().gender }} <span class="required">*</span></label>
              <select class="form-select" formControlName="gender">
                <option [value]="1">{{ t().male }}</option>
                <option [value]="2">{{ t().female }}</option>
                <option [value]="3">{{ t().other }}</option>
              </select>
            </div>

            <div class="form-group">
              <label class="form-label">{{ t().bloodGroup }}</label>
              <select class="form-select" formControlName="bloodGroup">
                <option [value]="0">Unknown</option>
                <option [value]="1">A+ (A Positive)</option>
                <option [value]="2">A- (A Negative)</option>
                <option [value]="3">B+ (B Positive)</option>
                <option [value]="4">B- (B Negative)</option>
                <option [value]="5">AB+ (AB Positive)</option>
                <option [value]="6">AB- (AB Negative)</option>
                <option [value]="7">O+ (O Positive)</option>
                <option [value]="8">O- (O Negative)</option>
              </select>
            </div>

            <div class="form-group">
              <label class="form-label">Occupation</label>
              <input type="text" class="form-input" formControlName="occupation" placeholder="e.g. Teacher, Engineer, Farmer" />
            </div>
          </div>
        </div>

        <!-- STEP 2: Contact & Location (Bangladesh Geo Hierarchy) -->
        <div *ngIf="currentStep() === 2" class="step-content">
          <h2 class="step-heading">
            <span class="step-num-pill">2</span> {{ t().step2Title }}
          </h2>
          <p class="step-desc">Provide primary contact number and cascaded geo-hierarchy address.</p>

          <div class="form-grid">
            <div class="form-group">
              <label class="form-label">{{ t().primaryPhone }} <span class="required">*</span></label>
              <input type="text" class="form-input" formControlName="phonePrimary" placeholder="01712345678 or +88017..." />
              <span class="hint-text">Standard 11-digit BD mobile format</span>
            </div>

            <div class="form-group">
              <label class="form-label">{{ t().secondaryPhone }}</label>
              <input type="text" class="form-input" formControlName="phoneSecondary" placeholder="Alternative number" />
            </div>

            <div class="form-group">
              <label class="form-label">{{ t().email }}</label>
              <input type="email" class="form-input" formControlName="email" placeholder="patient@example.com" />
            </div>

            <!-- Cascaded Geo-Hierarchy -->
            <div class="form-group">
              <label class="form-label">{{ t().division }}</label>
              <select class="form-select" formControlName="divisionId" (change)="onDivisionChange()">
                <option [ngValue]="null">Select Division</option>
                <option *ngFor="let div of divisions()" [ngValue]="div.id">
                  {{ div.nameEn }} ({{ div.nameBn }})
                </option>
              </select>
            </div>

            <div class="form-group">
              <label class="form-label">{{ t().district }}</label>
              <select class="form-select" formControlName="districtId" (change)="onDistrictChange()" [disabled]="districts().length === 0">
                <option [ngValue]="null">Select District</option>
                <option *ngFor="let dist of districts()" [ngValue]="dist.id">
                  {{ dist.nameEn }} ({{ dist.nameBn }})
                </option>
              </select>
            </div>

            <div class="form-group">
              <label class="form-label">{{ t().upazila }}</label>
              <select class="form-select" formControlName="upazilaId" [disabled]="upazilas().length === 0">
                <option [ngValue]="null">Select Upazila / Thana</option>
                <option *ngFor="let upz of upazilas()" [ngValue]="upz.id">
                  {{ upz.nameEn }} ({{ upz.nameBn }})
                </option>
              </select>
            </div>

            <div class="form-group full-width">
              <label class="form-label">{{ t().streetAddress }}</label>
              <input type="text" class="form-input" formControlName="addressLine" placeholder="House/Holding no., Road, Village, Area" />
            </div>
          </div>
        </div>

        <!-- STEP 3: Identity & Emergency Contact -->
        <div *ngIf="currentStep() === 3" class="step-content">
          <h2 class="step-heading">
            <span class="step-num-pill">3</span> {{ t().step3Title }}
          </h2>
          <p class="step-desc">Enter identification documents and next-of-kin emergency contact.</p>

          <div class="form-grid">
            <div class="form-group">
              <label class="form-label">{{ t().nid }}</label>
              <input type="text" class="form-input" formControlName="nationalId" placeholder="10, 13, or 17 digits" />
              <span class="hint-text">Will be encrypted at rest; last 4 digits stored for duplicate verification</span>
            </div>

            <div class="form-group">
              <label class="form-label">{{ t().birthReg }}</label>
              <input type="text" class="form-input" formControlName="birthRegNo" placeholder="17-digit birth certificate number" />
            </div>

            <div class="section-divider full-width">
              <h3 class="divider-title">Next-of-Kin / Emergency Contact</h3>
            </div>

            <div class="form-group" formGroupName="emergencyContact">
              <label class="form-label">{{ t().emergencyName }}</label>
              <input type="text" class="form-input" formControlName="name" placeholder="Full name of contact" />
            </div>

            <div class="form-group" formGroupName="emergencyContact">
              <label class="form-label">{{ t().emergencyRelation }}</label>
              <select class="form-select" formControlName="relationship">
                <option value="">Select Relationship</option>
                <option value="Spouse">Spouse (স্বামী / স্ত্রী)</option>
                <option value="Father">Father (বাবা)</option>
                <option value="Mother">Mother (মা)</option>
                <option value="Son">Son (ছেলে)</option>
                <option value="Daughter">Daughter (মেয়ে)</option>
                <option value="Brother">Brother (ভাই)</option>
                <option value="Sister">Sister (বোন)</option>
                <option value="Guardian">Guardian (অভিভাবক)</option>
              </select>
            </div>

            <div class="form-group" formGroupName="emergencyContact">
              <label class="form-label">{{ t().emergencyPhone }}</label>
              <input type="text" class="form-input" formControlName="phone" placeholder="01XXXXXXXXX" />
            </div>
          </div>
        </div>

        <!-- STEP 4: Review, Duplicate Check & Submit -->
        <div *ngIf="currentStep() === 4" class="step-content">
          <h2 class="step-heading">
            <span class="step-num-pill">4</span> {{ t().step4Title }}
          </h2>

          <!-- Duplicate Warning Banner if Detected -->
          <div *ngIf="duplicateWarning()?.hasDuplicateWarning" class="duplicate-alert">
            <div class="alert-icon">⚠️</div>
            <div class="alert-body">
              <h4 class="alert-title">{{ t().duplicateWarning }}</h4>
              <p class="alert-text">{{ duplicateWarning()?.reason }}</p>
              <p class="alert-existing">
                Existing Patient: <strong>{{ duplicateWarning()?.existingPatientName }}</strong> (MRN: {{ duplicateWarning()?.existingMrn }})
              </p>
              <p class="alert-note">Per Bangladesh hospital rules, you may proceed if this is a family member or distinct record.</p>
            </div>
          </div>

          <!-- Review Summary Grid -->
          <div class="review-card">
            <h3 class="review-title">Summary of Registration Details</h3>

            <div class="review-grid">
              <div class="review-row">
                <span class="rev-label">Full Name:</span>
                <span class="rev-val">
                  {{ patientForm.value.firstNameEn }} {{ patientForm.value.lastNameEn }}
                  <span *ngIf="patientForm.value.firstNameBn" class="font-bn">
                    ({{ patientForm.value.firstNameBn }} {{ patientForm.value.lastNameBn }})
                  </span>
                </span>
              </div>

              <div class="review-row">
                <span class="rev-label">Date of Birth & Age:</span>
                <span class="rev-val">{{ patientForm.value.dateOfBirth }} ({{ calculatedAge() }} {{ t().years }})</span>
              </div>

              <div class="review-row">
                <span class="rev-label">Gender & Blood Group:</span>
                <span class="rev-val">
                  {{ getGenderLabel(patientForm.value.gender) }} | Blood: {{ getBloodGroupLabel(patientForm.value.bloodGroup) }}
                </span>
              </div>

              <div class="review-row">
                <span class="rev-label">Primary Mobile:</span>
                <span class="rev-val"><strong>{{ patientForm.value.phonePrimary }}</strong></span>
              </div>

              <div class="review-row">
                <span class="rev-label">Location:</span>
                <span class="rev-val">
                  {{ getSelectedGeoText() }}
                </span>
              </div>

              <div class="review-row">
                <span class="rev-label">National ID / Birth Reg:</span>
                <span class="rev-val">
                  NID: {{ patientForm.value.nationalId ? '•••• •••• ' + patientForm.value.nationalId.slice(-4) : 'N/A' }}
                  | BRN: {{ patientForm.value.birthRegNo || 'N/A' }}
                </span>
              </div>

              <div class="review-row">
                <span class="rev-label">Emergency Contact:</span>
                <span class="rev-val">
                  {{ patientForm.value.emergencyContact?.name || 'N/A' }}
                  ({{ patientForm.value.emergencyContact?.relationship || '-' }})
                  - {{ patientForm.value.emergencyContact?.phone || '-' }}
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Wizard Navigation Buttons -->
        <div class="wizard-actions">
          <button *ngIf="currentStep() > 1" type="button" class="btn-secondary" (click)="prevStep()">
            ← {{ t().back }}
          </button>
          <div class="spacer"></div>

          <button *ngIf="currentStep() < 4" type="button" class="btn-primary" (click)="nextStep()">
            {{ t().next }} →
          </button>

          <button *ngIf="currentStep() === 4" type="button" class="btn-primary btn-submit" [disabled]="isSubmitting()" (click)="submitForm()">
            <span *ngIf="isSubmitting()" class="button-spinner"></span>
            <span>{{ isSubmitting() ? 'Registering...' : t().submitRegistration }}</span>
          </button>
        </div>
      </form>

      <!-- Registration Success Modal -->
      <div *ngIf="registeredPatient()" class="modal-backdrop">
        <div class="modal-card animate-fade-in">
          <div class="success-icon">✓</div>
          <h2 class="modal-title">Patient Successfully Registered!</h2>
          <p class="modal-desc">A Medical Record Number has been generated for this patient.</p>

          <div class="mrn-box">
            <span class="mrn-box-label">ASSIGNED MRN</span>
            <span class="mrn-box-value">{{ registeredPatient()?.mrn }}</span>
          </div>

          <p class="patient-registered-name">
            {{ registeredPatient()?.displayName }}
          </p>

          <div class="modal-actions">
            <a [routerLink]="['/patients', registeredPatient()?.id]" class="btn-primary">
              View Patient Profile
            </a>
            <button class="btn-secondary" (click)="resetAndRegisterAnother()">
              + Register Another Patient
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .register-container {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
      max-width: 900px;
      margin: 0 auto;
    }

    .wizard-header .back-link {
      font-size: 0.8125rem;
      font-weight: 600;
      color: #0891b2;
      text-decoration: none;
      display: inline-block;
      margin-bottom: 0.5rem;
    }

    .wizard-title {
      font-size: 1.75rem;
      font-weight: 800;
      color: #0f172a;
    }

    .wizard-sub {
      font-size: 0.875rem;
      color: #64748b;
    }

    .stepper-bar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1.25rem 2rem;
      background: #ffffff;
    }

    .step-item {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      cursor: pointer;
    }

    .step-circle {
      width: 32px;
      height: 32px;
      border-radius: 50%;
      background: #f1f5f9;
      color: #64748b;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 700;
      font-size: 0.875rem;
      transition: all 0.2s;
    }

    .step-item.active .step-circle {
      background: #0891b2;
      color: #ffffff;
      box-shadow: 0 0 10px rgba(8, 145, 178, 0.5);
    }

    .step-item.completed .step-circle {
      background: #10b981;
      color: #ffffff;
    }

    .step-title {
      font-size: 0.875rem;
      font-weight: 600;
      color: #64748b;
    }

    .step-item.active .step-title {
      color: #0f172a;
      font-weight: 700;
    }

    .step-divider {
      flex: 1;
      height: 2px;
      background: #e2e8f0;
      margin: 0 1rem;
    }

    .step-divider.completed {
      background: #10b981;
    }

    .wizard-body {
      background: #ffffff;
      padding: 2rem;
    }

    .step-heading {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      font-size: 1.25rem;
      font-weight: 700;
      color: #0f172a;
      margin-bottom: 0.25rem;
    }

    .step-num-pill {
      width: 24px;
      height: 24px;
      border-radius: 50%;
      background: #ecfeff;
      color: #0891b2;
      font-size: 0.75rem;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 800;
    }

    .step-desc {
      font-size: 0.875rem;
      color: #64748b;
      margin-bottom: 1.75rem;
    }

    .form-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 1.25rem;
    }

    .full-width {
      grid-column: 1 / -1;
    }

    .section-divider {
      border-top: 1px solid #e2e8f0;
      padding-top: 1rem;
      margin-top: 0.5rem;
    }

    .divider-title {
      font-size: 0.9375rem;
      font-weight: 700;
      color: #334155;
    }

    .hint-text {
      font-size: 0.6875rem;
      color: #64748b;
    }

    .age-hint {
      font-size: 0.75rem;
      color: #0891b2;
      font-weight: 600;
      margin-top: 0.25rem;
    }

    .duplicate-alert {
      display: flex;
      gap: 1rem;
      padding: 1.25rem;
      background: #fffbeb;
      border: 1px solid #fde68a;
      border-radius: 10px;
      margin-bottom: 1.5rem;
    }

    .alert-icon {
      font-size: 1.5rem;
    }

    .alert-title {
      font-size: 0.9375rem;
      font-weight: 700;
      color: #b45309;
    }

    .alert-text {
      font-size: 0.8125rem;
      color: #78350f;
      margin: 0.25rem 0;
    }

    .alert-existing {
      font-size: 0.8125rem;
      color: #92400e;
    }

    .alert-note {
      font-size: 0.75rem;
      color: #b45309;
      margin-top: 0.5rem;
      font-style: italic;
    }

    .review-card {
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      padding: 1.5rem;
    }

    .review-title {
      font-size: 1rem;
      font-weight: 700;
      color: #0f172a;
      margin-bottom: 1rem;
      border-bottom: 1px solid #e2e8f0;
      padding-bottom: 0.5rem;
    }

    .review-grid {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .review-row {
      display: flex;
      justify-content: space-between;
      font-size: 0.875rem;
    }

    .rev-label {
      font-weight: 600;
      color: #64748b;
      width: 220px;
    }

    .rev-val {
      font-weight: 500;
      color: #0f172a;
      flex: 1;
      text-align: right;
    }

    .wizard-actions {
      display: flex;
      align-items: center;
      margin-top: 2rem;
      padding-top: 1.5rem;
      border-top: 1px solid #f1f5f9;
    }

    .spacer {
      flex: 1;
    }

    .btn-submit {
      background: linear-gradient(135deg, #059669 0%, #10b981 100%);
    }

    .button-spinner {
      width: 14px;
      height: 14px;
      border: 2px solid #ffffff;
      border-top-color: transparent;
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
      display: inline-block;
      margin-right: 0.5rem;
    }

    .modal-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(15, 23, 42, 0.7);
      backdrop-filter: blur(4px);
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
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.2);
    }

    .success-icon {
      width: 60px;
      height: 60px;
      border-radius: 50%;
      background: #ecfdf5;
      color: #10b981;
      font-size: 2rem;
      font-weight: 800;
      display: flex;
      align-items: center;
      justify-content: center;
      margin: 0 auto 1.25rem;
      border: 2px solid #a7f3d0;
    }

    .modal-title {
      font-size: 1.35rem;
      font-weight: 800;
      color: #0f172a;
    }

    .modal-desc {
      font-size: 0.875rem;
      color: #64748b;
      margin: 0.5rem 0 1.5rem;
    }

    .mrn-box {
      background: #ecfeff;
      border: 2px dashed #0891b2;
      border-radius: 12px;
      padding: 1rem;
      margin-bottom: 1rem;
    }

    .mrn-box-label {
      font-size: 0.6875rem;
      font-weight: 700;
      color: #0891b2;
      letter-spacing: 0.08em;
      display: block;
    }

    .mrn-box-value {
      font-size: 1.75rem;
      font-weight: 800;
      color: #0e7490;
      font-family: monospace;
    }

    .patient-registered-name {
      font-weight: 600;
      color: #334155;
      margin-bottom: 1.75rem;
    }

    .modal-actions {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }
  `]
})
export class PatientRegisterComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly apiService = inject(ApiService);
  private readonly router = inject(Router);
  readonly langService = inject(LanguageService);
  readonly t = this.langService.t;

  readonly currentStep = signal<number>(1);
  readonly calculatedAge = signal<number | null>(null);
  readonly isSubmitting = signal<boolean>(false);
  readonly duplicateWarning = signal<DuplicateWarningDto | null>(null);
  readonly registeredPatient = signal<any | null>(null);

  readonly divisions = signal<DivisionDto[]>([]);
  readonly districts = signal<DistrictDto[]>([]);
  readonly upazilas = signal<UpazilaDto[]>([]);

  patientForm: FormGroup = this.fb.group({
    firstNameEn: [''],
    lastNameEn: [''],
    firstNameBn: [''],
    lastNameBn: [''],
    dateOfBirth: ['', Validators.required],
    gender: [1, Validators.required],
    bloodGroup: [0],
    occupation: [''],
    phonePrimary: ['', [Validators.required, Validators.pattern(/^(?:\+?88)?01[3-9]\d{8}$/)]],
    phoneSecondary: [''],
    email: ['', Validators.email],
    divisionId: [null],
    districtId: [null],
    upazilaId: [null],
    addressLine: [''],
    nationalId: [''],
    birthRegNo: [''],
    emergencyContact: this.fb.group({
      name: [''],
      relationship: [''],
      phone: ['']
    })
  });

  ngOnInit(): void {
    this.loadDivisions();
  }

  loadDivisions(): void {
    this.apiService.getDivisions().subscribe({
      next: divs => this.divisions.set(divs),
      error: err => console.error('Failed to load divisions', err)
    });
  }

  onDivisionChange(): void {
    const divId = this.patientForm.value.divisionId;
    this.patientForm.patchValue({ districtId: null, upazilaId: null });
    this.districts.set([]);
    this.upazilas.set([]);

    if (divId) {
      this.apiService.getDistricts(divId).subscribe(dist => this.districts.set(dist));
    }
  }

  onDistrictChange(): void {
    const distId = this.patientForm.value.districtId;
    this.patientForm.patchValue({ upazilaId: null });
    this.upazilas.set([]);

    if (distId) {
      this.apiService.getUpazilas(distId).subscribe(upz => this.upazilas.set(upz));
    }
  }

  calculateAge(): void {
    const dobVal = this.patientForm.value.dateOfBirth;
    if (!dobVal) {
      this.calculatedAge.set(null);
      return;
    }
    const dob = new Date(dobVal);
    const today = new Date();
    let age = today.getFullYear() - dob.getFullYear();
    const m = today.getMonth() - dob.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < dob.getDate())) {
      age--;
    }
    this.calculatedAge.set(age >= 0 ? age : null);
  }

  goToStep(step: number): void {
    if (step < this.currentStep()) {
      this.currentStep.set(step);
    } else if (step === 4) {
      this.triggerDuplicateCheck();
      this.currentStep.set(step);
    } else {
      this.currentStep.set(step);
    }
  }

  nextStep(): void {
    if (this.currentStep() === 3) {
      this.triggerDuplicateCheck();
    }
    this.currentStep.update(s => Math.min(s + 1, 4));
  }

  prevStep(): void {
    this.currentStep.update(s => Math.max(s - 1, 1));
  }

  triggerDuplicateCheck(): void {
    const phone = this.patientForm.value.phonePrimary;
    const nid = this.patientForm.value.nationalId;
    const dob = this.patientForm.value.dateOfBirth;
    const nidLast4 = nid && nid.length >= 4 ? nid.slice(-4) : undefined;

    if (phone) {
      this.apiService.checkDuplicate(phone, nidLast4, dob).subscribe({
        next: warning => this.duplicateWarning.set(warning),
        error: () => this.duplicateWarning.set(null)
      });
    }
  }

  submitForm(): void {
    if (this.patientForm.invalid) {
      alert('Please fill in required fields (Name, Date of Birth, Primary Phone)');
      return;
    }

    this.isSubmitting.set(true);
    const formVal = this.patientForm.value;

    const request: CreatePatientRequest = {
      firstNameEn: formVal.firstNameEn || undefined,
      lastNameEn: formVal.lastNameEn || undefined,
      firstNameBn: formVal.firstNameBn || undefined,
      lastNameBn: formVal.lastNameBn || undefined,
      dateOfBirth: formVal.dateOfBirth ? new Date(formVal.dateOfBirth).toISOString() : undefined,
      gender: Number(formVal.gender),
      bloodGroup: Number(formVal.bloodGroup),
      phonePrimary: formVal.phonePrimary,
      phoneSecondary: formVal.phoneSecondary || undefined,
      email: formVal.email || undefined,
      nationalId: formVal.nationalId || undefined,
      birthRegNo: formVal.birthRegNo || undefined,
      divisionId: formVal.divisionId || undefined,
      districtId: formVal.districtId || undefined,
      upazilaId: formVal.upazilaId || undefined,
      addressLine: formVal.addressLine || undefined,
      occupation: formVal.occupation || undefined,
      emergencyContact: formVal.emergencyContact?.name ? formVal.emergencyContact : undefined
    };

    this.apiService.createPatient(request).subscribe({
      next: res => {
        this.isSubmitting.set(false);
        this.registeredPatient.set(res);
      },
      error: err => {
        this.isSubmitting.set(false);
        console.error('Registration failed', err);
        alert(err.error?.detail || err.error?.title || 'Patient registration failed');
      }
    });
  }

  resetAndRegisterAnother(): void {
    this.registeredPatient.set(null);
    this.patientForm.reset({
      gender: 1,
      bloodGroup: 0
    });
    this.calculatedAge.set(null);
    this.duplicateWarning.set(null);
    this.currentStep.set(1);
  }

  getSelectedGeoText(): string {
    const divId = this.patientForm.value.divisionId;
    const distId = this.patientForm.value.districtId;
    const upzId = this.patientForm.value.upazilaId;

    const div = this.divisions().find(d => d.id === divId);
    const dist = this.districts().find(d => d.id === distId);
    const upz = this.upazilas().find(u => u.id === upzId);

    const parts = [
      upz ? upz.nameEn : '',
      dist ? dist.nameEn : '',
      div ? div.nameEn : '',
      this.patientForm.value.addressLine || ''
    ].filter(Boolean);

    return parts.length > 0 ? parts.join(', ') : 'Not specified';
  }

  getGenderLabel(gender: any): string {
    switch (Number(gender)) {
      case 1: return this.t().male;
      case 2: return this.t().female;
      default: return this.t().other;
    }
  }

  getBloodGroupLabel(bg: any): string {
    switch (Number(bg)) {
      case 1: return 'A+';
      case 2: return 'A-';
      case 3: return 'B+';
      case 4: return 'B-';
      case 5: return 'AB+';
      case 6: return 'AB-';
      case 7: return 'O+';
      case 8: return 'O-';
      default: return 'Unknown';
    }
  }
}
