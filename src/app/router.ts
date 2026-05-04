import { routes, notFoundFactory } from './routes';
import type { PageInstance } from '../views/OverviewPage';

/**
 * Hash-based router.
 * - Empty hash  -> normalizes to #/ (via replaceState, no extra event)
 * - #/          -> OverviewPage
 * - #/vaktliste -> VaktlistePage
 * - unknown     -> NotFoundPage
 */
export class Router {
  private currentPage: PageInstance | null = null;

  constructor(
    private readonly container: HTMLElement,
    private readonly onNavigate?: (path: string) => void,
  ) {}

  start(): void {
    window.addEventListener('hashchange', () => { this.handleNavigation(); });
    this.handleNavigation();
  }

  navigate(path: string): void {
    history.pushState(null, '', `#${path}`);
    this.handleNavigation();
  }

  getCurrentPath(): string {
    return this.parsePath(window.location.hash);
  }

  private parsePath(hash: string): string {
    if (!hash || hash === '#') return '/';
    return hash.slice(1); // '#/vaktliste' -> '/vaktliste'
  }

  private handleNavigation(): void {
    const rawHash = window.location.hash;

    // Normalise empty or bare '#' to '#/' without firing another hashchange
    if (!rawHash || rawHash === '#') {
      history.replaceState(null, '', '#/');
    }

    const path = this.parsePath(window.location.hash);
    this.mountPage(path);
    this.onNavigate?.(path);
  }

  private mountPage(path: string): void {
    if (this.currentPage?.destroy) {
      this.currentPage.destroy();
    }

    const route = routes.find((r) => r.path === path);
    const factory = route ? route.factory : notFoundFactory;

    this.currentPage = factory();
    this.container.innerHTML = '';
    this.container.appendChild(this.currentPage.element);
  }
}
