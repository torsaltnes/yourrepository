import { Injectable, computed, inject, resource, signal } from '@angular/core';
import { lastValueFrom } from 'rxjs';
import { DashboardApiService } from '../../../core/services/dashboard-api.service';
import { DashboardSummaryDto, DeviationSummaryDto } from '../../../core/models/dashboard.model';
import { DeviationStatus } from '../../../core/models/deviation.model';

@Injectable()
export class DashboardStore {
  private readonly api = inject(DashboardApiService);

  // ── API resource (auto-fetches on init) ─────────────────────
  readonly summaryResource = resource<DashboardSummaryDto, void>({
    loader: () => lastValueFrom(this.api.getSummary()),
  });

  // ── Convenience aliases ──────────────────────────────────────
  readonly isLoading = computed(() => this.summaryResource.isLoading());
  readonly hasError = computed(() => !!this.summaryResource.error());

  private readonly summary = computed(() => this.summaryResource.value());

  // ── Summary stat cards ───────────────────────────────────────
  readonly totalDeviations = computed(() => this.summary()?.totalDeviations ?? 0);
  readonly openDeviations = computed(() => this.summary()?.openDeviations ?? 0);
  readonly overdueDeviations = computed(() => this.summary()?.overdueDeviations ?? 0);
  readonly inProgressDeviations = computed(() => {
    const s = this.summary()?.byStatus ?? {};
    return (s['UnderInvestigation'] ?? 0) + (s['CorrectiveAction'] ?? 0);
  });
  readonly closedDeviations = computed(() => this.summary()?.byStatus['Closed'] ?? 0);

  // ── Client-side filter state ─────────────────────────────────
  readonly searchQuery = signal('');
  readonly statusFilter = signal<DeviationStatus | ''>('');

  // ── Pagination state ─────────────────────────────────────────
  readonly currentPage = signal(1);
  readonly pageSize = signal(10);

  // ── All recent deviations from API ───────────────────────────
  private readonly allDeviations = computed<DeviationSummaryDto[]>(
    () => this.summary()?.recentDeviations ?? [],
  );

  // ── Filtered deviations ──────────────────────────────────────
  readonly filteredDeviations = computed(() => {
    const q = this.searchQuery().toLowerCase().trim();
    const status = this.statusFilter();
    return this.allDeviations().filter((d) => {
      const matchesSearch =
        !q ||
        d.title.toLowerCase().includes(q) ||
        d.id.toLowerCase().includes(q) ||
        (d.assignedTo?.toLowerCase().includes(q) ?? false) ||
        d.reportedBy.toLowerCase().includes(q) ||
        d.category.toLowerCase().includes(q);
      const matchesStatus = !status || d.status === status;
      return matchesSearch && matchesStatus;
    });
  });

  readonly totalCount = computed(() => this.filteredDeviations().length);
  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize())),
  );
  readonly hasNextPage = computed(() => this.currentPage() < this.totalPages());
  readonly hasPreviousPage = computed(() => this.currentPage() > 1);

  readonly paginatedDeviations = computed(() => {
    const page = this.currentPage();
    const size = this.pageSize();
    return this.filteredDeviations().slice((page - 1) * size, page * size);
  });

  readonly paginationLabel = computed(() => {
    const total = this.totalCount();
    if (total === 0) return 'No results';
    const from = (this.currentPage() - 1) * this.pageSize() + 1;
    const to = Math.min(this.currentPage() * this.pageSize(), total);
    return `${from}–${to} of ${total}`;
  });

  readonly activeFilterCount = computed(() => {
    let count = 0;
    if (this.searchQuery()) count++;
    if (this.statusFilter()) count++;
    return count;
  });

  // ── Actions ──────────────────────────────────────────────────
  setSearch(query: string): void {
    this.searchQuery.set(query);
    this.currentPage.set(1);
  }

  setStatusFilter(status: DeviationStatus | ''): void {
    this.statusFilter.set(status);
    this.currentPage.set(1);
  }

  clearFilters(): void {
    this.searchQuery.set('');
    this.statusFilter.set('');
    this.currentPage.set(1);
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
    }
  }

  reload(): void {
    this.summaryResource.reload();
  }
}
