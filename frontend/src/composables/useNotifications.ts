import { ElMessage, ElNotification } from 'element-plus';
import { i18n } from '../locales';

export type NotificationType = 'success' | 'warning' | 'info' | 'error';

export interface NotificationOptions {
  title?: string;
  message: string;
  type?: NotificationType;
  duration?: number;
  showClose?: boolean;
}

export function useNotifications() {
  const { t } = i18n.global;

  const showMessage = (options: NotificationOptions) => {
    const { type = 'info', message, duration = 3000, showClose = true } = options;

    ElMessage({
      message,
      type,
      duration,
      showClose
    });
  };

  const showNotification = (options: NotificationOptions) => {
    const { title, message, type = 'info', duration = 4500, showClose = true } = options;

    ElNotification({
      title: title || getDefaultTitle(type),
      message,
      type,
      duration,
      showClose
    });
  };

  const getDefaultTitle = (type: NotificationType): string => {
    switch (type) {
      case 'success':
        return t('notification.success');
      case 'warning':
        return t('notification.warning');
      case 'error':
        return t('notification.error');
      case 'info':
      default:
        return t('notification.info');
    }
  };

  // Convenience methods
  const success = (message: string, title?: string) => {
    showNotification({ message, type: 'success', title });
  };

  const error = (message: string, title?: string) => {
    showNotification({ message, type: 'error', title, duration: 0, showClose: true });
  };

  const warning = (message: string, title?: string) => {
    showNotification({ message, type: 'warning', title });
  };

  const info = (message: string, title?: string) => {
    showNotification({ message, type: 'info', title });
  };

  // Quick message methods (less intrusive)
  const successMessage = (message: string) => {
    showMessage({ message, type: 'success' });
  };

  const errorMessage = (message: string) => {
    showMessage({ message, type: 'error', duration: 0, showClose: true });
  };

  const warningMessage = (message: string) => {
    showMessage({ message, type: 'warning' });
  };

  const infoMessage = (message: string) => {
    showMessage({ message, type: 'info' });
  };

  return {
    showMessage,
    showNotification,
    success,
    error,
    warning,
    info,
    successMessage,
    errorMessage,
    warningMessage,
    infoMessage
  };
}