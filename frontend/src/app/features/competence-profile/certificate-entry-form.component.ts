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
  CertificateEntryDto,
  CreateCertificateRequest,
  UpdateCertificateRequest,
} from '../../core/models/competence-profile.model';

@Component({
  selector: 'app-certificate-entry-form',
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
        {{ mode() === 'create' ? 'Add Certificate' : 'Edit Certificate' }}
      </h3>

      <!-- Name -->
      <div class="flex flex-col gap-1">
        <label for="cert-name" class="text-xs font-medium text-gray-700 dark:text-gray-300">
          Certificate Name <span class="text-danger" aria-hidden="true">*</span>
        </label>
        <input
          id="cert-name"
          type="text"
          formControlName="name"
          placeholder="e.g. AWS Solutions Architect"
          class="rounded-md border border-gray-300 bg-white px-3 py-2 text-sm
                 text-gray-900 placeholder-gray-400
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary
                 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
        />
        @if (form.controls.name.invalid && form.controls.name.touched) {
          <p class="text-xs text-danger">Certificate name is required.</p>
        }
      </div>

      <!-- Issuing Organization -->
      <div class="flex flex-col gap-1">
        <label for="cert-org" class="text-xs font-medium text-gray-700 dark:text-gray-300">
          Issuing Organization <span class="text-danger" aria-hidden="true">*</span>
        </label>
        <input
          id="cert-org"
          type="text"
          formControlName="issuingOrganization"
          placeholder="e.g. Amazon Web Services"
          class="rounded-md border border-gray-300 bg-white px-3 py-2 text-sm
                 text-gray-900 placeholder-gray-400
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary
                 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
        />
        @if (form.controls.issuingOrganization.invalid && form.controls.issuingOrganization.touched) {
          <p class="text-xs text-danger">Issuing organization is required.</p>
        }
      </div>

      <!-- Issue Date -->
      <div class="flex flex-col gap-1">
        <label for="cert-issue" class="text-xs font-medium text-gray-700 dark:text-gray-300">
          Issue Date <span class="text-danger" aria-hidden="true">*</span>
        </label>
        <input
          id="cert-issue"
          type="date"
          formControlName="issueDate"
          class="rounded-md border border-gray-300 bg-white px-3 py-2 text-sm
                 text-gray-900
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary
                 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
        />
        @if (form.controls.issueDate.invalid && form.controls.issueDate.touched) {
          <p class="text-xs text-danger">Issue date is required.</p>
        }
      </div>

      <!-- Expiration Date -->
      <div class="flex flex-col gap-1">
        <label for="cert-expiry" class="text-xs font-medium text-gray-700 dark:text-gray-300">
          Expiration Date <span class="text-gray-400">(optional)</span>
        </label>
        <input
          id="cert-expiry"
          type="date"
          formControlName="expirationDate"
          class="rounded-md border border-gray-300 bg-white px-3 py-2 text-sm
                 text-gray-900
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary
                 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-100"
        />
        @if (form.errors?.['expirationBeforeIssue'] && form.controls.expirationDate.touched) {
          <p class="text-xs text-danger">Expiration date cannot be before the issue date.</p>
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
export class CertificateEntryFormComponent implements OnChanges {
  private readonly fb = inject(FormBuilder);

  readonly mode = input<'create' | 'edit'>('create');
  readonly entry = input<CertificateEntryDto | null>(null);
  readonly saving = input(false);

  readonly submitted = output<CreateCertificateRequest | UpdateCertificateRequest>();
  readonly cancelled = output<void>();

  readonly form = this.fb.nonNullable.group(
    {
      name: ['', [Validators.required, Validators.maxLength(200)]],
      issuingOrganization: ['', [Validators.required, Validators.maxLength(200)]],
      issueDate: ['', Validators.required],
      expirationDate: [''],
    },
    { validators: expirationAfterIssueDateValidator },
  );

  ngOnChanges(): void {
    const e = this.entry();
    if (e) {
      this.form.patchValue({
        name: e.name,
        issuingOrganization: e.issuingOrganization,
        issueDate: e.issueDate,
        expirationDate: e.expirationDate ?? '',
      });
    } else {
      this.form.reset({ name: '', issuingOrganization: '', issueDate: '', expirationDate: '' });
    }
  }

  submit(): void {
    if (this.form.invalid) return;
    const { name, issuingOrganization, issueDate, expirationDate } = this.form.getRawValue();
    this.submitted.emit({
      name,
      issuingOrganization,
      issueDate,
      expirationDate: expirationDate || null,
    });
  }
}

import { AbstractControl, ValidationErrors } from '@angular/forms';

function expirationAfterIssueDateValidator(group: AbstractControl): ValidationErrors | null {
  const issueDate = group.get('issueDate')?.value as string | null;
  const expirationDate = group.get('expirationDate')?.value as string | null;
  if (issueDate && expirationDate && expirationDate < issueDate) {
    return { expirationBeforeIssue: true };
  }
  return null;
}
