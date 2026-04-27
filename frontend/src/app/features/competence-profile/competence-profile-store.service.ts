import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
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
import { CompetenceProfileApiService } from './competence-profile-api.service';

export type EntryType = 'education' | 'certificate' | 'course';
export type FormMode = 'closed' | 'create' | 'edit';

export interface ActiveForm {
  type: EntryType;
  mode: FormMode;
  entryId: string | null;
}

@Injectable({ providedIn: 'root' })
export class CompetenceProfileStoreService {
  private readonly api = inject(CompetenceProfileApiService);

  // ── State signals ─────────────────────────────────────────────────────────
  readonly profile = signal<CompetenceProfileDto | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly activeForm = signal<ActiveForm>({ type: 'education', mode: 'closed', entryId: null });

  // ── Computed ──────────────────────────────────────────────────────────────
  readonly hasEntries = computed(() => {
    const p = this.profile();
    return p !== null &&
      (p.education.length > 0 || p.certificates.length > 0 || p.courses.length > 0);
  });

  readonly isFormOpen = computed(() => this.activeForm().mode !== 'closed');

  // ── Actions ───────────────────────────────────────────────────────────────

  async load(): Promise<void> {
    this.error.set(null);
    this.loading.set(true);
    try {
      const dto = await firstValueFrom(this.api.getProfile());
      this.profile.set(dto);
    } catch (err) {
      this.error.set(errorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  openCreate(type: EntryType): void {
    this.activeForm.set({ type, mode: 'create', entryId: null });
    this.error.set(null);
  }

  openEdit(type: EntryType, entryId: string): void {
    this.activeForm.set({ type, mode: 'edit', entryId });
    this.error.set(null);
  }

  closeForm(): void {
    this.activeForm.set({ type: 'education', mode: 'closed', entryId: null });
    this.error.set(null);
  }

  // ── Education mutations ───────────────────────────────────────────────────

  async saveEducation(request: CreateEducationRequest | UpdateEducationRequest): Promise<void> {
    await this.mutate(async () => {
      const form = this.activeForm();
      if (form.mode === 'create') {
        const entry = await firstValueFrom(this.api.addEducation(request as CreateEducationRequest));
        this.profile.update(p => p ? { ...p, education: [entry, ...p.education] } : p);
      } else if (form.entryId) {
        const entry = await firstValueFrom(this.api.updateEducation(form.entryId, request as UpdateEducationRequest));
        this.profile.update(p => p
          ? { ...p, education: p.education.map(e => e.id === entry.id ? entry : e) }
          : p);
      }
      this.closeForm();
    });
  }

  async deleteEducation(entryId: string): Promise<void> {
    await this.mutate(async () => {
      await firstValueFrom(this.api.deleteEducation(entryId));
      this.profile.update(p => p
        ? { ...p, education: p.education.filter(e => e.id !== entryId) }
        : p);
    });
  }

  // ── Certificate mutations ─────────────────────────────────────────────────

  async saveCertificate(request: CreateCertificateRequest | UpdateCertificateRequest): Promise<void> {
    await this.mutate(async () => {
      const form = this.activeForm();
      if (form.mode === 'create') {
        const entry = await firstValueFrom(this.api.addCertificate(request as CreateCertificateRequest));
        this.profile.update(p => p ? { ...p, certificates: [entry, ...p.certificates] } : p);
      } else if (form.entryId) {
        const entry = await firstValueFrom(this.api.updateCertificate(form.entryId, request as UpdateCertificateRequest));
        this.profile.update(p => p
          ? { ...p, certificates: p.certificates.map(c => c.id === entry.id ? entry : c) }
          : p);
      }
      this.closeForm();
    });
  }

  async deleteCertificate(entryId: string): Promise<void> {
    await this.mutate(async () => {
      await firstValueFrom(this.api.deleteCertificate(entryId));
      this.profile.update(p => p
        ? { ...p, certificates: p.certificates.filter(c => c.id !== entryId) }
        : p);
    });
  }

  // ── Course mutations ──────────────────────────────────────────────────────

  async saveCourse(request: CreateCourseRequest | UpdateCourseRequest): Promise<void> {
    await this.mutate(async () => {
      const form = this.activeForm();
      if (form.mode === 'create') {
        const entry = await firstValueFrom(this.api.addCourse(request as CreateCourseRequest));
        this.profile.update(p => p ? { ...p, courses: [entry, ...p.courses] } : p);
      } else if (form.entryId) {
        const entry = await firstValueFrom(this.api.updateCourse(form.entryId, request as UpdateCourseRequest));
        this.profile.update(p => p
          ? { ...p, courses: p.courses.map(c => c.id === entry.id ? entry : c) }
          : p);
      }
      this.closeForm();
    });
  }

  async deleteCourse(entryId: string): Promise<void> {
    await this.mutate(async () => {
      await firstValueFrom(this.api.deleteCourse(entryId));
      this.profile.update(p => p
        ? { ...p, courses: p.courses.filter(c => c.id !== entryId) }
        : p);
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  getEducationEntry(entryId: string): EducationEntryDto | undefined {
    return this.profile()?.education.find(e => e.id === entryId);
  }

  getCertificateEntry(entryId: string): CertificateEntryDto | undefined {
    return this.profile()?.certificates.find(c => c.id === entryId);
  }

  getCourseEntry(entryId: string): CourseEntryDto | undefined {
    return this.profile()?.courses.find(c => c.id === entryId);
  }

  private async mutate(action: () => Promise<void>): Promise<void> {
    this.error.set(null);
    this.saving.set(true);
    try {
      await action();
    } catch (err) {
      this.error.set(errorMessage(err));
    } finally {
      this.saving.set(false);
    }
  }
}

function errorMessage(err: unknown): string {
  if (err instanceof Error) return err.message;
  return 'An unexpected error occurred.';
}
