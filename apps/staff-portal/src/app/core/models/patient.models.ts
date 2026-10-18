export enum Gender {
  Unknown = 0,
  Male = 1,
  Female = 2,
  Other = 3
}

export enum BloodGroup {
  Unknown = 0,
  APositive = 1,
  ANegative = 2,
  BPositive = 3,
  BNegative = 4,
  ABPositive = 5,
  ABNegative = 6,
  OPositive = 7,
  ONegative = 8
}

export enum MaritalStatus {
  Single = 1,
  Married = 2,
  Widowed = 3,
  Divorced = 4
}

export interface PatientEmergencyContactDto {
  name: string;
  relationship: string;
  phone: string;
}

export interface DuplicateWarningDto {
  hasDuplicateWarning: boolean;
  reason?: string | null;
  existingMrn?: string | null;
  existingPatientName?: string | null;
}

export interface PatientResponse {
  id: string;
  hospitalId: string;
  mrn: string;
  firstNameEn?: string | null;
  lastNameEn?: string | null;
  firstNameBn?: string | null;
  lastNameBn?: string | null;
  displayName: string;
  dateOfBirth?: string | null;
  ageYears?: number | null;
  gender: Gender;
  bloodGroup: BloodGroup;
  phonePrimary: string;
  phoneSecondary?: string | null;
  email?: string | null;
  nidLast4?: string | null;
  birthRegNo?: string | null;
  divisionId?: number | null;
  divisionName?: string | null;
  districtId?: number | null;
  districtName?: string | null;
  upazilaId?: number | null;
  upazilaName?: string | null;
  addressLine?: string | null;
  occupation?: string | null;
  maritalStatus?: MaritalStatus | null;
  religion?: string | null;
  createdAt: string;
  duplicateWarning?: DuplicateWarningDto | null;
}

export interface CreatePatientRequest {
  firstNameEn?: string;
  lastNameEn?: string;
  firstNameBn?: string;
  lastNameBn?: string;
  dateOfBirth?: string;
  gender: Gender;
  bloodGroup: BloodGroup;
  phonePrimary: string;
  phoneSecondary?: string;
  email?: string;
  nationalId?: string;
  birthRegNo?: string;
  divisionId?: number;
  districtId?: number;
  upazilaId?: number;
  addressLine?: string;
  occupation?: string;
  maritalStatus?: MaritalStatus;
  religion?: string;
  emergencyContact?: PatientEmergencyContactDto;
}

export interface UpdatePatientRequest extends Partial<CreatePatientRequest> {}

export interface PatientSearchQuery {
  query?: string;
  phone?: string;
  nidLast4?: string;
  mrn?: string;
  page: number;
  pageSize: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface DivisionDto {
  id: number;
  code: string;
  nameEn: string;
  nameBn: string;
}

export interface DistrictDto {
  id: number;
  divisionId: number;
  code: string;
  nameEn: string;
  nameBn: string;
}

export interface UpazilaDto {
  id: number;
  districtId: number;
  nameEn: string;
  nameBn: string;
}
