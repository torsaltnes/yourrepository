import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { IdentityService } from '../services/identity.service';

/**
 * HTTP interceptor that attaches the current employee's identity as the
 * `X-Employee-Id` request header on every outgoing API call.
 *
 * The backend's `RequireUserIdentityFilter` uses this header to:
 *   1. Authenticate the caller (OWASP A01 gate).
 *   2. Scope profile data to the correct employee (IDOR prevention).
 *
 * When real auth (JWT/OIDC) is introduced, replace this interceptor with one
 * that attaches a `Bearer` token; the backend already prefers
 * `HttpContext.User.Identity.Name` from claims over the header.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const identity = inject(IdentityService);
  const employeeId = identity.employeeId();

  if (!employeeId) {
    return next(req);
  }

  const authedReq = req.clone({
    setHeaders: { 'X-Employee-Id': employeeId },
  });

  return next(authedReq);
};
