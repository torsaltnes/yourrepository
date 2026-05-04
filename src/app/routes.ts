import { createOverviewPage } from '../views/OverviewPage';
import { createVaktlistePage } from '../views/VaktlistePage';
import { createNotFoundPage } from '../views/NotFoundPage';
import type { PageInstance } from '../views/OverviewPage';

export interface RouteConfig {
  readonly path: string;
  readonly factory: () => PageInstance;
}

export const routes: readonly RouteConfig[] = [
  { path: '/', factory: createOverviewPage },
  { path: '/vaktliste', factory: createVaktlistePage },
];

export function notFoundFactory(): PageInstance {
  return createNotFoundPage();
}
