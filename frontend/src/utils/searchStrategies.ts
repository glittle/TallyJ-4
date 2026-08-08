import type { SearchablePersonDto } from "@/types/Person";

export interface SearchResult {
  person: SearchablePersonDto;
  weight: number;
  matchedStrategy: string;
}

/**
 * Normalize for language-agnostic matching:
 * lowercase, collapse whitespace, NFD + strip combining marks (diacritics).
 * Works for Latin accented names (José↔Jose) and leaves non-Latin scripts intact.
 */
export function normalizeSearchText(text: string): string {
  return text
    .toLowerCase()
    .trim()
    .replace(/\s+/g, " ")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "");
}

export function splitSearchTerms(query: string): string[] {
  return normalizeSearchText(query)
    .split(" ")
    .filter((term) => term.length > 0);
}

export function tokenizeNameForSearch(name: string): string[] {
  return normalizeSearchText(name)
    .split(/[\s,[\]()]+/)
    .filter((part) => part.length > 0);
}

export interface FrontDeskSearchableVoter {
  fullName?: string | null;
  bahaiId?: string | null;
  area?: string | null;
  /** Optional; when present, searchable as a primary field. */
  otherInfo?: string | null;
}

/**
 * Front-desk filter.
 * Primary fields: fullName, bahaiId, otherInfo.
 * Area may satisfy a term only when at least one other term matched a primary field
 * (area alone never matches).
 */
export function matchesFrontDeskVoterSearch(
  voter: FrontDeskSearchableVoter,
  query: string,
): boolean {
  const terms = splitSearchTerms(query);
  if (terms.length === 0) {
    return true;
  }

  const fullName = voter.fullName ?? "";
  const normalizedName = normalizeSearchText(fullName);
  const nameParts = tokenizeNameForSearch(fullName);
  const bahaiId = voter.bahaiId ? normalizeSearchText(voter.bahaiId) : "";
  const otherInfo = voter.otherInfo
    ? normalizeSearchText(voter.otherInfo)
    : "";
  const area = voter.area ? normalizeSearchText(voter.area) : "";

  const termMatchesPrimary = (term: string): boolean =>
    nameParts.some((part) => part.startsWith(term)) ||
    normalizedName.includes(term) ||
    (bahaiId.length > 0 && bahaiId.includes(term)) ||
    (otherInfo.length > 0 && otherInfo.includes(term));

  const termMatchesArea = (term: string): boolean =>
    area.length > 0 && area.includes(term);

  if (terms.length === 1) {
    return termMatchesPrimary(terms[0]);
  }

  let anyPrimary = false;
  for (const term of terms) {
    const primary = termMatchesPrimary(term);
    if (primary) {
      anyPrimary = true;
      continue;
    }
    if (!termMatchesArea(term)) {
      return false;
    }
  }
  return anyPrimary;
}

/**
 * Damerau–Levenshtein distance (includes adjacent transpositions).
 */
export function calculateLevenshteinDistance(a: string, b: string): number {
  if (a.length === 0) {
    return b.length;
  }
  if (b.length === 0) {
    return a.length;
  }

  const matrix: number[][] = [];

  for (let i = 0; i <= b.length; i++) {
    matrix[i] = [i];
  }

  for (let j = 0; j <= a.length; j++) {
    matrix[0][j] = j;
  }

  for (let i = 1; i <= b.length; i++) {
    for (let j = 1; j <= a.length; j++) {
      const cost = b.charAt(i - 1) === a.charAt(j - 1) ? 0 : 1;
      matrix[i][j] = Math.min(
        matrix[i - 1][j - 1] + cost,
        matrix[i][j - 1] + 1,
        matrix[i - 1][j] + 1,
      );

      if (
        i > 1 &&
        j > 1 &&
        b.charAt(i - 1) === a.charAt(j - 2) &&
        b.charAt(i - 2) === a.charAt(j - 1)
      ) {
        matrix[i][j] = Math.min(matrix[i][j], matrix[i - 2][j - 2] + cost);
      }
    }
  }

  return matrix[b.length][a.length];
}

export function compareSoundexCodes(
  codes1: string[],
  codes2: string[],
): number {
  if (codes1.length === 0 || codes2.length === 0) {
    return 0;
  }

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

export function exactMatch(
  searchTerm: string,
  person: SearchablePersonDto,
): number | null {
  const normalizedSearch = normalizeSearchText(searchTerm);
  const normalizedPersonText = normalizeSearchText(person._searchText);

  if (normalizedPersonText === normalizedSearch) {
    return 100;
  }

  return null;
}

export function prefixMatch(
  searchTerm: string,
  person: SearchablePersonDto,
): number | null {
  const normalizedSearch = normalizeSearchText(searchTerm);

  if (normalizedSearch.length === 0) {
    return null;
  }

  const normalizedPersonText = normalizeSearchText(person._searchText);

  if (normalizedPersonText.startsWith(normalizedSearch)) {
    return 90;
  }

  return null;
}

export function wordBoundaryMatch(
  searchTerm: string,
  person: SearchablePersonDto,
): number | null {
  const normalizedSearch = normalizeSearchText(searchTerm);
  const searchWords = normalizedSearch.split(" ").filter((w) => w.length > 0);

  if (searchWords.length === 0) {
    return null;
  }

  const normalizedPersonText = normalizeSearchText(person._searchText);
  const personWords = normalizedPersonText
    .split(" ")
    .filter((w) => w.length > 0);

  let allWordsMatch = true;
  for (const searchWord of searchWords) {
    const matchesAnyPersonWord = personWords.some((personWord) =>
      personWord.startsWith(searchWord),
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

export function substringMatch(
  searchTerm: string,
  person: SearchablePersonDto,
): number | null {
  const normalizedSearch = normalizeSearchText(searchTerm);
  const normalizedPersonText = normalizeSearchText(person._searchText);

  if (normalizedPersonText.includes(normalizedSearch)) {
    return 80;
  }

  return null;
}

export function otherNamesMatch(
  searchTerm: string,
  person: SearchablePersonDto,
): number | null {
  const normalizedSearch = normalizeSearchText(searchTerm);

  const otherNames = normalizeSearchText(person.otherNames || "");
  const otherLastNames = normalizeSearchText(person.otherLastNames || "");

  if (otherNames && otherNames.includes(normalizedSearch)) {
    return 70;
  }

  if (otherLastNames && otherLastNames.includes(normalizedSearch)) {
    return 70;
  }

  return null;
}

export function otherInfoMatch(
  searchTerm: string,
  person: SearchablePersonDto,
): number | null {
  const normalizedSearch = normalizeSearchText(searchTerm);
  if (normalizedSearch.length === 0) {
    return null;
  }

  const otherInfo = normalizeSearchText(person.otherInfo || "");
  if (!otherInfo) {
    return null;
  }

  if (otherInfo.includes(normalizedSearch)) {
    return 68;
  }

  const searchWords = normalizedSearch.split(" ").filter((w) => w.length > 0);
  if (
    searchWords.length > 1 &&
    searchWords.every((w) => otherInfo.includes(w))
  ) {
    return 68;
  }

  return null;
}

function getPersonSoundexCodes(person: SearchablePersonDto): string[] {
  const codes = new Set<string>();

  for (const code of person._soundexCodes || []) {
    if (code) codes.add(code);
  }

  const nameParts = [
    person.firstName || "",
    person.lastName || "",
    person.otherNames || "",
    person.otherLastNames || "",
  ]
    .join(" ")
    .split(/[\s,;/]+/)
    .filter((w) => w.length > 0);

  for (const code of generateSoundexCodesForWords(nameParts)) {
    codes.add(code);
  }

  return [...codes];
}

export function phoneticMatch(
  searchTerm: string,
  person: SearchablePersonDto,
): number | null {
  if (searchTerm.length < 3) {
    return null;
  }

  const searchWords = normalizeSearchText(searchTerm)
    .split(" ")
    .filter((w) => w.length > 0);
  if (searchWords.length === 0) {
    return null;
  }

  const searchSoundex = generateSoundexCodesForWords(searchWords);
  if (searchSoundex.length === 0) {
    return null;
  }

  const personSoundex = getPersonSoundexCodes(person);
  if (personSoundex.length === 0) {
    return null;
  }

  let matchedSearchTokens = 0;
  for (const sCode of searchSoundex) {
    if (personSoundex.includes(sCode)) {
      matchedSearchTokens++;
    }
  }

  if (matchedSearchTokens === 0) {
    const similarity = compareSoundexCodes(searchSoundex, personSoundex);
    if (similarity >= 50) {
      return 60;
    }
    return null;
  }

  if (matchedSearchTokens === searchSoundex.length) {
    return 75;
  }
  return 65;
}

export function fuzzyMatch(
  searchTerm: string,
  person: SearchablePersonDto,
): number | null {
  if (searchTerm.length < 3) {
    return null;
  }

  const normalizedSearch = normalizeSearchText(searchTerm);
  const normalizedPersonText = normalizeSearchText(person._searchText);

  const distance = calculateLevenshteinDistance(
    normalizedSearch,
    normalizedPersonText,
  );

  if (distance <= 2) {
    return 50;
  }

  const personWords = normalizedPersonText
    .split(" ")
    .filter((w) => w.length > 0);

  const searchWords = normalizedSearch.split(" ").filter((w) => w.length > 0);
  const wordsToCheck =
    searchWords.length > 0 ? searchWords : [normalizedSearch];

  for (const searchWord of wordsToCheck) {
    for (const word of personWords) {
      const wordDistance = calculateLevenshteinDistance(searchWord, word);
      const maxDist =
        Math.min(searchWord.length, word.length) >= 5 ? 3 : 2;
      if (wordDistance <= maxDist) {
        return 50;
      }
    }
  }

  return null;
}

function areaBonus(searchTerm: string, person: SearchablePersonDto): number {
  const area = normalizeSearchText(person.area || "");
  if (!area) {
    return 0;
  }

  // Require a meaningful term length. Single letters like "g" / "l" match almost
  // any area string and unfairly push some names to a higher weight band.
  const terms = splitSearchTerms(searchTerm).filter((term) => term.length >= 3);
  if (terms.some((term) => area.includes(term) || area.startsWith(term))) {
    return 5;
  }
  return 0;
}

function generateSoundexCodesForWords(words: string[]): string[] {
  return words
    .map((word) => generateSoundex(word))
    .filter((code) => code !== "");
}

function generateSoundex(word: string): string {
  if (!word || word.length === 0) {
    return "";
  }

  const cleaned = word.toUpperCase().replace(/[^A-Z]/g, "");
  if (cleaned.length === 0) {
    return "";
  }

  const firstLetter = cleaned[0];

  const soundexMap: Record<string, string> = {
    B: "1",
    F: "1",
    P: "1",
    V: "1",
    C: "2",
    G: "2",
    J: "2",
    K: "2",
    Q: "2",
    S: "2",
    X: "2",
    Z: "2",
    D: "3",
    T: "3",
    L: "4",
    M: "5",
    N: "5",
    R: "6",
  };

  let code = firstLetter;
  let prevCode = soundexMap[firstLetter] || "";

  for (let i = 1; i < cleaned.length && code.length < 4; i++) {
    const char = cleaned[i];
    const currentCode = soundexMap[char];

    if (currentCode && currentCode !== prevCode) {
      code += currentCode;
      prevCode = currentCode;
    } else if (!currentCode) {
      prevCode = "";
    }
  }

  while (code.length < 4) {
    code += "0";
  }

  return code.substring(0, 4);
}

function getPersonNameTokens(person: SearchablePersonDto): string[] {
  const raw = [
    person.firstName || "",
    person.lastName || "",
    person.otherNames || "",
    person.otherLastNames || "",
  ]
    .join(" ")
    .split(/[\s,;/]+/)
    .map((w) => normalizeSearchText(w))
    .filter((w) => w.length > 0);

  const seen = new Set<string>();
  const tokens: string[] = [];
  for (const t of raw) {
    if (!seen.has(t)) {
      seen.add(t);
      tokens.push(t);
    }
  }
  return tokens;
}

function scoreTokenPair(searchToken: string, personToken: string): number {
  if (!searchToken || !personToken) {
    return 0;
  }

  if (personToken.startsWith(searchToken) || searchToken.startsWith(personToken)) {
    const shorter = Math.min(searchToken.length, personToken.length);
    const longer = Math.max(searchToken.length, personToken.length);
    if (shorter >= 3 || (shorter >= 2 && longer <= shorter + 3)) {
      return 90;
    }
    if (shorter >= 2) {
      return 70;
    }
    // Single-character prefix (e.g. "g" → "Glen", "l" → "Little")
    if (shorter === 1 && personToken.startsWith(searchToken)) {
      return 55;
    }
  }

  const sCode = generateSoundex(searchToken);
  const pCode = generateSoundex(personToken);
  if (sCode && pCode && sCode === pCode && searchToken.length >= 3) {
    return 80;
  }

  if (searchToken.length >= 3 && personToken.length >= 3) {
    const dist = calculateLevenshteinDistance(searchToken, personToken);
    const maxDist = Math.min(searchToken.length, personToken.length) >= 5 ? 3 : 2;
    if (dist <= maxDist) {
      return dist === 0 ? 95 : dist === 1 ? 75 : 55;
    }
  }

  return 0;
}

/**
 * Multi-term coverage: each search term pairs with at most one person-name token.
 */
export function multiTokenCoverageMatch(
  searchTerm: string,
  person: SearchablePersonDto,
): number | null {
  const searchWords = normalizeSearchText(searchTerm)
    .split(" ")
    .filter((w) => w.length > 0);

  if (searchWords.length < 2) {
    return null;
  }

  const personTokens = getPersonNameTokens(person);
  if (personTokens.length === 0) {
    return null;
  }

  type Pair = { si: number; pi: number; score: number };
  const pairs: Pair[] = [];
  for (let si = 0; si < searchWords.length; si++) {
    for (let pi = 0; pi < personTokens.length; pi++) {
      const score = scoreTokenPair(searchWords[si], personTokens[pi]);
      if (score > 0) {
        pairs.push({ si, pi, score });
      }
    }
  }
  pairs.sort((a, b) => b.score - a.score);

  const usedSearch = new Set<number>();
  const usedPerson = new Set<number>();
  let sumScores = 0;
  let matched = 0;

  for (const pair of pairs) {
    if (usedSearch.has(pair.si) || usedPerson.has(pair.pi)) {
      continue;
    }
    usedSearch.add(pair.si);
    usedPerson.add(pair.pi);
    sumScores += pair.score;
    matched++;
  }

  if (matched === 0) {
    return null;
  }

  const coverage = matched / searchWords.length;
  const avgQuality = sumScores / matched;

  if (coverage === 1) {
    return Math.min(88, Math.round(80 + avgQuality / 20));
  }

  if (coverage >= 0.5 && matched >= 1) {
    return Math.min(72, Math.round(55 + avgQuality / 15));
  }

  return null;
}

export function applyAllStrategies(
  searchTerm: string,
  person: SearchablePersonDto,
): SearchResult | null {
  const strategies = [
    { name: "exact", fn: exactMatch },
    { name: "prefix", fn: prefixMatch },
    { name: "wordBoundary", fn: wordBoundaryMatch },
    { name: "multiToken", fn: multiTokenCoverageMatch },
    { name: "substring", fn: substringMatch },
    { name: "otherNames", fn: otherNamesMatch },
    { name: "otherInfo", fn: otherInfoMatch },
    { name: "phonetic", fn: phoneticMatch },
    { name: "fuzzy", fn: fuzzyMatch },
  ];

  let bestWeight = 0;
  let bestStrategy = "";

  for (const strategy of strategies) {
    const weight = strategy.fn(searchTerm, person);
    if (weight !== null && weight > bestWeight) {
      bestWeight = weight;
      bestStrategy = strategy.name;
    }
  }

  if (bestWeight > 0) {
    bestWeight = Math.min(100, bestWeight + areaBonus(searchTerm, person));
    return {
      person,
      weight: bestWeight,
      matchedStrategy: bestStrategy,
    };
  }

  return null;
}
