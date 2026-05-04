import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createVaktlisteStore } from '../../src/state/vaktlisteStore';
import { parseShiftDate, EMPTY_SCHEDULER_STATE } from '../../src/domain/types';
import type { SchedulerState, SaveResult } from '../../src/domain/types';
import type { Repository } from '../../src/state/vaktlisteStore';

/** Create an in-memory repository for isolated store testing */
function makeRepo(initialState: SchedulerState = EMPTY_SCHEDULER_STATE): Repository & {
  saveCallCount: number;
  lastSavedState: SchedulerState | null;
  nextSaveResult: SaveResult;
} {
  let stored = initialState;
  const repo = {
    saveCallCount: 0,
    lastSavedState: null as SchedulerState | null,
    nextSaveResult: { ok: true } as SaveResult,
    load: () => stored,
    save: (state: SchedulerState): SaveResult => {
      repo.saveCallCount++;
      repo.lastSavedState = state;
      const result = repo.nextSaveResult;
      if (result.ok) stored = state;
      return result;
    },
  };
  return repo;
}

describe('vaktlisteStore', () => {
  it('getSnapshot() returns empty committed state initially', () => {
    const store = createVaktlisteStore(makeRepo());
    expect(store.getSnapshot().assignments).toHaveLength(0);
  });

  it('getSubmitStatus() starts as idle', () => {
    const store = createVaktlisteStore(makeRepo());
    expect(store.getSubmitStatus().kind).toBe('idle');
  });

  it('successful mutation persists candidate state before committing snapshot', () => {
    const repo = makeRepo();
    const store = createVaktlisteStore(repo);

    const date = parseShiftDate('2026-05-04');
    store.addAssignment({ employee: 'Ola', date: date!, shift: 'morning' });

    // save() must have been called with the candidate state
    expect(repo.saveCallCount).toBe(1);
    expect(repo.lastSavedState?.assignments).toHaveLength(1);
    // Snapshot is committed
    expect(store.getSnapshot().assignments).toHaveLength(1);
    expect(store.getSubmitStatus().kind).toBe('success');
  });

  it('save failure preserves previous committed snapshot', () => {
    const repo = makeRepo();
    const store = createVaktlisteStore(repo);

    // First add succeeds
    const d1 = parseShiftDate('2026-05-04');
    store.addAssignment({ employee: 'Ola', date: d1!, shift: 'morning' });
    expect(store.getSnapshot().assignments).toHaveLength(1);

    // Make next save fail
    repo.nextSaveResult = {
      ok: false,
      error: { code: 'WRITE_FAILED', message: 'QuotaExceeded' },
    };

    const d2 = parseShiftDate('2026-05-05');
    store.addAssignment({ employee: 'Kari', date: d2!, shift: 'afternoon' });

    // Snapshot must still have only the first assignment
    expect(store.getSnapshot().assignments).toHaveLength(1);
    // Status surfaced as persistence error
    expect(store.getSubmitStatus().kind).toBe('persistence_error');
  });

  it('duplicate occupancy creates transient KONFLIKT status without mutating or persisting state', () => {
    const repo = makeRepo();
    const store = createVaktlisteStore(repo);
    const date = parseShiftDate('2026-05-04');

    store.addAssignment({ employee: 'Ola', date: date!, shift: 'morning' });
    const saveCountBefore = repo.saveCallCount;

    store.addAssignment({ employee: 'Kari', date: date!, shift: 'morning' }); // conflict

    // State must NOT change
    expect(store.getSnapshot().assignments).toHaveLength(1);
    // No new write to storage
    expect(repo.saveCallCount).toBe(saveCountBefore);
    // Transient conflict status
    expect(store.getSubmitStatus().kind).toBe('conflict');
  });

  it('getSnapshot() exposes only committed state (not transient conflict)', () => {
    const repo = makeRepo();
    const store = createVaktlisteStore(repo);
    const date = parseShiftDate('2026-05-04');

    store.addAssignment({ employee: 'Ola', date: date!, shift: 'morning' });
    store.addAssignment({ employee: 'Kari', date: date!, shift: 'morning' }); // conflict

    const snapshot = store.getSnapshot();
    expect(snapshot.assignments).toHaveLength(1);
    expect(snapshot.assignments[0]?.employee).toBe('Ola');
  });

  it('subscribe() notifies listener on successful committed update', () => {
    const store = createVaktlisteStore(makeRepo());
    const listener = vi.fn();
    store.subscribe(listener);

    const date = parseShiftDate('2026-05-04');
    store.addAssignment({ employee: 'Ola', date: date!, shift: 'morning' });

    expect(listener).toHaveBeenCalledTimes(1);
  });

  it('subscribe() notifies listener on conflict (transient status change)', () => {
    const repo = makeRepo();
    const store = createVaktlisteStore(repo);
    const date = parseShiftDate('2026-05-04');
    store.addAssignment({ employee: 'Ola', date: date!, shift: 'morning' });

    const listener = vi.fn();
    store.subscribe(listener);
    store.addAssignment({ employee: 'Kari', date: date!, shift: 'morning' });

    expect(listener).toHaveBeenCalledTimes(1);
  });

  it('subscribe() notifies listener on persistence error', () => {
    const repo = makeRepo();
    const store = createVaktlisteStore(repo);

    const d1 = parseShiftDate('2026-05-04');
    store.addAssignment({ employee: 'Ola', date: d1!, shift: 'morning' });

    repo.nextSaveResult = { ok: false, error: { code: 'WRITE_FAILED', message: 'fail' } };

    const listener = vi.fn();
    store.subscribe(listener);

    const d2 = parseShiftDate('2026-05-05');
    store.addAssignment({ employee: 'Kari', date: d2!, shift: 'afternoon' });

    expect(listener).toHaveBeenCalledTimes(1);
  });

  it('subscribe() unsubscribe stops future notifications', () => {
    const store = createVaktlisteStore(makeRepo());
    const listener = vi.fn();
    const unsub = store.subscribe(listener);
    unsub();

    const date = parseShiftDate('2026-05-04');
    store.addAssignment({ employee: 'Ola', date: date!, shift: 'morning' });

    expect(listener).not.toHaveBeenCalled();
  });

  it('rehydrates state from repository on creation', () => {
    const date = parseShiftDate('2026-05-04');
    if (!date) throw new Error('bad date');
    const preloaded: SchedulerState = {
      assignments: [{ id: 'x1', employee: 'Persisted Person', date, shift: 'morning' }],
    };
    const repo = makeRepo(preloaded);
    const store = createVaktlisteStore(repo);

    expect(store.getSnapshot().assignments).toHaveLength(1);
    expect(store.getSnapshot().assignments[0]?.employee).toBe('Persisted Person');
  });

  it('reset() reloads from repository (empty state after repo cleared)', () => {
    const repo = makeRepo();
    const store = createVaktlisteStore(repo);
    const date = parseShiftDate('2026-05-04');
    store.addAssignment({ employee: 'Ola', date: date!, shift: 'morning' });
    expect(store.getSnapshot().assignments).toHaveLength(1);

    // Simulate clearing – repo now returns empty
    repo.load = () => EMPTY_SCHEDULER_STATE;
    store.reset();

    expect(store.getSnapshot().assignments).toHaveLength(0);
    expect(store.getSubmitStatus().kind).toBe('idle');
  });
});
