import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { CompetenceProfileStoreService } from './competence-profile-store.service';
import { EducationEntryFormComponent } from './education-entry-form.component';
import { CertificateEntryFormComponent } from './certificate-entry-form.component';
import { CourseEntryFormComponent } from './course-entry-form.component';
import {
  CertificateEntryDto,
  CourseEntryDto,
  CreateCertificateRequest,
  CreateCourseRequest,
  CreateEducationRequest,
  EducationEntryDto,
  UpdateCertificateRequest,
  UpdateCourseRequest,
  UpdateEducationRequest,
} from '../../core/models/competence-profile.model';

@Component({
  selector: 'app-competence-profile-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    EducationEntryFormComponent,
    CertificateEntryFormComponent,
    CourseEntryFormComponent,
  ],
  template: `
    <!-- ── Page header ──────────────────────────────────────────────────── -->
    <div class="mb-6 flex flex-col gap-1 sm:flex-row sm:items-center sm:justify-between">
      <div>
        <h2 class="text-xl font-semibold text-balance text-gray-900 dark:text-gray-100">
          Competence Profile
        </h2>
        <p class="mt-0.5 text-sm text-gray-500 dark:text-gray-400">
          Manage your education, certificates, and completed courses.
        </p>
      </div>
    </div>

    <!-- ── Global error banner ──────────────────────────────────────────── -->
    @if (store.error()) {
      <div
        role="alert"
        class="mb-6 flex items-center gap-3 rounded-lg border border-red-200 bg-red-50
               px-4 py-3 text-sm text-red-700
               dark:border-red-800/40 dark:bg-red-900/20 dark:text-red-300"
      >
        <span class="font-medium">Error:</span> {{ store.error() }}
      </div>
    }

    <!-- ── Loading state ─────────────────────────────────────────────────── -->
    @if (store.loading()) {
      <div class="flex items-center justify-center py-20 text-sm text-gray-400">
        Loading profile…
      </div>
    } @else {

      <!-- ── Education section ──────────────────────────────────────────── -->
      <section class="mb-8" aria-labelledby="education-heading">
        <div class="mb-3 flex items-center justify-between gap-4">
          <h3 id="education-heading"
              class="text-base font-semibold text-gray-800 dark:text-gray-200">
            Education
          </h3>
          <button
            (click)="openCreate('education')"
            class="rounded-md bg-primary px-3 py-1.5 text-xs font-medium text-white
                   transition-colors duration-150 hover:bg-primary/90
                   focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
          >
            + Add
          </button>
        </div>

        @if (activeForm().type === 'education' && activeForm().mode !== 'closed') {
          <div class="mb-4">
            <app-education-entry-form
              [mode]="activeForm().mode === 'create' ? 'create' : 'edit'"
              [entry]="editingEducation"
              [saving]="store.saving()"
              (submitted)="onEducationSubmit($event)"
              (cancelled)="store.closeForm()"
            />
          </div>
        }

        @if (store.profile()?.education?.length === 0 && activeForm().type !== 'education') {
          <p class="text-sm text-gray-400 dark:text-gray-500 italic">
            No education entries yet. Add one above.
          </p>
        } @else {
          <ul class="space-y-3">
            @for (entry of store.profile()?.education ?? []; track entry.id) {
              <li
                class="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm
                       transition-shadow hover:shadow-md
                       dark:border-gray-700 dark:bg-gray-800"
              >
                <div class="flex items-start justify-between gap-4 px-5 py-4">
                  <div class="min-w-0 flex-1">
                    <p class="font-medium text-gray-900 dark:text-gray-100">{{ entry.degree }}</p>
                    <p class="mt-0.5 text-sm text-gray-500 dark:text-gray-400">
                      {{ entry.institution }} · {{ entry.graduationYear }}
                    </p>
                    @if (entry.notes) {
                      <p class="mt-1 text-sm text-gray-600 dark:text-gray-300">{{ entry.notes }}</p>
                    }
                  </div>
                  <div class="flex shrink-0 gap-2">
                    <button
                      (click)="openEdit('education', entry.id)"
                      class="rounded px-2 py-1 text-xs font-medium text-primary
                             transition-colors duration-150 hover:bg-primary/10
                             focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
                    >
                      Edit
                    </button>
                    <button
                      (click)="deleteEducation(entry)"
                      class="rounded px-2 py-1 text-xs font-medium text-danger
                             transition-colors duration-150 hover:bg-danger/10
                             focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-danger"
                    >
                      Delete
                    </button>
                  </div>
                </div>
              </li>
            }
          </ul>
        }
      </section>

      <!-- ── Certificates section ───────────────────────────────────────── -->
      <section class="mb-8" aria-labelledby="certs-heading">
        <div class="mb-3 flex items-center justify-between gap-4">
          <h3 id="certs-heading"
              class="text-base font-semibold text-gray-800 dark:text-gray-200">
            Certificates
          </h3>
          <button
            (click)="openCreate('certificate')"
            class="rounded-md bg-primary px-3 py-1.5 text-xs font-medium text-white
                   transition-colors duration-150 hover:bg-primary/90
                   focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
          >
            + Add
          </button>
        </div>

        @if (activeForm().type === 'certificate' && activeForm().mode !== 'closed') {
          <div class="mb-4">
            <app-certificate-entry-form
              [mode]="activeForm().mode === 'create' ? 'create' : 'edit'"
              [entry]="editingCertificate"
              [saving]="store.saving()"
              (submitted)="onCertificateSubmit($event)"
              (cancelled)="store.closeForm()"
            />
          </div>
        }

        @if (store.profile()?.certificates?.length === 0 && activeForm().type !== 'certificate') {
          <p class="text-sm text-gray-400 dark:text-gray-500 italic">
            No certificate entries yet. Add one above.
          </p>
        } @else {
          <ul class="space-y-3">
            @for (entry of store.profile()?.certificates ?? []; track entry.id) {
              <li
                class="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm
                       transition-shadow hover:shadow-md
                       dark:border-gray-700 dark:bg-gray-800"
              >
                <div class="flex items-start justify-between gap-4 px-5 py-4">
                  <div class="min-w-0 flex-1">
                    <p class="font-medium text-gray-900 dark:text-gray-100">{{ entry.name }}</p>
                    <p class="mt-0.5 text-sm text-gray-500 dark:text-gray-400">
                      {{ entry.issuingOrganization }}
                    </p>
                    <p class="mt-0.5 text-xs text-gray-400 dark:text-gray-500">
                      Issued: {{ entry.issueDate | date:'mediumDate' }}
                      @if (entry.expirationDate) {
                        · Expires: {{ entry.expirationDate | date:'mediumDate' }}
                      } @else {
                        · No expiration
                      }
                    </p>
                  </div>
                  <div class="flex shrink-0 gap-2">
                    <button
                      (click)="openEdit('certificate', entry.id)"
                      class="rounded px-2 py-1 text-xs font-medium text-primary
                             transition-colors duration-150 hover:bg-primary/10
                             focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
                    >
                      Edit
                    </button>
                    <button
                      (click)="deleteCertificate(entry)"
                      class="rounded px-2 py-1 text-xs font-medium text-danger
                             transition-colors duration-150 hover:bg-danger/10
                             focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-danger"
                    >
                      Delete
                    </button>
                  </div>
                </div>
              </li>
            }
          </ul>
        }
      </section>

      <!-- ── Courses section ────────────────────────────────────────────── -->
      <section aria-labelledby="courses-heading">
        <div class="mb-3 flex items-center justify-between gap-4">
          <h3 id="courses-heading"
              class="text-base font-semibold text-gray-800 dark:text-gray-200">
            Courses
          </h3>
          <button
            (click)="openCreate('course')"
            class="rounded-md bg-primary px-3 py-1.5 text-xs font-medium text-white
                   transition-colors duration-150 hover:bg-primary/90
                   focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
          >
            + Add
          </button>
        </div>

        @if (activeForm().type === 'course' && activeForm().mode !== 'closed') {
          <div class="mb-4">
            <app-course-entry-form
              [mode]="activeForm().mode === 'create' ? 'create' : 'edit'"
              [entry]="editingCourse"
              [saving]="store.saving()"
              (submitted)="onCourseSubmit($event)"
              (cancelled)="store.closeForm()"
            />
          </div>
        }

        @if (store.profile()?.courses?.length === 0 && activeForm().type !== 'course') {
          <p class="text-sm text-gray-400 dark:text-gray-500 italic">
            No course entries yet. Add one above.
          </p>
        } @else {
          <ul class="space-y-3">
            @for (entry of store.profile()?.courses ?? []; track entry.id) {
              <li
                class="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm
                       transition-shadow hover:shadow-md
                       dark:border-gray-700 dark:bg-gray-800"
              >
                <div class="px-5 py-4">
                  <div class="flex items-start justify-between gap-4">
                    <div class="min-w-0 flex-1">
                      <p class="font-medium text-gray-900 dark:text-gray-100">{{ entry.name }}</p>
                      <p class="mt-0.5 text-sm text-gray-500 dark:text-gray-400">
                        {{ entry.provider }} · Completed {{ entry.completionDate | date:'mediumDate' }}
                      </p>
                    </div>
                    <div class="flex shrink-0 gap-2">
                      <button
                        (click)="openEdit('course', entry.id)"
                        class="rounded px-2 py-1 text-xs font-medium text-primary
                               transition-colors duration-150 hover:bg-primary/10
                               focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
                      >
                        Edit
                      </button>
                      <button
                        (click)="deleteCourse(entry)"
                        class="rounded px-2 py-1 text-xs font-medium text-danger
                               transition-colors duration-150 hover:bg-danger/10
                               focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-danger"
                      >
                        Delete
                      </button>
                    </div>
                  </div>

                  @if (entry.skillsAcquired.length > 0) {
                    <div class="mt-3 flex flex-wrap gap-1.5">
                      @for (skill of entry.skillsAcquired; track skill) {
                        <span
                          class="inline-flex items-center rounded-full
                                 bg-blue-100 px-2.5 py-0.5 text-xs font-medium text-blue-800
                                 transition-colors duration-150
                                 dark:bg-blue-900/30 dark:text-blue-300"
                        >
                          {{ skill }}
                        </span>
                      }
                    </div>
                  }
                </div>
              </li>
            }
          </ul>
        }
      </section>

    }
  `,
})
export class CompetenceProfilePageComponent implements OnInit {
  readonly store = inject(CompetenceProfileStoreService);

  // Alias for cleaner template usage
  readonly activeForm = this.store.activeForm;

  ngOnInit(): void {
    void this.store.load();
  }

  openCreate(type: 'education' | 'certificate' | 'course'): void {
    this.store.openCreate(type);
  }

  openEdit(type: 'education' | 'certificate' | 'course', entryId: string): void {
    this.store.openEdit(type, entryId);
  }

  // ── Education ─────────────────────────────────────────────────────────────

  get editingEducation(): EducationEntryDto | null {
    const form = this.store.activeForm();
    return form.mode === 'edit' && form.type === 'education' && form.entryId
      ? (this.store.getEducationEntry(form.entryId) ?? null)
      : null;
  }

  onEducationSubmit(req: CreateEducationRequest | UpdateEducationRequest): void {
    void this.store.saveEducation(req);
  }

  deleteEducation(entry: EducationEntryDto): void {
    void this.store.deleteEducation(entry.id);
  }

  // ── Certificates ──────────────────────────────────────────────────────────

  get editingCertificate(): CertificateEntryDto | null {
    const form = this.store.activeForm();
    return form.mode === 'edit' && form.type === 'certificate' && form.entryId
      ? (this.store.getCertificateEntry(form.entryId) ?? null)
      : null;
  }

  onCertificateSubmit(req: CreateCertificateRequest | UpdateCertificateRequest): void {
    void this.store.saveCertificate(req);
  }

  deleteCertificate(entry: CertificateEntryDto): void {
    void this.store.deleteCertificate(entry.id);
  }

  // ── Courses ───────────────────────────────────────────────────────────────

  get editingCourse(): CourseEntryDto | null {
    const form = this.store.activeForm();
    return form.mode === 'edit' && form.type === 'course' && form.entryId
      ? (this.store.getCourseEntry(form.entryId) ?? null)
      : null;
  }

  onCourseSubmit(req: CreateCourseRequest | UpdateCourseRequest): void {
    void this.store.saveCourse(req);
  }

  deleteCourse(entry: CourseEntryDto): void {
    void this.store.deleteCourse(entry.id);
  }
}
