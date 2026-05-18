# Design: GitHub Pages + GitHub Gist static rewrite

**Date:** 2026-05-18  
**Status:** Approved

## Goal

Convert the ASP.NET Core 9 MVC app into a static site hosted on GitHub Pages, with all data persisted in a private GitHub Gist (acting as the database). This enables cross-machine sync without any additional hosting or services.

---

## Architecture

### Hosting
- GitHub Pages serves the `docs/` subdirectory of the existing repo.
- The .NET code remains in the repo root, unchanged and still runnable locally.

### Database
- One private GitHub Gist owned by the user.
- Contains a single file: `applications.json` — an array of job application objects.
- Format is identical to the existing Export JSON, enabling zero-friction migration.

### Authentication
- User generates a GitHub Personal Access Token with `gist` scope.
- On first visit to the app, a **Settings page** (`settings.html`) prompts for:
  - GitHub PAT
  - Gist ID (or a "Create new Gist" button that calls the API to create one)
- Both values are stored in `localStorage` on that device (one-time setup per machine).

### Concurrency
- Last-write-wins. Every write does: read current Gist → merge/replace → PATCH.
- Acceptable for a single-user personal tracker. Two simultaneous edits from different machines are extremely unlikely.

---

## Repository structure

```
docs/                       ← served by GitHub Pages
├── index.html              ← Dashboard
├── applications.html       ← Application list
├── create.html             ← New application form
├── edit.html               ← Edit form (?id=N)
├── settings.html           ← PAT + Gist ID setup
├── css/
│   └── site.css            ← Same CSS, copied verbatim from wwwroot/css/site.css
└── js/
    ├── gist.js             ← GitHub Gist API wrapper (read, write, create)
    └── app.js              ← App logic: data helpers, status transitions, UI utilities

Controllers/                ← .NET code, untouched
Models/
...
```

---

## Data format

Same JSON schema as the existing Export endpoint. Each record:

```json
{
  "id": 1,
  "companyName": "Acme Corp",
  "jobTitle": "Développeur Full Stack",
  "jobUrl": "https://...",
  "location": "Paris, France",
  "appliedDate": "2026-04-01T00:00:00",
  "lastResponseDate": null,
  "status": 0,
  "interviewDate": null,
  "notes": "",
  "salaryExpectation": 55000,
  "rating": 3,
  "refusalMailUrl": null,
  "teamsUrl": null,
  "createdAt": "2026-04-01T10:00:00Z",
  "updatedAt": "2026-04-01T10:00:00Z"
}
```

`status` is stored as the integer enum value (0–12), matching the C# enum.

ID assignment: track `maxId` as `Math.max(...apps.map(a => a.id))` on each write; new records get `maxId + 1`.

---

## Pages and features

### Settings (`settings.html`)
- Fields: GitHub PAT, Gist ID
- Button: "Créer un nouveau Gist" → calls `POST /gists`, stores returned ID
- Button: "Tester la connexion" → fetches Gist, shows success/error
- Saves to `localStorage`: `gist_pat`, `gist_id`
- All other pages redirect to `/docs/settings.html` if either value is missing

### Dashboard (`index.html`)
- Reads all applications from Gist on load
- Runs auto-transitions before rendering (see below)
- Stats: Total, En cours, Offres, Refusés, Sans réponse
- Breakdown by status (bar chart)
- 5 most recent applications

### Application list (`applications.html`)
- Reads and auto-transitions on load; writes back if any transitions fired
- Search (company/title), filter by status, sort (date / company / status)
- Inline status `<select>` → PATCH Gist immediately
- Inline interview date picker → PATCH Gist immediately
- Delete button → confirm → remove from array → PATCH Gist

### Create (`create.html`)
- Same fields as current `_Form.cshtml`
- URL auto-fill: `fetch(url)` via a CORS proxy (`api.allorigins.win`), parse `<title>` and `<meta og:*>` tags. Fails silently for Cloudflare-protected sites (Indeed); falls back to manual entry with a warning toast.
- Google Maps Places autocomplete: API key stored in Settings, passed as a `<script>` tag at runtime if set.
- On submit: append new record → PATCH Gist → redirect to list

### Edit (`edit.html?id=N`)
- Load record by ID from Gist
- Same form as Create
- On submit: replace record in array → PATCH Gist → redirect to list

### Export
- Button on list page: JSON.stringify the in-memory array → trigger browser download. No Gist call needed.

### Import
- File input on list page: parse JSON → overwrite entire Gist file → reload. Skips records whose ID already exists (same logic as current C# Import).

### CV page
- **Dropped** from the static version. No server to receive file uploads.
- Future option: link to a PDF hosted on Google Drive / Dropbox.

---

## Auto-transitions (JS port of ApplicationsController.Index logic)

Run on every page load that reads data, before rendering. If any transitions fired, write the updated array back to the Gist.

```
1. For each app where interviewDate < today AND status ∈ {EntretienRecruteur, EntretienTech, TestTechnique, EntretienRH}:
   → set status to corresponding Attente* value
   → clear interviewDate

2. For each app where status == Candidature AND appliedDate <= today - 21 days:
   → set status to SansReponse
   → set lastResponseDate = appliedDate + 21 days
```

---

## Status enum (JS constant, mirrors C# enum)

```js
const STATUS = {
  Candidature: 0, AppelRecruteur: 1, EntretienRecruteur: 2, AttenteRecruteur: 3,
  EntretienTech: 4, AttenteTech: 5, TestTechnique: 6, AttenteTest: 7,
  EntretienRH: 8, AttenteRH: 9, Offre: 10, Refuse: 11, SansReponse: 12
};
const STATUS_LABEL = {
  0: 'Candidature', 1: 'Appel recruteur', 2: 'Entretien recruteur',
  3: 'En attente (recruteur)', 4: 'Entretien tech', 5: 'En attente (tech)',
  6: 'Test technique', 7: 'En attente (test)', 8: 'Entretien RH',
  9: 'En attente (RH)', 10: 'Offre', 11: 'Refusé', 12: 'Sans réponse'
};
const STATUS_CLASS = {
  0: 'candidature', 1: 'appelrecruteur', 2: 'entretienrecruteur',
  3: 'attenterecruteur', 4: 'entretientech', 5: 'attentetech',
  6: 'testtechnique', 7: 'attentetest', 8: 'entretienrh',
  9: 'attenterh', 10: 'offre', 11: 'refuse', 12: 'sansreponse'
};
```

CSS class names derived from `STATUS_CLASS[status]`, not from French label text.

---

## QuickStatus side-effects (JS port of ApplicationsController.QuickStatus)

Replicate the C# side-effects when status changes inline:

- `AppelRecruteur` → set `interviewDate = today`
- `EntretienRecruteur | EntretienTech | TestTechnique | EntretienRH` → clear `interviewDate`
- `AttenteRecruteur | AttenteTech | AttenteTest | AttenteRH | Refuse | SansReponse` → clear `interviewDate`
- Any status except `Candidature | AppelRecruteur` → set `lastResponseDate = today`

---

## GitHub Pages configuration

- Enable GitHub Pages on the repo: **Source → Deploy from branch → `main`, folder `/docs`**.
- No build step required — pure static files.
- URL: `https://<username>.github.io/<repo-name>/`

---

## Migration from .NET app

1. Open the local .NET app → Applications → Export → download `jobtracker-export-YYYY-MM-DD.json`
2. Open the GitHub Pages app → Settings → create/configure Gist
3. Applications list → Import → select the exported JSON file
4. Done. The existing data is now in the Gist.

---

## Error handling

- **Gist read failure** (network error, bad PAT, wrong Gist ID): show a full-page error message with a link to Settings. Do not render stale/empty data silently.
- **Gist write failure** (inline status / date update): show a toast error; the UI change is already reflected in memory, so the user can try again by refreshing or navigating away.
- **Import parse error**: show a toast, do not overwrite the Gist.
- **URL auto-fill failure**: show a warning toast ("Impossible de récupérer les informations automatiquement"), leave form fields empty for manual entry.

---

## Out of scope

- Real-time sync / conflict resolution
- CV upload / viewer
- User authentication (app is personal / private)
- Offline support (Service Worker / cache)
