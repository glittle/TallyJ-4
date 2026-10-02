import type { FormInstance } from "element-plus";

/**
 * ASP.NET ValidationProblemDetails keys are PascalCase (`Name`, `NumberToElect`).
 * Element Plus form item props are camelCase (`name`, `numberToElect`).
 */
export function mapServerValidationErrors(
  errors: Record<string, string[]>,
): Record<string, string[]> {
  const fieldErrors: Record<string, string[]> = {};
  for (const [serverField, messages] of Object.entries(errors)) {
    const formField =
      serverField.charAt(0).toLowerCase() + serverField.slice(1);
    fieldErrors[formField] = messages;
  }
  return fieldErrors;
}

/**
 * Apply ASP.NET validation errors onto an Element Plus form.
 * FormInstance has getField, not setFields.
 */
export function applyServerFieldErrors(
  form: FormInstance | undefined,
  fieldErrors: Record<string, string[]>,
): void {
  if (!form) {
    return;
  }

  for (const [prop, messages] of Object.entries(fieldErrors)) {
    const field = form.getField(prop);
    if (!field) {
      continue;
    }
    field.validateState = "error";
    field.validateMessage = messages[0] ?? "";
  }
}
