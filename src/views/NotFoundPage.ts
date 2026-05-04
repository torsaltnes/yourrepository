import type { PageInstance } from './OverviewPage';

/**
 * 404 Not Found page for unrecognised hash routes.
 */
export function createNotFoundPage(): PageInstance {
  const el = document.createElement('div');
  el.className = 'not-found-container';

  const heading = document.createElement('h1');
  heading.className = 'text-title not-found-title';
  heading.textContent = '404 – Side ikke funnet';

  const para = document.createElement('p');
  para.className = 'not-found-message';
  para.textContent = 'Siden du leter etter finnes ikke.';

  const link = document.createElement('a');
  link.href = '#/';
  link.className = 'btn btn--primary';
  link.textContent = '← Tilbake til oversikt';

  el.appendChild(heading);
  el.appendChild(para);
  el.appendChild(link);

  return { element: el };
}
