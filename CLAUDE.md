# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this project is
A personal French-language job application tracker hosted on GitHub Pages. Data is stored in a private GitHub Gist (`applications.json`), read/written via the GitHub API. No build step, no framework, no backend.

## Commands
```bash
# Run unit tests (pure logic only, no browser needed)
node js/app.test.js

# Local dev server (any static server works)
npx serve .
```

GitHub Pages serves from branch `main`, folder `/` (root). Live at `https://mathieu-chateigner.github.io/JobTracker/`.

## Stack
- Vanilla HTML/CSS/JS (ES2020), no bundler
- GitHub Gist API v3 as the database
- localStorage for PAT, Gist ID, and Google Maps API key
- DM Serif Display + DM Sans fonts

## Architecture

### Two shared JS files loaded by every page
- **`js/gist.js`** — side-effectful: `getSettings()`, `saveSettings()`, `hasSettings()`, `redirectIfNoSettings()`, `readApplications()`, `writeApplications()`, `createGist()`, `testConnection()`. Reads/writes localStorage and calls the GitHub API.
- **`js/app.js`** — pure logic + UI helpers: status constants, auto-transitions, sort, formatters, toast, Google Maps autocomplete loader. Has a CommonJS export guard at the bottom so `app.test.js` can `require()` it in Node.

Every page loads both scripts before its own inline `<script>`.

### Status enum (`js/app.js` → `STATUS`)
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
CSS class names come from the hardcoded `STATUS_CLASS` map (int → lowercase string). **Never derive class names from `STATUS_LABEL` text** — accents in French labels break the match.

### Auto-transitions (`applyAutoTransitions` in `app.js`)
Called on every page load that reads data. Two rules:
1. Interview status with a past `interviewDate` → corresponding `Attente*` status (clears `interviewDate`)
2. `Candidature` with `appliedDate` ≥ 21 days ago → `SansReponse`

If `changed === true` the caller must `writeApplications()` back to the Gist.

### Sort order (`sortApplications`)
Default (`date`) sort: upcoming interviews first, then by `statusSortOrder()` (Offre → … → SansReponse), then by `interviewDate`, then by `appliedDate` desc.

### Status colors (`css/site.css`)
Defined as CSS variables under `/* ── Status color map ── */`. Four rule sets must stay in sync when adding a status: `.status-dot`, `.breakdown-bar`, `.status-badge`, `.status-select`.

### Data shape (each entry in `applications.json`)
```js
{
  id, companyName, jobTitle, jobUrl, location,
  appliedDate,       // ISO string, time always T00:00:00
  status,            // int
  interviewDate,     // ISO datetime string or null
  lastResponseDate,  // date string YYYY-MM-DD or null
  salaryExpectation, // int (€) or null
  rating,            // 1-5 or null
  refusalMailUrl,    // string or null
  teamsUrl,          // string or null
  notes,             // string or null
  createdAt, updatedAt
}
```

### URL auto-fill (`create.html`)
Uses `https://api.allorigins.win/raw?url=` as a CORS proxy to fetch job page HTML, then parses `og:title`, `og:site_name`, and `<title>` tags. Falls back gracefully with a warning if the site is protected.

### Settings redirect
`index.html` handles the no-settings redirect inline (`if (!hasSettings()) ...`) because `redirectIfNoSettings()` in `gist.js` hardcodes `./settings.html`, which only resolves correctly from pages in the same directory. The four `docs/` pages use `redirectIfNoSettings()` directly.
