import { useState } from 'react'
import type { FormEvent } from 'react'
import { scoreJobMatch } from './match'
import type { MatchReport } from './match'

function MatchPage() {
  const [jd, setJd] = useState('')
  const [report, setReport] = useState<MatchReport | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const text = jd.trim()
    if (!text || loading) {
      return
    }

    setLoading(true)
    setError(null)
    setReport(null)
    try {
      setReport(await scoreJobMatch(text))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Match failed.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="scan">
      <p className="tagline">Paste a job description to score it against your profile.</p>

      <form className="scan-form" onSubmit={handleSubmit}>
        <label className="scan-label" htmlFor="match-jd">
          Job description
        </label>
        <textarea
          id="match-jd"
          className="scan-input"
          value={jd}
          onChange={(e) => setJd(e.target.value)}
          placeholder={'Senior .NET Engineer\nFindex is hiring a Senior .NET Engineer to join our Payments platform team...'}
          rows={12}
          spellCheck={false}
        />
        <button className="scan-button" type="submit" disabled={loading || !jd.trim()}>
          {loading ? 'Scoring…' : 'Score match'}
        </button>
      </form>

      <div className="scan-status" role="status" aria-live="polite">
        {loading && (
          <p>Scoring with the local model. This runs the match agent plus an embedding, so give it a minute.</p>
        )}
        {error && <p className="scan-error">{error}</p>}
      </div>

      {report && <ScoreBreakdownCard report={report} />}
    </main>
  )
}

function ScoreBreakdownCard({ report }: { report: MatchReport }) {
  return (
    <section className="match-card">
      <div className="match-overall">
        <span className={`match-overall-value ${tone(report.overall)}`}>{report.overall}</span>
        <span className="match-overall-label">Overall match</span>
      </div>

      <div className="match-bars">
        <ScoreBar label="Skills" value={report.skills} />
        <ScoreBar label="Seniority" value={report.seniority} />
        <ScoreBar label="Fit" value={report.fit} />
        <ScoreBar label="Similarity" value={report.similarity} />
      </div>

      <p className="match-summary">{report.summary}</p>

      <div className="match-grid">
        <ScoreList title="Strengths" items={report.strengths} />
        <ScoreList title="Gaps" items={report.gaps} />
      </div>
    </section>
  )
}

function ScoreBar({ label, value }: { label: string; value: number }) {
  const clamped = Math.max(0, Math.min(100, value))
  return (
    <div className="match-bar">
      <span className="match-bar-label">{label}</span>
      <span className="match-bar-track">
        <span className={`match-bar-fill ${tone(clamped)}`} style={{ width: `${clamped}%` }} />
      </span>
      <span className="match-bar-value">{clamped}</span>
    </div>
  )
}

function ScoreList({ title, items }: { title: string; items: string[] }) {
  if (items.length === 0) {
    return null
  }

  return (
    <div className="result-section">
      <h3>{title}</h3>
      <ul className="result-list">
        {items.map((item) => (
          <li key={item}>{item}</li>
        ))}
      </ul>
    </div>
  )
}

function tone(value: number): string {
  if (value >= 75) {
    return 'tone-good'
  }
  if (value >= 50) {
    return 'tone-mid'
  }
  return 'tone-low'
}

export default MatchPage
