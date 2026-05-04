/**
 * ScheduleService – thin wrapper around vaktlisteStore.
 * Provides a compatible service-layer API without forking any business logic.
 */
import { vaktlisteStore } from '../state/vaktlisteStore';
import type { AssignmentCommand, SchedulerState, SubmitStatus } from '../domain/types';

export class ScheduleService {
  getState(): SchedulerState {
    return vaktlisteStore.getSnapshot();
  }

  getSubmitStatus(): SubmitStatus {
    return vaktlisteStore.getSubmitStatus();
  }

  createAssignment(command: AssignmentCommand): void {
    vaktlisteStore.addAssignment(command);
  }

  subscribe(listener: () => void): () => void {
    return vaktlisteStore.subscribe(listener);
  }

  reset(): void {
    vaktlisteStore.reset();
  }
}

export const scheduleService = new ScheduleService();
