import { Routes } from '@angular/router';
import { ShellComponent } from './core/layout/shell.component';

export const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'patients'
      },
      {
        path: 'patients',
        loadComponent: () => import('./features/patients/patient-list/patient-list.component').then(m => m.PatientListComponent)
      },
      {
        path: 'patients/register',
        loadComponent: () => import('./features/patients/patient-register/patient-register.component').then(m => m.PatientRegisterComponent)
      },
      {
        path: 'patients/:id',
        loadComponent: () => import('./features/patients/patient-profile/patient-profile.component').then(m => m.PatientProfileComponent)
      },
      {
        path: 'appointments',
        loadComponent: () => import('./features/appointments/appointment-booking/appointment-booking.component').then(m => m.AppointmentBookingComponent)
      },
      {
        path: 'opd-queue',
        loadComponent: () => import('./features/appointments/opd-queue/opd-queue.component').then(m => m.OpdQueueComponent)
      },
      {
        path: '**',
        redirectTo: 'patients'
      }
    ]
  }
];
