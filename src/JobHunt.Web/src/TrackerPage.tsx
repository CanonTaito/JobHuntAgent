import { useEffect, useState } from 'react'
import {
  SALARY_BANDS,
  STATUS_ORDER,
  approveApplication,
  discoverJobs,
  listApplications,
  rejectApplication,
  requestApproval,
  tailorApplication,
} from './tracker'
import type { ApplicationView } from './tracker'

type GroupBy = 'band' | 'status' | 'none'
type ActionKind = 'tailor' | 'request' | 'approve' | 'reject'

function TrackerPage() {
  const [applications, setApplications] = useState<ApplicationView[]>([])
  const [groupBy, setGroupBy] = useState<GroupBy>('band')
  const [onlyAwaiting, setOnlyAwaiting] = useState(false)
  const [expandedId, setExpandedId] = useState<number | null>(null)
  const [pending, setPending] = useState<number | null>(null)
  const [loading, setLoading] = useState(true)
  const [discovering, setDiscovering] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  async function refresh(showSpinner = true) {
    if (showSpinner) {
      setLoading(true)
    }

    setError(null)
    try {
      setApplications(await listApplications())
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Could not load applications.')
    } finally {
      if (showSpinner) {
        setLoading(false)
      }
    }
  }

  useEffect(() => {
    let active = true
    void (async () => {
      try {
        const loaded = await listApplications()
        if (active) {
          setApplications(loaded)
        }
      } catch (e) {
        if (active) {
          setError(e instanceof Error ? e.message : 'Could not load applications.')
        }
      } finally {
        if (active) {
          setLoading(false)
        }
      }
    })()

    return () => {
      active = false
    }
  }, [])

  async function handleDiscover() {
    setDiscovering(true)
    setError(null)
    setNotice(null)
    try {
      const report = await discoverJobs(5)
      setNotice(
        `Search complete: ${report.discovered} found, ${report.scored} scored, ${report.skipped} already tracked.`,
      )
      await refresh(false)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Discovery failed.')
    } finally {
      setDiscovering(false)
    }
  }

  async function handleAction(id: number, kind: ActionKind) {
    setPending(id)
    setError(null)
    setNotice(null)
    try {
      switch (kind) {
        case 'tailor':
          await tailorApplication(id)
          setNotice('Draft created — request approval when it looks right.')
          break
        case 'request':
          await requestApproval(id)
          setNotice('Sent for your approval.')
          break
        case 'approve':
          await approveApplication(id)
          setNotice('Approved — marked as applied.')
          break
        case 'reject':
          await rejectApplication(id)
          setNotice('Application rejected.')
          break
      }

      await refresh(false)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Action failed.')
    } finally {
      setPending(null)
    }
  }

  const visible = onlyAwaiting
    ? applications.filter((application) => application.status === 'AwaitingApproval')
    : applications
  const groups = groupApplications(visible, groupBy)

  return (
    <main className="tracker">
      <p className="tagline">
        Your applications, ranked and grouped by salary band. Nothing is applied without you.
      </p>

      <div className="tracker-controls">
        <button className="scan-button" type="button" onClick={handleDiscover} disabled={discovering}>
          {discovering ? 'Searching…' : 'Search jobs'}
        </button>
        <label className="tracker-field">
          Group by
          <select value={groupBy} onChange={(e) => setGroupBy(e.target.value as GroupBy)}>
            <option value="band">Salary band</option>
            <option value="status">Status</option>
            <option value="none">None</option>
          </select>
        </label>
        <label className="tracker-check">
          <input
            type="checkbox"
            checked={onlyAwaiting}
            onChange={(e) => setOnlyAwaiting(e.target.checked)}
          />
          Awaiting approval only
        </label>
        <button className="job-action job-action-ghost" type="button" onClick={() => refresh()} disabled={loading}>
          Refresh
        </button>
      </div>

      <div className="tracker-status" role="status" aria-live="polite">
        {loading && <p>Loading applications…</p>}
        {discovering && <p>Searching with the local model — this can take a minute.</p>}
        {notice && <p>{notice}</p>}
        {error && <p className="scan-error">{error}</p>}
      </div>

      {applications.length === 0 && !loading ? (
        <p className="tracker-empty">
          No applications yet. Hit <strong>Search jobs</strong> to discover and score listings from your
          profile preferences.
        </p>
      ) : visible.length === 0 ? (
        <p className="tracker-empty">Nothing awaiting approval.</p>
      ) : (
        groups.map((group) => (
          <section className="tracker-group" key={group.key}>
            <h2 className="tracker-group-heading">
              {group.key}
              <span className="tracker-group-count">{group.items.length}</span>
            </h2>
            <div className="job-list">
              {group.items.map((application) => (
                <JobCard
                  key={application.id}
                  application={application}
                  expanded={expandedId === application.id}
                  pending={pending === application.id}
                  onToggle={() =>
                    setExpandedId((current) =>
                      current === application.id ? null : application.id,
                    )
                  }
                  onAction={handleAction}
                />
              ))}
            </div>
          </section>
        ))
      )}
    </main>
  )
}

function JobCard({
  application,
  expanded,
  pending,
  onToggle,
  onAction,
}: {
  application: ApplicationView
  expanded: boolean
  pending: boolean
  onToggle: () => void
  onAction: (id: number, kind: ActionKind) => void
}) {
  const canTailor =
    application.status === 'Scored' ||
    application.status === 'Discovered' ||
    application.status === 'Shortlisted'

  return (
    <article className="job-card">
      <div className="job-card-head">
        <div>
          <h3 className="job-title">{application.title}</h3>
          <p className="job-company">
            {application.company}
            {application.location ? ` · ${application.location}` : ''}
            {application.workModel ? ` · ${application.workModel}` : ''}
          </p>
        </div>
        <span className={`status-badge status-${application.status.toLowerCase()}`}>
          {application.status}
        </span>
      </div>

      <div className="job-facts">
        <span className="badge badge-salary">{formatSalary(application)}</span>
        {application.overallScore != null && (
          <span className="job-score">{application.overallScore} match</span>
        )}
      </div>

      {application.overallScore != null && <MiniBars application={application} />}

      <div className="job-actions">
        {canTailor && (
          <button className="job-action" type="button" disabled={pending} onClick={() => onAction(application.id, 'tailor')}>
            Tailor
          </button>
        )}
        {application.status === 'Tailored' && (
          <button className="job-action" type="button" disabled={pending} onClick={() => onAction(application.id, 'request')}>
            Request approval
          </button>
        )}
        {application.status === 'AwaitingApproval' && (
          <>
            <button className="job-action job-action-primary" type="button" disabled={pending} onClick={() => onAction(application.id, 'approve')}>
              Approve
            </button>
            <button className="job-action" type="button" disabled={pending} onClick={() => onAction(application.id, 'reject')}>
              Reject
            </button>
          </>
        )}
        <button className="job-action job-action-ghost" type="button" onClick={onToggle}>
          {expanded ? 'Hide details' : 'Details'}
        </button>
      </div>

      {expanded && <JobDetail application={application} />}
    </article>
  )
}

function MiniBars({ application }: { application: ApplicationView }) {
  const bars: [string, number | null][] = [
    ['Skills', application.skillsScore],
    ['Seniority', application.seniorityScore],
    ['Fit', application.fitScore],
    ['Similarity', application.similarityScore],
  ]

  return (
    <div className="mini-bars">
      {bars.map(([label, value]) =>
        value == null ? null : (
          <div className="mini-bar" key={label}>
            <span className="mini-bar-label">{label}</span>
            <span className="mini-bar-track">
              <span className="mini-bar-fill" style={{ width: `${clamp(value)}%` }} />
            </span>
            <span className="mini-bar-value">{value}</span>
          </div>
        ),
      )}
    </div>
  )
}

function JobDetail({ application }: { application: ApplicationView }) {
  return (
    <div className="job-detail">
      {application.matchSummary && <p className="job-summary">{application.matchSummary}</p>}

      {application.tailoredBullets.length > 0 && (
        <div className="job-detail-section">
          <h4>Tailored resume bullets</h4>
          <ul className="result-list">
            {application.tailoredBullets.map((bullet) => (
              <li key={bullet}>{bullet}</li>
            ))}
          </ul>
        </div>
      )}

      {application.coverLetter && (
        <div className="job-detail-section">
          <h4>Cover letter</h4>
          <pre className="cover-letter">{application.coverLetter}</pre>
        </div>
      )}

      {application.url && (
        <p className="job-detail-section">
          <a href={application.url} target="_blank" rel="noreferrer">
            Open listing
          </a>
        </p>
      )}
    </div>
  )
}

function groupApplications(
  applications: ApplicationView[],
  groupBy: GroupBy,
): { key: string; items: ApplicationView[] }[] {
  if (groupBy === 'none') {
    return applications.length === 0 ? [] : [{ key: 'All applications', items: applications }]
  }

  if (groupBy === 'band') {
    const buckets = [...SALARY_BANDS, 'Unknown']
    return buckets
      .map((band) => ({
        key: band,
        items: applications.filter((application) => (application.salaryBand ?? 'Unknown') === band),
      }))
      .filter((group) => group.items.length > 0)
  }

  return STATUS_ORDER.map((status) => ({
    key: status,
    items: applications.filter((application) => application.status === status),
  })).filter((group) => group.items.length > 0)
}

function formatSalary(application: ApplicationView): string {
  const { salaryMin, salaryMax, salaryCurrency } = application
  if (salaryMin == null && salaryMax == null) {
    return application.salaryBand
      ? `${application.salaryBand} (${application.salarySource.toLowerCase()})`
      : 'Salary unknown'
  }

  const currency = salaryCurrency ? `${salaryCurrency} ` : ''
  if (salaryMin != null && salaryMax != null) {
    return `${currency}${asThousands(salaryMin)}–${asThousands(salaryMax)}`
  }

  return `${currency}${asThousands((salaryMin ?? salaryMax) as number)}`
}

function asThousands(amount: number): string {
  return `${Math.round(amount / 1000)}k`
}

function clamp(value: number): number {
  return Math.max(0, Math.min(100, value))
}

export default TrackerPage
