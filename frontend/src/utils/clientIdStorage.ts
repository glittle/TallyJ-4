const CLIENT_ID_KEY = "tallyj.clientId";

function createClientId(): string {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }

  return `client-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

export function getOrCreateClientId(): string {
  const existing = (localStorage.getItem(CLIENT_ID_KEY) ?? "").trim();
  if (existing) {
    return existing;
  }

  const created = createClientId();
  localStorage.setItem(CLIENT_ID_KEY, created);
  return created;
}
