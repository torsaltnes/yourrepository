import { describe, it, expect, vi, beforeEach } from 'vitest';
import { scheduleService } from '../../src/services/ScheduleService';
import { vaktlisteStore } from '../../src/state/vaktlisteStore';
import { parseShiftDate } from '../../src/domain/types';

describe('ScheduleService', () => {
  beforeEach(() => {
    sessionStorage.clear();
    vaktlisteStore.reset();
  });

  it('getState() returns committed state from the store', () => {
    const state = scheduleService.getState();
    expect(state.assignments).toHaveLength(0);
  });

  it('createAssignment adds an assignment', () => {
    const date = parseShiftDate('2026-05-04');
    expect(date).not.toBeNull();

    scheduleService.createAssignment({
      employee: 'Ola Nordmann',
      date: date!,
      shift: 'morning',
    });

    expect(scheduleService.getState().assignments).toHaveLength(1);
    expect(scheduleService.getState().assignments[0]?.employee).toBe('Ola Nordmann');
  });

  it('subscribe() is notified when an assignment is added', () => {
    const listener = vi.fn();
    const unsub = scheduleService.subscribe(listener);

    const date = parseShiftDate('2026-05-05');
    scheduleService.createAssignment({ employee: 'Kari', date: date!, shift: 'afternoon' });

    expect(listener).toHaveBeenCalledTimes(1);
    unsub();
  });

  it('subscribe() unsubscribe stops notifications', () => {
    const listener = vi.fn();
    const unsub = scheduleService.subscribe(listener);
    unsub();

    const date = parseShiftDate('2026-05-05');
    scheduleService.createAssignment({ employee: 'Kari', date: date!, shift: 'afternoon' });
    expect(listener).not.toHaveBeenCalled();
  });

  it('getSubmitStatus returns conflict on duplicate', () => {
    const date = parseShiftDate('2026-05-04');
    scheduleService.createAssignment({ employee: 'Ola', date: date!, shift: 'morning' });
    scheduleService.createAssignment({ employee: 'Kari', date: date!, shift: 'morning' });

    const status = scheduleService.getSubmitStatus();
    expect(status.kind).toBe('conflict');
  });
});
