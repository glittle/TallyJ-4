export class UnexpectedLocaleFolderError extends Error {
  folders: string[];
  constructor(folders: string[]);
}

export function classifyLocaleFolders(localesDir: string): {
  locales: string[];
  unexpected: string[];
};

export function mergeLocaleBundles(localesDir: string, outputDir: string): void;
