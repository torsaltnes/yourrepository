import type { ShiftDate, ShiftType } from '../domain/types';

const DAYS_NO = ['Søn', 'Man', 'Tir', 'Ons', 'Tor', 'Fre', 'Lør'] as const;

/**
 * Format a ShiftDate as "Man 04.05" (Norwegian day abbreviation + day.month).
 * Uses the date value directly – never a JS Date object in domain state.
 */
export function formatShiftDate(date: ShiftDate): string {
  const parts = date.split('-');
  const yearStr = parts[0];
  const monthStr = parts[1];
  const dayStr = parts[2];

  if (!yearStr || !monthStr || !dayStr) return date;

  const year = parseInt(yearStr, 10);
  const month = parseInt(monthStr, 10);
  const day = parseInt(dayStr, 10);

  const jsDate = new Date(year, month - 1, day);
  const dayName = DAYS_NO[jsDate.getDay()] ?? 'N/A';
  const dd = String(day).padStart(2, '0');
  const mm = String(month).padStart(2, '0');

  return `${dayName} ${dd}.${mm}`;
}

/** Norwegian display name for a shift type */
export function formatShiftType(shift: ShiftType): string {
  return shift === 'morning' ? 'Morgen' : 'Ettermiddag';
}
