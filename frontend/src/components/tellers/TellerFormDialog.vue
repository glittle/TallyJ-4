<script setup lang="ts">
import { ref, reactive, watch } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { useTellerStore } from '@/stores/tellerStore'
import type { Teller, CreateTellerDto, UpdateTellerDto } from '@/types/teller'

const props = defineProps<{
  modelValue: boolean
  electionGuid: string
  teller?: Teller | null
  isEdit?: boolean
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  success: []
}>()

const tellerStore = useTellerStore()

const formRef = ref<FormInstance>()
const submitting = ref(false)

const form = reactive({
  name: '',
  usingComputerCode: '',
  isHeadTeller: false
})

const rules = reactive<FormRules>({
  name: [
    { required: true, message: 'Teller name is required', trigger: 'blur' },
    { max: 50, message: 'Teller name cannot exceed 50 characters', trigger: 'blur' }
  ],
  usingComputerCode: [
    { 
      pattern: /^[A-Z]{2}$/, 
      message: 'Computer code must be 2 uppercase letters (AA-ZZ)', 
      trigger: 'blur' 
    }
  ]
})

watch(() => props.teller, (teller) => {
  if (teller) {
    form.name = teller.name
    form.usingComputerCode = teller.usingComputerCode || ''
    form.isHeadTeller = teller.isHeadTeller
  }
}, { immediate: true })

watch(() => props.modelValue, (value) => {
  if (!value) {
    resetForm()
  } else if (!props.isEdit) {
    resetForm()
  }
})

function resetForm() {
  if (!props.isEdit) {
    form.name = ''
    form.usingComputerCode = ''
    form.isHeadTeller = false
  }
}

async function handleSubmit() {
  if (!formRef.value) return
  
  await formRef.value.validate(async (valid) => {
    if (valid) {
      submitting.value = true
      try {
        if (props.isEdit && props.teller) {
          const dto: UpdateTellerDto = {
            name: form.name,
            usingComputerCode: form.usingComputerCode || undefined,
            isHeadTeller: form.isHeadTeller
          }
          await tellerStore.updateTeller(props.electionGuid, props.teller.rowId, dto)
        } else {
          const dto: CreateTellerDto = {
            electionGuid: props.electionGuid,
            name: form.name,
            usingComputerCode: form.usingComputerCode || undefined,
            isHeadTeller: form.isHeadTeller
          }
          await tellerStore.createTeller(props.electionGuid, dto)
        }
        emit('success')
      } catch (error: any) {
        ElMessage.error(error.message || 'Failed to save teller')
      } finally {
        submitting.value = false
      }
    }
  })
}

function handleClose() {
  formRef.value?.resetFields()
  emit('update:modelValue', false)
}
</script>

<template>
  <el-dialog
    :model-value="modelValue"
    :title="isEdit ? 'Edit Teller' : 'Add Teller'"
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
      <el-form-item label="Teller Name" prop="name">
        <el-input v-model="form.name" placeholder="Enter teller name" />
      </el-form-item>

      <el-form-item label="Computer Code" prop="usingComputerCode">
        <el-input 
          v-model="form.usingComputerCode" 
          placeholder="e.g., AA"
          maxlength="2"
          style="text-transform: uppercase"
        />
        <div class="form-help-text">Two uppercase letters (AA-ZZ) or leave empty</div>
      </el-form-item>

      <el-form-item label="Head Teller">
        <el-switch v-model="form.isHeadTeller" />
        <div class="form-help-text">Head tellers have additional permissions</div>
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
