/**
 * Error alert component.
 * Uses textContent for safe rendering of user/system messages.
 */
export function createErrorAlert(message: string): HTMLElement {
  const div = document.createElement('div');
  div.className = 'error-alert';
  div.setAttribute('role', 'alert');
  div.setAttribute('aria-live', 'assertive');

  // Circle icon (inline SVG, static markup – no user data)
  const iconWrapper = document.createElement('span');
  iconWrapper.className = 'error-alert__icon';
  iconWrapper.setAttribute('aria-hidden', 'true');
  iconWrapper.innerHTML =
    '<svg viewBox="0 0 20 20" fill="currentColor" width="20" height="20" aria-hidden="true">' +
    '<path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-8-5a.75.75 0 01.75.75v4.5a.75.75 0 01-1.5 0v-4.5A.75.75 0 0110 5zm0 10a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd"/>' +
    '</svg>';

  const text = document.createElement('span');
  text.textContent = message; // safe textContent

  div.appendChild(iconWrapper);
  div.appendChild(text);
  return div;
}

/**
 * Updates or hides an existing error alert.
 * Pass null/empty to remove the message.
 */
export function updateErrorAlert(container: HTMLElement, message: string | null): void {
  container.innerHTML = '';
  if (message) {
    container.appendChild(createErrorAlert(message));
  }
}
