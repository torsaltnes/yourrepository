import { beforeEach, afterEach, vi } from 'vitest';
import { vaktlisteStore } from '../state/vaktlisteStore';

beforeEach(() => {
  // Start each test with clean sessionStorage and empty store
  sessionStorage.clear();
  vaktlisteStore.reset();
  document.body.innerHTML = '';
});

afterEach(() => {
  vi.restoreAllMocks();
  document.body.innerHTML = '';
});
