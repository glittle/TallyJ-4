<script setup lang="ts">
import { useComputerCode } from "@/composables/useComputerCode";
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { useNotifications } from "@/composables/useNotifications";
import { getActiveTellerPayload } from "@/utils/activeTellerStorage";
import { type FormInstance, type FormRules } from "element-plus";
import { reactive, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useBallotStore } from "../../stores/ballotStore";
import type { CreateBallotDto } from "../../types";

const { showSuccessMessage } = useNotifications();
const { handleApiError } = useApiErrorHandler();

const props = defineProps<{
  modelValue: boolean;
  electionGuid: string;
}>();

const emit = defineEmits<{
  "update:modelValue": [value: boolean];
  success: [];
}>();

const { t } = useI18n();
const ballotStore = useBallotStore();
const { computerCode } = useComputerCode(props.electionGuid);

const formRef = ref<FormInstance>();
const submitting = ref(false);

const form = reactive({
  locationGuid: "",
});

const locations = ref([
  { value: "00000000-0000-0000-0000-000000000001", label: "Main Hall" },
  { value: "00000000-0000-0000-0000-000000000002", label: "Room A" },
  { value: "00000000-0000-0000-0000-000000000003", label: "Room B" },
]);

const rules = reactive<FormRules>({
  locationGuid: [
    {
      required: true,
      message: t("ballots.locationRequired"),
      trigger: "change",
    },
  ],
});

async function handleSubmit() {
  if (!formRef.value) {
    return;
  }

  if (!computerCode.value) {
    handleApiError(new Error(t("ballots.computerCodeRequired")));
    return;
  }

  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitting.value = true;
      try {
        const dto: CreateBallotDto = {
          electionGuid: props.electionGuid,
          locationGuid: form.locationGuid,
          computerCode: computerCode.value,
          ...getActiveTellerPayload(),
          statusCode: "Ok",
        };
        await ballotStore.createBallot(dto);
        showSuccessMessage(t("ballots.createSuccess"));
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
    :title="$t('ballots.addBallot')"
    width="500px"
    @update:model-value="$emit('update:modelValue', $event)"
    @close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-width="120px"
      label-position="left"
    >
      <el-form-item :label="$t('ballots.location')" prop="locationGuid">
        <el-select v-model="form.locationGuid" style="width: 100%">
          <el-option
            v-for="location in locations"
            :key="location.value"
            :label="location.label"
            :value="location.value"
          />
        </el-select>
      </el-form-item>

      <el-form-item :label="$t('ballots.computer')">
        <span class="assigned-computer-code">
          {{
            computerCode
              ? $t("ballots.computerCodeShort", { code: computerCode })
              : $t("ballots.computerCodeUnset")
          }}
        </span>
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">{{ $t("common.cancel") }}</el-button>
      <el-button type="primary" :loading="submitting" @click="handleSubmit">
        {{ $t("common.create") }}
      </el-button>
    </template>
  </el-dialog>
</template>

<style lang="less">
.assigned-computer-code {
  font-weight: var(--font-weight-medium, 500);
  letter-spacing: 0.04em;
}
</style>