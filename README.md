# Ticker Search App (React + C#)

This project includes:

- **Backend**: ASP.NET Core minimal API (`/backend`) that fetches latest ticker prices from Yahoo Finance.
- **Frontend**: React + Vite app (`/frontend`) for searching a ticker and displaying the latest price.

## Run backend

```bash
cd backend
dotnet run --urls http://localhost:5107
```

## Run frontend

```bash
cd frontend
npm install
npm run dev
```

Open http://localhost:5173 and search for symbols like `AAPL`, `MSFT`, or `TSLA`.
