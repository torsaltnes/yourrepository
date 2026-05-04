import { describe, it, expect } from 'vitest';
import { parseShiftDate } from '../../src/domain/types';
import { scheduleAssignment } from '../../src/domain/scheduler';
import type { SchedulerState } from '../../src/domain/types';

const EMPTY_STATE: SchedulerState = { assignments: [] };

describe('scheduleAssignment (pure domain function)', () => {
  it('accepts a valid assignment into an empty slot', () => {
    const date = parseShiftDate('2026-05-04');
    expect(date).not.toBeNull();

    const result = scheduleAssignment(EMPTY_STATE, {
      employee: 'Ola Nordmann',
      date: date!,
      shift: 'morning',
    });

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.state.assignments).toHaveLength(1);
      const first = result.state.assignments[0];
      expect(first?.employee).toBe('Ola Nordmann');
      expect(first?.shift).toBe('morning');
      expect(first?.date).toBe('2026-05-04');
      expect(typeof first?.id).toBe('string');
    }
  });

  it('rejects duplicate occupancy for same date + shift and returns structured conflict', () => {
    const date = parseShiftDate('2026-05-04');
    expect(date).not.toBeNull();

    const r1 = scheduleAssignment(EMPTY_STATE, {
      employee: 'Ola Nordmann',
      date: date!,
      shift: 'morning',
    });
    expect(r1.ok).toBe(true);
    if (!r1.ok) return;

    const r2 = scheduleAssignment(r1.state, {
      employee: 'Kari Nordmann',
      date: date!,
      shift: 'morning',
    });

    expect(r2.ok).toBe(false);
    if (!r2.ok) {
      expect(r2.error.code).toBe('DUPLICATE_SLOT');
      expect(r2.error.date).toBe('2026-05-04');
      expect(r2.error.shift).toBe('morning');
      expect(r2.error.existingEmployee).toBe('Ola Nordmann');
      expect(r2.error.attemptedEmployee).toBe('Kari Nordmann');
      expect(r2.error.message).toMatch(/En ansatt kan ikke/);
    }
  });

  it('does not mutate the original state on success', () => {
    const date = parseShiftDate('2026-05-04');
    const original = EMPTY_STATE;
    const result = scheduleAssignment(original, {
      employee: 'Ola',
      date: date!,
      shift: 'morning',
    });
    expect(result.ok).toBe(true);
    // Original state must be unchanged
    expect(original.assignments).toHaveLength(0);
  });

  it('assigns unique ids to each new assignment', () => {
    const d1 = parseShiftDate('2026-05-04');
    const d2 = parseShiftDate('2026-05-05');

    const r1 = scheduleAssignment(EMPTY_STATE, { employee: 'Ola', date: d1!, shift: 'morning' });
    expect(r1.ok).toBe(true);
    if (!r1.ok) return;

    const r2 = scheduleAssignment(r1.state, { employee: 'Kari', date: d2!, shift: 'afternoon' });
    expect(r2.ok).toBe(true);
    if (!r2.ok) return;

    const ids = r2.state.assignments.map((a) => a.id);
    expect(new Set(ids).size).toBe(ids.length); // all unique
  });
});
