/** BCP-47 folder source, without anchors, for embedding in a path regex. */
export const LOCALE_DIRECTORY_SOURCE: string;

export const LOCALE_DIRECTORY_PATTERN: RegExp;

export function isLocaleDirectoryName(name: string): boolean;

export const IGNORED_LOCALE_DIRECTORIES: ReadonlySet<string>;
