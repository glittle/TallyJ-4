<script setup lang="ts">
import { ref, reactive, watch } from 'vue';
import { type FormInstance, type FormRules } from 'element-plus';
import { useNotifications } from '@/composables/useNotifications';
import { useApiErrorHandler } from '@/composables/useApiErrorHandler';
import { useLocationStore } from '../../stores/locationStore';
import type { LocationDto, CreateLocationDto, UpdateLocationDto } from '../../types';

const props = defineProps<{
  modelValue: boolean;
  electionGuid: string;
  location?: LocationDto | null;
  isEdit?: boolean;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean];
  success: [];
}>();

const locationStore = useLocationStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();
const { handleApiError } = useApiErrorHandler();

const formRef = ref<FormInstance>();
const submitting = ref(false);

const form = reactive({
  name: '',
  contactInfo: '',
  longitude: '',
  latitude: '',
  sortOrder: 0
});

const rules = reactive<FormRules>({
  name: [
    { required: true, message: 'Location name is required', trigger: 'blur' },
    { max: 50, message: 'Location name cannot exceed 50 characters', trigger: 'blur' }
  ],
  contactInfo: [
    { max: 250, message: 'Contact info cannot exceed 250 characters', trigger: 'blur' }
  ],
  longitude: [
    { 
      pattern: /^-?([0-9]{1,3}(\.[0-9]+)?|180(\.0+)?)$/, 
      message: 'Longitude must be a valid coordinate between -180 and 180', 
      trigger: 'blur' 
    }
  ],
  latitude: [
    { 
      pattern: /^-?([0-9]{1,2}(\.[0-9]+)?|90(\.0+)?)$/, 
      message: 'Latitude must be a valid coordinate between -90 and 90', 
      trigger: 'blur' 
    }
  ],
  sortOrder: [
    { type: 'number', message: 'Sort order must be a number', trigger: 'blur' }
  ]
});

watch(() => props.location, (location) => {
  if (location) {
    form.name = location.name;
    form.contactInfo = location.contactInfo || '';
    form.longitude = location.longitude || '';
    form.latitude = location.latitude || '';
    form.sortOrder = location.sortOrder ?? 0;
  }
}, { immediate: true });

watch(() => props.modelValue, (value) => {
  if (!value) {
    resetForm();
  } else if (!props.isEdit) {
    resetForm();
  }
});

function resetForm() {
  if (!props.isEdit) {
    form.name = '';
    form.contactInfo = '';
    form.longitude = '';
    form.latitude = '';
    form.sortOrder = 0;
  }
}

async function handleSubmit() {
  if (!formRef.value) return;
  
  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitting.value = true;
      try {
        if (props.isEdit && props.location) {
          const dto: UpdateLocationDto = {
            name: form.name,
            contactInfo: form.contactInfo || undefined,
            longitude: form.longitude || undefined,
            latitude: form.latitude || undefined,
            sortOrder: form.sortOrder
          };
          await locationStore.updateLocation(props.electionGuid, props.location.locationGuid, dto);
          showSuccessMessage('Location updated successfully');
        } else {
          const dto: CreateLocationDto = {
            electionGuid: props.electionGuid,
            name: form.name,
            contactInfo: form.contactInfo || undefined,
            longitude: form.longitude || undefined,
            latitude: form.latitude || undefined,
            sortOrder: form.sortOrder
          };
          await locationStore.createLocation(props.electionGuid, dto);
          showSuccessMessage('Location created successfully');
        }
        emit('success');
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
  emit('update:modelValue', false);
}
</script>

<template>
  <el-dialog
    :model-value="modelValue"
    :title="isEdit ? 'Edit Location' : 'Add Location'"
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
      <el-form-item label="Location Name" prop="name">
        <el-input v-model="form.name" placeholder="Enter location name" />
      </el-form-item>

      <el-form-item label="Contact Info" prop="contactInfo">
        <el-input 
          v-model="form.contactInfo" 
          type="textarea" 
          :rows="3"
          placeholder="Enter contact information"
        />
      </el-form-item>

      <el-form-item label="Longitude" prop="longitude">
        <el-input 
          v-model="form.longitude" 
          placeholder="e.g., -122.4194"
        >
          <template #append>°</template>
        </el-input>
        <div class="form-help-text">Range: -180 to 180</div>
      </el-form-item>

      <el-form-item label="Latitude" prop="latitude">
        <el-input 
          v-model="form.latitude" 
          placeholder="e.g., 37.7749"
        >
          <template #append>°</template>
        </el-input>
        <div class="form-help-text">Range: -90 to 90</div>
      </el-form-item>

      <el-form-item label="Sort Order" prop="sortOrder">
        <el-input-number 
          v-model="form.sortOrder" 
          :min="0" 
          :step="1"
          style="width: 100%"
        />
        <div class="form-help-text">Used to order locations in lists</div>
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">Cancel</el-button>
      <el-button type="primary" @click="handleSubmit" :loading="submitting">
        {{ isEdit ? 'Save' : 'Create' }}
      </el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.form-help-text {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-top: 4px;
}
</style>
