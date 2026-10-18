import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { LanguageService } from '../services/language.service';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="hms-layout">
      <!-- Top Navigation Bar -->
      <header class="topbar">
        <div class="topbar-left">
          <div class="brand">
            <div class="brand-icon">
              <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                <path d="M12 2v20M2 12h20M7 7l10 10M17 7L7 17"/>
              </svg>
            </div>
            <div class="brand-text">
              <span class="brand-name">AVELA<span class="highlight">CARE</span></span>
              <span class="brand-badge">PRO v1.0</span>
            </div>
          </div>

          <div class="hospital-selector">
            <span class="status-indicator-dot"></span>
            <div class="hospital-info">
              <span class="hospital-name">{{ t().hospitalName }}</span>
              <span class="hospital-sub">Govt. Reg #BD-DHK-2026</span>
            </div>
          </div>
        </div>

        <div class="topbar-right">
          <!-- Language Toggle Button -->
          <button class="lang-toggle-btn" (click)="toggleLanguage()" [title]="'Toggle Language'">
            <span class="lang-badge" [class.active]="langService.currentLang() === 'en'">EN</span>
            <span class="lang-divider">/</span>
            <span class="lang-badge" [class.active]="langService.currentLang() === 'bn'">বাংলা</span>
          </button>

          <!-- Emergency Hotline Pill -->
          <div class="emergency-pill">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z"/>
            </svg>
            <span>ER: 16263</span>
          </div>

          <!-- Staff User Profile -->
          <div class="user-profile">
            <div class="user-avatar">NJ</div>
            <div class="user-details">
              <span class="user-name">{{ authService.currentUser().fullName }}</span>
              <span class="user-role">{{ authService.currentUser().role }}</span>
            </div>
          </div>
        </div>
      </header>

      <div class="hms-body-container">
        <!-- Sidebar Navigation -->
        <aside class="sidebar">
          <div class="nav-section">
            <span class="section-label">PATIENT CARE</span>
            <nav class="nav-menu">
              <a routerLink="/patients" routerLinkActive="active" class="nav-item">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/>
                  <circle cx="9" cy="7" r="4"/>
                  <path d="M22 21v-2a4 4 0 0 0-3-3.87"/>
                  <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
                </svg>
                <span>{{ t().navPatients }}</span>
                <span class="item-badge">200+</span>
              </a>

              <a routerLink="/appointments" routerLinkActive="active" class="nav-item">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
                  <line x1="16" y1="2" x2="16" y2="6"/>
                  <line x1="8" y1="2" x2="8" y2="6"/>
                  <line x1="3" y1="10" x2="21" y2="10"/>
                </svg>
                <span>{{ t().navAppointments }}</span>
              </a>

              <a routerLink="/opd-queue" routerLinkActive="active" class="nav-item">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/>
                </svg>
                <span>{{ t().navOpdQueue }}</span>
                <span class="live-dot" title="Live SignalR Queue"></span>
              </a>
            </nav>
          </div>

          <div class="nav-section">
            <span class="section-label">CLINICAL & WARDS</span>
            <nav class="nav-menu">
              <a routerLink="/consultation" routerLinkActive="active" class="nav-item">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                  <polyline points="14 2 14 8 20 8"/>
                  <line x1="16" y1="13" x2="8" y2="13"/>
                  <line x1="16" y1="17" x2="8" y2="17"/>
                  <polyline points="10 9 9 9 8 9"/>
                </svg>
                <span>{{ t().navConsultation }}</span>
              </a>

              <a routerLink="/beds" routerLinkActive="active" class="nav-item">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M2 4v16"/>
                  <path d="M2 8h18a2 2 0 0 1 2 2v10"/>
                  <path d="M2 17h20"/>
                  <path d="M6 8v9"/>
                </svg>
                <span>{{ t().navBeds }}</span>
              </a>

              <a routerLink="/pharmacy" routerLinkActive="active" class="nav-item">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="2" y="7" width="20" height="14" rx="2" ry="2"/>
                  <path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"/>
                </svg>
                <span>{{ t().navPharmacy }}</span>
              </a>
            </nav>
          </div>

          <div class="nav-section">
            <span class="section-label">FINANCE & SYSTEM</span>
            <nav class="nav-menu">
              <a routerLink="/billing" routerLinkActive="active" class="nav-item">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <rect x="1" y="4" width="22" height="16" rx="2" ry="2"/>
                  <line x1="1" y1="10" x2="23" y2="10"/>
                </svg>
                <span>{{ t().navBilling }}</span>
              </a>

              <a routerLink="/reports" routerLinkActive="active" class="nav-item">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <line x1="18" y1="20" x2="18" y2="10"/>
                  <line x1="12" y1="20" x2="12" y2="4"/>
                  <line x1="6" y1="20" x2="6" y2="14"/>
                </svg>
                <span>{{ t().navReports }}</span>
              </a>
            </nav>
          </div>

          <div class="sidebar-footer">
            <div class="edge-server-status">
              <span class="status-pulse"></span>
              <div class="edge-text">
                <span class="edge-title">LOCAL EDGE NODE</span>
                <span class="edge-sub">Cloud Sync: Active (0 lag)</span>
              </div>
            </div>
          </div>
        </aside>

        <!-- Main Content View -->
        <main class="main-content">
          <router-outlet></router-outlet>
        </main>
      </div>
    </div>
  `,
  styles: [`
    .hms-layout {
      display: flex;
      flex-direction: column;
      height: 100vh;
      overflow: hidden;
    }

    .topbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      height: 64px;
      padding: 0 1.5rem;
      background: #0f172a;
      color: #ffffff;
      border-bottom: 1px solid rgba(255, 255, 255, 0.08);
      z-index: 50;
    }

    .topbar-left {
      display: flex;
      align-items: center;
      gap: 2rem;
    }

    .brand {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .brand-icon {
      width: 38px;
      height: 38px;
      border-radius: 10px;
      background: linear-gradient(135deg, #06b6d4 0%, #0891b2 100%);
      color: #ffffff;
      display: flex;
      align-items: center;
      justify-content: center;
      box-shadow: 0 0 16px rgba(6, 182, 212, 0.5);
    }

    .brand-text {
      display: flex;
      align-items: baseline;
      gap: 0.5rem;
    }

    .brand-name {
      font-size: 1.125rem;
      font-weight: 800;
      letter-spacing: 0.05em;
      color: #ffffff;
    }

    .brand-name .highlight {
      color: #22d3ee;
    }

    .brand-badge {
      font-size: 0.65rem;
      font-weight: 700;
      background: rgba(6, 182, 212, 0.2);
      color: #67e8f9;
      padding: 0.15rem 0.4rem;
      border-radius: 4px;
      border: 1px solid rgba(6, 182, 212, 0.4);
    }

    .hospital-selector {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      padding: 0.35rem 0.85rem;
      background: rgba(255, 255, 255, 0.05);
      border: 1px solid rgba(255, 255, 255, 0.1);
      border-radius: 8px;
    }

    .status-indicator-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: #10b981;
      box-shadow: 0 0 8px #10b981;
    }

    .hospital-info {
      display: flex;
      flex-direction: column;
    }

    .hospital-name {
      font-size: 0.8125rem;
      font-weight: 600;
      color: #f1f5f9;
    }

    .hospital-sub {
      font-size: 0.6875rem;
      color: #94a3b8;
    }

    .topbar-right {
      display: flex;
      align-items: center;
      gap: 1.25rem;
    }

    .lang-toggle-btn {
      display: flex;
      align-items: center;
      gap: 0.35rem;
      background: rgba(255, 255, 255, 0.08);
      border: 1px solid rgba(255, 255, 255, 0.15);
      padding: 0.4rem 0.75rem;
      border-radius: 20px;
      color: #cbd5e1;
      font-size: 0.8125rem;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .lang-toggle-btn:hover {
      background: rgba(255, 255, 255, 0.15);
      border-color: #06b6d4;
    }

    .lang-badge.active {
      color: #22d3ee;
      font-weight: 700;
    }

    .lang-divider {
      color: #64748b;
    }

    .emergency-pill {
      display: flex;
      align-items: center;
      gap: 0.4rem;
      padding: 0.35rem 0.75rem;
      background: rgba(239, 68, 68, 0.15);
      border: 1px solid rgba(239, 68, 68, 0.4);
      color: #fca5a5;
      font-size: 0.75rem;
      font-weight: 700;
      border-radius: 20px;
    }

    .user-profile {
      display: flex;
      align-items: center;
      gap: 0.625rem;
    }

    .user-avatar {
      width: 36px;
      height: 36px;
      border-radius: 50%;
      background: linear-gradient(135deg, #6366f1 0%, #a855f7 100%);
      color: #ffffff;
      font-weight: 700;
      font-size: 0.8125rem;
      display: flex;
      align-items: center;
      justify-content: center;
      box-shadow: 0 0 10px rgba(168, 85, 247, 0.4);
    }

    .user-details {
      display: flex;
      flex-direction: column;
    }

    .user-name {
      font-size: 0.8125rem;
      font-weight: 600;
      color: #f8fafc;
    }

    .user-role {
      font-size: 0.6875rem;
      color: #22d3ee;
      font-weight: 500;
    }

    .hms-body-container {
      display: flex;
      flex: 1;
      overflow: hidden;
    }

    .sidebar {
      width: 250px;
      background: #0f172a;
      border-right: 1px solid rgba(255, 255, 255, 0.08);
      display: flex;
      flex-direction: column;
      padding: 1.25rem 0.875rem;
      overflow-y: auto;
      flex-shrink: 0;
    }

    .nav-section {
      margin-bottom: 1.5rem;
    }

    .section-label {
      font-size: 0.6875rem;
      font-weight: 700;
      color: #64748b;
      letter-spacing: 0.08em;
      padding: 0 0.75rem;
      margin-bottom: 0.5rem;
      display: block;
    }

    .nav-menu {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .nav-item {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.625rem 0.75rem;
      color: #94a3b8;
      text-decoration: none;
      font-size: 0.875rem;
      font-weight: 500;
      border-radius: 8px;
      transition: all 0.2s ease;
      position: relative;
    }

    .nav-item:hover {
      background: rgba(255, 255, 255, 0.05);
      color: #f1f5f9;
    }

    .nav-item.active {
      background: linear-gradient(90deg, rgba(6, 182, 212, 0.15) 0%, rgba(6, 182, 212, 0.05) 100%);
      color: #22d3ee;
      font-weight: 600;
      border-left: 3px solid #06b6d4;
    }

    .item-badge {
      margin-left: auto;
      font-size: 0.6875rem;
      font-weight: 700;
      background: rgba(6, 182, 212, 0.2);
      color: #67e8f9;
      padding: 0.15rem 0.5rem;
      border-radius: 9999px;
    }

    .live-dot {
      margin-left: auto;
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: #10b981;
      box-shadow: 0 0 8px #10b981;
      animation: pulse 2s infinite;
    }

    @keyframes pulse {
      0%, 100% { opacity: 1; transform: scale(1); }
      50% { opacity: 0.4; transform: scale(0.9); }
    }

    .sidebar-footer {
      margin-top: auto;
      padding-top: 1rem;
      border-top: 1px solid rgba(255, 255, 255, 0.08);
    }

    .edge-server-status {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      padding: 0.625rem 0.75rem;
      background: rgba(16, 185, 129, 0.08);
      border: 1px solid rgba(16, 185, 129, 0.2);
      border-radius: 8px;
    }

    .status-pulse {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      background: #10b981;
      box-shadow: 0 0 10px #10b981;
    }

    .edge-text {
      display: flex;
      flex-direction: column;
    }

    .edge-title {
      font-size: 0.6875rem;
      font-weight: 700;
      color: #34d399;
      letter-spacing: 0.04em;
    }

    .edge-sub {
      font-size: 0.625rem;
      color: #94a3b8;
    }

    .main-content {
      flex: 1;
      overflow-y: auto;
      padding: 1.5rem 2rem;
      background: #f8fafc;
    }
  `]
})
export class ShellComponent {
  readonly langService = inject(LanguageService);
  readonly authService = inject(AuthService);
  readonly t = this.langService.t;

  toggleLanguage(): void {
    this.langService.toggleLanguage();
  }
}
