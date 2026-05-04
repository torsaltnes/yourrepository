import { vaktlisteStore } from '../state/vaktlisteStore';
import { parseShiftDate } from '../domain/types';
import { createScheduleTable } from '../components/ScheduleTable';
import { createScheduleLegend } from '../components/ScheduleLegend';
import { createShiftAssignmentDialog } from '../components/ShiftAssignmentDialog';
import { updateErrorAlert } from '../components/ErrorAlert';
import type { PageInstance } from './OverviewPage';
import type { DialogSubmitPayload } from '../components/ShiftAssignmentDialog';

/**
 * Vaktliste page – main scheduling UI.
 * Subscribes to vaktlisteStore; re-renders table and feedback on state change.
 */
export function createVaktlistePage(): PageInstance {
  const el = document.createElement('div');

  // ── Breadcrumb ────────────────────────────────────────────────────
  const breadcrumb = document.createElement('nav');
  breadcrumb.className = 'page-breadcrumb';
  breadcrumb.setAttribute('aria-label', 'Brødsmulesti');

  const overviewLink = document.createElement('a');
  overviewLink.href = '#/';
  overviewLink.textContent = 'Oversikt';

  const sep = document.createTextNode(' / ');

  const current = document.createElement('span');
  current.textContent = 'Vaktliste';

  breadcrumb.appendChild(overviewLink);
  breadcrumb.appendChild(sep);
  breadcrumb.appendChild(current);

  // ── Page header ──────────────────────────────────────────────────
  const pageHeader = document.createElement('div');
  pageHeader.className = 'vaktliste-header';

  const heading = document.createElement('h1');
  heading.className = 'text-title vaktliste-title';
  heading.textContent = 'Vaktplan: Avdeling Sør';

  const legend = createScheduleLegend();

  pageHeader.appendChild(heading);
  pageHeader.appendChild(legend);

  // ── Table container ───────────────────────────────────────────────
  const tableContainer = document.createElement('div');

  // ── Date validation error (form-level) ───────────────────────────
  const formValidationEl = document.createElement('div');
  formValidationEl.setAttribute('aria-live', 'polite');

  // ── Footer ────────────────────────────────────────────────────────
  const footer = document.createElement('div');
  footer.className = 'vaktliste-footer';

  const newShiftBtn = document.createElement('button');
  newShiftBtn.type = 'button';
  newShiftBtn.className = 'btn btn--primary';
  newShiftBtn.textContent = 'Ny vakt +';
  newShiftBtn.setAttribute('aria-label', 'Ny vakt +');

  const alertContainer = document.createElement('div');

  footer.appendChild(newShiftBtn);
  footer.appendChild(alertContainer);

  // ── Dialog ────────────────────────────────────────────────────────
  const dialogInstance = createShiftAssignmentDialog(handleDialogSubmit);

  function handleDialogSubmit(payload: DialogSubmitPayload): void {
    const date = parseShiftDate(payload.rawDate);
    if (!date) {
      updateErrorAlert(
        alertContainer,
        `Ugyldig dato "${payload.rawDate}". Bruk format ÅÅÅÅ-MM-DD.`,
      );
      return;
    }
    updateErrorAlert(alertContainer, null); // clear previous error
    vaktlisteStore.addAssignment({
      employee: payload.employee,
      date,
      shift: payload.shift,
    });
  }

  newShiftBtn.addEventListener('click', () => {
    dialogInstance.open();
  });

  // ── Render helpers ────────────────────────────────────────────────
  function renderTable(): void {
    const snapshot = vaktlisteStore.getSnapshot();
    const status = vaktlisteStore.getSubmitStatus();

    const newTable = createScheduleTable({
      assignments: snapshot.assignments,
      submitStatus: status,
    });

    tableContainer.innerHTML = '';
    tableContainer.appendChild(newTable);
  }

  function renderFeedback(): void {
    const status = vaktlisteStore.getSubmitStatus();

    if (status.kind === 'conflict') {
      updateErrorAlert(alertContainer, status.error.message);
    } else if (status.kind === 'persistence_error') {
      updateErrorAlert(
        alertContainer,
        `Lagringsfeil: ${status.error.message}`,
      );
    } else if (status.kind === 'success') {
      updateErrorAlert(alertContainer, null);
    }
    // 'idle' – leave existing message in place (cleared on next success)
  }

  // ── Subscribe to store ────────────────────────────────────────────
  const unsubscribe = vaktlisteStore.subscribe(() => {
    renderTable();
    renderFeedback();
  });

  // ── Initial render ────────────────────────────────────────────────
  renderTable();
  renderFeedback();

  // ── Assemble ──────────────────────────────────────────────────────
  el.appendChild(breadcrumb);
  el.appendChild(pageHeader);
  el.appendChild(tableContainer);
  el.appendChild(formValidationEl);
  el.appendChild(footer);
  el.appendChild(dialogInstance.dialog);

  return {
    element: el,
    destroy(): void {
      unsubscribe();
    },
  };
}
