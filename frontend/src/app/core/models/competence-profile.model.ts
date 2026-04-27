// ── Response DTOs ─────────────────────────────────────────────────────────────

export interface CompetenceProfileDto {
  education: EducationEntryDto[];
  certificates: CertificateEntryDto[];
  courses: CourseEntryDto[];
}

export interface EducationEntryDto {
  id: string;
  degree: string;
  institution: string;
  graduationYear: number;
  notes: string | null;
  createdUtc: string;
  updatedUtc: string;
}

export interface CertificateEntryDto {
  id: string;
  name: string;
  issuingOrganization: string;
  issueDate: string;       // ISO date string (YYYY-MM-DD)
  expirationDate: string | null;
  createdUtc: string;
  updatedUtc: string;
}

export interface CourseEntryDto {
  id: string;
  name: string;
  provider: string;
  completionDate: string;  // ISO date string (YYYY-MM-DD)
  skillsAcquired: string[];
  createdUtc: string;
  updatedUtc: string;
}

// ── Request models ────────────────────────────────────────────────────────────

export interface CreateEducationRequest {
  degree: string;
  institution: string;
  graduationYear: number;
  notes?: string | null;
}

export interface UpdateEducationRequest {
  degree: string;
  institution: string;
  graduationYear: number;
  notes?: string | null;
}

export interface CreateCertificateRequest {
  name: string;
  issuingOrganization: string;
  issueDate: string;
  expirationDate?: string | null;
}

export interface UpdateCertificateRequest {
  name: string;
  issuingOrganization: string;
  issueDate: string;
  expirationDate?: string | null;
}

export interface CreateCourseRequest {
  name: string;
  provider: string;
  completionDate: string;
  skillsAcquired?: string[] | null;
}

export interface UpdateCourseRequest {
  name: string;
  provider: string;
  completionDate: string;
  skillsAcquired?: string[] | null;
}
