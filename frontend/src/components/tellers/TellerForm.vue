<script setup lang="ts">
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { useNotifications } from "@/composables/useNotifications";
import { useTellerStore } from "@/stores/tellerStore";
import type { CreateTellerDto, Teller, UpdateTellerDto } from "@/types/teller";
import { type FormInstance, type FormRules, ElMessageBox } from "element-plus";
import { reactive, ref, watch } from "vue";
import { useI18n } from "vue-i18n";

const props = defineProps<{
  electionGuid: string;
  teller?: Teller | null;
  isEdit?: boolean;
  showDelete?: boolean;
}>();

const emit = defineEmits<{
  success: [];
  deleted: [];
  cancel: [];
}>();

const { t } = useI18n();
const { showSuccessMessage } = useNotifications();
const { handleApiError } = useApiErrorHandler();
const tellerStore = useTellerStore();

const formRef = ref<FormInstance>();
const submitting = ref(false);
const deleting = ref(false);

const form = reactive({
  name: "",
});

const rules: FormRules = {
  name: [
    { required: true, message: t("teller.form.nameRequired"), trigger: "blur" },
    { max: 50, message: t("teller.form.nameMaxLength"), trigger: "blur" },
  ],
};

const isEditMode = () => props.isEdit === true;

watch(
  () => props.teller,
  (teller) => {
    if (teller) {
      form.name = teller.name;
    } else if (!props.isEdit) {
      resetForm();
    }
  },
  { immediate: true },
);

function resetForm() {
  form.name = "";
}

async function handleSubmit() {
  if (!formRef.value) {
    return;
  }

  await formRef.value.validate(async (valid) => {
    if (!valid) {
      return;
    }

    submitting.value = true;
    try {
      if (props.isEdit && props.teller) {
        const dto: UpdateTellerDto = {
          name: form.name,
        };
        await tellerStore.updateTeller(
          props.electionGuid,
          props.teller.rowId,
          dto,
        );
      } else {
        const dto: CreateTellerDto = {
          electionGuid: props.electionGuid,
          name: form.name,
        };
        await tellerStore.createTeller(props.electionGuid, dto);
      }
      showSuccessMessage(t("teller.form.saved"));
      emit("success");
    } catch (error) {
      handleApiError(error);
    } finally {
      submitting.value = false;
    }
  });
}

async function handleDelete() {
  if (!props.teller) {
    return;
  }

  try {
    await ElMessageBox.confirm(
      t("teller.confirm.deleteTellerMessage", { name: props.teller.name }),
      t("teller.confirm.deleteTellerTitle"),
      {
        confirmButtonText: t("teller.confirm.delete"),
        cancelButtonText: t("teller.confirm.cancel"),
        type: "warning",
      },
    );

    deleting.value = true;
    await tellerStore.deleteTeller(props.electionGuid, props.teller.rowId);
    showSuccessMessage(t("teller.success.tellerDeleted"));
    emit("deleted");
  } catch (error: unknown) {
    if (error !== "cancel") {
      handleApiError(error);
    }
  } finally {
    deleting.value = false;
  }
}

function handleCancel() {
  formRef.value?.resetFields();
  emit("cancel");
}
</script>

<template>
  <div class="teller-form">
    <el-form ref="formRef" :model="form" :rules="rules" label-width="120px">
      <el-form-item :label="$t('teller.form.name')" prop="name">
        <el-input
          v-model="form.name"
          :placeholder="$t('teller.form.namePlaceholder')"
        />
      </el-form-item>
    </el-form>

    <div class="teller-form-actions">
      <el-button type="primary" :loading="submitting" @click="handleSubmit">
        {{ isEditMode() ? $t("teller.form.save") : $t("teller.form.create") }}
      </el-button>
      <el-button @click="handleCancel">{{
        $t("teller.form.cancel")
      }}</el-button>
    </div>

    <div v-if="isEditMode() && showDelete" class="teller-form-delete">
      <el-button type="danger" :loading="deleting" @click="handleDelete">
        {{ $t("teller.form.delete") }}
      </el-button>
    </div>
  </div>
</template>

<style lang="less">
.teller-form {
  .teller-form-actions {
    display: flex;
    justify-content: space-between;
    gap: var(--spacing-2);
    margin-top: var(--spacing-4);
    padding-top: var(--spacing-4);
    border-top: 1px solid var(--el-border-color-lighter);
  }

  .teller-form-delete {
    margin-top: var(--spacing-6);
    padding-top: var(--spacing-4);
    border-top: 1px solid var(--el-border-color-lighter);
    text-align: right;
  }
}
</style>
