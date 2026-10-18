import { Component, inject, signal, OnInit, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../../core/services/api.service';
import { LanguageService } from '../../../core/services/language.service';
import { PatientResponse, Gender, BloodGroup } from '../../../core/models/patient.models';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';

@Component({
  selector: 'app-patient-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="patient-list-container animate-fade-in">
      <!-- Page Header & Stats Banner -->
      <div class="header-section">
        <div class="header-titles">
          <h1 class="page-title">{{ t().navPatients }}</h1>
          <p class="page-subtitle">{{ t().systemSubtitle }} — {{ t().hospitalName }}</p>
        </div>
        <div class="header-actions">
          <a routerLink="/patients/register" class="btn-primary">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <line x1="12" y1="5" x2="12" y2="19"></line>
              <line x1="5" y1="12" x2="19" y2="12"></line>
            </svg>
            <span>{{ t().registerNewPatient }}</span>
          </a>
        </div>
      </div>

      <!-- Quick Metrics Cards -->
      <div class="stats-grid">
        <div class="stat-card stat-teal">
          <div class="stat-icon">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
              <circle cx="9" cy="7" r="4"></circle>
              <path d="M23 21v-2a4 4 0 0 0-3-3.87"></path>
              <path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
            </svg>
          </div>
          <div class="stat-info">
            <span class="stat-label">{{ t().totalPatients }}</span>
            <span class="stat-value">{{ totalCount() }}</span>
          </div>
          <span class="stat-trend">+14% this month</span>
        </div>

        <div class="stat-card stat-purple">
          <div class="stat-icon">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="3" y="4" width="18" height="18" rx="2" ry="2"></rect>
              <line x1="16" y1="2" x2="16" y2="6"></line>
              <line x1="8" y1="2" x2="8" y2="6"></line>
              <line x1="3" y1="10" x2="21" y2="10"></line>
            </svg>
          </div>
          <div class="stat-info">
            <span class="stat-label">{{ t().todayAdmissions }}</span>
            <span class="stat-value">38</span>
          </div>
          <span class="stat-badge">General + ICU</span>
        </div>

        <div class="stat-card stat-amber">
          <div class="stat-icon">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"></polyline>
            </svg>
          </div>
          <div class="stat-info">
            <span class="stat-label">{{ t().opdQueueActive }}</span>
            <span class="stat-value">64</span>
          </div>
          <span class="stat-badge">12 Doctors active</span>
        </div>

        <div class="stat-card stat-rose">
          <div class="stat-icon">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon>
            </svg>
          </div>
          <div class="stat-info">
            <span class="stat-label">{{ t().emergencyTriage }}</span>
            <span class="stat-value">9</span>
          </div>
          <span class="stat-badge">Red: 2 | Yellow: 7</span>
        </div>
      </div>

      <!-- Search & Filter Controls -->
      <div class="glass-card search-card">
        <div class="search-input-wrapper">
          <svg class="search-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="11" cy="11" r="8"></circle>
            <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
          </svg>
          <input
            type="text"
            class="search-input"
            [placeholder]="t().searchPlaceholder"
            [(ngModel)]="searchQuery"
            (ngModelChange)="onSearchInput($event)"
          />
          <button *ngIf="searchQuery" class="clear-btn" (click)="clearSearch()">✕</button>
        </div>

        <div class="filter-actions">
          <span class="results-badge">
            <span class="results-count">{{ totalCount() }}</span> records found
          </span>
        </div>
      </div>

      <!-- Patients Data Grid -->
      <div class="glass-card table-card">
        <div *ngIf="isLoading()" class="loading-state">
          <div class="spinner"></div>
          <p>{{ t().loading }}</p>
        </div>

        <div *ngIf="!isLoading() && patients().length === 0" class="empty-state">
          <div class="empty-icon">📂</div>
          <h3>{{ t().noPatientsFound }}</h3>
          <p>Try searching with another keyword or click Register New Patient.</p>
        </div>

        <div *ngIf="!isLoading() && patients().length > 0" class="table-responsive">
          <table class="modern-table">
            <thead>
              <tr>
                <th>{{ t().mrn }}</th>
                <th>{{ t().patientName }}</th>
                <th>{{ t().age }} / {{ t().gender }}</th>
                <th>{{ t().bloodGroup }}</th>
                <th>{{ t().phone }}</th>
                <th>{{ t().location }}</th>
                <th class="text-right">{{ t().actions }}</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let p of patients()" class="table-row">
                <!-- MRN Badge -->
                <td>
                  <span class="mrn-badge">
                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
                    </svg>
                    {{ p.mrn }}
                  </span>
                </td>

                <!-- Bilingual Name & Avatar -->
                <td>
                  <div class="patient-cell">
                    <div class="patient-avatar" [style.background]="getAvatarColor(p.mrn)">
                      {{ getInitials(p.firstNameEn || p.displayName) }}
                    </div>
                    <div class="name-block">
                      <span class="name-primary">{{ p.displayName }}</span>
                      <span *ngIf="p.firstNameBn" class="name-bengali font-bn">
                        {{ p.firstNameBn }} {{ p.lastNameBn }}
                      </span>
                    </div>
                  </div>
                </td>

                <!-- Age / Gender -->
                <td>
                  <div class="age-gender">
                    <span class="age-val">{{ p.ageYears ?? '-' }} {{ t().years }}</span>
                    <span class="gender-pill" [class.male]="p.gender === 1" [class.female]="p.gender === 2">
                      {{ getGenderLabel(p.gender) }}
                    </span>
                  </div>
                </td>

                <!-- Blood Group -->
                <td>
                  <span class="blood-pill">
                    <span class="drop">🩸</span>
                    {{ getBloodGroupLabel(p.bloodGroup) }}
                  </span>
                </td>

                <!-- Phone -->
                <td>
                  <div class="phone-block">
                    <span class="phone-num">{{ p.phonePrimary }}</span>
                  </div>
                </td>

                <!-- Location -->
                <td>
                  <span class="location-text">
                    {{ p.upazilaName || p.districtName || 'Dhaka' }}
                  </span>
                </td>

                <!-- Actions -->
                <td class="text-right">
                  <div class="actions-group">
                    <a [routerLink]="['/patients', p.id]" class="btn-action btn-view" title="View Patient Profile">
                      <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                        <circle cx="12" cy="12" r="3"></circle>
                      </svg>
                      <span>{{ t().viewProfile }}</span>
                    </a>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>

          <!-- Pagination Footer -->
          <div class="pagination-footer">
            <span class="page-summary">
              Showing page {{ currentPage() }} of {{ totalPages() }} ({{ totalCount() }} total)
            </span>
            <div class="pagination-controls">
              <button class="page-btn" [disabled]="currentPage() <= 1" (click)="setPage(currentPage() - 1)">
                ← Previous
              </button>
              <button
                *ngFor="let pageNum of getVisiblePages()"
                class="page-btn"
                [class.active]="pageNum === currentPage()"
                (click)="setPage(pageNum)"
              >
                {{ pageNum }}
              </button>
              <button class="page-btn" [disabled]="currentPage() >= totalPages()" (click)="setPage(currentPage() + 1)">
                Next →
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .patient-list-container {
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
      letter-spacing: -0.02em;
    }

    .page-subtitle {
      font-size: 0.875rem;
      color: #64748b;
      margin-top: 0.25rem;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 1rem;
    }

    .stat-card {
      background: #ffffff;
      border-radius: 14px;
      padding: 1.25rem;
      border: 1px solid #e2e8f0;
      display: flex;
      flex-direction: column;
      gap: 0.625rem;
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);
      position: relative;
      overflow: hidden;
    }

    .stat-card::before {
      content: '';
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      height: 4px;
    }

    .stat-teal::before { background: linear-gradient(90deg, #06b6d4, #0891b2); }
    .stat-purple::before { background: linear-gradient(90deg, #8b5cf6, #7c3aed); }
    .stat-amber::before { background: linear-gradient(90deg, #f59e0b, #d97706); }
    .stat-rose::before { background: linear-gradient(90deg, #f43f5e, #e11d48); }

    .stat-icon {
      width: 40px;
      height: 40px;
      border-radius: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .stat-teal .stat-icon { background: #ecfeff; color: #0891b2; }
    .stat-purple .stat-icon { background: #faf5ff; color: #7c3aed; }
    .stat-amber .stat-icon { background: #fffbeb; color: #d97706; }
    .stat-rose .stat-icon { background: #fff1f2; color: #e11d48; }

    .stat-label {
      font-size: 0.8125rem;
      font-weight: 600;
      color: #64748b;
      display: block;
    }

    .stat-value {
      font-size: 1.75rem;
      font-weight: 800;
      color: #0f172a;
      line-height: 1.2;
    }

    .stat-trend, .stat-badge {
      font-size: 0.75rem;
      font-weight: 600;
      color: #059669;
    }

    .stat-badge {
      color: #64748b;
    }

    .search-card {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0.875rem 1.25rem;
      background: #ffffff;
    }

    .search-input-wrapper {
      position: relative;
      display: flex;
      align-items: center;
      flex: 1;
      max-width: 600px;
    }

    .search-icon {
      position: absolute;
      left: 1rem;
      color: #94a3b8;
    }

    .search-input {
      width: 100%;
      padding: 0.75rem 2.5rem 0.75rem 2.75rem;
      border-radius: 10px;
      border: 1px solid #cbd5e1;
      background: #f8fafc;
      font-size: 0.875rem;
      outline: none;
      transition: all 0.2s;
    }

    .search-input:focus {
      border-color: #06b6d4;
      background: #ffffff;
      box-shadow: 0 0 0 3px rgba(6, 182, 212, 0.15);
    }

    .clear-btn {
      position: absolute;
      right: 0.875rem;
      background: none;
      border: none;
      color: #94a3b8;
      cursor: pointer;
      font-size: 0.875rem;
    }

    .results-badge {
      font-size: 0.8125rem;
      color: #64748b;
      font-weight: 500;
    }

    .results-count {
      color: #0891b2;
      font-weight: 700;
    }

    .table-card {
      padding: 0;
      overflow: hidden;
      background: #ffffff;
    }

    .modern-table {
      width: 100%;
      border-collapse: collapse;
      text-align: left;
    }

    .modern-table th {
      padding: 0.875rem 1.25rem;
      font-size: 0.75rem;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: #64748b;
      background: #f8fafc;
      border-bottom: 1px solid #e2e8f0;
    }

    .modern-table td {
      padding: 1rem 1.25rem;
      font-size: 0.875rem;
      border-bottom: 1px solid #f1f5f9;
      vertical-align: middle;
    }

    .table-row:hover {
      background: #f0fdfa;
    }

    .mrn-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
      padding: 0.25rem 0.6rem;
      background: #ecfeff;
      color: #0891b2;
      border: 1px solid #a5f3fc;
      border-radius: 6px;
      font-family: monospace;
      font-weight: 700;
      font-size: 0.8125rem;
    }

    .patient-cell {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .patient-avatar {
      width: 36px;
      height: 36px;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      color: #ffffff;
      font-weight: 700;
      font-size: 0.8125rem;
      flex-shrink: 0;
    }

    .name-block {
      display: flex;
      flex-direction: column;
    }

    .name-primary {
      font-weight: 600;
      color: #0f172a;
    }

    .name-bengali {
      font-size: 0.75rem;
      color: #64748b;
    }

    .age-gender {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .age-val {
      font-weight: 500;
      color: #334155;
    }

    .gender-pill {
      font-size: 0.6875rem;
      padding: 0.15rem 0.45rem;
      border-radius: 4px;
      font-weight: 600;
    }

    .gender-pill.male { background: #e0f2fe; color: #0284c7; }
    .gender-pill.female { background: #fce7f3; color: #db2777; }

    .blood-pill {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
      padding: 0.25rem 0.6rem;
      background: #fff1f2;
      color: #be123c;
      border: 1px solid #fecdd3;
      border-radius: 20px;
      font-weight: 700;
      font-size: 0.75rem;
    }

    .phone-block {
      font-family: monospace;
      font-size: 0.8125rem;
      color: #475569;
    }

    .location-text {
      color: #64748b;
      font-size: 0.8125rem;
    }

    .text-right {
      text-align: right;
    }

    .actions-group {
      display: flex;
      justify-content: flex-end;
      gap: 0.5rem;
    }

    .btn-action {
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
      padding: 0.4rem 0.75rem;
      border-radius: 6px;
      font-size: 0.75rem;
      font-weight: 600;
      text-decoration: none;
      cursor: pointer;
      border: 1px solid #cbd5e1;
      background: #ffffff;
      color: #334155;
      transition: all 0.2s;
    }

    .btn-action:hover {
      background: #0891b2;
      border-color: #0891b2;
      color: #ffffff;
    }

    .pagination-footer {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1rem 1.25rem;
      border-top: 1px solid #e2e8f0;
      background: #f8fafc;
    }

    .page-summary {
      font-size: 0.8125rem;
      color: #64748b;
    }

    .pagination-controls {
      display: flex;
      align-items: center;
      gap: 0.35rem;
    }

    .page-btn {
      padding: 0.35rem 0.75rem;
      border: 1px solid #cbd5e1;
      background: #ffffff;
      color: #334155;
      border-radius: 6px;
      font-size: 0.8125rem;
      cursor: pointer;
    }

    .page-btn.active {
      background: #0891b2;
      border-color: #0891b2;
      color: #ffffff;
      font-weight: 700;
    }

    .page-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .loading-state, .empty-state {
      padding: 3rem;
      text-align: center;
      color: #64748b;
    }

    .spinner {
      width: 32px;
      height: 32px;
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
export class PatientListComponent implements OnInit {
  private readonly apiService = inject(ApiService);
  readonly langService = inject(LanguageService);
  readonly t = this.langService.t;

  readonly patients = signal<PatientResponse[]>([]);
  readonly totalCount = signal<number>(0);
  readonly currentPage = signal<number>(1);
  readonly pageSize = signal<number>(15);
  readonly isLoading = signal<boolean>(false);

  searchQuery = '';
  private searchSubject = new Subject<string>();

  ngOnInit(): void {
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(query => {
      this.currentPage.set(1);
      this.loadPatients(query);
    });

    this.loadPatients();
  }

  onSearchInput(value: string): void {
    this.searchSubject.next(value);
  }

  clearSearch(): void {
    this.searchQuery = '';
    this.searchSubject.next('');
  }

  loadPatients(query?: string): void {
    this.isLoading.set(true);
    this.apiService.searchPatients({
      query: query !== undefined ? query : this.searchQuery,
      page: this.currentPage(),
      pageSize: this.pageSize()
    }).subscribe({
      next: res => {
        this.patients.set(res.items);
        this.totalCount.set(res.totalCount);
        this.isLoading.set(false);
      },
      error: err => {
        console.error('Failed to load patients', err);
        this.isLoading.set(false);
      }
    });
  }

  setPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this.loadPatients();
  }

  totalPages(): number {
    return Math.ceil(this.totalCount() / this.pageSize()) || 1;
  }

  getVisiblePages(): number[] {
    const total = this.totalPages();
    const cur = this.currentPage();
    const pages: number[] = [];
    for (let i = Math.max(1, cur - 2); i <= Math.min(total, cur + 2); i++) {
      pages.push(i);
    }
    return pages;
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
    const colors = [
      'linear-gradient(135deg, #06b6d4, #0891b2)',
      'linear-gradient(135deg, #8b5cf6, #7c3aed)',
      'linear-gradient(135deg, #f59e0b, #d97706)',
      'linear-gradient(135deg, #10b981, #059669)',
      'linear-gradient(135deg, #ec4899, #be185d)'
    ];
    let hash = 0;
    for (let i = 0; i < mrn.length; i++) hash = mrn.charCodeAt(i) + ((hash << 5) - hash);
    const index = Math.abs(hash) % colors.length;
    return colors[index];
  }
}
