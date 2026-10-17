export type CandidateProfile = {
  name: string
  headline: string | null
  location: string | null
  yearsOfExperience: number | null
  seniority: string | null
  summary: string | null
  skills: string[]
  experience: ProfileExperience[]
  education: ProfileEducation[]
  highlights: string[]
  preferences: ProfilePreferences
}

export type ProfileExperience = {
  company: string | null
  title: string | null
  period: string | null
  highlights: string[]
}

export type ProfileEducation = {
  qualification: string | null
  institution: string | null
  year: number | null
}

export type ProfilePreferences = {
  roles: string[]
  locations: string[]
  workModels: string[]
  employmentTypes: string[]
  salaryExpectation: SalaryExpectation | null
}

export type SalaryExpectation = {
  min: number | null
  max: number | null
  currency: string | null
}

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''

export async function uploadResume(file: File, name: string, signal?: AbortSignal): Promise<CandidateProfile> {
  const form = new FormData()
  form.append('name', name)
  form.append('file', file)

  const response = await fetch(`${API_BASE}/api/resume`, {
    method: 'POST',
    body: form,
    signal,
  })

  if (!response.ok) {
    throw new Error(await readError(response))
  }

  return (await response.json()) as CandidateProfile
}

async function readError(response: Response): Promise<string> {
  const fallback = `Upload failed (HTTP ${response.status}).`
  try {
    const body = (await response.json()) as { error?: string; detail?: string; title?: string }
    return body.error ?? body.detail ?? body.title ?? fallback
  } catch {
    return fallback
  }
}
