import { SEED_EMPLOYEES } from '../services/seedData';
import { formatShiftType } from '../utils/dateFormatting';
import type { ShiftType } from '../domain/types';

export interface DialogSubmitPayload {
  readonly employee: string;
  readonly shift: ShiftType;
  readonly rawDate: string;
}

export interface ShiftAssignmentDialogInstance {
  readonly dialog: HTMLDialogElement;
  open(): void;
  close(): void;
}

/**
 * Creates a native <dialog> element for new shift assignment.
 * Calls onSubmit with raw (unvalidated) form values; date validation
 * must happen at the view boundary via parseShiftDate().
 * All text rendered via textContent (safe rendering).
 */
export function createShiftAssignmentDialog(
  onSubmit: (payload: DialogSubmitPayload) => void,
): ShiftAssignmentDialogInstance {
  const dialog = document.createElement('dialog');
  dialog.setAttribute('aria-modal', 'true');
  dialog.setAttribute('aria-labelledby', 'dialog-title');

  // ── Header ──────────────────────────────────────────────────────
  const header = document.createElement('div');
  header.className = 'dialog-header';
  const title = document.createElement('h2');
  title.id = 'dialog-title';
  title.textContent = 'Ny vakt';
  header.appendChild(title);

  // ── Body / Form ─────────────────────────────────────────────────
  const body = document.createElement('div');
  body.className = 'dialog-body';

  // Employee field
  const empGroup = document.createElement('div');
  empGroup.className = 'form-group';
  const empLabel = document.createElement('label');
  empLabel.htmlFor = 'dlg-employee';
  empLabel.className = 'form-label';
  empLabel.textContent = 'Ansatt';
  const empInput = document.createElement('input');
  empInput.type = 'text';
  empInput.id = 'dlg-employee';
  empInput.name = 'employee';
  empInput.className = 'form-input';
  empInput.placeholder = 'Navn på ansatt';
  empInput.setAttribute('list', 'employee-suggestions');
  empInput.required = true;
  empInput.autocomplete = 'off';

  const datalist = document.createElement('datalist');
  datalist.id = 'employee-suggestions';
  for (const name of SEED_EMPLOYEES) {
    const option = document.createElement('option');
    option.value = name;
    datalist.appendChild(option);
  }

  const empError = document.createElement('span');
  empError.className = 'form-error';
  empError.setAttribute('role', 'alert');
  empError.setAttribute('aria-live', 'polite');

  empGroup.appendChild(empLabel);
  empGroup.appendChild(empInput);
  empGroup.appendChild(datalist);
  empGroup.appendChild(empError);

  // Shift type field
  const shiftGroup = document.createElement('div');
  shiftGroup.className = 'form-group';
  const shiftLabel = document.createElement('label');
  shiftLabel.htmlFor = 'dlg-shift';
  shiftLabel.className = 'form-label';
  shiftLabel.textContent = 'Vakttype';
  const shiftSelect = document.createElement('select');
  shiftSelect.id = 'dlg-shift';
  shiftSelect.name = 'shift';
  shiftSelect.className = 'form-select';
  shiftSelect.required = true;

  const shiftTypes: ShiftType[] = ['morning', 'afternoon'];
  for (const t of shiftTypes) {
    const opt = document.createElement('option');
    opt.value = t;
    opt.textContent = formatShiftType(t);
    shiftSelect.appendChild(opt);
  }

  shiftGroup.appendChild(shiftLabel);
  shiftGroup.appendChild(shiftSelect);

  // Date field
  const dateGroup = document.createElement('div');
  dateGroup.className = 'form-group';
  const dateLabel = document.createElement('label');
  dateLabel.htmlFor = 'dlg-date';
  dateLabel.className = 'form-label';
  dateLabel.textContent = 'Dato (ÅÅÅÅ-MM-DD)';
  const dateInput = document.createElement('input');
  dateInput.type = 'date';
  dateInput.id = 'dlg-date';
  dateInput.name = 'date';
  dateInput.className = 'form-input';
  dateInput.required = true;

  const dateError = document.createElement('span');
  dateError.className = 'form-error';
  dateError.setAttribute('role', 'alert');
  dateError.setAttribute('aria-live', 'polite');

  dateGroup.appendChild(dateLabel);
  dateGroup.appendChild(dateInput);
  dateGroup.appendChild(dateError);

  body.appendChild(empGroup);
  body.appendChild(shiftGroup);
  body.appendChild(dateGroup);

  // ── Footer ───────────────────────────────────────────────────────
  const footer = document.createElement('div');
  footer.className = 'dialog-footer';

  const cancelBtn = document.createElement('button');
  cancelBtn.type = 'button';
  cancelBtn.className = 'btn btn--secondary';
  cancelBtn.textContent = 'Avbryt';

  const submitBtn = document.createElement('button');
  submitBtn.type = 'submit';
  submitBtn.className = 'btn btn--primary';
  submitBtn.textContent = 'Legg til vakt';

  footer.appendChild(cancelBtn);
  footer.appendChild(submitBtn);

  // ── Form wrapper ─────────────────────────────────────────────────
  const form = document.createElement('form');
  form.noValidate = true;
  form.appendChild(body);
  form.appendChild(footer);

  // ── Assemble ──────────────────────────────────────────────────────
  dialog.appendChild(header);
  dialog.appendChild(form);

  // ── Event handlers ────────────────────────────────────────────────
  function clearErrors(): void {
    empError.textContent = '';
    dateError.textContent = '';
  }

  function resetForm(): void {
    empInput.value = '';
    shiftSelect.value = 'morning';
    dateInput.value = '';
    clearErrors();
  }

  function validateForm(): boolean {
    let valid = true;
    clearErrors();

    if (empInput.value.trim() === '') {
      empError.textContent = 'Ansattnavn er påkrevd.';
      valid = false;
    }

    if (dateInput.value === '') {
      dateError.textContent = 'Dato er påkrevd.';
      valid = false;
    }

    return valid;
  }

  cancelBtn.addEventListener('click', () => {
    resetForm();
    dialog.close();
  });

  form.addEventListener('submit', (e) => {
    e.preventDefault();
    if (!validateForm()) return;

    const shift = shiftSelect.value as ShiftType;
    onSubmit({
      employee: empInput.value.trim(),
      shift,
      rawDate: dateInput.value,
    });
    resetForm();
    dialog.close();
  });

  // Close on backdrop click
  dialog.addEventListener('click', (e) => {
    if (e.target === dialog) {
      resetForm();
      dialog.close();
    }
  });

  function open(): void {
    resetForm();
    if (typeof dialog.showModal === 'function') {
      dialog.showModal();
    } else {
      dialog.setAttribute('open', '');
    }
  }

  function close(): void {
    dialog.close();
  }

  return { dialog, open, close };
}
