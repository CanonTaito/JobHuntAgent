import { useState } from 'react'
import ScanPage from './ScanPage'
import MatchPage from './MatchPage'

type Tab = 'scan' | 'match'

function App() {
  const [tab, setTab] = useState<Tab>('scan')

  return (
    <div className="app">
      <header className="app-header">
        <h1>JobHuntAgent</h1>
        <nav className="tabs" aria-label="Features">
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
      {tab === 'scan' ? <ScanPage /> : <MatchPage />}
    </div>
  )
}

export default App
