import { Injectable, signal } from '@angular/core';

/**
 * Holds the current employee's identity for use during development / demo.
 *
 * In production this would be replaced by a real auth service (OAuth2/OIDC)
 * that populates the identity from a JWT or session token.  For now it
 * exposes a writable signal so the demo UI can switch identities without
 * reloading the page.
 */
@Injectable({ providedIn: 'root' })
export class IdentityService {
  /**
   * The current employee identifier.  Defaults to `employee-001` so the app
   * works out-of-the-box during development.  The backend reads this value
   * from the `X-Employee-Id` request header to scope all profile data.
   */
  readonly employeeId = signal<string>('employee-001');
}
