import { describe, it, expect } from 'vitest';
import { createRepository } from '../../src/state/vaktlisteRepository';
import { parseShiftDate } from '../../src/domain/types';
import type { SchedulerState } from '../../src/domain/types';
import type { RepositoryStorage } from '../../src/state/vaktlisteRepository';

/** Build a minimal in-memory storage for tests */
function makeMemStorage(): RepositoryStorage & { _data: Map<string, string> } {
  const _data = new Map<string, string>();
  return {
    _data,
    getItem(key: string) { return _data.get(key) ?? null; },
    setItem(key: string, value: string) { _data.set(key, value); },
  };
}

function makeValidState(): SchedulerState {
  const date = parseShiftDate('2026-05-04');
  if (!date) throw new Error('bad date');
  return {
    assignments: [
      { id: 'a1', employee: 'Ola Nordmann', date, shift: 'morning' },
      { id: 'a2', employee: 'Kari Nordmann', date, shift: 'afternoon' },
    ],
  };
}

describe('vaktlisteRepository (createRepository factory)', () => {
  // ── load() ────────────────────────────────────────────────────────

  it('returns empty state when storage key is missing', () => {
    const repo = createRepository(makeMemStorage());
    expect(repo.load().assignments).toHaveLength(0);
  });

  it('rehydrates previously saved valid state', () => {
    const storage = makeMemStorage();
    const repo = createRepository(storage);
    repo.save(makeValidState());

    const loaded = repo.load();
    expect(loaded.assignments).toHaveLength(2);
    // Preserved in storage order: Ola first, then Kari
    expect(loaded.assignments[0]?.employee).toBe('Ola Nordmann');
    expect(loaded.assignments[1]?.employee).toBe('Kari Nordmann');
  });

  it('returns empty state on malformed JSON', () => {
    const storage = makeMemStorage();
    storage.setItem('vaktliste_v1', '{bad json{{');
    expect(createRepository(storage).load().assignments).toHaveLength(0);
  });

  it('returns empty state when assignments array is missing', () => {
    const storage = makeMemStorage();
    storage.setItem('vaktliste_v1', JSON.stringify({ employees: [] }));
    expect(createRepository(storage).load().assignments).toHaveLength(0);
  });

  it('returns empty state when an assignment has invalid shift type', () => {
    const storage = makeMemStorage();
    const date = parseShiftDate('2026-05-04');
    storage.setItem(
      'vaktliste_v1',
      JSON.stringify({ assignments: [{ id: 'a1', employee: 'Ola', date, shift: 'evening' }] }),
    );
    expect(createRepository(storage).load().assignments).toHaveLength(0);
  });

  it('returns empty state when an assignment has invalid date', () => {
    const storage = makeMemStorage();
    storage.setItem(
      'vaktliste_v1',
      JSON.stringify({
        assignments: [{ id: 'a1', employee: 'Ola', date: '2026-13-01', shift: 'morning' }],
      }),
    );
    expect(createRepository(storage).load().assignments).toHaveLength(0);
  });

  it('returns empty state when an assignment has empty employee string', () => {
    const storage = makeMemStorage();
    const date = parseShiftDate('2026-05-04');
    storage.setItem(
      'vaktliste_v1',
      JSON.stringify({ assignments: [{ id: 'a1', employee: '', date, shift: 'morning' }] }),
    );
    expect(createRepository(storage).load().assignments).toHaveLength(0);
  });

  it('returns empty state for structurally invalid payload (not an object)', () => {
    const storage = makeMemStorage();
    storage.setItem('vaktliste_v1', JSON.stringify([1, 2, 3]));
    expect(createRepository(storage).load().assignments).toHaveLength(0);
  });

  it('returns empty state when any single assignment is invalid (all-or-nothing)', () => {
    const storage = makeMemStorage();
    const date = parseShiftDate('2026-05-04');
    storage.setItem(
      'vaktliste_v1',
      JSON.stringify({
        assignments: [
          { id: 'a1', employee: 'Ola', date, shift: 'morning' },
          { id: 'a2', employee: 'Kari', date, shift: 'INVALID_SHIFT' },
        ],
      }),
    );
    expect(createRepository(storage).load().assignments).toHaveLength(0);
  });

  // ── save() ────────────────────────────────────────────────────────

  it('save() returns ok:true on successful write', () => {
    const result = createRepository(makeMemStorage()).save({ assignments: [] });
    expect(result.ok).toBe(true);
  });

  it('save() returns structured WRITE_FAILED error when setItem throws', () => {
    const throwingStorage: RepositoryStorage = {
      getItem: () => null,
      setItem: () => {
        throw new DOMException('QuotaExceededError', 'QuotaExceededError');
      },
    };

    const result = createRepository(throwingStorage).save({ assignments: [] });
    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.error.code).toBe('WRITE_FAILED');
      expect(typeof result.error.message).toBe('string');
    }
  });
});
