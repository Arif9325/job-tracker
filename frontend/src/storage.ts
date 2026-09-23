// Wraps localStorage (survives closing the browser — "remember me") and
// sessionStorage (cleared when the tab/browser closes) behind one
// interface, so the rest of the app doesn't need to know which one is
// in use. Only one of the two ever holds a token at a time; logging in
// clears the other, so switching "remember me" on/off on a later login
// can't leave a stale token behind in the un-chosen storage.

const TOKEN_KEY = "token";
const EMAIL_KEY = "email";

export function saveAuth(token: string, email: string, remember: boolean): void {
  const store = remember ? localStorage : sessionStorage;
  const other = remember ? sessionStorage : localStorage;

  store.setItem(TOKEN_KEY, token);
  store.setItem(EMAIL_KEY, email);
  other.removeItem(TOKEN_KEY);
  other.removeItem(EMAIL_KEY);
}

export function clearAuth(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(EMAIL_KEY);
  sessionStorage.removeItem(TOKEN_KEY);
  sessionStorage.removeItem(EMAIL_KEY);
}

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY) ?? sessionStorage.getItem(TOKEN_KEY);
}

export function getEmail(): string | null {
  return localStorage.getItem(EMAIL_KEY) ?? sessionStorage.getItem(EMAIL_KEY);
}
