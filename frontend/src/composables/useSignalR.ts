import { onUnmounted, ref, computed } from 'vue';
import { signalrService } from '@/services/signalrService';
import { ConnectionState } from '@/types/SignalRConnection';
import type { HubConnection } from '@microsoft/signalr';

export function useSignalR(hubPath: string) {
  const connection = ref<HubConnection | undefined>(signalrService.getConnection(hubPath));
  const connectionState = ref<ConnectionState>(signalrService.getConnectionState(hubPath));

  const isConnected = computed(() => connectionState.value === ConnectionState.Connected);
  const isConnecting = computed(() => connectionState.value === ConnectionState.Connecting);
  const isDisconnected = computed(() => connectionState.value === ConnectionState.Disconnected);
  const isReconnecting = computed(() => connectionState.value === ConnectionState.Reconnecting);

  function on(eventName: string, callback: (...args: any[]) => void) {
    connection.value?.on(eventName, callback);
  }

  function off(eventName: string, callback?: (...args: any[]) => void) {
    if (callback) {
      connection.value?.off(eventName, callback);
    } else {
      connection.value?.off(eventName);
    }
  }

  async function invoke(methodName: string, ...args: any[]) {
    if (!connection.value) {
      throw new Error(`SignalR connection for ${hubPath} is not established`);
    }
    return connection.value.invoke(methodName, ...args);
  }

  async function connect(accessToken?: string) {
    try {
      connection.value = await signalrService.connect(hubPath, accessToken);
      connectionState.value = signalrService.getConnectionState(hubPath);
    } catch (error) {
      console.error(`Failed to connect to ${hubPath}:`, error);
      throw error;
    }
  }

  async function disconnect() {
    await signalrService.disconnect(hubPath);
    connection.value = undefined;
    connectionState.value = ConnectionState.Disconnected;
  }

  onUnmounted(() => {
  });

  return {
    connection,
    connectionState,
    isConnected,
    isConnecting,
    isDisconnected,
    isReconnecting,
    on,
    off,
    invoke,
    connect,
    disconnect
  };
}

export function useMainHub() {
  return useSignalR('/hubs/main');
}

export function useAnalyzeHub() {
  return useSignalR('/hubs/analyze');
}

export function useBallotImportHub() {
  return useSignalR('/hubs/ballot-import');
}

export function useFrontDeskHub() {
  return useSignalR('/hubs/front-desk');
}

export function usePublicHub() {
  return useSignalR('/hubs/public');
}
