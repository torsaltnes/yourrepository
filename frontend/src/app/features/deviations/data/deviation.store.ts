import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { DeviationApiService } from '../../../core/services/deviation-api.service';
import { Deviation } from '../../../core/models/deviation.model';
import { DeviationForm } from '../../../core/models/deviation-form.model';

function extractErrorMessage(err: unknown): string {
  if (err instanceof HttpErrorResponse) {
    const body = err.error;
    if (body && typeof body === 'object') {
      // ValidationProblemDetails: has .errors dict
      if (body['errors']) {
        const msgs = Object.values(body['errors'] as Record<string, string[]>)
          .flat()
          .join('; ');
        return msgs || body['title'] || 'Validation failed.';
      }
      // ProblemDetails: has .detail or .title
      if (body['detail']) return body['detail'];
      if (body['title']) return body['title'];
    }
    return err.message || 'An unexpected error occurred.';
  }
  if (err instanceof Error) return err.message;
  return 'An unexpected error occurred.';
}

@Injectable({ providedIn: 'root' })
export class DeviationStore {
  readonly #api = inject(DeviationApiService);

  // State signals
  readonly deviations = signal<Deviation[]>([]);
  readonly selectedDeviation = signal<Deviation | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  // Computed
  readonly hasItems = computed(() => this.deviations().length > 0);
  readonly isEmpty = computed(() => !this.loading() && this.deviations().length === 0);

  loadAll(): void {
    this.loading.set(true);
    this.error.set(null);
    this.#api.getAll().subscribe({
      next: (items) => {
        this.deviations.set(items);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(extractErrorMessage(err));
        this.loading.set(false);
      }
    });
  }

  loadById(id: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.#api.getById(id).subscribe({
      next: (item) => {
        this.selectedDeviation.set(item);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(extractErrorMessage(err));
        this.loading.set(false);
      }
    });
  }

  create(payload: DeviationForm): Promise<Deviation | null> {
    this.saving.set(true);
    this.error.set(null);
    return new Promise((resolve) => {
      this.#api.create(payload).subscribe({
        next: (created) => {
          this.deviations.update(list => [created, ...list]);
          this.saving.set(false);
          resolve(created);
        },
        error: (err) => {
          this.error.set(extractErrorMessage(err));
          this.saving.set(false);
          resolve(null);
        }
      });
    });
  }

  update(id: string, payload: DeviationForm): Promise<Deviation | null> {
    this.saving.set(true);
    this.error.set(null);
    return new Promise((resolve) => {
      this.#api.update(id, payload).subscribe({
        next: (updated) => {
          this.deviations.update(list => list.map(d => d.id === id ? updated : d));
          this.selectedDeviation.set(updated);
          this.saving.set(false);
          resolve(updated);
        },
        error: (err) => {
          this.error.set(extractErrorMessage(err));
          this.saving.set(false);
          resolve(null);
        }
      });
    });
  }

  remove(id: string): Promise<boolean> {
    this.saving.set(true);
    this.error.set(null);
    return new Promise((resolve) => {
      this.#api.delete(id).subscribe({
        next: () => {
          this.deviations.update(list => list.filter(d => d.id !== id));
          this.saving.set(false);
          resolve(true);
        },
        error: (err) => {
          this.error.set(extractErrorMessage(err));
          this.saving.set(false);
          resolve(false);
        }
      });
    });
  }

  clearSelection(): void {
    this.selectedDeviation.set(null);
  }

  clearError(): void {
    this.error.set(null);
  }
}
