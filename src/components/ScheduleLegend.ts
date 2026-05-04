import { createShiftBadge } from './ShiftBadge';

/**
 * Legend showing morning / afternoon color coding.
 */
export function createScheduleLegend(): HTMLElement {
  const nav = document.createElement('div');
  nav.className = 'flex items-center gap-3';
  nav.setAttribute('aria-label', 'Skiftforklaring');

  const morningBadge = createShiftBadge('morning');
  const eveningBadge = createShiftBadge('evening');

  nav.appendChild(morningBadge);
  nav.appendChild(eveningBadge);

  return nav;
}
