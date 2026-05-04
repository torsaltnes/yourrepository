/**
 * Application shell: sticky header with title + nav, plus content region.
 * The router mounts page content into contentEl.
 */
export class AppShell {
  readonly element: HTMLElement;
  readonly contentEl: HTMLElement;
  private readonly navLinks: Map<string, HTMLAnchorElement> = new Map();

  constructor() {
    this.element = document.createElement('div');
    this.element.className = 'flex flex-col min-h-screen';

    const header = this.buildHeader();
    this.contentEl = document.createElement('main');
    this.contentEl.className = 'app-content';

    this.element.appendChild(header);
    this.element.appendChild(this.contentEl);
  }

  /** Highlight the nav link matching the current path */
  updateNavHighlight(path: string): void {
    for (const [linkPath, anchor] of this.navLinks.entries()) {
      if (linkPath === path) {
        anchor.classList.add('nav-link--active');
        anchor.setAttribute('aria-current', 'page');
      } else {
        anchor.classList.remove('nav-link--active');
        anchor.removeAttribute('aria-current');
      }
    }
  }

  private buildHeader(): HTMLElement {
    const header = document.createElement('header');
    header.className = 'app-header';

    const titleLink = document.createElement('a');
    titleLink.href = '#/';
    titleLink.className = 'app-title';
    titleLink.textContent = 'Bemanningssystem';

    const nav = document.createElement('nav');
    nav.className = 'app-nav';
    nav.setAttribute('aria-label', 'Navigasjon');

    const navItems: Array<{ label: string; path: string }> = [
      { label: 'Oversikt', path: '/' },
      { label: 'Vaktliste', path: '/vaktliste' },
    ];

    for (const item of navItems) {
      const a = document.createElement('a');
      a.href = `#${item.path}`;
      a.className = 'nav-link';
      a.textContent = item.label;
      this.navLinks.set(item.path, a);
      nav.appendChild(a);
    }

    header.appendChild(titleLink);
    header.appendChild(nav);
    return header;
  }
}
