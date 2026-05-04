import { describe, it, expect } from 'vitest';
import { parseShiftDate } from '../../src/domain/types';
import { validateAndSchedule } from '../../src/domain/ShiftValidationService';
import type { SchedulerState } from '../../src/domain/types';

describe('ShiftValidationService (facade over scheduler)', () => {
  function makeState(...overrides: Parameters<typeof parseShiftDate>[]): SchedulerState {
    return { assignments: [] };
  }

  const emptyState: SchedulerState = { assignments: [] };

  it('accepts a valid assignment into an empty slot', () => {
    const date = parseShiftDate('2026-05-04');
    expect(date).not.toBeNull();

    const result = validateAndSchedule(emptyState, {
      employee: 'Ola Nordmann',
      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
      date: date!,
      shift: 'morning',
    });

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.state.assignments).toHaveLength(1);
      expect(result.state.assignments[0]?.employee).toBe('Ola Nordmann');
    }
  });

  it('rejects duplicate occupancy for the same date + shift', () => {
    const date = parseShiftDate('2026-05-04');
    expect(date).not.toBeNull();

    const firstResult = validateAndSchedule(emptyState, {
      employee: 'Ola Nordmann',
      date: date!,
      shift: 'morning',
    });
    expect(firstResult.ok).toBe(true);
    if (!firstResult.ok) return;

    const secondResult = validateAndSchedule(firstResult.state, {
      employee: 'Kari Nordmann',
      date: date!,
      shift: 'morning',
    });

    expect(secondResult.ok).toBe(false);
    if (!secondResult.ok) {
      expect(secondResult.error.code).toBe('DUPLICATE_SLOT');
      expect(secondResult.error.existingEmployee).toBe('Ola Nordmann');
      expect(secondResult.error.attemptedEmployee).toBe('Kari Nordmann');
      expect(secondResult.error.shift).toBe('morning');
      expect(typeof secondResult.error.message).toBe('string');
      expect(secondResult.error.message.length).toBeGreaterThan(0);
    }
  });

  it('allows the same employee on different shifts on the same date', () => {
    const date = parseShiftDate('2026-05-04');
    expect(date).not.toBeNull();

    const r1 = validateAndSchedule(emptyState, { employee: 'Ola', date: date!, shift: 'morning' });
    expect(r1.ok).toBe(true);
    if (!r1.ok) return;

    const r2 = validateAndSchedule(r1.state, { employee: 'Kari', date: date!, shift: 'afternoon' });
    expect(r2.ok).toBe(true);
    if (r2.ok) {
      expect(r2.state.assignments).toHaveLength(2);
    }
  });

  it('allows same shift type on different dates', () => {
    const d1 = parseShiftDate('2026-05-04');
    const d2 = parseShiftDate('2026-05-05');
    expect(d1).not.toBeNull();
    expect(d2).not.toBeNull();

    const r1 = validateAndSchedule(emptyState, { employee: 'Ola', date: d1!, shift: 'morning' });
    expect(r1.ok).toBe(true);
    if (!r1.ok) return;

    const r2 = validateAndSchedule(r1.state, { employee: 'Kari', date: d2!, shift: 'morning' });
    expect(r2.ok).toBe(true);
  });

  it('parseShiftDate rejects invalid dates', () => {
    expect(parseShiftDate('not-a-date')).toBeNull();
    expect(parseShiftDate('2026-13-01')).toBeNull();
    expect(parseShiftDate('2026-02-30')).toBeNull();
    expect(parseShiftDate('20260504')).toBeNull();
    expect(parseShiftDate('')).toBeNull();
  });

  it('parseShiftDate accepts valid dates', () => {
    expect(parseShiftDate('2026-01-01')).toBe('2026-01-01');
    expect(parseShiftDate('2026-12-31')).toBe('2026-12-31');
    expect(parseShiftDate('2024-02-29')).toBe('2024-02-29'); // leap year
  });

  it('parseShiftDate rejects invalid leap day', () => {
    expect(parseShiftDate('2026-02-29')).toBeNull(); // 2026 is not a leap year
  });
});
