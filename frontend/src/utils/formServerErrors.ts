import type { FormInstance } from "element-plus";

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
