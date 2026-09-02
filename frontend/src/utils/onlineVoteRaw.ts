import type { VoteDto } from "@/types/Vote";
import { resolveVoteStatus } from "@/utils/voteDtoNormalization";

export type OnlineRawVote = {
  first: string;
  last: string;
  otherInfo: string;
};

export type FindRawPart = "FL" | "F" | "L" | "O";

export type FindShortenState = {
  voteId: number;
  part: FindRawPart;
  lastNum: number;
  maxNum: number;
};

function asText(value: unknown): string {
  return typeof value === "string" ? value : "";
}

function parseNameText(text: string): OnlineRawVote {
  if (text.includes(",")) {
    const split = text.split(",");
    return {
      first: split.slice(1).join(",").trim(),
      last: split[0].trim(),
      otherInfo: text,
    };
  }

  const split = text.trim().split(/\s+/).filter(Boolean);
  return {
    first: split.slice(0, -1).join(" "),
    last: split.at(-1) ?? "",
    otherInfo: text,
  };
}

export function parseOnlineVoteRaw(raw?: string | null): OnlineRawVote | null {
  if (!raw || !raw.trim()) {
    return null;
  }

  const trimmed = raw.trim();
  if (trimmed.startsWith("{")) {
    try {
      const parsed = JSON.parse(trimmed) as Record<string, unknown>;
      const first = asText(parsed.First ?? parsed.first);
      const last = asText(parsed.Last ?? parsed.last);
      const otherInfo = asText(parsed.OtherInfo ?? parsed.otherInfo);
      if (first || last || otherInfo) {
        return { first, last, otherInfo };
      }
    } catch {
      // Fall through to free-text parsing
    }
  }

  return parseNameText(raw);
}

export function rawVoteDisplayName(raw: OnlineRawVote): string {
  const name = `${raw.first} ${raw.last}`.trim();
  return name || raw.otherInfo;
}

export function rawVoteReferenceName(raw: OnlineRawVote): string {
  return raw.otherInfo.trim() || rawVoteDisplayName(raw);
}

export function namePartsFromRaw(raw: OnlineRawVote): {
  first: string;
  last: string;
} {
  if (raw.first.trim() || raw.last.trim()) {
    return { first: raw.first.trim(), last: raw.last.trim() };
  }
  if (raw.otherInfo.trim()) {
    const parsed = parseNameText(raw.otherInfo);
    return { first: parsed.first, last: parsed.last };
  }
  return { first: "", last: "" };
}

/**
 * True when the vote still has unresolved `OnlineVoteRaw`: truthy raw text,
 * no person, no spoil reason, and status Raw or Ok. Online typed names and
 * CDN/import mismatches both store that payload. Paper votes without
 * `onlineVoteRaw` never qualify. Matching keeps the original text, so a
 * person or spoil reason is what clears the mark — not the absence of raw text.
 */
export function isUnresolvedRawVote(vote: VoteDto | null | undefined): boolean {
  if (!vote?.onlineVoteRaw) {
    return false;
  }
  if (vote.personGuid || vote.ineligibleReasonCode) {
    return false;
  }
  const status = resolveVoteStatus(vote).toLowerCase();
  return status === "raw" || status === "ok";
}

export function hasDisplayableRawVote(
  vote: VoteDto | null | undefined,
): boolean {
  return !!parseOnlineVoteRaw(vote?.onlineVoteRaw);
}

export function nextFindQuery(
  raw: OnlineRawVote,
  part: FindRawPart,
  voteId: number,
  previous: FindShortenState | null,
): { query: string; state: FindShortenState } {
  const source =
    part === "F" ? raw.first : part === "L" ? raw.last : raw.otherInfo;
  const maxNum =
    part === "FL" ? Math.max(raw.first.length, raw.last.length) : source.length;

  let dropCount = 0;
  if (previous && previous.part === part && previous.voteId === voteId) {
    dropCount = previous.maxNum - previous.lastNum + 1;
  }

  const trimBy = (value: string, dropped: number) =>
    value.slice(0, Math.max(0, value.length - dropped));

  const buildQuery = (dropped: number) =>
    (part === "FL"
      ? `${trimBy(raw.first, dropped)} ${trimBy(raw.last, dropped)}`
      : trimBy(source, dropped)
    ).trim();

  let query = buildQuery(dropCount);
  if (!query) {
    dropCount = 0;
    query = buildQuery(0);
  }

  return {
    query,
    state: {
      voteId,
      part,
      lastNum: maxNum - dropCount,
      maxNum,
    },
  };
}
