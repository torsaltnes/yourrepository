import { fakeAsync, flushMicrotasks, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { DashboardStore } from './dashboard.store';
import { DashboardApiService } from '../../../core/services/dashboard-api.service';
import { DashboardSummaryDto, DeviationSummaryDto } from '../../../core/models/dashboard.model';
import { of } from 'rxjs';

function makeSummaryDeviation(overrides: Partial<DeviationSummaryDto> = {}): DeviationSummaryDto {
  return {
    id: '00000000-0000-0000-0000-000000000001',
    title: 'Test Deviation',
    status: 'Registered',
    severity: 'Medium',
    category: 'Quality',
    reportedBy: 'Jane Doe',
    assignedTo: null,
    createdAt: '2026-04-01T10:00:00Z',
    updatedAt: '2026-04-01T10:00:00Z',
    dueDate: null,
    tags: [],
    attachmentCount: 0,
    commentCount: 0,
    ...overrides,
  };
}

function makeSummary(overrides: Partial<DashboardSummaryDto> = {}): DashboardSummaryDto {
  return {
    totalDeviations: 10,
    openDeviations: 4,
    overdueDeviations: 2,
    byStatus: {
      Registered: 2,
      UnderAssessment: 1,
      UnderInvestigation: 1,
      CorrectiveAction: 2,
      Closed: 4,
    },
    bySeverity: { Critical: 1, High: 2, Medium: 4, Low: 3 },
    byCategory: { Safety: 5, Quality: 5 },
    monthlyTrend: [{ month: '2026-04', count: 10 }],
    recentDeviations: [
      makeSummaryDeviation({
        id: '00000000-0000-0000-0000-000000000001',
        title: 'First',
        status: 'Registered',
      }),
      makeSummaryDeviation({
        id: '00000000-0000-0000-0000-000000000002',
        title: 'Second',
        status: 'Closed',
      }),
      makeSummaryDeviation({
        id: '00000000-0000-0000-0000-000000000003',
        title: 'Third',
        status: 'UnderInvestigation',
      }),
    ],
    ...overrides,
  };
}

describe('DashboardStore', () => {
  let store: DashboardStore;
  let apiSpy: jasmine.SpyObj<DashboardApiService>;

  beforeEach(fakeAsync(() => {
    apiSpy = jasmine.createSpyObj('DashboardApiService', ['getSummary']);
    apiSpy.getSummary.and.returnValue(of(makeSummary()));

    TestBed.configureTestingModule({
      providers: [
        DashboardStore,
        { provide: DashboardApiService, useValue: apiSpy },
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    store = TestBed.runInInjectionContext(() => TestBed.inject(DashboardStore));
    // Flush the Promise created by lastValueFrom so resource data is available
    flushMicrotasks();
    TestBed.flushEffects();
  }));

  it('should create with default filter signal values', () => {
    expect(store).toBeTruthy();
    expect(store.searchQuery()).toBe('');
    expect(store.statusFilter()).toBe('');
    expect(store.currentPage()).toBe(1);
    expect(store.pageSize()).toBe(10);
  });

  it('should expose summary stats from API', () => {
    expect(store.totalDeviations()).toBe(10);
    expect(store.openDeviations()).toBe(4);
    expect(store.overdueDeviations()).toBe(2);
    expect(store.closedDeviations()).toBe(4);
  });

  it('inProgressDeviations sums UnderInvestigation + CorrectiveAction', () => {
    // byStatus has UnderInvestigation: 1, CorrectiveAction: 2 → total 3
    expect(store.inProgressDeviations()).toBe(3);
  });

  it('filteredDeviations() returns all when no filters active', () => {
    expect(store.filteredDeviations().length).toBe(3);
  });

  it('setSearch() filters deviations by title', () => {
    store.setSearch('First');
    expect(store.filteredDeviations().length).toBe(1);
    expect(store.filteredDeviations()[0].title).toBe('First');
  });

  it('setSearch() filters case-insensitively', () => {
    store.setSearch('second');
    expect(store.filteredDeviations().length).toBe(1);
  });

  it('setSearch() filters by id prefix', () => {
    store.setSearch('00000000-0000-0000-0000-000000000003');
    expect(store.filteredDeviations().length).toBe(1);
    expect(store.filteredDeviations()[0].title).toBe('Third');
  });

  it('setStatusFilter() filters by status', () => {
    store.setStatusFilter('Closed');
    expect(store.filteredDeviations().length).toBe(1);
    expect(store.filteredDeviations()[0].status).toBe('Closed');
  });

  it('setStatusFilter() filters by UnderInvestigation', () => {
    store.setStatusFilter('UnderInvestigation');
    expect(store.filteredDeviations().length).toBe(1);
    expect(store.filteredDeviations()[0].status).toBe('UnderInvestigation');
  });

  it('setStatusFilter() with empty string shows all', () => {
    store.setStatusFilter('Closed');
    store.setStatusFilter('');
    expect(store.filteredDeviations().length).toBe(3);
  });

  it('clearFilters() resets all filters and page', () => {
    store.setSearch('test');
    store.setStatusFilter('Registered');
    store['currentPage'].set(3);

    store.clearFilters();

    expect(store.searchQuery()).toBe('');
    expect(store.statusFilter()).toBe('');
    expect(store.currentPage()).toBe(1);
  });

  it('setSearch() resets currentPage to 1', () => {
    store['currentPage'].set(3);
    store.setSearch('anything');
    expect(store.currentPage()).toBe(1);
  });

  it('setStatusFilter() resets currentPage to 1', () => {
    store['currentPage'].set(2);
    store.setStatusFilter('Closed');
    expect(store.currentPage()).toBe(1);
  });

  it('goToPage() changes current page within valid range', () => {
    // totalPages is 1 (3 items, pageSize 10) → page 1 is valid
    store.goToPage(1);
    expect(store.currentPage()).toBe(1);
  });

  it('goToPage() does nothing for invalid page 0', () => {
    store.goToPage(0);
    expect(store.currentPage()).toBe(1);
  });

  it('goToPage() does nothing for page beyond totalPages', () => {
    store.goToPage(999);
    expect(store.currentPage()).toBe(1);
  });

  it('paginationLabel() returns "No results" when no items match', () => {
    store.setSearch('xxxxxxxxxx-not-found');
    expect(store.paginationLabel()).toBe('No results');
  });

  it('paginationLabel() formats range correctly', () => {
    // 3 items, page 1, size 10 → "1–3 of 3"
    expect(store.paginationLabel()).toBe('1–3 of 3');
  });

  it('activeFilterCount() counts active filters', () => {
    expect(store.activeFilterCount()).toBe(0);
    store.setSearch('test');
    expect(store.activeFilterCount()).toBe(1);
    store.setStatusFilter('Closed');
    expect(store.activeFilterCount()).toBe(2);
    store.clearFilters();
    expect(store.activeFilterCount()).toBe(0);
  });

  it('hasPreviousPage() is false on page 1', () => {
    expect(store.hasPreviousPage()).toBeFalse();
  });

  it('hasNextPage() is false when only one page', () => {
    // 3 items, pageSize 10 → 1 page
    expect(store.hasNextPage()).toBeFalse();
  });

  it('paginatedDeviations() returns correct slice', () => {
    store['pageSize'].set(2);
    expect(store.paginatedDeviations().length).toBe(2);

    store.goToPage(2);
    expect(store.paginatedDeviations().length).toBe(1);
  });

  it('isLoading() is false after data resolves', () => {
    expect(store.isLoading()).toBeFalse();
  });

  it('hasError() is false when API succeeds', () => {
    expect(store.hasError()).toBeFalse();
  });
});
