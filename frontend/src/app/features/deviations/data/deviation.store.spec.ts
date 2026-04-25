import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { DeviationStore } from './deviation.store';
import { DeviationApiService } from '../../../core/services/deviation-api.service';
import { Deviation } from '../../../core/models/deviation.model';
import { environment } from '../../../../environments/environment';

const base = `${environment.apiBaseUrl}/deviations`;

const mockDeviation: Deviation = {
  id: 'abc-123',
  title: 'Test Deviation',
  description: 'A description',
  severity: 'High',
  status: 'Open',
  reportedBy: 'Alice',
  reportedAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-02T00:00:00Z'
};

describe('DeviationStore', () => {
  let store: DeviationStore;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        DeviationApiService,
        DeviationStore
      ]
    });
    store = TestBed.inject(DeviationStore);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  // ─── Initial state ─────────────────────────────────────────────────────────

  it('should initialise with empty state', () => {
    expect(store.deviations()).toEqual([]);
    expect(store.loading()).toBeFalse();
    expect(store.saving()).toBeFalse();
    expect(store.error()).toBeNull();
    expect(store.selectedDeviation()).toBeNull();
  });

  it('isEmpty should be true when no deviations and not loading', () => {
    expect(store.isEmpty()).toBeTrue();
  });

  it('hasItems should be false initially', () => {
    expect(store.hasItems()).toBeFalse();
  });

  // ─── loadAll ───────────────────────────────────────────────────────────────

  it('loadAll() sets loading=true then resolves with deviations', fakeAsync(() => {
    store.loadAll();
    expect(store.loading()).toBeTrue();

    const req = httpMock.expectOne(base);
    req.flush([mockDeviation]);
    tick();

    expect(store.loading()).toBeFalse();
    expect(store.deviations().length).toBe(1);
    expect(store.deviations()[0].id).toBe('abc-123');
    expect(store.hasItems()).toBeTrue();
    expect(store.isEmpty()).toBeFalse();
  }));

  it('loadAll() sets error on failure', fakeAsync(() => {
    store.loadAll();
    const req = httpMock.expectOne(base);
    req.flush({ title: 'Server Error', status: 500 }, { status: 500, statusText: 'Internal Server Error' });
    tick();

    expect(store.loading()).toBeFalse();
    expect(store.error()).toBeTruthy();
  }));

  // ─── loadById ──────────────────────────────────────────────────────────────

  it('loadById() sets selectedDeviation on success', fakeAsync(() => {
    store.loadById('abc-123');
    const req = httpMock.expectOne(`${base}/abc-123`);
    req.flush(mockDeviation);
    tick();

    expect(store.selectedDeviation()).toEqual(mockDeviation);
    expect(store.loading()).toBeFalse();
  }));

  // ─── create ────────────────────────────────────────────────────────────────

  it('create() prepends new deviation and resolves with it', fakeAsync(() => {
    let result: Deviation | null = null as Deviation | null;
    store.create({
      title: 'New', description: '', severity: 'Low', status: 'Open',
      reportedBy: 'Bob', reportedAt: '2024-01-01'
    }).then((r: Deviation | null) => { result = r; });

    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    req.flush(mockDeviation);
    tick();

    expect(result as Deviation | null).toEqual(mockDeviation);
    expect(store.deviations()[0]).toEqual(mockDeviation);
    expect(store.saving()).toBeFalse();
  }));

  it('create() sets error on failure and resolves with null', fakeAsync(() => {
    let result: Deviation | null = mockDeviation;
    store.create({
      title: '', description: '', severity: 'Low', status: 'Open',
      reportedBy: '', reportedAt: '2024-01-01'
    }).then(r => result = r);

    const req = httpMock.expectOne(base);
    req.flush(
      { title: 'Validation Failed', errors: { title: ['Title is required.'] } },
      { status: 400, statusText: 'Bad Request' }
    );
    tick();

    expect(result).toBeNull();
    expect(store.error()).toContain('Title is required.');
  }));

  // ─── update ────────────────────────────────────────────────────────────────

  it('update() replaces deviation in list and sets selectedDeviation', fakeAsync(() => {
    // Pre-populate list
    store.deviations.set([mockDeviation]);

    const updated = { ...mockDeviation, title: 'Updated' };
    store.update('abc-123', {
      title: 'Updated', description: '', severity: 'Low', status: 'Open',
      reportedBy: 'Alice', reportedAt: '2024-01-01'
    });

    const req = httpMock.expectOne(`${base}/abc-123`);
    expect(req.request.method).toBe('PUT');
    req.flush(updated);
    tick();

    expect(store.deviations()[0].title).toBe('Updated');
    expect(store.selectedDeviation()?.title).toBe('Updated');
  }));

  // ─── remove ────────────────────────────────────────────────────────────────

  it('remove() removes deviation from list', fakeAsync(() => {
    store.deviations.set([mockDeviation]);

    store.remove('abc-123');
    const req = httpMock.expectOne(`${base}/abc-123`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
    tick();

    expect(store.deviations()).toEqual([]);
    expect(store.saving()).toBeFalse();
  }));

  // ─── clearSelection & clearError ──────────────────────────────────────────

  it('clearSelection() resets selectedDeviation to null', () => {
    store.selectedDeviation.set(mockDeviation);
    store.clearSelection();
    expect(store.selectedDeviation()).toBeNull();
  });

  it('clearError() resets error to null', () => {
    store.error.set('some error');
    store.clearError();
    expect(store.error()).toBeNull();
  });

  // ─── ProblemDetails error extraction ──────────────────────────────────────

  it('loadAll() extracts detail from ProblemDetails on 404', fakeAsync(() => {
    store.loadAll();
    const req = httpMock.expectOne(base);
    req.flush(
      { title: 'Not Found', detail: 'Resource not found', status: 404 },
      { status: 404, statusText: 'Not Found' }
    );
    tick();

    expect(store.error()).toBe('Resource not found');
  }));
});
