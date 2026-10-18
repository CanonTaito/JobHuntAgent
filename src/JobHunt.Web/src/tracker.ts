import type { CandidateProfile } from './resume'

export type ApplicationView = {
  id: number
  source: string
  sourceId: string
  title: string
  company: string
  location: string | null
  workModel: string | null
  url: string | null
  salaryMin: number | null
  salaryMax: number | null
  salaryCurrency: string | null
  salaryBand: string | null
  salarySource: string
  status: string
  overallScore: number | null
  similarityScore: number | null
  skillsScore: number | null
  seniorityScore: number | null
  fitScore: number | null
  matchSummary: string | null
  tailoredBullets: string[]
  coverLetter: string | null
  tailoredAt: string | null
  tailoredProfileJson?: string
  tailoredProfile?: CandidateProfile | null
  postedAt: string | null
  discoveredAt: string
  updatedAt: string
}

export type DiscoveryReport = {
  discovered: number
  scored: number
  skipped: number
  items: DiscoveryItem[]
}

export type DiscoveryItem = {
  applicationId: number
  sourceId: string
  title: string
  company: string
  overall: number
  salaryBand: string | null
  status: string
}

export type TailoringDraft = {
  resumeBullets: string[]
  coverLetter: string
}

export const SALARY_BANDS = [
  'Under 80k',
  '80-99k',
  '100-119k',
  '120-149k',
  '150-199k',
  '200k+',
] as const

export const STATUS_ORDER = [
  'Discovered',
  'Scored',
  'Shortlisted',
  'Tailored',
  'AwaitingApproval',
  'Applied',
  'Rejected',
  'Archived',
] as const

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''

export async function listApplications(status?: string): Promise<ApplicationView[]> {
  const query = status ? `?status=${encodeURIComponent(status)}` : ''
  const response = await fetch(`${API_BASE}/api/applications${query}`)
  if (!response.ok) {
    throw new Error(await readError(response, 'Could not load applications'))
  }

  const apps = (await response.json()) as ApplicationView[]
  // Safely parse any TailoredProfileJson returned from the server into a
  // CandidateProfile object for easier consumption by the UI.
  for (const app of apps) {
    if (app.tailoredProfileJson) {
      try {
        app.tailoredProfile = JSON.parse(app.tailoredProfileJson) as CandidateProfile
      } catch (e) {
        // ignore parse errors and leave tailoredProfile undefined
        // eslint-disable-next-line no-console
        console.warn('Failed to parse tailoredProfileJson for application', app.id, e)
        app.tailoredProfile = null
      }
    }
  }

  return apps
}

export async function discoverJobs(max = 5): Promise<DiscoveryReport> {
  const response = await fetch(`${API_BASE}/api/discover?max=${max}`, { method: 'POST' })
  if (!response.ok) {
    throw new Error(await readError(response, 'Discovery failed'))
  }

  return (await response.json()) as DiscoveryReport
}

export function tailorApplication(id: number): Promise<TailoringDraft> {
  return post<TailoringDraft>(`/api/applications/${id}/tailor`, 'Tailoring failed')
}

export function requestApproval(id: number): Promise<ApplicationView> {
  return post<ApplicationView>(`/api/applications/${id}/request-approval`, 'Could not request approval')
}

export function approveApplication(id: number): Promise<ApplicationView> {
  return post<ApplicationView>(`/api/applications/${id}/approve`, 'Approval failed')
}

export function rejectApplication(id: number): Promise<ApplicationView> {
  return post<ApplicationView>(`/api/applications/${id}/reject`, 'Rejection failed')
}

async function post<T>(path: string, fallback: string): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, { method: 'POST' })
  if (!response.ok) {
    throw new Error(await readError(response, fallback))
  }

  return (await response.json()) as T
}

async function readError(response: Response, fallback: string): Promise<string> {
  const message = `${fallback} (HTTP ${response.status}).`
  try {
    const body = (await response.json()) as { error?: string }
    return body.error ?? message
  } catch {
    return message
  }
}
