import { useState } from 'react'
import type { FormEvent } from 'react'
import { scanJobDescription } from './scan'
import type { JdAnalysis } from './scan'

function ScanPage() {
  const [jd, setJd] = useState('')
  const [analysis, setAnalysis] = useState<JdAnalysis | null>(null)
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
    setAnalysis(null)
    try {
      setAnalysis(await scanJobDescription(text))
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Scan failed.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="scan">
      <p className="tagline">Paste a job description to get a structured breakdown.</p>

      <form className="scan-form" onSubmit={handleSubmit}>
        <label className="scan-label" htmlFor="jd">
          Job description
        </label>
        <textarea
          id="jd"
          className="scan-input"
          value={jd}
          onChange={(e) => setJd(e.target.value)}
          placeholder={'Senior .NET Engineer\nFindex is hiring a Senior .NET Engineer to join our Payments platform team...'}
          rows={12}
          spellCheck={false}
        />
        <button className="scan-button" type="submit" disabled={loading || !jd.trim()}>
          {loading ? 'Scanning…' : 'Scan job description'}
        </button>
      </form>

      <div className="scan-status" role="status" aria-live="polite">
        {loading && (
          <p>Scanning with the local model. Small models can take a minute — the first run may also need to load the model.</p>
        )}
        {error && <p className="scan-error">{error}</p>}
      </div>

      {analysis && <ScanResult analysis={analysis} />}
    </main>
  )
}

function ScanResult({ analysis }: { analysis: JdAnalysis }) {
  return (
    <section className="result">
      <h2>{analysis.title}</h2>
      <p className="result-meta">
        {analysis.company ?? 'Unknown company'}
        {analysis.location ? ` · ${analysis.location}` : ''}
      </p>

      <div className="badges">
        <span className="badge">{analysis.employmentType}</span>
        <span className="badge">{analysis.remote}</span>
        <span className="badge">{analysis.seniority}</span>
        {analysis.salaryText && <span className="badge badge-salary">{analysis.salaryText}</span>}
      </div>

      <p className="result-summary">{analysis.summary}</p>

      <div className="result-grid">
        <ChipSection title="Required skills" items={analysis.requiredSkills} />
        <ChipSection title="Nice to have" items={analysis.niceToHaveSkills} />
        <ListSection title="Responsibilities" items={analysis.responsibilities} />
        <ListSection title="Benefits" items={analysis.benefits} />
        <ChipSection title="Keywords" items={analysis.keywords} wide />
      </div>
    </section>
  )
}

function ChipSection({ title, items, wide }: { title: string; items: string[]; wide?: boolean }) {
  if (items.length === 0) {
    return null
  }

  return (
    <div className={wide ? 'result-section result-section-wide' : 'result-section'}>
      <h3>{title}</h3>
      <ul className="chips">
        {items.map((item) => (
          <li className="chip" key={item}>
            {item}
          </li>
        ))}
      </ul>
    </div>
  )
}

function ListSection({ title, items }: { title: string; items: string[] }) {
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

export default ScanPage