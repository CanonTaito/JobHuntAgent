import { useState } from 'react'
import ScanPage from './ScanPage'
import MatchPage from './MatchPage'
import TrackerPage from './TrackerPage'

type Tab = 'tracker' | 'scan' | 'match'

function App() {
  const [tab, setTab] = useState<Tab>('tracker')

  return (
    <div className="app">
      <header className="app-header">
        <h1>JobHuntAgent</h1>
        <nav className="tabs" aria-label="Features">
          <button
            type="button"
            className={tab === 'tracker' ? 'tab tab-active' : 'tab'}
            aria-current={tab === 'tracker' ? 'page' : undefined}
            onClick={() => setTab('tracker')}
          >
            Tracker
          </button>
          <button
            type="button"
            className={tab === 'scan' ? 'tab tab-active' : 'tab'}
            aria-current={tab === 'scan' ? 'page' : undefined}
            onClick={() => setTab('scan')}
          >
            Scan a job description
          </button>
          <button
            type="button"
            className={tab === 'match' ? 'tab tab-active' : 'tab'}
            aria-current={tab === 'match' ? 'page' : undefined}
            onClick={() => setTab('match')}
          >
            Score a match
          </button>
        </nav>
      </header>
      {tab === 'tracker' && <TrackerPage />}
      {tab === 'scan' && <ScanPage />}
      {tab === 'match' && <MatchPage />}
    </div>
  )
}

export default App
