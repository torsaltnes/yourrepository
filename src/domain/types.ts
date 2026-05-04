// ── Primitive types ──────────────────────────────────────────────

/** Allowed shift types */
export type ShiftType = 'morning' | 'afternoon';

/**
 * Opaque branded type for validated ISO date-only strings.
 * Create values ONLY via parseShiftDate().
 */
export type ShiftDate = string & { readonly __brand: 'ShiftDate' };

/**
 * Parse a raw string as a ShiftDate.
 * Accepts only canonical real calendar dates in YYYY-MM-DD form.
 * Returns null for any invalid or non-calendar input.
 */
export function parseShiftDate(raw: string): ShiftDate | null {
  if (typeof raw !== 'string') return null;
  const pattern = /^\d{4}-(?:0[1-9]|1[0-2])-(?:0[1-9]|[12]\d|3[01])$/;
  if (!pattern.test(raw)) return null;

  const parts = raw.split('-');
  const yearStr = parts[0];
  const monthStr = parts[1];
  const dayStr = parts[2];
  if (!yearStr || !monthStr || !dayStr) return null;

  const year = parseInt(yearStr, 10);
  const month = parseInt(monthStr, 10);
  const day = parseInt(dayStr, 10);

  // Validate against JS Date (catches Feb 30, Apr 31, etc.)
  const d = new Date(year, month - 1, day);
  if (d.getFullYear() !== year || d.getMonth() !== month - 1 || d.getDate() !== day) {
    return null;
  }

  return raw as ShiftDate;
}

// ── Domain entities ──────────────────────────────────────────────

export interface ShiftAssignment {
  readonly id: string;
  readonly employee: string;
  readonly date: ShiftDate;
  readonly shift: ShiftType;
}

export interface SchedulerState {
  readonly assignments: readonly ShiftAssignment[];
}

export const EMPTY_SCHEDULER_STATE: SchedulerState = {
  assignments: [],
};

export interface AssignmentCommand {
  readonly employee: string;
  readonly date: ShiftDate;
  readonly shift: ShiftType;
}

// ── Scheduler result types ───────────────────────────────────────

/** Structured conflict error carrying all info needed for UI and alerts */
export interface ConflictError {
  readonly code: 'DUPLICATE_SLOT';
  readonly date: ShiftDate;
  readonly shift: ShiftType;
  /** Employee who already holds the conflicting slot */
  readonly existingEmployee: string;
  /** Employee who was attempting the assignment */
  readonly attemptedEmployee: string;
  readonly message: string;
}

export type SchedulerResult =
  | { readonly ok: true; readonly state: SchedulerState }
  | { readonly ok: false; readonly error: ConflictError };

// ── Persistence result types ─────────────────────────────────────

export interface PersistenceError {
  readonly code: 'SERIALIZE_FAILED' | 'WRITE_FAILED' | 'UNKNOWN_ERROR';
  readonly message: string;
}

export type SaveResult =
  | { readonly ok: true }
  | { readonly ok: false; readonly error: PersistenceError };

// ── Store / UI status types ──────────────────────────────────────

export type SubmitStatus =
  | { readonly kind: 'idle' }
  | { readonly kind: 'conflict'; readonly error: ConflictError }
  | { readonly kind: 'persistence_error'; readonly error: PersistenceError }
  | { readonly kind: 'success' };
