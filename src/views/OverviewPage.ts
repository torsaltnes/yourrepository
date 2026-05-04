export interface PageInstance {
  readonly element: HTMLElement;
  destroy?(): void;
}

/**
 * Overview / landing page.
 * Static content; no store subscription needed.
 */
export function createOverviewPage(): PageInstance {
  const el = document.createElement('div');
  el.className = 'overview-hero';

  const heading = document.createElement('h1');
  heading.className = 'text-title overview-title';
  heading.textContent = 'Bemanningssystem';

  const description = document.createElement('p');
  description.className = 'overview-description';
  description.textContent =
    'Planlegg og administrer vakter for ansatte. ' +
    'Tildel morgen- og ettermiddagsvakter for spesifikke datoer, ' +
    'og unngå dobbeltbooking av skiftplassen.';

  const cta = document.createElement('a');
  cta.href = '#/vaktliste';
  cta.className = 'btn btn--primary overview-cta';
  cta.textContent = 'Åpne Vaktliste →';

  el.appendChild(heading);
  el.appendChild(description);
  el.appendChild(cta);

  return { element: el };
}
