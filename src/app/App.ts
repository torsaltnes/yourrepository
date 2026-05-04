import { AppShell } from './shell/AppShell';
import { Router } from './router';

/**
 * Bootstrap the application into the given root element.
 * Wires shell + router together.
 */
export function createApp(rootEl: HTMLElement): void {
  const shell = new AppShell();
  rootEl.appendChild(shell.element);

  const router = new Router(shell.contentEl, (path) => {
    shell.updateNavHighlight(path);
  });

  router.start();
}
