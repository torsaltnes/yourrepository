/**
 * Observable store for Vaktliste state.
 * Follows save-before-commit: persists candidate state before updating the
 * in-memory snapshot. If save fails the snapshot is NOT updated.
 * Accepts an injectable repository for dependency injection and testing.
 */
import { load as defaultLoad, save as defaultSave } from './vaktlisteRepository';
import { scheduleAssignment } from '../domain/scheduler';
import type {
  SchedulerState,
  AssignmentCommand,
  SubmitStatus,
  SaveResult,
} from '../domain/types';

type Listener = () => void;

export interface Repository {
  load(): SchedulerState;
  save(state: SchedulerState): SaveResult;
}

export interface VaktlisteStore {
  getSnapshot(): SchedulerState;
  getSubmitStatus(): SubmitStatus;
  subscribe(listener: Listener): () => void;
  addAssignment(command: AssignmentCommand): void;
  reset(): void;
}

interface InternalState {
  committed: SchedulerState;
  submitStatus: SubmitStatus;
}

export function createVaktlisteStore(
  repo: Repository = { load: defaultLoad, save: defaultSave },
): VaktlisteStore {
  let state: InternalState = {
    committed: repo.load(),
    submitStatus: { kind: 'idle' },
  };

  const listeners = new Set<Listener>();

  function notify(): void {
    for (const listener of listeners) {
      listener();
    }
  }

  function getSnapshot(): SchedulerState {
    return state.committed;
  }

  function getSubmitStatus(): SubmitStatus {
    return state.submitStatus;
  }

  function subscribe(listener: Listener): () => void {
    listeners.add(listener);
    return () => { listeners.delete(listener); };
  }

  function addAssignment(command: AssignmentCommand): void {
    const result = scheduleAssignment(state.committed, command);

    if (!result.ok) {
      // Conflict: keep committed state unchanged, surface transient status only
      state = { ...state, submitStatus: { kind: 'conflict', error: result.error } };
      notify();
      return;
    }

    // Build candidate and persist BEFORE committing
    const candidateState = result.state;
    const saveResult = repo.save(candidateState);

    if (!saveResult.ok) {
      // Save failed: keep previous committed snapshot intact
      state = { ...state, submitStatus: { kind: 'persistence_error', error: saveResult.error } };
      notify();
      return;
    }

    // Commit only after successful save
    state = { committed: candidateState, submitStatus: { kind: 'success' } };
    notify();
  }

  function reset(): void {
    state = {
      committed: repo.load(),
      submitStatus: { kind: 'idle' },
    };
    // No notify – reset is used during test setup, not UI lifecycle
  }

  return { getSnapshot, getSubmitStatus, subscribe, addAssignment, reset };
}

/** Application-level singleton store backed by sessionStorage */
export const vaktlisteStore: VaktlisteStore = createVaktlisteStore();
