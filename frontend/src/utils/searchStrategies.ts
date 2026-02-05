import type { SearchablePersonDto } from '@/types/Person';

export interface SearchResult {
  person: SearchablePersonDto;
  weight: number;
  matchedStrategy: string;
}

export function normalizeSearchText(text: string): string {
  return text
    .toLowerCase()
    .trim()
    .replace(/\s+/g, ' ')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '');
}

export function calculateLevenshteinDistance(a: string, b: string): number {
  if (a.length === 0) return b.length;
  if (b.length === 0) return a.length;

  const matrix: number[][] = [];

  for (let i = 0; i <= b.length; i++) {
    matrix[i] = [i];
  }

  for (let j = 0; j <= a.length; j++) {
    matrix[0][j] = j;
  }

  for (let i = 1; i <= b.length; i++) {
    for (let j = 1; j <= a.length; j++) {
      if (b.charAt(i - 1) === a.charAt(j - 1)) {
        matrix[i][j] = matrix[i - 1][j - 1];
      } else {
        matrix[i][j] = Math.min(
          matrix[i - 1][j - 1] + 1,
          matrix[i][j - 1] + 1,
          matrix[i - 1][j] + 1
        );
      }
    }
  }

  return matrix[b.length][a.length];
}

export function compareSoundexCodes(codes1: string[], codes2: string[]): number {
  if (codes1.length === 0 || codes2.length === 0) return 0;

  let matches = 0;
  const totalCodes = Math.max(codes1.length, codes2.length);

  for (const code1 of codes1) {
    for (const code2 of codes2) {
      if (code1 === code2) {
        matches++;
        break;
      }
    }
  }

  return totalCodes > 0 ? (matches / totalCodes) * 100 : 0;
}

export function exactMatch(searchTerm: string, person: SearchablePersonDto): number | null {
  const normalizedSearch = normalizeSearchText(searchTerm);
  const normalizedPersonText = normalizeSearchText(person._searchText);

  if (normalizedPersonText === normalizedSearch) {
    return 100;
  }

  return null;
}

export function prefixMatch(searchTerm: string, person: SearchablePersonDto): number | null {
  const normalizedSearch = normalizeSearchText(searchTerm);
  
  if (normalizedSearch.length === 0) return null;
  
  const normalizedPersonText = normalizeSearchText(person._searchText);

  if (normalizedPersonText.startsWith(normalizedSearch)) {
    return 90;
  }

  return null;
}

export function wordBoundaryMatch(searchTerm: string, person: SearchablePersonDto): number | null {
  const normalizedSearch = normalizeSearchText(searchTerm);
  const searchWords = normalizedSearch.split(' ').filter(w => w.length > 0);
  
  if (searchWords.length === 0) return null;

  const normalizedPersonText = normalizeSearchText(person._searchText);
  const personWords = normalizedPersonText.split(' ').filter(w => w.length > 0);

  let allWordsMatch = true;
  for (const searchWord of searchWords) {
    const matchesAnyPersonWord = personWords.some(personWord => 
      personWord.startsWith(searchWord)
    );
    if (!matchesAnyPersonWord) {
      allWordsMatch = false;
      break;
    }
  }

  if (allWordsMatch) {
    return 85;
  }

  return null;
}

export function substringMatch(searchTerm: string, person: SearchablePersonDto): number | null {
  const normalizedSearch = normalizeSearchText(searchTerm);
  const normalizedPersonText = normalizeSearchText(person._searchText);

  if (normalizedPersonText.includes(normalizedSearch)) {
    return 80;
  }

  return null;
}

export function otherNamesMatch(searchTerm: string, person: SearchablePersonDto): number | null {
  const normalizedSearch = normalizeSearchText(searchTerm);
  
  const otherNames = normalizeSearchText(person.otherNames || '');
  const otherLastNames = normalizeSearchText(person.otherLastNames || '');
  
  if (otherNames && otherNames.includes(normalizedSearch)) {
    return 70;
  }
  
  if (otherLastNames && otherLastNames.includes(normalizedSearch)) {
    return 70;
  }

  return null;
}

export function phoneticMatch(searchTerm: string, person: SearchablePersonDto): number | null {
  if (searchTerm.length < 3) return null;
  if (!person._soundexCodes || person._soundexCodes.length === 0) return null;

  const searchWords = normalizeSearchText(searchTerm).split(' ').filter(w => w.length > 0);
  if (searchWords.length === 0) return null;

  const searchSoundex = generateSoundexCodesForWords(searchWords);
  if (searchSoundex.length === 0) return null;

  const similarity = compareSoundexCodes(searchSoundex, person._soundexCodes);

  if (similarity >= 75) return 75;
  if (similarity >= 50) return 65;
  if (similarity >= 25) return 60;

  return null;
}

export function fuzzyMatch(searchTerm: string, person: SearchablePersonDto): number | null {
  if (searchTerm.length < 3) return null;

  const normalizedSearch = normalizeSearchText(searchTerm);
  const normalizedPersonText = normalizeSearchText(person._searchText);

  const distance = calculateLevenshteinDistance(normalizedSearch, normalizedPersonText);

  if (distance <= 2) {
    return 50;
  }

  const personWords = normalizedPersonText.split(' ').filter(w => w.length > 0);
  for (const word of personWords) {
    const wordDistance = calculateLevenshteinDistance(normalizedSearch, word);
    if (wordDistance <= 2) {
      return 50;
    }
  }

  return null;
}

function generateSoundexCodesForWords(words: string[]): string[] {
  return words.map(word => generateSoundex(word)).filter(code => code !== '');
}

function generateSoundex(word: string): string {
  if (!word || word.length === 0) return '';

  const cleaned = word.toUpperCase().replace(/[^A-Z]/g, '');
  if (cleaned.length === 0) return '';

  const firstLetter = cleaned[0];
  
  const soundexMap: Record<string, string> = {
    'B': '1', 'F': '1', 'P': '1', 'V': '1',
    'C': '2', 'G': '2', 'J': '2', 'K': '2', 'Q': '2', 'S': '2', 'X': '2', 'Z': '2',
    'D': '3', 'T': '3',
    'L': '4',
    'M': '5', 'N': '5',
    'R': '6'
  };

  let code = firstLetter;
  let prevCode = soundexMap[firstLetter] || '';

  for (let i = 1; i < cleaned.length && code.length < 4; i++) {
    const char = cleaned[i];
    const currentCode = soundexMap[char];

    if (currentCode && currentCode !== prevCode) {
      code += currentCode;
      prevCode = currentCode;
    } else if (!currentCode) {
      prevCode = '';
    }
  }

  while (code.length < 4) {
    code += '0';
  }

  return code.substring(0, 4);
}

export function applyAllStrategies(
  searchTerm: string,
  person: SearchablePersonDto
): SearchResult | null {
  const strategies = [
    { name: 'exact', fn: exactMatch },
    { name: 'prefix', fn: prefixMatch },
    { name: 'wordBoundary', fn: wordBoundaryMatch },
    { name: 'substring', fn: substringMatch },
    { name: 'otherNames', fn: otherNamesMatch },
    { name: 'phonetic', fn: phoneticMatch },
    { name: 'fuzzy', fn: fuzzyMatch }
  ];

  let bestWeight = 0;
  let bestStrategy = '';

  for (const strategy of strategies) {
    const weight = strategy.fn(searchTerm, person);
    if (weight !== null && weight > bestWeight) {
      bestWeight = weight;
      bestStrategy = strategy.name;
    }
  }

  if (bestWeight > 0) {
    return {
      person,
      weight: bestWeight,
      matchedStrategy: bestStrategy
    };
  }

  return null;
}
