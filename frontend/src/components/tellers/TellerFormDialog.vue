<script setup lang="ts">
import { ref, reactive, watch } from "vue";
import { type FormInstance, type FormRules } from "element-plus";
import { useI18n } from "vue-i18n";
import { useTellerStore } from "@/stores/tellerStore";
import { useNotifications } from "@/composables/useNotifications";
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import type { Teller, CreateTellerDto, UpdateTellerDto } from "@/types/teller";

const props = defineProps<{
  modelValue: boolean;
  electionGuid: string;
  teller?: Teller | null;
  isEdit?: boolean;
}>();

const emit = defineEmits<{
  "update:modelValue": [value: boolean];
  success: [];
}>();

const { t } = useI18n();
const tellerStore = useTellerStore();
const { showErrorMessage: _showErrorMessage } = useNotifications();
const { handleApiError } = useApiErrorHandler();

const formRef = ref<FormInstance>();
const submitting = ref(false);

const form = reactive({
  name: "",
  usingComputerCode: "",
  isHeadTeller: false,
});

const rules = reactive<FormRules>({
  name: [
    { required: true, message: t("teller.form.nameRequired"), trigger: "blur" },
    { max: 50, message: t("teller.form.nameMaxLength"), trigger: "blur" },
  ],
  usingComputerCode: [
    {
      pattern: /^[A-Z]{2}$/,
      message: t("teller.form.computerCodeInvalid"),
      trigger: "blur",
    },
  ],
});

watch(
  () => props.teller,
  (teller) => {
    if (teller) {
      form.name = teller.name;
      form.usingComputerCode = teller.usingComputerCode || "";
      form.isHeadTeller = teller.isHeadTeller;
    }
  },
  { immediate: true },
);

watch(
  () => props.modelValue,
  (value) => {
    if (!value) {
      resetForm();
    } else if (!props.isEdit) {
      resetForm();
    }
  },
);

function resetForm() {
  if (!props.isEdit) {
    form.name = "";
    form.usingComputerCode = "";
    form.isHeadTeller = false;
  }
}

async function handleSubmit() {
  if (!formRef.value) {
    return;
  }

  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitting.value = true;
      try {
        if (props.isEdit && props.teller) {
          const dto: UpdateTellerDto = {
            name: form.name,
            usingComputerCode: form.usingComputerCode || undefined,
            isHeadTeller: form.isHeadTeller,
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
            usingComputerCode: form.usingComputerCode || undefined,
            isHeadTeller: form.isHeadTeller,
          };
          await tellerStore.createTeller(props.electionGuid, dto);
        }
        emit("success");
      } catch (error) {
        handleApiError(error);
      } finally {
        submitting.value = false;
      }
    }
  });
}

function handleClose() {
  formRef.value?.resetFields();
  emit("update:modelValue", false);
}
</script>

<template>
  <el-dialog
    :model-value="modelValue"
    :title="isEdit ? $t('teller.form.titleEdit') : $t('teller.form.titleAdd')"
    width="600px"
    @update:model-value="$emit('update:modelValue', $event)"
    @close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-width="150px"
      label-position="left"
    >
      <el-form-item :label="$t('teller.form.name')" prop="name">
        <el-input
          v-model="form.name"
          :placeholder="$t('teller.form.namePlaceholder')"
        />
      </el-form-item>

      <el-form-item
        :label="$t('teller.form.computerCode')"
        prop="usingComputerCode"
      >
        <el-input
          v-model="form.usingComputerCode"
          :placeholder="$t('teller.form.computerCodePlaceholder')"
          maxlength="2"
          style="text-transform: uppercase"
        />
        <div class="form-help-text">
          {{ $t("teller.form.computerCodeHelp") }}
        </div>
      </el-form-item>

      <el-form-item :label="$t('teller.form.headTeller')">
        <el-switch v-model="form.isHeadTeller" />
        <div class="form-help-text">{{ $t("teller.form.headTellerHelp") }}</div>
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">{{ $t("teller.form.cancel") }}</el-button>
      <el-button type="primary" :loading="submitting" @click="handleSubmit">
        {{ isEdit ? $t("teller.form.save") : $t("teller.form.create") }}
      </el-button>
    </template>
  </el-dialog>
</template>

<style lang="less">
.form-help-text {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-top: 4px;
}
</style>
