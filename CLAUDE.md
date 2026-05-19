# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this project is
A personal French-language job application tracker web app built with ASP.NET Core 9 MVC + SQLite. Local-only, no auth, no deployment.

## Commands
```bash
cd JobTracker
dotnet restore
dotnet build
dotnet run                  # serves at https://localhost:5001
# First time only — install Playwright's Chromium browser:
powershell bin/Debug/net9.0/playwright.ps1 install chromium
```

## GitHub Pages (static version)
Served from `docs/` — configure GitHub Pages in repo settings to use branch `main`, folder `/docs`.
One-time setup per device: visit `/settings.html`, enter your GitHub PAT (gist scope) and Gist ID.
Data migration from .NET app: Export → JSON → Import in the static app.
Tests: `node docs/js/app.test.js`

## Stack
- **Framework**: ASP.NET Core 9 MVC, .NET 9
- **Database**: SQLite via EF Core 9 (`jobtracker.db`, auto-created on startup)
- **Scraping**: Microsoft.Playwright (headless Chromium) — bypasses Indeed's Cloudflare bot protection
- **Maps**: Google Places Autocomplete API for the Location field (`appsettings.json` → `GoogleMaps:ApiKey`)
- **Frontend**: Vanilla JS, custom CSS (no frameworks), DM Serif Display + DM Sans fonts

## Architecture

### No EF Migrations
`Program.cs` calls `EnsureCreated()` then manually `ALTER TABLE ADD COLUMN` for any new columns via `PRAGMA table_info`. **When adding a new column to the model, also add the ALTER TABLE check in `Program.cs`.**

### ApplicationStatus enum (Models/JobApplication.cs)
```
0  Candidature
1  AppelRecruteur      — recruiter reached out
2  EntretienRecruteur
3  AttenteRecruteur    — waiting after recruiter interview
4  EntretienTech
5  AttenteTech         — waiting after tech interview
6  TestTechnique
7  AttenteTest         — waiting after technical test
8  EntretienRH
9  AttenteRH           — waiting after HR interview
10 Offre
11 Refuse
12 SansReponse
```
CSS class names are derived from `status.ToString().ToLower()` in Razor. JS uses a hardcoded `STATUS_CLASSES` map (int → class name) — **do NOT derive class names from French label text**, accents break the match.

### Auto-transitions (ApplicationsController.Index)
Two background transitions run on every list load:
1. Past `InterviewDate` on active interview statuses → corresponding `Attente*` status
2. `Candidature` with no response after 21 days → `SansReponse`

When setting a terminal status (`Refuse`, `SansReponse`) via `QuickStatus`, `InterviewDate` is cleared so the entry doesn't stay pinned at the top of the default sort.

### Default sort order
Upcoming events first (InterviewDate ≥ today), then by `StatusSortOrder()` (Offre → AttenteRH → … → SansReponse), then by InterviewDate, then by AppliedDate desc.

### Status colors
All defined as CSS variables in `site.css` under `/* ── Status color map ── */`. Four rule sets must stay in sync: `.status-dot`, `.breakdown-bar`, `.status-badge`, `.status-select`. When adding a new status, update all four.

### CV page
`CvController` stores the uploaded PDF at `wwwroot/cv/cv.pdf` (fixed path, one file). Displayed via browser's native `<embed>`.

### Export/Import
- Export: JSON download of all rows
- Import: resets `Id = 0` before insert, skips entries whose original ID already exists

### AJAX endpoints
- `POST /Applications/QuickStatus` — inline status update from list table
- `POST /Applications/UpdateInterviewDate` — inline date picker in list table
- `GET /Applications/FetchJobInfo?url=` — Playwright scrape, returns `{success, jobTitle, companyName, location, error}`
