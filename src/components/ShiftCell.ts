import { formatShiftType } from '../utils/dateFormatting';

export type CellVariant = 'morning' | 'evening' | 'conflict';

export interface ShiftCellContent {
  readonly variant: CellVariant;
  readonly label?: string;
}

/**
 * Creates a shift card <div> for use inside a table cell.
 * Text set via textContent (safe rendering).
 */
export function createShiftCard(content: ShiftCellContent): HTMLDivElement {
  const div = document.createElement('div');
  div.className = `shift-card shift-card--${content.variant}`;

  let text: string;
  if (content.label !== undefined) {
    text = content.label;
  } else if (content.variant === 'morning') {
    text = formatShiftType('morning');
  } else if (content.variant === 'evening') {
    text = formatShiftType('afternoon');
  } else {
    text = 'KONFLIKT';
  }

  div.textContent = text;
  return div;
}

/**
 * Creates a <td> containing zero or more shift cards.
 */
export function createShiftCell(contents: readonly ShiftCellContent[]): HTMLTableCellElement {
  const td = document.createElement('td');

  if (contents.length === 0) {
    td.className = 'text-gray-400 text-center';
    td.textContent = '—';
  } else {
    for (const content of contents) {
      td.appendChild(createShiftCard(content));
    }
  }

  return td;
}
