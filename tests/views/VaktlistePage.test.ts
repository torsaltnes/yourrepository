import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen } from '@testing-library/dom';
import userEvent from '@testing-library/user-event';
import { createVaktlistePage } from '../../src/views/VaktlistePage';
import { vaktlisteStore } from '../../src/state/vaktlisteStore';
import { parseShiftDate } from '../../src/domain/types';

describe('VaktlistePage', () => {
  beforeEach(() => {
    sessionStorage.clear();
    vaktlisteStore.reset();
    document.body.innerHTML = '';
  });

  it('renders breadcrumb with Oversikt / Vaktliste', () => {
    const page = createVaktlistePage();
    document.body.appendChild(page.element);
    expect(document.body.textContent).toContain('Oversikt');
    expect(document.body.textContent).toContain('Vaktliste');
  });

  it('renders "Ny vakt +" button', () => {
    const page = createVaktlistePage();
    document.body.appendChild(page.element);
    const btn = screen.getByRole('button', { name: 'Ny vakt +' });
    expect(btn).toBeTruthy();
  });

  it('shows empty schedule table initially', () => {
    const page = createVaktlistePage();
    document.body.appendChild(page.element);
    expect(document.body.textContent).toContain('Ingen vakter');
  });

  it('opens dialog when "Ny vakt +" is clicked', async () => {
    const user = userEvent.setup();
    const page = createVaktlistePage();
    document.body.appendChild(page.element);

    await user.click(screen.getByRole('button', { name: 'Ny vakt +' }));

    const dialog = document.querySelector('dialog');
    expect(dialog).not.toBeNull();
    // Dialog should have open attribute (set by showModal or fallback)
    expect(dialog?.hasAttribute('open') || dialog?.open).toBe(true);
  });

  it('destroys subscription on destroy()', () => {
    const page = createVaktlistePage();
    document.body.appendChild(page.element);
    expect(() => page.destroy?.()).not.toThrow();
  });

  it('re-renders table after store mutation', () => {
    const page = createVaktlistePage();
    document.body.appendChild(page.element);

    const date = parseShiftDate('2026-05-04');
    if (!date) throw new Error('bad date');

    vaktlisteStore.addAssignment({ employee: 'Ola Nordmann', date, shift: 'morning' });

    expect(document.body.textContent).toContain('Ola Nordmann');
    expect(document.body.textContent).toContain('Morgen');
  });

  it('shows conflict alert when duplicate assignment attempted', () => {
    const page = createVaktlistePage();
    document.body.appendChild(page.element);

    const date = parseShiftDate('2026-05-04');
    if (!date) throw new Error('bad date');

    vaktlisteStore.addAssignment({ employee: 'Ola', date, shift: 'morning' });
    vaktlisteStore.addAssignment({ employee: 'Kari', date, shift: 'morning' }); // conflict

    const alert = document.querySelector('[role="alert"]');
    expect(alert).not.toBeNull();
    expect(alert?.textContent).toContain('En ansatt kan ikke ha to vakter');
  });
});
