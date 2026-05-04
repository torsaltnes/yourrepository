/**
 * Seed employee data for form suggestions.
 * These are not committed to the store; they are used as initial options
 * in the assignment dialog's employee field.
 * Do NOT seed conflicting or duplicate assignments.
 */
export const SEED_EMPLOYEES = [
  'Ola Nordmann',
  'Kari Nordmann',
  'Per Hansen',
  'Anne Olsen',
  'Lars Bakke',
] as const;

export type SeedEmployee = (typeof SEED_EMPLOYEES)[number];
