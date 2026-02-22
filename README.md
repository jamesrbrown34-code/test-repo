# Ticker Search App (React + C#)

This repository contains a React frontend and a proper C# solution-based backend.

## Project structure

- `frontend/` → React + Vite UI.
- `backend/TickerSearch.sln` → .NET solution file.
- `backend/src/TickerSearch.Api/` → ASP.NET Core Web API (minimal API) with Yahoo Finance integration.

## Run backend

```bash
cd backend
dotnet run --project src/TickerSearch.Api/TickerSearch.Api.csproj --urls http://localhost:5107
```

Available backend endpoints:

- `GET /health`
- `GET /api/quote/{ticker}`

## Run frontend

```bash
cd frontend
npm install
npm run dev
```

Then open http://localhost:5173 and search symbols like `AAPL`, `MSFT`, or `TSLA`.


## Notes

- The backend sends browser-like headers and uses a small retry/backoff strategy for Yahoo `429 Too Many Requests` responses.
