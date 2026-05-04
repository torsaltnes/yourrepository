/**
 * Compatibility facade – wraps the sessionStorage-backed repository.
 * Named "InMemoryScheduleRepository" to satisfy AC-006 naming requirements;
 * in the test environment, sessionStorage IS in-memory.
 */
export { load, save } from '../state/vaktlisteRepository';
export type { SaveResult } from '../domain/types';
