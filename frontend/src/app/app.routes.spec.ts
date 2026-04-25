import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { routes } from './app.routes';
import { Component } from '@angular/core';
import { DeviationStore } from './features/deviations/data/deviation.store';
import { signal } from '@angular/core';

// Minimal stubs so lazy-loaded components can be created without full deps
function createStoreMock() {
  return {
    deviations: signal([]),
    loading: signal(false),
    saving: signal(false),
    error: signal<string | null>(null),
    selectedDeviation: signal(null),
    hasItems: signal(false),
    isEmpty: signal(true),
    loadAll: jasmine.createSpy('loadAll'),
    loadById: jasmine.createSpy('loadById'),
    clearSelection: jasmine.createSpy('clearSelection'),
    create: jasmine.createSpy('create').and.returnValue(Promise.resolve(null)),
    update: jasmine.createSpy('update').and.returnValue(Promise.resolve(null)),
    remove: jasmine.createSpy('remove').and.returnValue(Promise.resolve(false)),
    clearError: jasmine.createSpy('clearError')
  };
}

describe('app.routes', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: DeviationStore, useValue: createStoreMock() }
      ]
    }).compileComponents();
  });

  it('should redirect "" to /deviations', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/');
    const router = TestBed.inject(Router);
    expect(router.url).toBe('/deviations');
  });

  it('should resolve /deviations to DeviationListComponent', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/deviations');
    expect(harness.routeNativeElement).toBeTruthy();
  });

  it('should resolve /deviations/new to DeviationFormComponent', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/deviations/new');
    expect(harness.routeNativeElement).toBeTruthy();
  });

  it('should resolve /deviations/:id/edit to DeviationFormComponent', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/deviations/some-uuid/edit');
    expect(harness.routeNativeElement).toBeTruthy();
  });

  it('should redirect unknown paths to /deviations', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/unknown-route');
    const router = TestBed.inject(Router);
    expect(router.url).toBe('/deviations');
  });
});
