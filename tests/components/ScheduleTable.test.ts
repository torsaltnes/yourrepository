import { describe, it, expect } from 'vitest';
import { createScheduleTable } from '../../src/components/ScheduleTable';
import { parseShiftDate } from '../../src/domain/types';
import type { ShiftAssignment, SubmitStatus } from '../../src/domain/types';

function makeAssignment(
  employee: string,
  rawDate: string,
  shift: 'morning' | 'afternoon',
): ShiftAssignment {
  const date = parseShiftDate(rawDate);
  if (!date) throw new Error(`Invalid date: ${rawDate}`);
  return { id: `id-${Math.random()}`, employee, date, shift };
}

const idleStatus: SubmitStatus = { kind: 'idle' };

describe('ScheduleTable', () => {
  it('renders a table element', () => {
    const wrapper = createScheduleTable({ assignments: [], submitStatus: idleStatus });
    document.body.appendChild(wrapper);
    const table = wrapper.querySelector('table');
    expect(table).not.toBeNull();
  });

  it('shows empty state message when no assignments', () => {
    const wrapper = createScheduleTable({ assignments: [], submitStatus: idleStatus });
    document.body.appendChild(wrapper);
    expect(wrapper.textContent).toContain('Ingen vakter');
  });

  it('renders employee as row header', () => {
    const a = makeAssignment('Ola Nordmann', '2026-05-04', 'morning');
    const wrapper = createScheduleTable({ assignments: [a], submitStatus: idleStatus });
    document.body.appendChild(wrapper);
    expect(wrapper.textContent).toContain('Ola Nordmann');
  });

  it('renders date as column header', () => {
    const a = makeAssignment('Ola', '2026-05-04', 'morning');
    const wrapper = createScheduleTable({ assignments: [a], submitStatus: idleStatus });
    document.body.appendChild(wrapper);
    // May 4 = Man 04.05
    expect(wrapper.textContent).toContain('04.05');
  });

  it('renders morning shift card', () => {
    const a = makeAssignment('Ola', '2026-05-04', 'morning');
    const wrapper = createScheduleTable({ assignments: [a], submitStatus: idleStatus });
    document.body.appendChild(wrapper);
    const card = wrapper.querySelector('.shift-card--morning');
    expect(card).not.toBeNull();
    expect(card?.textContent).toContain('Morgen');
  });

  it('renders afternoon shift card', () => {
    const a = makeAssignment('Kari', '2026-05-05', 'afternoon');
    const wrapper = createScheduleTable({ assignments: [a], submitStatus: idleStatus });
    document.body.appendChild(wrapper);
    const card = wrapper.querySelector('.shift-card--evening');
    expect(card).not.toBeNull();
    expect(card?.textContent).toContain('Ettermiddag');
  });

  it('renders KONFLIKT card for transient conflict state', () => {
    const date = parseShiftDate('2026-05-04');
    if (!date) throw new Error('bad date');

    const existing = makeAssignment('Ola Nordmann', '2026-05-04', 'morning');

    const conflictStatus: SubmitStatus = {
      kind: 'conflict',
      error: {
        code: 'DUPLICATE_SLOT',
        date,
        shift: 'morning',
        existingEmployee: 'Ola Nordmann',
        attemptedEmployee: 'Kari Nordmann',
        message: 'Test conflict message',
      },
    };

    const wrapper = createScheduleTable({
      assignments: [existing],
      submitStatus: conflictStatus,
    });
    document.body.appendChild(wrapper);

    const conflictCard = wrapper.querySelector('.shift-card--conflict');
    expect(conflictCard).not.toBeNull();
    expect(conflictCard?.textContent).toContain('KONFLIKT');

    // Kari (attempted) should appear as a row
    expect(wrapper.textContent).toContain('Kari Nordmann');
  });

  it('does not render KONFLIKT card when status is idle', () => {
    const a = makeAssignment('Ola', '2026-05-04', 'morning');
    const wrapper = createScheduleTable({ assignments: [a], submitStatus: idleStatus });
    document.body.appendChild(wrapper);
    expect(wrapper.querySelector('.shift-card--conflict')).toBeNull();
  });

  it('renders multiple employees as separate rows', () => {
    const a1 = makeAssignment('Ola', '2026-05-04', 'morning');
    const a2 = makeAssignment('Kari', '2026-05-04', 'afternoon');
    const wrapper = createScheduleTable({ assignments: [a1, a2], submitStatus: idleStatus });
    document.body.appendChild(wrapper);
    const rows = wrapper.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
  });
});
