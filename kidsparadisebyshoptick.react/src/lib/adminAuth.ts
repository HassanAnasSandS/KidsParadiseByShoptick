const TOKEN_KEY = 'adminToken';
const REMEMBER_KEY = 'adminRememberMe';
const USERNAME_KEY = 'adminUsername';

function decodeTokenExp(token: string): number | null {
  try {
    const payload = JSON.parse(atob(token.split('.')[1])) as { exp?: number };
    return payload.exp ?? null;
  } catch {
    return null;
  }
}

export function isAdminTokenExpired(token: string): boolean {
  const exp = decodeTokenExp(token);
  if (!exp) return true;
  return Date.now() >= exp * 1000;
}

export function getAdminToken(): string | null {
  const token = sessionStorage.getItem(TOKEN_KEY) ?? localStorage.getItem(TOKEN_KEY);
  if (!token || isAdminTokenExpired(token)) {
    clearAdminToken();
    return null;
  }
  return token;
}

export function setAdminToken(token: string, remember: boolean) {
  clearAdminToken();
  const storage = remember ? localStorage : sessionStorage;
  storage.setItem(TOKEN_KEY, token);
  localStorage.setItem(REMEMBER_KEY, remember ? '1' : '0');
}

export function clearAdminToken() {
  localStorage.removeItem(TOKEN_KEY);
  sessionStorage.removeItem(TOKEN_KEY);
}

export function isAdminLoggedIn() {
  return !!getAdminToken();
}

export function getAdminRememberMe(): boolean {
  return localStorage.getItem(REMEMBER_KEY) !== '0';
}

export function getRememberedUsername(): string {
  return getAdminRememberMe() ? localStorage.getItem(USERNAME_KEY) ?? '' : '';
}

export function setRememberedUsername(username: string, remember: boolean) {
  if (remember) {
    localStorage.setItem(USERNAME_KEY, username);
  } else {
    localStorage.removeItem(USERNAME_KEY);
  }
}
