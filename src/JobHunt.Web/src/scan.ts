export type JdAnalysis = {
  title: string
  company: string | null
  location: string | null
  employmentType: string
  remote: string
  seniority: string
  salaryText: string | null
  requiredSkills: string[]
  niceToHaveSkills: string[]
  responsibilities: string[]
  benefits: string[]
  summary: string
  keywords: string[]
}

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''

export async function scanJobDescription(jd: string, signal?: AbortSignal): Promise<JdAnalysis> {
  const response = await fetch(`${API_BASE}/api/scan`, {
    method: 'POST',
    headers: { 'Content-Type': 'text/plain' },
    body: jd,
    signal,
  })

  if (!response.ok) {
    throw new Error(await readError(response))
  }

  const analysis = (await response.json()) as JdAnalysis
  return {
    ...analysis,
    requiredSkills: trimmed(analysis.requiredSkills),
    niceToHaveSkills: trimmed(analysis.niceToHaveSkills),
    responsibilities: trimmed(analysis.responsibilities),
    benefits: trimmed(analysis.benefits),
    keywords: trimmed(analysis.keywords),
  }
}

// Small models occasionally emit list items with stray whitespace (e.g. " .NET").
function trimmed(items: string[]): string[] {
  return items.map((item) => item.trim())
}

async function readError(response: Response): Promise<string> {
  try {
    const body = (await response.json()) as { error?: string }
    if (body.error) {
      return body.error
    }
  } catch {
    // non-JSON error body; fall back to the status code
  }

  return `Scan failed (HTTP ${response.status}).`
}