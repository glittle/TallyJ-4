<script setup lang="ts">
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { useNotifications } from "@/composables/useNotifications";
import { useTellerStore } from "@/stores/tellerStore";
import type { Teller, CreateTellerDto, UpdateTellerDto } from "@/types/teller";
import type { FormInstance, FormRules } from "element-plus";
import { reactive, ref, watch } from "vue";
import { useI18n } from "vue-i18n";

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
const { showSuccessMessage } = useNotifications();
const { handleApiError } = useApiErrorHandler();
const tellerStore = useTellerStore();

const formRef = ref<FormInstance>();
const submitting = ref(false);

const form = reactive({
  name: "",
});

const rules: FormRules = {
  name: [
    { required: true, message: t("teller.form.nameRequired"), trigger: "blur" },
    { max: 50, message: t("teller.form.nameMaxLength"), trigger: "blur" },
  ],
};

watch(
  () => props.teller,
  (teller) => {
    if (teller) {
      form.name = teller.name;
    }
  },
  { immediate: true },
);

function resetForm() {
  form.name = "";
}

function handleClose() {
  emit("update:modelValue", false);
  resetForm();
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
      handleClose();
    } catch (error) {
      handleApiError(error);
    } finally {
      submitting.value = false;
    }
  });
}
</script>

<template>
  <el-dialog
    :model-value="modelValue"
    :title="isEdit ? $t('teller.form.titleEdit') : $t('teller.form.titleAdd')"
    width="420px"
    @update:model-value="emit('update:modelValue', $event)"
    @closed="resetForm"
  >
    <el-form ref="formRef" :model="form" :rules="rules" label-width="120px">
      <el-form-item :label="$t('teller.form.name')" prop="name">
        <el-input
          v-model="form.name"
          :placeholder="$t('teller.form.namePlaceholder')"
        />
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
