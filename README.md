# JobTracker

A clean ASP.NET Core 9 MVC app to track your job applications, with automatic
job info extraction from Indeed (and other job sites) via a real headless browser.

## Setup

### Requirements
- .NET 9 SDK — https://dotnet.microsoft.com/download
- No SQL Server needed — uses SQLite (zero config, file-based)

### First-time setup

```bash
cd JobTracker

# 1. Restore packages
dotnet restore

# 2. Install Playwright's Chromium browser (one-time, ~120MB)
dotnet build
pwsh bin/Debug/net9.0/playwright.ps1 install chromium
# If you don't have PowerShell, use:
# node node_modules/.bin/playwright install chromium

# 3. Run
dotnet run
```

Open https://localhost:5001 in your browser.
The SQLite database (jobtracker.db) is created automatically on first run.

### Playwright install note
If `pwsh` is not found, you can also run:
```
dotnet run  # it will fail with a clear message pointing to the install command
```
Playwright will tell you exactly what to run to install the browser.

## Features
- **Dashboard** — stats (total, in-progress, offers, rejections) + breakdown bars + recent list
- **Applications list** — search, filter by status, sort
- **URL auto-fill** — paste an Indeed (or any job site) URL, a headless Chromium browser
  opens it and extracts job title, company, and location automatically
- **Add / Edit / Delete** applications
- **Quick status update** inline in the list

## Status flow
Applied → Phone Screen → Interview → Technical Test → Offer
                                  ↘ Rejected / Withdrawn / No Response

## Structure
```
JobTracker/
├── Controllers/
│   ├── HomeController.cs
│   └── ApplicationsController.cs   ← includes FetchJobInfo endpoint
├── Data/AppDbContext.cs
├── Models/JobApplication.cs
├── Services/
│   └── JobScraperService.cs        ← Playwright-based scraper
├── Views/
│   ├── Home/Index.cshtml           ← Dashboard
│   ├── Applications/
│   │   ├── Index.cshtml
│   │   ├── Create.cshtml           ← URL import panel
│   │   ├── Edit.cshtml
│   │   └── _Form.cshtml
│   └── Shared/_Layout.cshtml
└── wwwroot/css/site.css
```
