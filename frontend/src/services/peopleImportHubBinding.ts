export interface PeopleImportHubHandlers {
  onProgress: (data: unknown) => void;
  onError: (message: string) => void;
  onComplete: () => void;
}

interface HubConnectionLike {
  on: (event: string, handler: (payload?: unknown) => void) => void;
}

let bindPromise: Promise<void> | null = null;
let listener: PeopleImportHubHandlers | null = null;

export function setPeopleImportHubListener(
  next: PeopleImportHubHandlers | null,
): void {
  listener = next;
}

/** Register hub handlers once. Later visits only replace the listener. */
export function ensurePeopleImportHubBound(
  connect: () => Promise<HubConnectionLike>,
): Promise<void> {
  if (!bindPromise) {
    bindPromise = connect()
      .then((connection) => {
        connection.on("importProgress", (data) => {
          listener?.onProgress(data);
        });
        connection.on("importError", (message) => {
          listener?.onError(typeof message === "string" ? message : "");
        });
        connection.on("importComplete", () => {
          listener?.onComplete();
        });
      })
      .catch((error: unknown) => {
        bindPromise = null;
        throw error;
      });
  }
  return bindPromise;
}

export function resetPeopleImportHubBindingForTests(): void {
  bindPromise = null;
  listener = null;
}
