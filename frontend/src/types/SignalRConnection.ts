export enum ConnectionState {
  Disconnected = 'Disconnected',
  Connecting = 'Connecting',
  Connected = 'Connected',
  Reconnecting = 'Reconnecting'
}

export interface SignalRConfig {
  hubUrl: string;
  automaticReconnect?: boolean;
  accessTokenFactory?: () => string | null;
}
