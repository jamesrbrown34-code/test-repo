import { useState } from 'react'

export default function App() {
  const [ticker, setTicker] = useState('')
  const [quote, setQuote] = useState(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const onSearch = async (event) => {
    event.preventDefault()

    const cleanTicker = ticker.trim()
    if (!cleanTicker) {
      setError('Please enter a ticker symbol (for example: AAPL).')
      setQuote(null)
      return
    }

    setLoading(true)
    setError('')

    try {
      const response = await fetch(`/api/quote/${encodeURIComponent(cleanTicker)}`)
      const data = await response.json()

      if (!response.ok) {
        setError(data.error ?? 'Unable to fetch quote right now.')
        setQuote(null)
        return
      }

      setQuote(data)
    } catch {
      setError('Network error while fetching quote.')
      setQuote(null)
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="app-shell">
      <section className="card">
        <h1>Ticker Price Lookup</h1>
        <p>Search a stock ticker and get the latest market price from Yahoo Finance.</p>

        <form onSubmit={onSearch} className="search-form">
          <input
            value={ticker}
            onChange={(event) => setTicker(event.target.value.toUpperCase())}
            placeholder="Enter ticker (AAPL, MSFT, TSLA...)"
            aria-label="Ticker symbol"
          />
          <button disabled={loading}>{loading ? 'Searching...' : 'Search'}</button>
        </form>

        {error && <div className="error">{error}</div>}

        {quote && (
          <div className="result">
            <h2>{quote.ticker}</h2>
            <p className="price">
              {quote.currency} {Number(quote.price).toFixed(2)}
            </p>
            <p>Exchange: {quote.exchange}</p>
            <p className="timestamp">Fetched: {new Date(quote.fetchedAtUtc).toLocaleString()}</p>
          </div>
        )}
      </section>
    </main>
  )
}
