import { useEffect, useState } from 'react'
import type { ChangeEvent, FormEvent } from 'react'
import { getCurrentProfile, uploadResume } from './resume'
import type { CandidateProfile } from './resume'

function ResumePage() {
  const [name, setName] = useState('')
  const [file, setFile] = useState<File | null>(null)
  const [profile, setProfile] = useState<CandidateProfile | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [loadingProfile, setLoadingProfile] = useState(true)
  const [showUpload, setShowUpload] = useState(false)

  useEffect(() => {
    const controller = new AbortController()

    async function load() {
      setLoadingProfile(true)
      setError(null)
      try {
        setProfile(await getCurrentProfile(controller.signal))
        setShowUpload(false)
      } catch (e) {
        if (controller.signal.aborted) {
          return
        }
        setProfile(null)
        setShowUpload(true)
      } finally {
        if (!controller.signal.aborted) {
          setLoadingProfile(false)
        }
      }
    }

    load()

    return () => {
      controller.abort()
    }
  }, [])

  function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const f = event.target.files?.[0] ?? null
    setFile(f)
    setError(null)
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!name.trim() || !file || loading) {
      return
    }

    setLoading(true)
    setError(null)
    try {
      const uploaded = await uploadResume(file, name.trim())
      setProfile(uploaded)
      setShowUpload(false)
      setFile(null)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Upload failed.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="scan">
      <p className="tagline">Upload your resume and the agent will parse it into a structured profile.</p>

      {loadingProfile && (
        <div className="scan-status" role="status" aria-live="polite">
          <p>Loading your saved profile...</p>
        </div>
      )}

      {profile && !loadingProfile && (
        <section className="result">
          <div className="result-header">
            <h2>Current profile</h2>
            <button
              type="button"
              className="tab"
              onClick={() => setShowUpload((s) => !s)}
            >
              {showUpload ? 'Hide upload form' : 'Replace profile'}
            </button>
          </div>
          <ProfileResult profile={profile} />
        </section>
      )}

      {!loadingProfile && (!profile || showUpload) && (
        <form className="scan-form" onSubmit={handleSubmit}>
          <label className="scan-label" htmlFor="resume-name">
            Your name
          </label>
          <input
            id="resume-name"
            className="scan-input"
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Canon Taito"
            spellCheck={false}
          />

          <label className="scan-label" htmlFor="resume-file">
            Resume file (.txt, .md, .markdown, .pdf, .docx)
          </label>
          <input
            id="resume-file"
            className="scan-input"
            type="file"
            accept=".txt,.md,.markdown,.pdf,.docx,text/plain,text/markdown,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            onChange={handleFileChange}
          />

          <button
            className="scan-button"
            type="submit"
            disabled={loading || !name.trim() || !file}
          >
            {loading ? 'Parsing…' : 'Upload resume'}
          </button>
        </form>
      )}

      <div className="scan-status" role="status" aria-live="polite">
        {loading && (
          <p>Parsing your resume with the local model. Small models can take a minute.</p>
        )}
        {error && <p className="scan-error">{error}</p>}
      </div>
    </main>
  )
}

function ProfileResult({ profile }: { profile: CandidateProfile }) {
  return (
    <section className="result">
      <h2>{profile.name}</h2>
      <p className="result-meta">
        {profile.headline ?? ''}
        {profile.location ? (profile.headline ? ` • ${profile.location}` : profile.location) : ''}
      </p>

      <div className="badges">
        {profile.seniority && <span className="badge">{profile.seniority}</span>}
        {profile.yearsOfExperience != null && (
          <span className="badge">{profile.yearsOfExperience} yrs</span>
        )}
      </div>

      {profile.summary && <p className="result-summary">{profile.summary}</p>}

      <div className="result-grid">
        {profile.skills.length > 0 && (
          <div className="result-section result-section-wide">
            <h3>Skills</h3>
            <ul className="chips">
              {profile.skills.map((s) => (
                <li className="chip" key={s}>
                  {s}
                </li>
              ))}
            </ul>
          </div>
        )}

        {profile.experience.length > 0 && (
          <div className="result-section result-section-wide">
            <h3>Experience</h3>
            <ul className="result-list">
              {profile.experience.map((exp, i) => (
                <li key={`${exp.company}-${i}`}>
                  <strong>{exp.title ?? ''}</strong>
                  {(exp.company || exp.period) && (
                    <div>
                      {exp.company}
                      {exp.company && exp.period ? ' • ' : ''}
                      {exp.period}
                    </div>
                  )}
                  {exp.highlights.length > 0 && (
                    <ul>
                      {exp.highlights.map((h, j) => (
                        <li key={`${i}-${j}`}>{h}</li>
                      ))}
                    </ul>
                  )}
                </li>
              ))}
            </ul>
          </div>
        )}

        {profile.education.length > 0 && (
          <div className="result-section">
            <h3>Education</h3>
            <ul className="result-list">
              {profile.education.map((ed, i) => (
                <li key={`${ed.institution}-${i}`}>
                  <div>{ed.qualification ?? ''}</div>
                  {(ed.institution || ed.year != null) && (
                    <div>
                      {ed.institution}
                      {ed.institution && ed.year != null ? ' • ' : ''}
                      {ed.year}
                    </div>
                  )}
                </li>
              ))}
            </ul>
          </div>
        )}

        {profile.highlights.length > 0 && (
          <div className="result-section">
            <h3>Highlights</h3>
            <ul className="result-list">
              {profile.highlights.map((h, i) => (
                <li key={`h-${i}`}>{h}</li>
              ))}
            </ul>
          </div>
        )}
      </div>
    </section>
  )
}

export default ResumePage
