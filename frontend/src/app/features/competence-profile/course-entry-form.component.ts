import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
  OnChanges,
  output,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  CourseEntryDto,
  CreateCourseRequest,
  UpdateCourseRequest,
} from '../../core/models/competence-profile.model';

@Component({
  selector: 'app-course-entry-form',
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
        {{ mode() === 'create' ? 'Add Course' : 'Edit Course' }}
      </h3>

      <!-- Name -->
      <div class="flex flex-col gap-1">
        <label for="course-name" class="text-xs font-medium text-gray-700 dark:text-gray-300">
          Course Name <span class="text-danger" aria-hidden="true">*</span>
        </label>
        <input
          id="course-name"
          type="text"
          formControlName="name"
          placeholder="e.g. Clean Architecture in .NET"
          class="rounded-md border border-gray-300 bg-white px-3 py-2 text-sm
                 text-gray-900 placeholder-gray-400
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary
                 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
        />
        @if (form.controls.name.invalid && form.controls.name.touched) {
          <p class="text-xs text-danger">Course name is required.</p>
        }
      </div>

      <!-- Provider -->
      <div class="flex flex-col gap-1">
        <label for="course-provider" class="text-xs font-medium text-gray-700 dark:text-gray-300">
          Provider <span class="text-danger" aria-hidden="true">*</span>
        </label>
        <input
          id="course-provider"
          type="text"
          formControlName="provider"
          placeholder="e.g. Pluralsight, Udemy, Coursera"
          class="rounded-md border border-gray-300 bg-white px-3 py-2 text-sm
                 text-gray-900 placeholder-gray-400
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary
                 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
        />
        @if (form.controls.provider.invalid && form.controls.provider.touched) {
          <p class="text-xs text-danger">Provider is required.</p>
        }
      </div>

      <!-- Completion Date -->
      <div class="flex flex-col gap-1">
        <label for="course-date" class="text-xs font-medium text-gray-700 dark:text-gray-300">
          Completion Date <span class="text-danger" aria-hidden="true">*</span>
        </label>
        <input
          id="course-date"
          type="date"
          formControlName="completionDate"
          class="rounded-md border border-gray-300 bg-white px-3 py-2 text-sm
                 text-gray-900
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary
                 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
        />
        @if (form.controls.completionDate.invalid && form.controls.completionDate.touched) {
          <p class="text-xs text-danger">Completion date is required.</p>
        }
      </div>

      <!-- Skills Acquired -->
      <div class="flex flex-col gap-1">
        <label for="course-skills" class="text-xs font-medium text-gray-700 dark:text-gray-300">
          Skills Acquired <span class="text-gray-400">(optional — comma-separated)</span>
        </label>
        <input
          id="course-skills"
          type="text"
          formControlName="skillsRaw"
          placeholder="e.g. C#, SOLID, Clean Architecture"
          class="rounded-md border border-gray-300 bg-white px-3 py-2 text-sm
                 text-gray-900 placeholder-gray-400
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary
                 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
        />
        <p class="text-xs text-gray-400 dark:text-gray-500">
          Separate skills with commas. Duplicates and blanks are removed automatically.
        </p>
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
export class CourseEntryFormComponent implements OnChanges {
  private readonly fb = inject(FormBuilder);

  readonly mode = input<'create' | 'edit'>('create');
  readonly entry = input<CourseEntryDto | null>(null);
  readonly saving = input(false);

  readonly submitted = output<CreateCourseRequest | UpdateCourseRequest>();
  readonly cancelled = output<void>();

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    provider: ['', [Validators.required, Validators.maxLength(200)]],
    completionDate: ['', Validators.required],
    skillsRaw: [''],
  });

  ngOnChanges(): void {
    const e = this.entry();
    if (e) {
      this.form.patchValue({
        name: e.name,
        provider: e.provider,
        completionDate: e.completionDate,
        skillsRaw: e.skillsAcquired.join(', '),
      });
    } else {
      this.form.reset({ name: '', provider: '', completionDate: '', skillsRaw: '' });
    }
  }

  submit(): void {
    if (this.form.invalid) return;
    const { name, provider, completionDate, skillsRaw } = this.form.getRawValue();
    const skillsAcquired = parseSkills(skillsRaw);
    this.submitted.emit({ name, provider, completionDate, skillsAcquired });
  }
}

/** Splits comma-separated input into a normalized, deduplicated, trimmed skill array. */
function parseSkills(raw: string): string[] {
  return [...new Set(
    raw.split(',')
      .map(s => s.trim())
      .filter(s => s.length > 0),
  )];
}
