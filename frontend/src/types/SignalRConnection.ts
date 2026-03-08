export const ConnectionState = {
  Disconnected: "Disconnected",
  Connecting: "Connecting",
  Connected: "Connected",
  Reconnecting: "Reconnecting",
} as const;

export type ConnectionState =
  (typeof ConnectionState)[keyof typeof ConnectionState];

export interface SignalRConfig {
  hubUrl: string;
  automaticReconnect?: boolean;
  accessTokenFactory?: () => string | null;
}
