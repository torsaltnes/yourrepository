import { formatShiftType } from '../utils/dateFormatting';

export type BadgeVariant = 'morning' | 'evening' | 'conflict';

/**
 * Creates an inline shift badge <span> element.
 * Text is set via textContent (safe rendering).
 */
export function createShiftBadge(variant: BadgeVariant, label?: string): HTMLSpanElement {
  const span = document.createElement('span');
  span.className = `badge badge--${variant}`;

  let text: string;
  if (label !== undefined) {
    text = label;
  } else if (variant === 'morning') {
    text = formatShiftType('morning');
  } else if (variant === 'evening') {
    text = formatShiftType('afternoon');
  } else {
    text = 'KONFLIKT';
  }

  span.textContent = text;
  return span;
}
