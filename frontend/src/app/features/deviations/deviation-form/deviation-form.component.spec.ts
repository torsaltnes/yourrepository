import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { provideRouter, Router } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { DeviationFormComponent } from './deviation-form.component';
import { DeviationStore } from '../data/deviation.store';
import { signal } from '@angular/core';
import { Deviation } from '../../../core/models/deviation.model';

const mockDeviation: Deviation = {
  id: 'test-id',
  title: 'Existing Title',
  description: 'Existing Desc',
  severity: 'High',
  status: 'InProgress',
  reportedBy: 'Alice',
  reportedAt: '2024-03-15T00:00:00Z',
  updatedAt: '2024-03-15T00:00:00Z'
};

function createStoreMock(selectedDev: Deviation | null = null) {
  return {
    loading: signal(false),
    saving: signal(false),
    error: signal<string | null>(null),
    selectedDeviation: signal<Deviation | null>(selectedDev),
    loadById: jasmine.createSpy('loadById'),
    clearSelection: jasmine.createSpy('clearSelection'),
    create: jasmine.createSpy('create').and.returnValue(Promise.resolve(mockDeviation)),
    update: jasmine.createSpy('update').and.returnValue(Promise.resolve(mockDeviation))
  };
}

describe('DeviationFormComponent – create mode', () => {
  let fixture: ComponentFixture<DeviationFormComponent>;
  let component: DeviationFormComponent;
  let storeMock: ReturnType<typeof createStoreMock>;
  let router: Router;

  beforeEach(async () => {
    storeMock = createStoreMock();
    await TestBed.configureTestingModule({
      imports: [DeviationFormComponent, ReactiveFormsModule],
      providers: [
        provideRouter([{ path: 'deviations', redirectTo: '' }]),
        { provide: DeviationStore, useValue: storeMock },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => null } } }
        }
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));

    fixture = TestBed.createComponent(DeviationFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be in create mode', () => {
    expect(component.isEditMode).toBeFalse();
  });

  it('should call clearSelection on init in create mode', () => {
    expect(storeMock.clearSelection).toHaveBeenCalled();
  });

  it('form should be invalid when empty title', () => {
    component.form.get('title')!.setValue('');
    expect(component.form.get('title')!.invalid).toBeTrue();
  });

  it('form should be valid with required fields filled', () => {
    component.form.setValue({
      title: 'Test Title',
      description: '',
      severity: 'Low',
      status: 'Open',
      reportedBy: 'Jane',
      reportedAt: '2024-01-01'
    });
    expect(component.form.valid).toBeTrue();
  });

  it('form should have reportedAt control (not occurredAt)', () => {
    expect(component.form.get('reportedAt')).toBeTruthy();
    expect(component.form.get('occurredAt')).toBeNull();
  });

  it('submit should mark form touched if invalid', async () => {
    component.form.get('title')!.setValue('');
    await component.submit();
    expect(component.form.get('title')!.touched).toBeTrue();
    expect(storeMock.create).not.toHaveBeenCalled();
  });

  it('submit should call store.create on valid form', async () => {
    component.form.setValue({
      title: 'New Title',
      description: 'Desc',
      severity: 'Medium',
      status: 'Open',
      reportedBy: 'User',
      reportedAt: '2024-01-15'
    });
    await component.submit();
    expect(storeMock.create).toHaveBeenCalled();
  });

  it('submit should navigate to /deviations after successful create', async () => {
    component.form.setValue({
      title: 'New Title',
      description: '',
      severity: 'Low',
      status: 'Open',
      reportedBy: 'User',
      reportedAt: '2024-01-15'
    });
    await component.submit();
    expect(router.navigate).toHaveBeenCalledWith(['/deviations']);
  });

  it('cancel() should navigate to /deviations', () => {
    component.cancel();
    expect(router.navigate).toHaveBeenCalledWith(['/deviations']);
  });

  it('should show required validation message for title', () => {
    component.form.get('title')!.markAsTouched();
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Title is required');
  });

  it('should show required validation message for reportedAt', () => {
    component.form.get('reportedAt')!.markAsTouched();
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Reported At is required');
  });

  it('should render store error banner when error is set', () => {
    storeMock.error.set('Backend validation failed');
    fixture.detectChanges();
    const alert = fixture.nativeElement.querySelector('[role="alert"]');
    expect(alert).toBeTruthy();
    expect(alert.textContent).toContain('Backend validation failed');
  });
});

describe('DeviationFormComponent – edit mode', () => {
  let fixture: ComponentFixture<DeviationFormComponent>;
  let component: DeviationFormComponent;
  let storeMock: ReturnType<typeof createStoreMock>;
  let router: Router;

  beforeEach(async () => {
    storeMock = createStoreMock(mockDeviation);
    await TestBed.configureTestingModule({
      imports: [DeviationFormComponent, ReactiveFormsModule],
      providers: [
        provideRouter([{ path: 'deviations', redirectTo: '' }]),
        { provide: DeviationStore, useValue: storeMock },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => 'test-id' } } }
        }
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));

    fixture = TestBed.createComponent(DeviationFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be in edit mode', () => {
    expect(component.isEditMode).toBeTrue();
  });

  it('should call loadById on init', () => {
    expect(storeMock.loadById).toHaveBeenCalledWith('test-id');
  });

  it('should prefill form with existing deviation via effect', () => {
    expect(component.form.get('title')!.value).toBe('Existing Title');
    expect(component.form.get('reportedBy')!.value).toBe('Alice');
    expect(component.form.get('reportedAt')!.value).toBe('2024-03-15');
  });

  it('submit should call store.update in edit mode', async () => {
    component.form.patchValue({ title: 'Updated Title' });
    await component.submit();
    expect(storeMock.update).toHaveBeenCalledWith('test-id', jasmine.any(Object));
  });

  it('submit should navigate to /deviations after successful update', async () => {
    await component.submit();
    expect(router.navigate).toHaveBeenCalledWith(['/deviations']);
  });
});
