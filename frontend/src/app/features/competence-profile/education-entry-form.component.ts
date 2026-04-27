import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  OnChanges,
  output,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CreateEducationRequest, EducationEntryDto, UpdateEducationRequest } from '../../core/models/competence-profile.model';

const CURRENT_YEAR = new Date().getFullYear();

@Component({
  selector: 'app-education-entry-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  template: `
    <form
      [formGroup]="form"
      (ngSubmit)="submit()"
      class="space-y-4 rounded-xl border border-gray-200 bg-white p-5 shadow-sm
             dark:border-gray-700 dark:bg-gray-800"
    >
      <h3 class="text-base font-semibold text-gray-900 dark:text-gray-100">
        {{ mode() === 'create' ? 'Add Education' : 'Edit Education' }}
      </h3>

      <!-- Degree -->
      <div class="flex flex-col gap-1">
        <label for="edu-degree" class="text-xs font-medium text-gray-700 dark:text-gray-300">
          Degree <span class="text-danger" aria-hidden="true">*</span>
        </label>
        <input
          id="edu-degree"
          type="text"
          formControlName="degree"
          placeholder="e.g. BSc Computer Science"
          class="rounded-md border border-gray-300 bg-white px-3 py-2 text-sm
                 text-gray-900 placeholder-gray-400
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary
                 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
        />
        @if (form.controls.degree.invalid && form.controls.degree.touched) {
          <p class="text-xs text-danger">Degree is required.</p>
        }
      </div>

      <!-- Institution -->
      <div class="flex flex-col gap-1">
        <label for="edu-institution" class="text-xs font-medium text-gray-700 dark:text-gray-300">
          Institution <span class="text-danger" aria-hidden="true">*</span>
        </label>
        <input
          id="edu-institution"
          type="text"
          formControlName="institution"
          placeholder="e.g. MIT"
          class="rounded-md border border-gray-300 bg-white px-3 py-2 text-sm
                 text-gray-900 placeholder-gray-400
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary
                 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
        />
        @if (form.controls.institution.invalid && form.controls.institution.touched) {
          <p class="text-xs text-danger">Institution is required.</p>
        }
      </div>

      <!-- Graduation Year -->
      <div class="flex flex-col gap-1">
        <label for="edu-year" class="text-xs font-medium text-gray-700 dark:text-gray-300">
          Graduation Year <span class="text-danger" aria-hidden="true">*</span>
        </label>
        <input
          id="edu-year"
          type="number"
          formControlName="graduationYear"
          [min]="1900"
          [max]="currentYear + 1"
          class="rounded-md border border-gray-300 bg-white px-3 py-2 text-sm
                 text-gray-900 placeholder-gray-400
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary
                 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
        />
        @if (form.controls.graduationYear.invalid && form.controls.graduationYear.touched) {
          <p class="text-xs text-danger">
            Graduation year must be between 1900 and {{ currentYear + 1 }}.
          </p>
        }
      </div>

      <!-- Notes -->
      <div class="flex flex-col gap-1">
        <label for="edu-notes" class="text-xs font-medium text-gray-700 dark:text-gray-300">
          Notes <span class="text-gray-400">(optional)</span>
        </label>
        <textarea
          id="edu-notes"
          formControlName="notes"
          rows="2"
          placeholder="Any additional notes…"
          class="rounded-md border border-gray-300 bg-white px-3 py-2 text-sm
                 text-gray-900 placeholder-gray-400 resize-none
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary
                 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
        ></textarea>
        @if (form.controls.notes.invalid && form.controls.notes.touched) {
          <p class="text-xs text-danger">Notes must not exceed 500 characters.</p>
        }
      </div>

      <!-- Actions -->
      <div class="flex gap-3 pt-1">
        <button
          type="submit"
          [disabled]="form.invalid || saving()"
          class="rounded-md bg-primary px-4 py-2 text-sm font-medium text-white
                 transition-colors duration-150 hover:bg-primary/90
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary
                 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {{ saving() ? 'Saving…' : 'Save' }}
        </button>
        <button
          type="button"
          (click)="cancelled.emit()"
          class="rounded-md border border-gray-300 px-4 py-2 text-sm font-medium
                 text-gray-700 transition-colors duration-150 hover:bg-gray-50
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary
                 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700"
        >
          Cancel
        </button>
      </div>
    </form>
  `,
})
export class EducationEntryFormComponent implements OnChanges {
  private readonly fb = inject(FormBuilder);

  readonly mode = input<'create' | 'edit'>('create');
  readonly entry = input<EducationEntryDto | null>(null);
  readonly saving = input(false);

  readonly submitted = output<CreateEducationRequest | UpdateEducationRequest>();
  readonly cancelled = output<void>();

  readonly currentYear = CURRENT_YEAR;

  readonly form = this.fb.nonNullable.group({
    degree: ['', [Validators.required, Validators.maxLength(200)]],
    institution: ['', [Validators.required, Validators.maxLength(200)]],
    graduationYear: [
      CURRENT_YEAR,
      [Validators.required, Validators.min(1900), Validators.max(CURRENT_YEAR + 1)],
    ],
    notes: ['', [Validators.maxLength(500)]],
  });

  ngOnChanges(): void {
    const e = this.entry();
    if (e) {
      this.form.patchValue({
        degree: e.degree,
        institution: e.institution,
        graduationYear: e.graduationYear,
        notes: e.notes ?? '',
      });
    } else {
      this.form.reset({ degree: '', institution: '', graduationYear: CURRENT_YEAR, notes: '' });
    }
  }

  submit(): void {
    if (this.form.invalid) return;
    const { degree, institution, graduationYear, notes } = this.form.getRawValue();
    this.submitted.emit({
      degree,
      institution,
      graduationYear,
      notes: notes || null,
    });
  }
}
