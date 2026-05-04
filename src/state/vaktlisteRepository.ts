/**
 * sessionStorage-backed repository for Vaktliste state.
 * Fixed storage key, strict payload validation, structured error results.
 * Exposes createRepository() factory for dependency injection / testing.
 */
import {
  parseShiftDate,
  EMPTY_SCHEDULER_STATE,
} from '../domain/types';
import type {
  SchedulerState,
  ShiftAssignment,
  ShiftType,
  SaveResult,
} from '../domain/types';

const STORAGE_KEY = 'vaktliste_v1';
const VALID_SHIFT_TYPES: ReadonlySet<string> = new Set<string>(['morning', 'afternoon']);

function isValidShiftType(value: unknown): value is ShiftType {
  return typeof value === 'string' && VALID_SHIFT_TYPES.has(value);
}

function validateAssignment(raw: unknown): ShiftAssignment | null {
  if (typeof raw !== 'object' || raw === null) return null;
  const obj = raw as Record<string, unknown>;

  if (typeof obj['id'] !== 'string' || obj['id'].length === 0) return null;
  if (typeof obj['employee'] !== 'string' || obj['employee'].trim() === '') return null;
  if (!isValidShiftType(obj['shift'])) return null;

  const rawDate = obj['date'];
  const date = typeof rawDate === 'string' ? parseShiftDate(rawDate) : null;
  if (!date) return null;

  return {
    id: obj['id'],
    employee: obj['employee'],
    date,
    shift: obj['shift'],
  };
}

function validateState(raw: unknown): SchedulerState | null {
  if (typeof raw !== 'object' || raw === null) return null;
  const obj = raw as Record<string, unknown>;

  if (!Array.isArray(obj['assignments'])) return null;

  const assignments: ShiftAssignment[] = [];
  for (const item of obj['assignments']) {
    const validated = validateAssignment(item);
    if (!validated) return null;
    assignments.push(validated);
  }

  return { assignments };
}

export interface RepositoryStorage {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
}

export interface VaktlisteRepository {
  load(): SchedulerState;
  save(state: SchedulerState): SaveResult;
}

/** Factory: create a repository backed by any Storage implementation */
export function createRepository(
  storage: RepositoryStorage = sessionStorage,
): VaktlisteRepository {
  function load(): SchedulerState {
    try {
      const raw = storage.getItem(STORAGE_KEY);
      if (raw === null) return EMPTY_SCHEDULER_STATE;

      const parsed: unknown = JSON.parse(raw);
      const validated = validateState(parsed);
      return validated ?? EMPTY_SCHEDULER_STATE;
    } catch {
      return EMPTY_SCHEDULER_STATE;
    }
  }

  function save(state: SchedulerState): SaveResult {
    let serialized: string;
    try {
      serialized = JSON.stringify(state);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Serialization failed';
      return { ok: false, error: { code: 'SERIALIZE_FAILED', message } };
    }

    try {
      storage.setItem(STORAGE_KEY, serialized);
      return { ok: true };
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Storage write failed';
      return { ok: false, error: { code: 'WRITE_FAILED', message } };
    }
  }

  return { load, save };
}

/** Default repository using real sessionStorage */
const defaultRepository = createRepository();

export const load = (): SchedulerState => defaultRepository.load();
export const save = (state: SchedulerState): SaveResult => defaultRepository.save(state);
