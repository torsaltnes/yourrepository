import { createShiftCell } from './ShiftCell';
import { formatShiftDate } from '../utils/dateFormatting';
import type { ShiftAssignment, ShiftDate, SubmitStatus } from '../domain/types';
import type { ShiftCellContent } from './ShiftCell';

export interface ScheduleTableProps {
  readonly assignments: readonly ShiftAssignment[];
  readonly submitStatus: SubmitStatus;
}

interface MutableCellData {
  morningAssignment: ShiftAssignment | null;
  afternoonAssignment: ShiftAssignment | null;
  morningConflict: boolean;
  afternoonConflict: boolean;
}

interface TableData {
  readonly dates: readonly ShiftDate[];
  readonly employees: readonly string[];
  readonly cells: ReadonlyMap<string, ReadonlyMap<ShiftDate, MutableCellData>>;
}

function computeTableData(props: ScheduleTableProps): TableData {
  const dateSet = new Set<ShiftDate>();
  const employeeSet = new Set<string>();
  const cells = new Map<string, Map<ShiftDate, MutableCellData>>();

  function ensureCell(employee: string, date: ShiftDate): MutableCellData {
    let empMap = cells.get(employee);
    if (!empMap) {
      empMap = new Map<ShiftDate, MutableCellData>();
      cells.set(employee, empMap);
    }
    let cell = empMap.get(date);
    if (!cell) {
      cell = {
        morningAssignment: null,
        afternoonAssignment: null,
        morningConflict: false,
        afternoonConflict: false,
      };
      empMap.set(date, cell);
    }
    return cell;
  }

  for (const a of props.assignments) {
    dateSet.add(a.date);
    employeeSet.add(a.employee);
    const cell = ensureCell(a.employee, a.date);
    if (a.shift === 'morning') cell.morningAssignment = a;
    else cell.afternoonAssignment = a;
  }

  // Overlay transient conflict info without mutating committed state
  if (props.submitStatus.kind === 'conflict') {
    const { date, shift, attemptedEmployee } = props.submitStatus.error;
    dateSet.add(date);
    employeeSet.add(attemptedEmployee);
    const cell = ensureCell(attemptedEmployee, date);
    if (shift === 'morning') cell.morningConflict = true;
    else cell.afternoonConflict = true;
  }

  return {
    dates: ([...dateSet] as ShiftDate[]).sort(),
    employees: [...employeeSet].sort(),
    cells,
  };
}

/**
 * Renders the schedule as a semantic HTML table.
 * Returns an overflow-scroll wrapper containing the table.
 * All user-supplied text is set via textContent (safe rendering).
 */
export function createScheduleTable(props: ScheduleTableProps): HTMLElement {
  const wrapper = document.createElement('div');
  wrapper.className = 'overflow-x-auto';

  const { dates, employees, cells } = computeTableData(props);

  const table = document.createElement('table');
  table.className = 'schedule-table';
  table.setAttribute('role', 'table');

  // ── THEAD ────────────────────────────────────────────────────────
  const thead = document.createElement('thead');
  const headerRow = document.createElement('tr');

  const employeeHeader = document.createElement('th');
  employeeHeader.scope = 'col';
  employeeHeader.textContent = 'Ansatt';
  headerRow.appendChild(employeeHeader);

  for (const date of dates) {
    const th = document.createElement('th');
    th.scope = 'col';
    th.textContent = formatShiftDate(date);
    headerRow.appendChild(th);
  }

  thead.appendChild(headerRow);
  table.appendChild(thead);

  // ── TBODY ────────────────────────────────────────────────────────
  const tbody = document.createElement('tbody');

  if (employees.length === 0) {
    const emptyRow = document.createElement('tr');
    const emptyCell = document.createElement('td');
    emptyCell.colSpan = dates.length + 1;
    emptyCell.className = 'empty-state';
    emptyCell.textContent = 'Ingen vakter registrert ennå.';
    emptyRow.appendChild(emptyCell);
    tbody.appendChild(emptyRow);
  } else {
    for (const employee of employees) {
      const tr = document.createElement('tr');

      // Row header cell
      const rowHeader = document.createElement('th');
      rowHeader.scope = 'row';
      rowHeader.textContent = employee;
      tr.appendChild(rowHeader);

      const empMap = cells.get(employee);

      for (const date of dates) {
        const cellData = empMap?.get(date);
        const cellContents: ShiftCellContent[] = [];

        if (cellData) {
          if (cellData.morningConflict) {
            cellContents.push({ variant: 'conflict' });
          } else if (cellData.morningAssignment) {
            cellContents.push({ variant: 'morning' });
          }

          if (cellData.afternoonConflict) {
            cellContents.push({ variant: 'conflict' });
          } else if (cellData.afternoonAssignment) {
            cellContents.push({ variant: 'evening' });
          }
        }

        const td = createShiftCell(cellContents);
        tr.appendChild(td);
      }

      tbody.appendChild(tr);
    }
  }

  table.appendChild(tbody);
  wrapper.appendChild(table);
  return wrapper;
}
