/** Color mappings for consistent styling across front-desk filters and table. */
export const methodTagColors: Record<string, string> = {
  "In Person": "#10b981", // green (success)
  Mail: "#3b82f6", // blue
  Online: "#6366f1", // indigo / primary-ish
  "Call-In": "#f59e0b", // amber
};

const flagColorPalette = [
  "#8b5cf6", // violet
  "#ec4899", // pink
  "#14b8a6", // teal
  "#f97316", // orange
  "#06b6d4", // cyan
  "#a855f7", // purple
];

export function getFlagColor(flag: string, electionFlags: unknown[]): string {
  if (!electionFlags || electionFlags.length === 0) {
    return "#64748b";
  }
  const index = electionFlags.findIndex((f) => f === flag);
  return flagColorPalette[index % flagColorPalette.length] || "#64748b";
}

export function getFlagAbbr(flag: string, electionFlags: unknown[]): string {
  const found = electionFlags?.find((f) => f === flag);
  if (found && typeof found === "object" && found !== null && "abbr" in found) {
    return String(
      (found as { abbr?: string }).abbr || flag.substring(0, 1).toUpperCase(),
    );
  }
  return flag.substring(0, 1).toUpperCase();
}

export function getMethodFilterStyle(method: string, isActive: boolean) {
  const color = methodTagColors[method] || "#64748b";
  if (isActive) {
    return {
      backgroundColor: color,
      color: "#ffffff",
      borderColor: color,
    };
  }
  return {
    backgroundColor: "#ffffff",
    color: color,
    borderColor: color,
    borderWidth: "1.5px",
  };
}

export function getFlagFilterStyle(
  flag: string,
  electionFlags: unknown[],
  isActive: boolean,
) {
  const color = getFlagColor(flag, electionFlags);
  if (isActive) {
    return {
      backgroundColor: color,
      color: "#ffffff",
      borderColor: color,
    };
  }
  return {
    backgroundColor: "#ffffff",
    color: color,
    borderColor: color,
    borderWidth: "1.5px",
  };
}
