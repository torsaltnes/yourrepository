/**
 * Thin facade over the pure scheduler domain function.
 * Do NOT re-implement the duplicate-slot rule here; delegate to scheduler.ts.
 * This file exists as a compatibility shim for acceptance criteria naming.
 */
import { scheduleAssignment } from './scheduler';
export { scheduleAssignment as validateAndSchedule };
export { scheduleAssignment };
export type {
  SchedulerResult,
  ConflictError,
  AssignmentCommand,
  SchedulerState,
} from './types';
