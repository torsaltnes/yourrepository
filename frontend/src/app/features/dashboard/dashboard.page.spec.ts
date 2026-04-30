import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { DashboardPage } from './dashboard.page';
import { DashboardStore } from './data/dashboard.store';
import { DeviationSummaryDto } from '../../core/models/dashboard.model';

function makeSummaryDeviation(overrides: Partial<DeviationSummaryDto> = {}): DeviationSummaryDto {
  return {
    id: '00000000-0000-0000-0000-000000000001',
    title: 'Test Deviation',
    status: 'Registered',
    severity: 'Medium',
    category: 'Quality',
    reportedBy: 'Jane Doe',
    assignedTo: 'Bob',
    createdAt: '2026-04-01T10:00:00Z',
    updatedAt: '2026-04-01T10:00:00Z',
    dueDate: null,
    tags: [],
    attachmentCount: 0,
    commentCount: 0,
    ...overrides,
  };
}

class DashboardStoreMock {
  summaryResource = { reload: jasmine.createSpy('reload') };
  isLoading = signal(false);
  hasError = signal(false);
  totalDeviations = signal(10);
  openDeviations = signal(4);
  overdueDeviations = signal(2);
  inProgressDeviations = signal(3);
  closedDeviations = signal(4);
  searchQuery = signal('');
  statusFilter = signal('');
  currentPage = signal(1);
  pageSize = signal(10);
  activeFilterCount = signal(0);
  filteredDeviations = signal<DeviationSummaryDto[]>([]);
  paginatedDeviations = signal<DeviationSummaryDto[]>([]);
  totalCount = signal(0);
  totalPages = signal(1);
  hasNextPage = signal(false);
  hasPreviousPage = signal(false);
  paginationLabel = signal('No results');

  setSearch = jasmine.createSpy('setSearch');
  setStatusFilter = jasmine.createSpy('setStatusFilter');
  clearFilters = jasmine.createSpy('clearFilters');
  goToPage = jasmine.createSpy('goToPage');
  reload = jasmine.createSpy('reload');
}

describe('DashboardPage', () => {
  let fixture: ComponentFixture<DashboardPage>;
  let mockStore: DashboardStoreMock;

  beforeEach(async () => {
    mockStore = new DashboardStoreMock();

    await TestBed.configureTestingModule({
      imports: [DashboardPage],
      providers: [provideRouter([])],
    })
      .overrideComponent(DashboardPage, {
        set: {
          providers: [{ provide: DashboardStore, useValue: mockStore }],
        },
      })
      .compileComponents();

    fixture = TestBed.createComponent(DashboardPage);
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should display page heading "Avvik"', () => {
    const h1: HTMLElement = fixture.nativeElement.querySelector('h1');
    expect(h1.textContent?.trim()).toBe('Avvik');
  });

  it('should render a "Nytt avvik" link to /deviations/new', () => {
    const links: NodeListOf<HTMLAnchorElement> =
      fixture.nativeElement.querySelectorAll('a');
    const newLink = Array.from(links).find((a) =>
      a.textContent?.trim().includes('Nytt avvik'),
    );
    expect(newLink).toBeTruthy();
    expect(newLink!.getAttribute('href')).toContain('/deviations/new');
  });

  it('should render 4 stat cards', () => {
    const cards = fixture.nativeElement.querySelectorAll('app-dashboard-stat-card');
    expect(cards.length).toBe(4);
  });

  it('should show loading skeleton when isLoading is true', () => {
    mockStore.isLoading.set(true);
    fixture.detectChanges();
    const skeleton = fixture.nativeElement.querySelector('.animate-pulse');
    expect(skeleton).toBeTruthy();
  });

  it('should not show loading skeleton when isLoading is false', () => {
    mockStore.isLoading.set(false);
    fixture.detectChanges();
    const skeleton = fixture.nativeElement.querySelector('.animate-pulse');
    expect(skeleton).toBeFalsy();
  });

  it('should show error state when hasError is true and not loading', () => {
    mockStore.hasError.set(true);
    mockStore.isLoading.set(false);
    fixture.detectChanges();
    const errorEl = fixture.nativeElement.querySelector('[role="alert"]');
    expect(errorEl).toBeTruthy();
  });

  it('should show empty state when no deviations', () => {
    mockStore.paginatedDeviations.set([]);
    fixture.detectChanges();
    const allParagraphs: NodeListOf<HTMLElement> =
      fixture.nativeElement.querySelectorAll('p, td');
    const emptyMsg = Array.from(allParagraphs).find((el) =>
      el.textContent?.includes('Ingen avvik funnet'),
    );
    expect(emptyMsg).toBeTruthy();
  });

  it('should render table rows for deviations', () => {
    mockStore.paginatedDeviations.set([
      makeSummaryDeviation({ id: '00000000-0000-0000-0000-000000000001', title: 'Alpha Issue' }),
      makeSummaryDeviation({ id: '00000000-0000-0000-0000-000000000002', title: 'Beta Issue' }),
    ]);
    fixture.detectChanges();
    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
  });

  it('should display a short ID (# prefix) in the ID column', () => {
    mockStore.paginatedDeviations.set([
      makeSummaryDeviation({ id: 'abcdef12-0000-0000-0000-000000000001', title: 'ID Test' }),
    ]);
    fixture.detectChanges();
    const firstCell: HTMLElement = fixture.nativeElement.querySelector('tbody td');
    expect(firstCell.textContent?.trim()).toBe('#ABCDEF12');
  });

  it('should display the category label (not type) in the category column', () => {
    mockStore.paginatedDeviations.set([
      makeSummaryDeviation({ category: 'Safety', title: 'Safety Issue' }),
    ]);
    fixture.detectChanges();
    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    const cells: NodeListOf<HTMLElement> = rows[0].querySelectorAll('td');
    // category is the 3rd column (index 2)
    expect(cells[2].textContent?.trim()).toBe('Safety');
  });

  it('should call setSearch when search input fires', () => {
    const input: HTMLInputElement = fixture.nativeElement.querySelector(
      'input[type="search"]',
    );
    expect(input).toBeTruthy();
    input.value = 'safety';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(mockStore.setSearch).toHaveBeenCalledWith('safety');
  });

  it('should call setStatusFilter when status select changes', () => {
    const select: HTMLSelectElement = fixture.nativeElement.querySelector('select');
    expect(select).toBeTruthy();
    select.value = 'Closed';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(mockStore.setStatusFilter).toHaveBeenCalledWith('Closed');
  });

  it('status filter options include UnderAssessment and UnderInvestigation', () => {
    const select: HTMLSelectElement = fixture.nativeElement.querySelector('select');
    const options = Array.from(select.options).map((o) => o.value);
    expect(options).toContain('UnderAssessment');
    expect(options).toContain('UnderInvestigation');
    expect(options).not.toContain('Assessed');
    expect(options).not.toContain('Investigating');
  });

  it('should show "Nullstill" button when activeFilterCount > 0', () => {
    mockStore.activeFilterCount.set(1);
    fixture.detectChanges();
    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('button');
    const clearBtn = Array.from(buttons).find((b) =>
      b.textContent?.trim().startsWith('Nullstill'),
    );
    expect(clearBtn).toBeTruthy();
  });

  it('should call clearFilters when Nullstill button is clicked', () => {
    mockStore.activeFilterCount.set(1);
    fixture.detectChanges();
    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('button');
    const clearBtn = Array.from(buttons).find((b) =>
      b.textContent?.trim().startsWith('Nullstill'),
    );
    expect(clearBtn).toBeTruthy();
    clearBtn!.click();
    expect(mockStore.clearFilters).toHaveBeenCalled();
  });

  it('should call goToPage with page-1 when Previous button is clicked', () => {
    mockStore.hasPreviousPage.set(true);
    mockStore.currentPage.set(3);
    fixture.detectChanges();
    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('button');
    const prevBtn = Array.from(buttons).find((b) =>
      b.textContent?.includes('Forrige'),
    );
    expect(prevBtn).toBeTruthy();
    prevBtn!.click();
    expect(mockStore.goToPage).toHaveBeenCalledWith(2);
  });

  it('should call goToPage with page+1 when Next button is clicked', () => {
    mockStore.hasNextPage.set(true);
    mockStore.currentPage.set(1);
    mockStore.totalPages.set(3);
    fixture.detectChanges();
    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('button');
    const nextBtn = Array.from(buttons).find((b) =>
      b.textContent?.includes('Neste'),
    );
    expect(nextBtn).toBeTruthy();
    nextBtn!.click();
    expect(mockStore.goToPage).toHaveBeenCalledWith(2);
  });

  it('Previous button should be disabled when hasPreviousPage is false', () => {
    mockStore.hasPreviousPage.set(false);
    fixture.detectChanges();
    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('button');
    const prevBtn = Array.from(buttons).find((b) =>
      b.textContent?.includes('Forrige'),
    );
    expect(prevBtn?.disabled).toBeTrue();
  });

  it('should call reload when reload button is clicked', () => {
    const buttons: NodeListOf<HTMLButtonElement> =
      fixture.nativeElement.querySelectorAll('button');
    const reloadBtn = Array.from(buttons).find((b) =>
      b.getAttribute('aria-label') === 'Reload data',
    );
    expect(reloadBtn).toBeTruthy();
    reloadBtn!.click();
    expect(mockStore.reload).toHaveBeenCalled();
  });
});
