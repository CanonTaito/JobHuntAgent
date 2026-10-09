export type MatchReport = {
  overall: number
  similarity: number
  skills: number
  seniority: number
  fit: number
  summary: string
  strengths: string[]
  gaps: string[]
}

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''

export async function scoreJobMatch(jd: string, signal?: AbortSignal): Promise<MatchReport> {
  const response = await fetch(`${API_BASE}/api/match`, {
    method: 'POST',
    headers: { 'Content-Type': 'text/plain' },
    body: jd,
    signal,
  })

  if (!response.ok) {
    throw new Error(await readError(response))
  }

  const report = (await response.json()) as MatchReport
  return {
    ...report,
    strengths: trimmed(report.strengths),
    gaps: trimmed(report.gaps),
  }
}

function trimmed(items: string[]): string[] {
  return items.map((item) => item.trim())
}

async function readError(response: Response): Promise<string> {
  const fallback = `Match failed (HTTP ${response.status}).`
  try {
    const body = (await response.json()) as { error?: string }
    return body.error ?? fallback
  } catch {
    return fallback
  }
}
