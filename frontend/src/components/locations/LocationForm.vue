<script setup lang="ts">
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { useNotifications } from "@/composables/useNotifications";
import { type FormInstance, type FormRules, ElMessageBox } from "element-plus";
import { computed, reactive, ref, watch } from "vue";
import { isOnlineLocationType } from "@/utils/ballotStartRequirements";
import { formatLocationLabel } from "@/utils/locationDisplay";
import { useI18n } from "vue-i18n";
import { useLocationStore } from "../../stores/locationStore";
import type {
  CreateLocationDto,
  LocationDto,
  UpdateLocationDto,
} from "../../types";

const props = defineProps<{
  electionGuid: string;
  location?: LocationDto | null;
  isEdit?: boolean;
  showDelete?: boolean;
}>();

const emit = defineEmits<{
  success: [];
  deleted: [];
  cancel: [];
}>();

const { t } = useI18n();
const locationStore = useLocationStore();
const { showSuccessMessage } = useNotifications();
const { handleApiError } = useApiErrorHandler();

const formRef = ref<FormInstance>();
const submitting = ref(false);
const deleting = ref(false);
const isOnlineLocation = computed(() =>
  isOnlineLocationType(props.location?.locationType),
);
const canDelete = computed(
  () => Boolean(props.showDelete) && !isOnlineLocation.value,
);
const onlineDisplayName = computed(() =>
  props.location ? formatLocationLabel(t, props.location) : t("locations.typeOnline"),
);

const form = reactive({
  name: "",
  contactInfo: "",
  longitude: "",
  latitude: "",
  sortOrder: 0,
});

const rules = reactive<FormRules>({
  name: [
    {
      required: true,
      message: t("locations.form.nameRequired"),
      trigger: "blur",
    },
    { max: 50, message: t("locations.form.nameMaxLength"), trigger: "blur" },
  ],
  contactInfo: [
    {
      max: 250,
      message: t("locations.form.contactInfoMaxLength"),
      trigger: "blur",
    },
  ],
  longitude: [
    {
      pattern: /^-?([0-9]{1,3}(\.[0-9]+)?|180(\.0+)?)$/,
      message: t("locations.form.longitudeInvalid"),
      trigger: "blur",
    },
  ],
  latitude: [
    {
      pattern: /^-?([0-9]{1,2}(\.[0-9]+)?|90(\.0+)?)$/,
      message: t("locations.form.latitudeInvalid"),
      trigger: "blur",
    },
  ],
  sortOrder: [
    {
      type: "number",
      message: t("locations.form.sortOrderInvalid"),
      trigger: "blur",
    },
  ],
});

const isEditMode = () => props.isEdit === true;

watch(
  () => props.location,
  (location) => {
    if (location) {
      form.name = location.name;
      form.contactInfo = location.contactInfo || "";
      form.longitude = location.longitude || "";
      form.latitude = location.latitude || "";
      form.sortOrder = location.sortOrder ?? 0;
    } else if (!props.isEdit) {
      resetForm();
    }
  },
  { immediate: true },
);

function resetForm() {
  form.name = "";
  form.contactInfo = "";
  form.longitude = "";
  form.latitude = "";
  form.sortOrder = 0;
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
      if (props.isEdit && props.location) {
        const dto: UpdateLocationDto = isOnlineLocation.value
          ? { sortOrder: form.sortOrder }
          : {
              name: form.name,
              contactInfo: form.contactInfo || undefined,
              longitude: form.longitude || undefined,
              latitude: form.latitude || undefined,
              sortOrder: form.sortOrder,
            };
        await locationStore.updateLocation(
          props.electionGuid,
          props.location.locationGuid,
          dto,
        );
        showSuccessMessage(t("locations.form.updated"));
      } else {
        const dto: CreateLocationDto = {
          electionGuid: props.electionGuid,
          name: form.name,
          contactInfo: form.contactInfo || undefined,
          longitude: form.longitude || undefined,
          latitude: form.latitude || undefined,
          sortOrder: form.sortOrder,
        };
        await locationStore.createLocation(props.electionGuid, dto);
        showSuccessMessage(t("locations.form.created"));
      }
      emit("success");
    } catch (error) {
      handleApiError(error);
    } finally {
      submitting.value = false;
    }
  });
}

async function handleDelete() {
  if (!props.location) {
    return;
  }

  try {
    await ElMessageBox.confirm(
      t("locations.confirm.deleteLocationMessage", {
        name: props.location.name,
      }),
      t("locations.confirm.deleteLocationTitle"),
      {
        confirmButtonText: t("locations.confirm.delete"),
        cancelButtonText: t("locations.confirm.cancel"),
        type: "warning",
      },
    );

    deleting.value = true;
    await locationStore.deleteLocation(
      props.electionGuid,
      props.location.locationGuid,
    );
    showSuccessMessage(t("locations.success.locationDeleted"));
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
  <div class="location-form">
    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-width="150px"
      label-position="left"
    >
      <el-form-item
        v-if="isOnlineLocation"
        :label="$t('locations.form.name')"
        data-testid="online-location-name"
      >
        <div class="location-form__readonly-name">{{ onlineDisplayName }}</div>
      </el-form-item>
      <el-form-item v-else :label="$t('locations.form.name')" prop="name">
        <el-input
          v-model="form.name"
          :placeholder="$t('locations.form.namePlaceholder')"
        />
      </el-form-item>

      <template v-if="!isOnlineLocation">
        <el-form-item
          :label="$t('locations.form.contactInfo')"
          prop="contactInfo"
        >
          <el-input
            v-model="form.contactInfo"
            type="textarea"
            :rows="3"
            :placeholder="$t('locations.form.contactInfoPlaceholder')"
          />
        </el-form-item>

        <el-form-item :label="$t('locations.form.longitude')" prop="longitude">
          <el-input
            v-model="form.longitude"
            :placeholder="$t('locations.form.longitudePlaceholder')"
          >
            <template #append>°</template>
          </el-input>
          <div class="form-help-text">
            {{ $t("locations.form.longitudeHelp") }}
          </div>
        </el-form-item>

        <el-form-item :label="$t('locations.form.latitude')" prop="latitude">
          <el-input
            v-model="form.latitude"
            :placeholder="$t('locations.form.latitudePlaceholder')"
          >
            <template #append>°</template>
          </el-input>
          <div class="form-help-text">
            {{ $t("locations.form.latitudeHelp") }}
          </div>
        </el-form-item>
      </template>

      <el-form-item :label="$t('locations.form.sortOrder')" prop="sortOrder">
        <el-input-number
          v-model="form.sortOrder"
          :min="0"
          :step="1"
          style="width: 100%"
        />
        <div class="form-help-text">
          {{
            isOnlineLocation
              ? $t("locations.form.onlineSortOnlyHelp")
              : $t("locations.form.sortOrderHelp")
          }}
        </div>
      </el-form-item>
    </el-form>

    <div class="location-form-actions">
      <el-button type="primary" :loading="submitting" @click="handleSubmit">
        {{
          isEditMode() ? $t("locations.form.save") : $t("locations.form.create")
        }}
      </el-button>
      <el-button @click="handleCancel">{{
        $t("locations.form.cancel")
      }}</el-button>
    </div>

    <div v-if="isEditMode() && canDelete" class="location-form-delete">
      <el-button type="danger" :loading="deleting" @click="handleDelete">
        {{ $t("locations.form.delete") }}
      </el-button>
    </div>
  </div>
</template>

<style lang="less">
.location-form {
  .location-form__readonly-name {
    min-height: 32px;
    display: flex;
    align-items: center;
    font-weight: 600;
  }

  .form-help-text {
    font-size: 12px;
    color: var(--el-text-color-secondary);
    margin-top: 4px;
  }

  .location-form-actions {
    display: flex;
    justify-content: space-between;
    gap: var(--spacing-2);
    margin-top: var(--spacing-4);
    padding-top: var(--spacing-4);
    border-top: 1px solid var(--el-border-color-lighter);
  }

  .location-form-delete {
    margin-top: var(--spacing-6);
    padding-top: var(--spacing-4);
    border-top: 1px solid var(--el-border-color-lighter);
    text-align: right;
  }
}
</style>
