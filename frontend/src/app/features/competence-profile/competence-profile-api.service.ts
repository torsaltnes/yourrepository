import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CertificateEntryDto,
  CompetenceProfileDto,
  CourseEntryDto,
  CreateCertificateRequest,
  CreateCourseRequest,
  CreateEducationRequest,
  EducationEntryDto,
  UpdateCertificateRequest,
  UpdateCourseRequest,
  UpdateEducationRequest,
} from '../../core/models/competence-profile.model';

const BASE = '/api/me/competence-profile';

@Injectable({ providedIn: 'root' })
export class CompetenceProfileApiService {
  private readonly http = inject(HttpClient);

  // ── Profile ───────────────────────────────────────────────────────────────
  getProfile(): Observable<CompetenceProfileDto> {
    return this.http.get<CompetenceProfileDto>(BASE);
  }

  // ── Education ─────────────────────────────────────────────────────────────
  addEducation(request: CreateEducationRequest): Observable<EducationEntryDto> {
    return this.http.post<EducationEntryDto>(`${BASE}/education`, request);
  }

  updateEducation(entryId: string, request: UpdateEducationRequest): Observable<EducationEntryDto> {
    return this.http.put<EducationEntryDto>(`${BASE}/education/${entryId}`, request);
  }

  deleteEducation(entryId: string): Observable<void> {
    return this.http.delete<void>(`${BASE}/education/${entryId}`);
  }

  // ── Certificates ──────────────────────────────────────────────────────────
  addCertificate(request: CreateCertificateRequest): Observable<CertificateEntryDto> {
    return this.http.post<CertificateEntryDto>(`${BASE}/certificates`, request);
  }

  updateCertificate(entryId: string, request: UpdateCertificateRequest): Observable<CertificateEntryDto> {
    return this.http.put<CertificateEntryDto>(`${BASE}/certificates/${entryId}`, request);
  }

  deleteCertificate(entryId: string): Observable<void> {
    return this.http.delete<void>(`${BASE}/certificates/${entryId}`);
  }

  // ── Courses ───────────────────────────────────────────────────────────────
  addCourse(request: CreateCourseRequest): Observable<CourseEntryDto> {
    return this.http.post<CourseEntryDto>(`${BASE}/courses`, request);
  }

  updateCourse(entryId: string, request: UpdateCourseRequest): Observable<CourseEntryDto> {
    return this.http.put<CourseEntryDto>(`${BASE}/courses/${entryId}`, request);
  }

  deleteCourse(entryId: string): Observable<void> {
    return this.http.delete<void>(`${BASE}/courses/${entryId}`);
  }
}
