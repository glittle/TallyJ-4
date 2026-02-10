#!/usr/bin/env node

import { readFileSync, readdirSync, statSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const LOCALES_DIR = __dirname;
const LOCALE_PATTERN = /^[a-z]{2}(-[A-Z]{2})?$/;

class ValidationError {
  constructor(type, message, details = {}) {
    this.type = type;
    this.message = message;
    this.details = details;
  }
}

function getAllJsonFiles(dir, baseDir = dir) {
  const files = [];
  const entries = readdirSync(dir);

  for (const entry of entries) {
    const fullPath = join(dir, entry);
    const stat = statSync(fullPath);

    if (stat.isDirectory()) {
      files.push(...getAllJsonFiles(fullPath, baseDir));
    } else if (entry.endsWith('.json') && entry !== 'package.json') {
      const relativePath = fullPath.substring(baseDir.length + 1).replace(/\\/g, '/');
      files.push(relativePath);
    }
  }

  return files;
}

function loadJsonFile(filePath) {
  try {
    const content = readFileSync(join(LOCALES_DIR, filePath), 'utf-8');
    return JSON.parse(content);
  } catch (error) {
    throw new ValidationError('FILE_READ_ERROR', `Failed to read ${filePath}: ${error.message}`, { filePath });
  }
}

function getAllKeys(obj, prefix = '') {
  const keys = [];
  
  for (const [key, value] of Object.entries(obj)) {
    const fullKey = prefix ? `${prefix}.${key}` : key;
    
    if (value && typeof value === 'object' && !Array.isArray(value) && Object.keys(value).length > 0) {
      const nestedKeys = getAllKeys(value, fullKey);
      keys.push(...nestedKeys);
    } else {
      keys.push(fullKey);
    }
  }
  
  return keys;
}

function getValue(obj, path) {
  if (obj.hasOwnProperty(path)) {
    return obj[path];
  }
  return path.split('.').reduce((current, key) => current?.[key], obj);
}

function checkDuplicateKeys(keys, filePath) {
  const errors = [];
  const seen = new Map();
  
  for (const key of keys) {
    if (seen.has(key)) {
      errors.push(new ValidationError(
        'DUPLICATE_KEY',
        `Duplicate key "${key}" in ${filePath}`,
        { filePath, key }
      ));
    }
    seen.set(key, true);
  }
  
  return errors;
}

function checkEmptyValues(data, filePath, isTranslationFile = true) {
  const errors = [];
  const keys = getAllKeys(data);
  
  for (const key of keys) {
    const value = getValue(data, key);
    
    if (isTranslationFile && typeof value !== 'string') {
      errors.push(new ValidationError(
        'INVALID_VALUE_TYPE',
        `Key "${key}" in ${filePath} has non-string value: ${typeof value}`,
        { filePath, key, valueType: typeof value }
      ));
    } else if (typeof value === 'string' && value.trim() === '') {
      errors.push(new ValidationError(
        'EMPTY_VALUE',
        `Key "${key}" in ${filePath} has empty value`,
        { filePath, key }
      ));
    }
  }
  
  return errors;
}

function categorizeFiles(files) {
  const rootFiles = [];
  const localeFiles = new Map();
  
  for (const file of files) {
    if (file.includes('/')) {
      const [locale, ...rest] = file.split('/');
      const fileName = rest.join('/');
      
      if (!localeFiles.has(locale)) {
        localeFiles.set(locale, []);
      }
      localeFiles.get(locale).push({ original: file, fileName });
    } else {
      rootFiles.push(file);
    }
  }
  
  return { rootFiles, localeFiles };
}

function getAllKeysInLocale(localeFiles, locale) {
  const allKeys = new Map();
  const files = localeFiles.get(locale) || [];
  
  for (const { original, fileName } of files) {
    const data = loadJsonFile(original);
    const keys = getAllKeys(data);
    
    for (const key of keys) {
      if (allKeys.has(key)) {
        allKeys.get(key).push(fileName);
      } else {
        allKeys.set(key, [fileName]);
      }
    }
  }
  
  return allKeys;
}

function checkDuplicateKeysInLocale(localeFiles) {
  const errors = [];
  
  for (const [locale, files] of localeFiles.entries()) {
    const keyMap = getAllKeysInLocale(localeFiles, locale);
    
    for (const [key, fileList] of keyMap.entries()) {
      if (fileList.length > 1) {
        errors.push(new ValidationError(
          'DUPLICATE_KEY_ACROSS_FILES',
          `Key "${key}" in locale "${locale}" is duplicated across files: ${fileList.join(', ')}`,
          { locale, key, files: fileList }
        ));
      }
    }
  }
  
  return errors;
}

function checkKeyConsistency(localeFiles) {
  const errors = [];
  const locales = Array.from(localeFiles.keys());
  
  if (locales.length < 2) {
    return errors;
  }
  
  const keysByLocale = new Map();
  for (const locale of locales) {
    const allKeys = getAllKeysInLocale(localeFiles, locale);
    keysByLocale.set(locale, new Set(allKeys.keys()));
  }
  
  const allKeys = new Set();
  for (const keys of keysByLocale.values()) {
    for (const key of keys) {
      allKeys.add(key);
    }
  }
  
  for (const key of allKeys) {
    const presentIn = [];
    const missingIn = [];
    
    for (const locale of locales) {
      const keys = keysByLocale.get(locale);
      if (keys && keys.has(key)) {
        presentIn.push(locale);
      } else {
        missingIn.push(locale);
      }
    }
    
    if (missingIn.length > 0 && presentIn.length > 0) {
      errors.push(new ValidationError(
        'MISSING_KEY',
        `Key "${key}" is missing in locales: ${missingIn.join(', ')} (present in: ${presentIn.join(', ')})`,
        { key, missingIn, presentIn }
      ));
    }
  }
  
  return errors;
}

function validateRootFiles(rootFiles) {
  const errors = [];
  
  for (const file of rootFiles) {
    const data = loadJsonFile(file);
    const keys = getAllKeys(data);
    const isConfigFile = file === 'common.json' || file === 'shared.json';
    
    errors.push(...checkDuplicateKeys(keys, file));
    errors.push(...checkEmptyValues(data, file, !isConfigFile));
  }
  
  return errors;
}

function validateLocaleFiles(localeFiles) {
  const errors = [];
  
  for (const [locale, files] of localeFiles.entries()) {
    for (const { original } of files) {
      const data = loadJsonFile(original);
      const keys = getAllKeys(data);
      
      errors.push(...checkDuplicateKeys(keys, original));
      errors.push(...checkEmptyValues(data, original));
    }
  }
  
  errors.push(...checkDuplicateKeysInLocale(localeFiles));
  errors.push(...checkKeyConsistency(localeFiles));
  
  return errors;
}

function printResults(errors) {
  if (errors.length === 0) {
    console.log('✅ All translation files are valid!');
    return true;
  }
  
  console.log(`\n❌ Found ${errors.length} validation error(s):\n`);
  
  const errorsByType = new Map();
  for (const error of errors) {
    if (!errorsByType.has(error.type)) {
      errorsByType.set(error.type, []);
    }
    errorsByType.get(error.type).push(error);
  }
  
  for (const [type, typeErrors] of errorsByType.entries()) {
    console.log(`\n${type} (${typeErrors.length}):`);
    for (const error of typeErrors) {
      console.log(`  - ${error.message}`);
    }
  }
  
  return false;
}

function main() {
  console.log('🔍 Validating translation files...\n');
  
  try {
    const allFiles = getAllJsonFiles(LOCALES_DIR);
    console.log(`Found ${allFiles.length} JSON file(s)\n`);
    
    const { rootFiles, localeFiles } = categorizeFiles(allFiles);
    
    if (rootFiles.length > 0) {
      console.log(`Root files: ${rootFiles.join(', ')}`);
    }
    
    if (localeFiles.size > 0) {
      console.log(`Locales: ${Array.from(localeFiles.keys()).join(', ')}`);
      for (const [locale, files] of localeFiles.entries()) {
        console.log(`  ${locale}: ${files.map(f => f.fileName).join(', ')}`);
      }
    }
    
    const errors = [];
    errors.push(...validateRootFiles(rootFiles));
    errors.push(...validateLocaleFiles(localeFiles));
    
    const isValid = printResults(errors);
    process.exit(isValid ? 0 : 1);
    
  } catch (error) {
    if (error instanceof ValidationError) {
      console.error(`\n❌ ${error.message}`);
      process.exit(1);
    }
    throw error;
  }
}

main();
