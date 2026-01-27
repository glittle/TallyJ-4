import { ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';

export interface ConfirmOptions {
  title?: string;
  message: string;
  confirmButtonText?: string;
  cancelButtonText?: string;
  type?: 'success' | 'info' | 'warning' | 'error';
  showCancelButton?: boolean;
  distinguishCancelAndClose?: boolean;
}

export function useConfirmDialog() {
  const { t } = useI18n();

  const confirm = async (options: ConfirmOptions): Promise<boolean> => {
    try {
      await ElMessageBox.confirm(
        options.message,
        options.title || t('common.confirm'),
        {
          confirmButtonText: options.confirmButtonText || t('common.confirm'),
          cancelButtonText: options.cancelButtonText || t('common.cancel'),
          type: options.type || 'warning',
          showCancelButton: options.showCancelButton !== false,
          distinguishCancelAndClose: options.distinguishCancelAndClose || false,
          customClass: 'confirm-dialog'
        }
      );
      return true;
    } catch {
      return false;
    }
  };

  // Convenience methods for common use cases
  const confirmDelete = async (itemName: string): Promise<boolean> => {
    return confirm({
      title: t('common.delete'),
      message: t('common.confirmDelete', { item: itemName }),
      type: 'warning',
      confirmButtonText: t('common.delete')
    });
  };

  const confirmAction = async (action: string, itemName?: string): Promise<boolean> => {
    const message = itemName
      ? t('common.confirmActionWithItem', { action, item: itemName })
      : t('common.confirmAction', { action });

    return confirm({
      title: t('common.confirm'),
      message,
      type: 'warning'
    });
  };

  return {
    confirm,
    confirmDelete,
    confirmAction
  };
}