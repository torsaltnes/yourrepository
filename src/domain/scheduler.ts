/**
 * Pure domain function for scheduling mutations.
 * This is the ONLY place where the duplicate-slot business rule lives.
 */
import type {
  SchedulerState,
  AssignmentCommand,
  SchedulerResult,
  ShiftAssignment,
} from './types';

export function scheduleAssignment(
  state: SchedulerState,
  command: AssignmentCommand,
): SchedulerResult {
  const duplicate = state.assignments.find(
    (a) => a.date === command.date && a.shift === command.shift,
  );

  if (duplicate) {
    return {
      ok: false,
      error: {
        code: 'DUPLICATE_SLOT',
        date: command.date,
        shift: command.shift,
        existingEmployee: duplicate.employee,
        attemptedEmployee: command.employee,
        message: `Systemfeil: En ansatt kan ikke ha to vakter samtidig. ${duplicate.employee} er allerede satt opp på denne vakten.`,
      },
    };
  }

  const newAssignment: ShiftAssignment = {
    id: crypto.randomUUID(),
    employee: command.employee,
    date: command.date,
    shift: command.shift,
  };

  return {
    ok: true,
    state: {
      assignments: [...state.assignments, newAssignment],
    },
  };
}
