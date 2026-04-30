import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { DeviationListStore } from '../data/deviation-list.store';
import { SeverityBadgeComponent } from '../components/severity-badge.component';
import { StatusBadgeComponent } from '../components/status-badge.component';
import {
  DeviationDto,
  DEVIATION_STATUS_LABELS,
  DEVIATION_SEVERITY_LABELS,
  DEVIATION_TYPE_LABELS,
  DeviationSeverity,
  DeviationStatus,
  DeviationType,
} from '../../../core/models/deviation.model';

@Component({
  selector: 'app-deviation-list-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [DeviationListStore],
  imports: [
    RouterLink,
    SeverityBadgeComponent,
    StatusBadgeComponent,
  ],
  template: `
    <div class="flex flex-col gap-6 p-4 md:p-6">

      <!-- ── Page header ──────────────────────────────────────────── -->
      <div class="flex flex-wrap items-center justify-between gap-4">
        <div class="flex flex-col gap-1">
          <h1 class="text-heading font-semibold text-balance text-text-primary">
            Deviations
          </h1>
          <p class="text-caption text-text-secondary">
            Manage and track all deviations
          </p>
        </div>

        <div class="flex flex-wrap items-center gap-3">
          <!-- Export CSV -->
          <button
            type="button"
            class="rounded-lg border border-border bg-surface-raised px-4 py-2
                   text-body font-medium text-text-primary
                   transition-colors duration-150
                   hover:bg-surface-overlay
                   focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary
                   disabled:opacity-40 disabled:cursor-not-allowed"
            [disabled]="store.exportLoading()"
            (click)="store.export()"
          >
            @if (store.exportLoading()) {
              Exporting…
            } @else {
              Export CSV
            }
          </button>

          <!-- New Deviation link -->
          <a
            routerLink="/deviations/new"
            class="inline-flex items-center gap-1.5 rounded-lg bg-primary px-4 py-2
                   text-body font-medium text-white
                   transition-colors duration-150
                   hover:bg-primary-hover
                   focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
          >
            + New Deviation
          </a>
        </div>
      </div>

      <!-- ── Filter bar ────────────────────────────────────────────── -->
      <div class="flex flex-wrap items-center gap-3">
        <!-- Search -->
        <div class="relative flex-1 min-w-48">
          <span
            class="pointer-events-none absolute inset-y-0 left-3
                   flex items-center text-text-secondary"
            aria-hidden="true"
          >🔍</span>
          <input
            type="search"
            placeholder="Search deviations…"
            class="w-full rounded-lg border border-border bg-surface pl-9 pr-4 py-2
                   text-body text-text-primary placeholder:text-text-secondary
                   transition-colors duration-150
                   focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
            [value]="store.searchQuery()"
            (input)="onSearchInput($event)"
            aria-label="Search deviations"
          />
        </div>

        <!-- Status filter -->
        <select
          class="rounded-lg border border-border bg-surface px-3 py-2
                 text-body text-text-primary
                 transition-colors duration-150
                 focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
          [value]="store.statusFilter()"
          (change)="onStatusChange($event)"
          aria-label="Filter by status"
        >
          <option value="">All statuses</option>
          @for (entry of statusOptions; track entry.value) {
            <option [value]="entry.value">{{ entry.label }}</option>
          }
        </select>

        <!-- Severity filter -->
        <select
          class="rounded-lg border border-border bg-surface px-3 py-2
                 text-body text-text-primary
                 transition-colors duration-150
                 focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
          [value]="store.severityFilter()"
          (change)="onSeverityChange($event)"
          aria-label="Filter by severity"
        >
          <option value="">All severities</option>
          @for (entry of severityOptions; track entry.value) {
            <option [value]="entry.value">{{ entry.label }}</option>
          }
        </select>

        <!-- Type filter -->
        <select
          class="rounded-lg border border-border bg-surface px-3 py-2
                 text-body text-text-primary
                 transition-colors duration-150
                 focus:outline-none focus:ring-2 focus:ring-primary focus:border-primary"
          [value]="store.typeFilter()"
          (change)="onTypeChange($event)"
          aria-label="Filter by type"
        >
          <option value="">All types</option>
          @for (entry of typeOptions; track entry.value) {
            <option [value]="entry.value">{{ entry.label }}</option>
          }
        </select>

        <!-- Clear filters -->
        @if (store.activeFilterCount() > 0) {
          <button
            type="button"
            class="rounded-lg border border-border px-3 py-2 text-body
                   text-text-secondary transition-colors duration-150
                   hover:bg-surface-raised hover:text-text-primary
                   focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
            (click)="store.clearFilters()"
          >
            Clear ({{ store.activeFilterCount() }})
          </button>
        }

        <!-- Reload -->
        <button
          type="button"
          class="rounded-lg border border-border px-3 py-2 text-body
                 text-text-secondary transition-colors duration-150
                 hover:bg-surface-raised hover:text-text-primary
                 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary
                 disabled:opacity-40 disabled:cursor-not-allowed"
          [disabled]="store.loading()"
          (click)="store.reload()"
          aria-label="Reload data"
          title="Reload"
        >↺</button>
      </div>

      <!-- ── Loading skeleton ───────────────────────────────────────── -->
      @if (store.loading()) {
        <div
          class="animate-pulse space-y-3 rounded-xl border border-border bg-surface p-4"
          role="status"
          aria-label="Loading"
        >
          <div class="h-4 w-1/3 rounded bg-surface-raised"></div>
          <div class="h-4 w-2/3 rounded bg-surface-raised"></div>
          <div class="h-4 w-1/2 rounded bg-surface-raised"></div>
          <div class="h-4 w-3/4 rounded bg-surface-raised"></div>
          <div class="h-4 w-2/5 rounded bg-surface-raised"></div>
        </div>
      }

      <!-- ── Error state ────────────────────────────────────────────── -->
      @if (store.error() && !store.loading()) {
        <div
          class="flex items-center gap-3 rounded-xl border border-danger/30
                 bg-danger/5 p-4 text-danger"
          role="alert"
        >
          <span aria-hidden="true">⚠</span>
          <div class="flex flex-1 flex-col gap-1">
            <p class="text-body font-medium">Failed to load deviations</p>
            <p class="text-caption text-text-secondary">
              Check your network connection and try again.
            </p>
          </div>
          <button
            type="button"
            class="rounded-lg border border-danger/30 px-3 py-1.5 text-caption font-medium
                   text-danger transition-colors duration-150
                   hover:bg-danger/10
                   focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-danger"
            (click)="store.reload()"
          >
            Retry
          </button>
        </div>
      }

      <!-- ── Deviations table ───────────────────────────────────────── -->
      @if (!store.loading()) {
        <div class="overflow-hidden rounded-xl border border-border bg-surface">

          <!-- Table toolbar -->
          <div class="flex items-center justify-between border-b border-border px-4 py-3">
            <h2 class="text-body font-semibold text-text-primary">Deviations</h2>
            <span class="text-caption text-text-secondary">
              {{ store.paginationLabel() }}
            </span>
          </div>

          <!-- Scrollable table -->
          <div class="overflow-x-auto">
            <table class="w-full text-body" role="table">
              <thead>
                <tr class="border-b border-border">
                  <th scope="col"
                      class="px-4 py-3 text-left text-caption font-medium
                             uppercase tracking-wider text-text-secondary">
                    <button
                      type="button"
                      class="flex items-center gap-1 transition-colors duration-150
                             hover:text-text-primary focus-visible:outline-none"
                      (click)="store.sort('referenceNumber')"
                    >
                      ID
                      <span class="text-[10px]">{{ sortIndicator('referenceNumber') }}</span>
                    </button>
                  </th>
                  <th scope="col"
                      class="px-4 py-3 text-left text-caption font-medium
                             uppercase tracking-wider text-text-secondary">
                    <button
                      type="button"
                      class="flex items-center gap-1 transition-colors duration-150
                             hover:text-text-primary focus-visible:outline-none"
                      (click)="store.sort('title')"
                    >
                      Title
                      <span class="text-[10px]">{{ sortIndicator('title') }}</span>
                    </button>
                  </th>
                  <th scope="col"
                      class="px-4 py-3 text-left text-caption font-medium
                             uppercase tracking-wider text-text-secondary">Type</th>
                  <th scope="col"
                      class="px-4 py-3 text-left text-caption font-medium
                             uppercase tracking-wider text-text-secondary">
                    <button
                      type="button"
                      class="flex items-center gap-1 transition-colors duration-150
                             hover:text-text-primary focus-visible:outline-none"
                      (click)="store.sort('severity')"
                    >
                      Severity
                      <span class="text-[10px]">{{ sortIndicator('severity') }}</span>
                    </button>
                  </th>
                  <th scope="col"
                      class="px-4 py-3 text-left text-caption font-medium
                             uppercase tracking-wider text-text-secondary">Status</th>
                  <th scope="col"
                      class="px-4 py-3 text-left text-caption font-medium
                             uppercase tracking-wider text-text-secondary">Assigned To</th>
                  <th scope="col"
                      class="px-4 py-3 text-left text-caption font-medium
                             uppercase tracking-wider text-text-secondary">
                    <button
                      type="button"
                      class="flex items-center gap-1 transition-colors duration-150
                             hover:text-text-primary focus-visible:outline-none"
                      (click)="store.sort('createdAt')"
                    >
                      Date
                      <span class="text-[10px]">{{ sortIndicator('createdAt') }}</span>
                    </button>
                  </th>
                  <th scope="col"
                      class="px-4 py-3 text-left text-caption font-medium
                             uppercase tracking-wider text-text-secondary">Actions</th>
                </tr>
              </thead>
              <tbody>
                @for (row of store.items(); track row.id) {
                  <tr
                    class="border-b border-border transition-colors duration-150
                           hover:bg-surface-raised last:border-0"
                  >
                    <td class="px-4 py-3 font-mono text-caption text-text-secondary">
                      {{ row.referenceNumber }}
                    </td>
                    <td class="max-w-xs truncate px-4 py-3 font-medium text-text-primary">
                      {{ row.title }}
                    </td>
                    <td class="px-4 py-3 text-text-secondary">
                      {{ typeLabel(row.type) }}
                    </td>
                    <td class="px-4 py-3">
                      <app-severity-badge [severity]="row.severity" />
                    </td>
                    <td class="px-4 py-3">
                      <app-status-badge [status]="row.status" />
                    </td>
                    <td class="px-4 py-3 text-text-secondary">
                      {{ row.assignedTo ?? '–' }}
                    </td>
                    <td class="whitespace-nowrap px-4 py-3 text-caption text-text-secondary">
                      {{ formatDate(row.createdAt) }}
                    </td>
                    <td class="px-4 py-3">
                      <a
                        [routerLink]="['/deviations', row.id]"
                        class="rounded px-2.5 py-1 text-caption font-medium text-primary
                               transition-colors duration-150
                               hover:bg-primary/10
                               focus-visible:outline-none focus-visible:ring-2
                               focus-visible:ring-primary"
                      >View</a>
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="8" class="px-4 py-12 text-center">
                      <p class="text-body text-text-secondary">No deviations found</p>
                      @if (store.activeFilterCount() > 0) {
                        <button
                          type="button"
                          class="mt-2 text-caption text-primary underline
                                 transition-colors duration-150 hover:text-primary-hover
                                 focus-visible:outline-none focus-visible:ring-2
                                 focus-visible:ring-primary"
                          (click)="store.clearFilters()"
                        >Clear filters</button>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <!-- Pagination -->
          <div class="flex items-center justify-between border-t border-border px-4 py-3">
            <span class="text-caption text-text-secondary">
              Page {{ store.currentPage() }} of {{ store.totalPages() }}
            </span>
            <div class="flex items-center gap-2">
              <button
                type="button"
                class="rounded-md border border-border px-3 py-1 text-caption
                       text-text-secondary transition-colors duration-150
                       hover:bg-surface-raised hover:text-text-primary
                       disabled:cursor-not-allowed disabled:opacity-40
                       focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
                [disabled]="!store.hasPreviousPage()"
                (click)="store.goToPage(store.currentPage() - 1)"
              >← Prev</button>

              @for (page of visiblePages(); track page) {
                @if (page > 0) {
                  <button
                    type="button"
                    class="rounded-md px-3 py-1 text-caption font-medium
                           transition-colors duration-150
                           focus-visible:outline-none focus-visible:ring-2
                           focus-visible:ring-primary"
                    [class]="page === store.currentPage()
                      ? 'bg-primary text-white'
                      : 'border border-border text-text-secondary hover:bg-surface-raised hover:text-text-primary'"
                    (click)="store.goToPage(page)"
                  >{{ page }}</button>
                } @else {
                  <span class="px-1 text-caption text-text-secondary">…</span>
                }
              }

              <button
                type="button"
                class="rounded-md border border-border px-3 py-1 text-caption
                       text-text-secondary transition-colors duration-150
                       hover:bg-surface-raised hover:text-text-primary
                       disabled:cursor-not-allowed disabled:opacity-40
                       focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary"
                [disabled]="!store.hasNextPage()"
                (click)="store.goToPage(store.currentPage() + 1)"
              >Next →</button>
            </div>
          </div>

        </div>
      }

    </div>
  `,
})
export class DeviationListPage {
  protected readonly store = inject(DeviationListStore);

  protected readonly statusOptions: { value: DeviationStatus; label: string }[] = [
    { value: 'Registered',         label: DEVIATION_STATUS_LABELS['Registered'] },
    { value: 'UnderAssessment',    label: DEVIATION_STATUS_LABELS['UnderAssessment'] },
    { value: 'UnderInvestigation', label: DEVIATION_STATUS_LABELS['UnderInvestigation'] },
    { value: 'CorrectiveAction',   label: DEVIATION_STATUS_LABELS['CorrectiveAction'] },
    { value: 'Closed',             label: DEVIATION_STATUS_LABELS['Closed'] },
  ];

  protected readonly severityOptions: { value: DeviationSeverity; label: string }[] = [
    { value: 'Critical', label: DEVIATION_SEVERITY_LABELS['Critical'] },
    { value: 'High',     label: DEVIATION_SEVERITY_LABELS['High'] },
    { value: 'Medium',   label: DEVIATION_SEVERITY_LABELS['Medium'] },
    { value: 'Low',      label: DEVIATION_SEVERITY_LABELS['Low'] },
  ];

  protected readonly typeOptions: { value: DeviationType; label: string }[] = [
    { value: 'Deviation',       label: DEVIATION_TYPE_LABELS['Deviation'] },
    { value: 'NonConformance',  label: DEVIATION_TYPE_LABELS['NonConformance'] },
    { value: 'Incident',        label: DEVIATION_TYPE_LABELS['Incident'] },
    { value: 'NearMiss',        label: DEVIATION_TYPE_LABELS['NearMiss'] },
  ];

  protected readonly visiblePages = computed(() => {
    const total = this.store.totalPages();
    const current = this.store.currentPage();
    if (total <= 7) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }
    const pages: number[] = [1];
    const start = Math.max(2, current - 1);
    const end = Math.min(total - 1, current + 1);
    if (start > 2) pages.push(-1);
    for (let i = start; i <= end; i++) pages.push(i);
    if (end < total - 1) pages.push(-2);
    pages.push(total);
    return pages;
  });

  protected onSearchInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.store.setSearch(input.value);
  }

  protected onStatusChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.store.setStatusFilter(select.value as DeviationStatus | '');
  }

  protected onSeverityChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.store.setSeverityFilter(select.value as DeviationSeverity | '');
  }

  protected onTypeChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.store.setTypeFilter(select.value as DeviationType | '');
  }

  protected typeLabel(type: string): string {
    return DEVIATION_TYPE_LABELS[type as keyof typeof DEVIATION_TYPE_LABELS] ?? type;
  }

  protected sortIndicator(column: string): string {
    if (this.store.sortBy() !== column) return '';
    return this.store.sortDir() === 'asc' ? '▲' : '▼';
  }

  protected formatDate(isoDate: string): string {
    try {
      return new Date(isoDate).toLocaleDateString(undefined, {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
      });
    } catch {
      return isoDate;
    }
  }
}
