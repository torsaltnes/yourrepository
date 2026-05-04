import { describe, it, expect, beforeEach } from 'vitest';
import { Router } from '../../src/app/router';

describe('Router', () => {
  let container: HTMLDivElement;
  let router: Router;
  let navigatedPaths: string[];

  beforeEach(() => {
    // Reset to clean URL to prevent hash bleeding between tests
    history.replaceState(null, '', '/');
    container = document.createElement('div');
    document.body.appendChild(container);
    navigatedPaths = [];
    router = new Router(container, (p) => { navigatedPaths.push(p); });
  });

  function setHash(hash: string): void {
    if (hash === '' || hash === '#') {
      // Clear hash by navigating to bare path
      history.replaceState(null, '', '/');
    } else {
      history.replaceState(null, '', hash);
    }
  }

  it('renders overview page for #/', () => {
    setHash('#/');
    router.start();
    expect(container.textContent).toContain('Bemanningssystem');
  });

  it('renders vaktliste page for #/vaktliste', () => {
    setHash('#/vaktliste');
    router.start();
    expect(container.textContent).toContain('Vaktliste');
  });

  it('renders 404 page for unknown route', () => {
    setHash('#/ukjent');
    router.start();
    expect(container.textContent).toContain('404');
  });

  it('normalizes empty hash (no hash in URL) to #/', () => {
    // URL has no hash at all
    setHash('');
    router.start();
    expect(navigatedPaths[0]).toBe('/');
  });

  it('normalizes bare # to #/', () => {
    setHash('#');
    router.start();
    expect(navigatedPaths[0]).toBe('/');
  });

  it('getCurrentPath returns current path', () => {
    setHash('#/vaktliste');
    router.start();
    expect(router.getCurrentPath()).toBe('/vaktliste');
  });

  it('navigate() updates to overview page', () => {
    setHash('#/vaktliste');
    router.start();
    router.navigate('/');
    expect(container.textContent).toContain('Bemanningssystem');
  });

  it('calls onNavigate callback with correct path', () => {
    setHash('#/vaktliste');
    router.start();
    expect(navigatedPaths).toContain('/vaktliste');
  });
});
