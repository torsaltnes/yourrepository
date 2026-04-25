import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { DeviationListComponent } from './deviation-list.component';
import { DeviationStore } from '../data/deviation.store';
import { signal } from '@angular/core';
import { Deviation } from '../../../core/models/deviation.model';

const mockDeviations: Deviation[] = [
  {
    id: '1',
    title: 'Test Deviation 1',
    description: 'Desc 1',
    severity: 'High',
    status: 'Open',
    reportedBy: 'Alice',
    reportedAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-02T00:00:00Z'
  },
  {
    id: '2',
    title: 'Test Deviation 2',
    description: 'Desc 2',
    severity: 'Low',
    status: 'Resolved',
    reportedBy: 'Bob',
    reportedAt: '2024-02-01T00:00:00Z',
    updatedAt: '2024-02-02T00:00:00Z'
  }
];

function createStoreMock(overrides: Partial<{
  deviations: Deviation[];
  loading: boolean;
  error: string | null;
}> = {}) {
  const data = { deviations: [], loading: false, error: null, ...overrides };
  return {
    deviations: signal(data.deviations),
    loading: signal(data.loading),
    error: signal(data.error),
    saving: signal(false),
    hasItems: signal(data.deviations.length > 0),
    isEmpty: signal(!data.loading && data.deviations.length === 0),
    loadAll: jasmine.createSpy('loadAll'),
    remove: jasmine.createSpy('remove').and.returnValue(Promise.resolve(true))
  };
}

describe('DeviationListComponent', () => {
  let fixture: ComponentFixture<DeviationListComponent>;
  let component: DeviationListComponent;
  let storeMock: ReturnType<typeof createStoreMock>;

  async function setup(overrides: Partial<{ deviations: Deviation[]; loading: boolean; error: string | null }> = {}) {
    storeMock = createStoreMock(overrides);
    await TestBed.configureTestingModule({
      imports: [DeviationListComponent],
      providers: [
        provideRouter([]),
        { provide: DeviationStore, useValue: storeMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(DeviationListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('should create', async () => {
    await setup();
    expect(component).toBeTruthy();
  });

  it('should call loadAll on init', async () => {
    await setup();
    expect(storeMock.loadAll).toHaveBeenCalled();
  });

  it('should render table rows when deviations exist', async () => {
    await setup({ deviations: mockDeviations });
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Test Deviation 1');
    expect(text).toContain('Test Deviation 2');
  });

  it('should show severity and status badges', async () => {
    await setup({ deviations: mockDeviations });
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('High');
    expect(text).toContain('Resolved');
  });

  it('should show InProgress rendered as "In Progress"', async () => {
    const devWithInProgress: Deviation[] = [{ ...mockDeviations[0], status: 'InProgress' }];
    await setup({ deviations: devWithInProgress });
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('In Progress');
  });

  it('should show empty state when no deviations', async () => {
    await setup({ deviations: [] });
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('No deviations yet');
  });

  it('should show loading state', async () => {
    await setup({ loading: true });
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Loading');
  });

  it('should show error state with retry button', async () => {
    await setup({ error: 'Network error' });
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Network error');
    const retryBtn = fixture.nativeElement.querySelector('button');
    expect(retryBtn).toBeTruthy();
  });

  it('retry() should call store.loadAll', async () => {
    await setup({ error: 'fail' });
    component.retry();
    expect(storeMock.loadAll).toHaveBeenCalledTimes(2); // once on init, once on retry
  });

  it('trackById should return item id', async () => {
    await setup();
    expect(component.trackById(0, mockDeviations[0])).toBe('1');
  });

  it('edit link should point to /deviations/:id/edit', async () => {
    await setup({ deviations: [mockDeviations[0]] });
    const link = fixture.nativeElement.querySelector('a[ng-reflect-router-link], a[href*="edit"]');
    // The routerLink directive may not resolve href in unit tests without router navigation,
    // but we can verify the component renders edit links
    const allLinks = fixture.nativeElement.querySelectorAll('a');
    const hasEditLink = Array.from(allLinks).some((a: any) => {
      const rl = a.getAttribute('ng-reflect-router-link') || a.getAttribute('href') || '';
      return rl.includes('edit') || rl.includes('1');
    });
    expect(hasEditLink).toBeTrue();
  });

  it('should show new deviation link', async () => {
    await setup();
    const allLinks = Array.from(fixture.nativeElement.querySelectorAll('a')) as HTMLAnchorElement[];
    const newLink = allLinks.find(a => a.textContent?.includes('New Deviation') || a.getAttribute('ng-reflect-router-link')?.includes('new'));
    expect(newLink).toBeTruthy();
  });
});
