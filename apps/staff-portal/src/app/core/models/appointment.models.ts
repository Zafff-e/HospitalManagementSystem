export interface DoctorDto {
  id: string;
  userId: string;
  departmentId: string;
  fullName: string;
  fullNameBn?: string | null;
  specialty: string;
  specialtyBn?: string | null;
  bmdcRegNo: string;
  consultationFee: number;
  qualification?: string | null;
}

export interface TimeSlotDto {
  slotTime: string;
  isAvailable: boolean;
  bookedCount: number;
  maxCapacity: number;
}

export interface BookAppointmentRequest {
  patientId: string;
  doctorId: string;
  scheduledAt: string;
  reason?: string;
}

export interface AppointmentResponse {
  id: string;
  hospitalId: string;
  patientId: string;
  patientName: string;
  patientMrn: string;
  patientPhone: string;
  doctorId: string;
  doctorName: string;
  specialty: string;
  scheduledAt: string;
  status: number;
  tokenNo?: number | null;
  reason?: string | null;
  createdAt: string;
}

export interface CheckInResponse {
  appointmentId: string;
  tokenNo: number;
  date: string;
  doctorName: string;
  patientName: string;
  patientMrn: string;
}

export interface OpdQueueItemDto {
  appointmentId: string;
  tokenNo: number;
  patientId: string;
  patientName: string;
  patientMrn: string;
  ageYears?: number | null;
  gender: number;
  scheduledAt: string;
  status: number;
}
