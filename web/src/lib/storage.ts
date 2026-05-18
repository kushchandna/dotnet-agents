const ONE_YEAR_SECONDS = 60 * 60 * 24 * 365;

export function getCookie(name: string): string | null {
  if (typeof document === 'undefined') return null;
  const target = `${encodeURIComponent(name)}=`;
  for (const part of document.cookie.split(';')) {
    const trimmed = part.trim();
    if (trimmed.startsWith(target)) {
      return decodeURIComponent(trimmed.slice(target.length));
    }
  }
  return null;
}

export function setCookie(name: string, value: string, maxAgeSeconds: number = ONE_YEAR_SECONDS): void {
  if (typeof document === 'undefined') return;
  document.cookie =
    `${encodeURIComponent(name)}=${encodeURIComponent(value)}` +
    `; path=/; max-age=${maxAgeSeconds}; samesite=lax`;
}

export function deleteCookie(name: string): void {
  if (typeof document === 'undefined') return;
  document.cookie = `${encodeURIComponent(name)}=; path=/; max-age=0; samesite=lax`;
}
