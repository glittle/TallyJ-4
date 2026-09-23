export interface RichEntry {
  t: string;
  s?: string;
  w?: string;
}

export function isRichEntry(value: unknown): value is RichEntry;

/** Rich leaves become `t`. Strings and non-message config values pass through. */
export function unwrapMessages(value: unknown): any;
