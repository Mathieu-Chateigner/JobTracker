# GitHub Pages + GitHub Gist Static Rewrite — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rewrite the ASP.NET Core MVC app as a static multi-page site hosted in `docs/` on GitHub Pages, using a private GitHub Gist as the cross-machine database.

**Architecture:** Five HTML pages (dashboard, list, create, edit, settings) share two JS files: `gist.js` (GitHub API calls + localStorage for PAT/Gist ID) and `app.js` (pure data logic: status constants, auto-transitions, sort). All data lives in a single `applications.json` file inside a user-owned private Gist, read/written via authenticated fetch calls.

**Tech Stack:** Vanilla HTML/CSS/JS (ES2020, no build step), GitHub Gist API v3, GitHub Pages, Node.js (for running unit tests of pure functions only)

---

## File map

| File | Role |
|---|---|
| `docs/css/site.css` | Existing CSS, copied verbatim |
| `docs/js/app.js` | Pure logic: STATUS constants, auto-transitions, sort, side-effects, formatters. CommonJS export guard for Node testing. |
| `docs/js/app.test.js` | Node-runnable unit tests for pure functions in app.js |
| `docs/js/gist.js` | GitHub Gist API wrapper + localStorage helpers |
| `docs/settings.html` | One-time PAT + Gist ID setup page |
| `docs/index.html` | Dashboard: stats, breakdown, recent list |
| `docs/applications.html` | Application list: search/filter/sort, inline status/date, export/import |
| `docs/create.html` | Create form with URL auto-fill fallback |
| `docs/edit.html` | Edit form (reads `?id=N` from URL) |

---

## Shared HTML snippets (reused in every page)

### `<head>` block (copy into every HTML page, change `<title>` per page)
```html
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>PAGE_TITLE — JobTracker</title>
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link href="https://fonts.googleapis.com/css2?family=DM+Serif+Display:ital@0;1&family=DM+Sans:wght@300;400;500;600&display=swap" rel="stylesheet">
  <link rel="icon" type="image/svg+xml" href="data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAzMiAzMiIgZmlsbD0ibm9uZSI+CiAgPHJlY3QgeD0iMyIgeT0iMTEiIHdpZHRoPSIyNiIgaGVpZ2h0PSIxNyIgcng9IjMiIGZpbGw9IiNmMGE4MzIiIG9wYWNpdHk9IjAuMiIvPgogIDxyZWN0IHg9IjMiIHk9IjExIiB3aWR0aD0iMjYiIGhlaWdodD0iMTciIHJ4PSIzIiBzdHJva2U9IiNmMGE4MzIiIHN0cm9rZS13aWR0aD0iMiIvPgogIDxwYXRoIGQ9Ik0xMSAxMVY5QzExIDcuMzQgMTIuMzQgNiAxNCA2SDE4QzE5LjY2IDYgMjEgNy4zNCAyMSA5VjExIiBzdHJva2U9IiNmMGE4MzIiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIi8+CiAgPGxpbmUgeDE9IjMiIHkxPSIyMCIgeDI9IjI5IiB5Mj0iMjAiIHN0cm9rZT0iI2YwYTgzMiIgc3Ryb2tlLXdpZHRoPSIxLjUiIG9wYWNpdHk9IjAuNCIvPgogIDxyZWN0IHg9IjE0IiB5PSIxOC41IiB3aWR0aD0iNCIgaGVpZ2h0PSIzIiByeD0iMSIgZmlsbD0iI2YwYTgzMiIvPgo8L3N2Zz4=" />
  <link rel="stylesheet" href="./css/site.css" />
</head>
```

### Navbar (copy into every HTML page; set `active` class on the right link)
```html
<nav class="navbar">
  <a class="navbar-brand" href="./index.html">
    <svg class="brand-logo" width="34" height="34" viewBox="0 0 32 32" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
      <rect x="3" y="11" width="26" height="17" rx="3" fill="#f0a832" opacity="0.15"/>
      <rect x="3" y="11" width="26" height="17" rx="3" stroke="#f0a832" stroke-width="1.75"/>
      <path d="M11 11V9C11 7.34 12.34 6 14 6H18C19.66 6 21 7.34 21 9V11" stroke="#f0a832" stroke-width="1.75" stroke-linecap="round"/>
      <line x1="3" y1="20" x2="29" y2="20" stroke="#f0a832" stroke-width="1.5" opacity="0.35"/>
      <rect x="14" y="18.5" width="4" height="3" rx="1" fill="#f0a832"/>
    </svg>
    <span class="brand-text">JobTracker</span>
  </a>
  <div class="nav-links">
    <a href="./index.html" class="nav-link">Tableau de bord</a>
    <a href="./applications.html" class="nav-link">Candidatures</a>
    <a href="./settings.html" class="nav-link">Paramètres</a>
    <a href="./create.html" class="nav-btn">+ Nouvelle</a>
  </div>
</nav>
```

### Script footer (copy into every HTML page, before `</body>`)
```html
  <script src="./js/gist.js"></script>
  <script src="./js/app.js"></script>
```

---

## Task 1: Scaffold `docs/` and copy CSS

**Files:**
- Create: `docs/css/site.css` (copy of `wwwroot/css/site.css`)
- Create: `docs/js/` (empty directory placeholder)

- [ ] **Step 1: Create directories and copy CSS**

```powershell
New-Item -ItemType Directory -Force docs\css, docs\js
Copy-Item wwwroot\css\site.css docs\css\site.css
```

- [ ] **Step 2: Verify**

```powershell
Test-Path docs\css\site.css
```
Expected output: `True`

- [ ] **Step 3: Commit**

```powershell
git add docs\css\site.css
git commit -m "feat: scaffold docs/ directory for GitHub Pages"
```

---

## Task 2: Write failing tests for pure app logic

**Files:**
- Create: `docs/js/app.test.js`

- [ ] **Step 1: Write `docs/js/app.test.js`**

```js
// Simple Node.js test runner — no external dependencies
let passed = 0, failed = 0;
function test(name, fn) {
  try { fn(); console.log(`  ✓ ${name}`); passed++; }
  catch (e) { console.error(`  ✗ ${name}: ${e.message}`); failed++; }
}
function assertEqual(a, b, msg) {
  const as = JSON.stringify(a), bs = JSON.stringify(b);
  if (as !== bs) throw new Error(msg || `expected ${bs}, got ${as}`);
}
function assert(cond, msg) {
  if (!cond) throw new Error(msg || 'expected truthy, got ' + cond);
}

const {
  STATUS, applyAutoTransitions, sortApplications,
  applyQuickStatusSideEffects, generateId, statusSortOrder
} = require('./app.js');

function makeApp(overrides = {}) {
  return {
    id: 1, companyName: 'ACME', jobTitle: 'Dev',
    appliedDate: new Date().toISOString(),
    status: STATUS.Candidature,
    interviewDate: null, lastResponseDate: null,
    updatedAt: new Date().toISOString(),
    ...overrides
  };
}

// ── applyAutoTransitions ──────────────────────────────────────────────────────
console.log('\napplyAutoTransitions:');

test('EntretienRecruteur past date → AttenteRecruteur', () => {
  const apps = [makeApp({ status: STATUS.EntretienRecruteur, interviewDate: '2020-01-01T10:00:00' })];
  const { apps: r, changed } = applyAutoTransitions(apps);
  assertEqual(r[0].status, STATUS.AttenteRecruteur);
  assertEqual(r[0].interviewDate, null);
  assert(changed);
});

test('EntretienTech past date → AttenteTech', () => {
  const apps = [makeApp({ status: STATUS.EntretienTech, interviewDate: '2020-01-01T10:00:00' })];
  const { apps: r } = applyAutoTransitions(apps);
  assertEqual(r[0].status, STATUS.AttenteTech);
});

test('TestTechnique past date → AttenteTest', () => {
  const apps = [makeApp({ status: STATUS.TestTechnique, interviewDate: '2020-01-01T10:00:00' })];
  const { apps: r } = applyAutoTransitions(apps);
  assertEqual(r[0].status, STATUS.AttenteTest);
});

test('EntretienRH past date → AttenteRH', () => {
  const apps = [makeApp({ status: STATUS.EntretienRH, interviewDate: '2020-01-01T10:00:00' })];
  const { apps: r } = applyAutoTransitions(apps);
  assertEqual(r[0].status, STATUS.AttenteRH);
});

test('Future interview date — no transition', () => {
  const future = new Date();
  future.setDate(future.getDate() + 7);
  const apps = [makeApp({ status: STATUS.EntretienTech, interviewDate: future.toISOString() })];
  const { apps: r, changed } = applyAutoTransitions(apps);
  assertEqual(r[0].status, STATUS.EntretienTech);
  assert(!changed);
});

test('Candidature 22 days old → SansReponse', () => {
  const old = new Date();
  old.setDate(old.getDate() - 22);
  old.setHours(0, 0, 0, 0);
  const apps = [makeApp({ status: STATUS.Candidature, appliedDate: old.toISOString() })];
  const { apps: r, changed } = applyAutoTransitions(apps);
  assertEqual(r[0].status, STATUS.SansReponse);
  assert(r[0].lastResponseDate != null);
  assert(changed);
});

test('Candidature exactly 21 days old → SansReponse', () => {
  const old = new Date();
  old.setDate(old.getDate() - 21);
  old.setHours(0, 0, 0, 0);
  const apps = [makeApp({ status: STATUS.Candidature, appliedDate: old.toISOString() })];
  const { apps: r, changed } = applyAutoTransitions(apps);
  assertEqual(r[0].status, STATUS.SansReponse);
  assert(changed);
});

test('Candidature 20 days old — no transition', () => {
  const recent = new Date();
  recent.setDate(recent.getDate() - 20);
  const apps = [makeApp({ status: STATUS.Candidature, appliedDate: recent.toISOString() })];
  const { apps: r, changed } = applyAutoTransitions(apps);
  assertEqual(r[0].status, STATUS.Candidature);
  assert(!changed);
});

test('Offre app — no transition, changed=false', () => {
  const { changed } = applyAutoTransitions([makeApp({ status: STATUS.Offre })]);
  assert(!changed);
});

// ── generateId ────────────────────────────────────────────────────────────────
console.log('\ngenerateId:');

test('Empty array → 1', () => { assertEqual(generateId([]), 1); });
test('Returns max+1', () => { assertEqual(generateId([{id:3},{id:1},{id:7}]), 8); });

// ── applyQuickStatusSideEffects ───────────────────────────────────────────────
console.log('\napplyQuickStatusSideEffects:');

test('AppelRecruteur sets interviewDate to today', () => {
  const app = makeApp();
  applyQuickStatusSideEffects(app, STATUS.AppelRecruteur);
  assert(app.interviewDate !== null, 'interviewDate should be set');
  assertEqual(app.status, STATUS.AppelRecruteur);
});

test('EntretienTech clears interviewDate', () => {
  const app = makeApp({ interviewDate: '2026-01-01T00:00:00' });
  applyQuickStatusSideEffects(app, STATUS.EntretienTech);
  assertEqual(app.interviewDate, null);
});

test('Refuse clears interviewDate', () => {
  const app = makeApp({ interviewDate: '2026-06-01T00:00:00' });
  applyQuickStatusSideEffects(app, STATUS.Refuse);
  assertEqual(app.interviewDate, null);
});

test('AttenteRH clears interviewDate', () => {
  const app = makeApp({ interviewDate: '2026-06-01T00:00:00' });
  applyQuickStatusSideEffects(app, STATUS.AttenteRH);
  assertEqual(app.interviewDate, null);
});

test('Non-Candidature status sets lastResponseDate', () => {
  const app = makeApp({ lastResponseDate: null });
  applyQuickStatusSideEffects(app, STATUS.Offre);
  assert(app.lastResponseDate !== null);
});

test('Candidature does NOT set lastResponseDate', () => {
  const app = makeApp({ lastResponseDate: null });
  applyQuickStatusSideEffects(app, STATUS.Candidature);
  assertEqual(app.lastResponseDate, null);
});

test('AppelRecruteur does NOT set lastResponseDate', () => {
  const app = makeApp({ lastResponseDate: null });
  applyQuickStatusSideEffects(app, STATUS.AppelRecruteur);
  assertEqual(app.lastResponseDate, null);
});

// ── sortApplications ──────────────────────────────────────────────────────────
console.log('\nsortApplications:');

test('company sort is alphabetical', () => {
  const apps = [makeApp({ companyName: 'Zeta' }), makeApp({ companyName: 'Alpha' })];
  const sorted = sortApplications(apps, 'company');
  assertEqual(sorted[0].companyName, 'Alpha');
});

test('status sort: Offre before Refuse', () => {
  const apps = [makeApp({ status: STATUS.Refuse }), makeApp({ status: STATUS.Offre })];
  const sorted = sortApplications(apps, 'status');
  assertEqual(sorted[0].status, STATUS.Offre);
});

test('date sort: upcoming interview before candidature with no date', () => {
  const future = new Date();
  future.setDate(future.getDate() + 3);
  const apps = [
    makeApp({ id: 1, status: STATUS.Candidature, interviewDate: null }),
    makeApp({ id: 2, status: STATUS.EntretienTech, interviewDate: future.toISOString() })
  ];
  const sorted = sortApplications(apps, 'date');
  assertEqual(sorted[0].id, 2);
});

test('statusSortOrder: Offre < Candidature < SansReponse', () => {
  assert(statusSortOrder(STATUS.Offre) < statusSortOrder(STATUS.Candidature));
  assert(statusSortOrder(STATUS.Candidature) < statusSortOrder(STATUS.SansReponse));
});

// ── Summary ───────────────────────────────────────────────────────────────────
console.log(`\n${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
```

- [ ] **Step 2: Run tests — expect failure (app.js doesn't exist yet)**

```powershell
node docs/js/app.test.js
```
Expected: `Error: Cannot find module './app.js'`

---

## Task 3: Write `docs/js/app.js` (make tests pass)

**Files:**
- Create: `docs/js/app.js`

- [ ] **Step 1: Write `docs/js/app.js`**

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

const INTERVIEW_TO_ATTENTE = {
  [STATUS.EntretienRecruteur]: STATUS.AttenteRecruteur,
  [STATUS.EntretienTech]:      STATUS.AttenteTech,
  [STATUS.TestTechnique]:      STATUS.AttenteTest,
  [STATUS.EntretienRH]:        STATUS.AttenteRH,
};

const INTERVIEW_STATUSES = new Set([
  STATUS.EntretienRecruteur, STATUS.EntretienTech,
  STATUS.TestTechnique, STATUS.EntretienRH
]);

const WAITING_OR_TERMINAL = new Set([
  STATUS.AttenteRecruteur, STATUS.AttenteTech, STATUS.AttenteTest, STATUS.AttenteRH,
  STATUS.Refuse, STATUS.SansReponse
]);

function today() {
  const d = new Date();
  d.setHours(0, 0, 0, 0);
  return d;
}

function applyAutoTransitions(apps) {
  const todayDate = today();
  const threeWeeksAgo = new Date(todayDate);
  threeWeeksAgo.setDate(threeWeeksAgo.getDate() - 21);
  let changed = false;

  for (const app of apps) {
    if (app.interviewDate && INTERVIEW_TO_ATTENTE[app.status] !== undefined) {
      const iv = new Date(app.interviewDate);
      iv.setHours(0, 0, 0, 0);
      if (iv < todayDate) {
        app.status = INTERVIEW_TO_ATTENTE[app.status];
        app.interviewDate = null;
        app.updatedAt = new Date().toISOString();
        changed = true;
      }
    }
    if (app.status === STATUS.Candidature) {
      const applied = new Date(app.appliedDate);
      applied.setHours(0, 0, 0, 0);
      if (applied <= threeWeeksAgo) {
        const responseDate = new Date(applied);
        responseDate.setDate(responseDate.getDate() + 21);
        app.status = STATUS.SansReponse;
        app.lastResponseDate = responseDate.toISOString().split('T')[0];
        app.updatedAt = new Date().toISOString();
        changed = true;
      }
    }
  }
  return { apps, changed };
}

function statusSortOrder(status) {
  const ORDER = {
    [STATUS.Offre]: 0, [STATUS.AttenteRH]: 1, [STATUS.EntretienRH]: 2,
    [STATUS.AttenteTest]: 3, [STATUS.TestTechnique]: 4, [STATUS.AttenteTech]: 5,
    [STATUS.EntretienTech]: 6, [STATUS.AttenteRecruteur]: 7,
    [STATUS.EntretienRecruteur]: 8, [STATUS.AppelRecruteur]: 9,
    [STATUS.Candidature]: 10, [STATUS.Refuse]: 11, [STATUS.SansReponse]: 12,
  };
  return ORDER[status] ?? 99;
}

function sortApplications(apps, sort) {
  const todayDate = today();
  const copy = [...apps];
  if (sort === 'company') {
    return copy.sort((a, b) => a.companyName.localeCompare(b.companyName, 'fr'));
  }
  if (sort === 'status') {
    return copy.sort((a, b) => {
      const sd = statusSortOrder(a.status) - statusSortOrder(b.status);
      if (sd !== 0) return sd;
      const ai = a.interviewDate ? new Date(a.interviewDate) : new Date('9999-12-31');
      const bi = b.interviewDate ? new Date(b.interviewDate) : new Date('9999-12-31');
      return ai - bi;
    });
  }
  return copy.sort((a, b) => {
    const aIv = a.interviewDate ? new Date(a.interviewDate) : null;
    const bIv = b.interviewDate ? new Date(b.interviewDate) : null;
    const aUp = (aIv && aIv >= todayDate) ? 0 : 1;
    const bUp = (bIv && bIv >= todayDate) ? 0 : 1;
    if (aUp !== bUp) return aUp - bUp;
    if (aUp === 0) return aIv - bIv;
    const ss = statusSortOrder(a.status) - statusSortOrder(b.status);
    if (ss !== 0) return ss;
    const ivCmp = (aIv || new Date('9999-12-31')) - (bIv || new Date('9999-12-31'));
    if (ivCmp !== 0) return ivCmp;
    return new Date(b.appliedDate) - new Date(a.appliedDate);
  });
}

function applyQuickStatusSideEffects(app, newStatus) {
  app.status = newStatus;
  app.updatedAt = new Date().toISOString();
  if (newStatus === STATUS.AppelRecruteur) {
    app.interviewDate = new Date().toISOString().split('T')[0] + 'T00:00:00';
  } else if (INTERVIEW_STATUSES.has(newStatus)) {
    app.interviewDate = null;
  } else if (WAITING_OR_TERMINAL.has(newStatus)) {
    app.interviewDate = null;
  }
  if (newStatus !== STATUS.Candidature && newStatus !== STATUS.AppelRecruteur) {
    app.lastResponseDate = new Date().toISOString().split('T')[0];
  }
  return app;
}

function generateId(apps) {
  if (apps.length === 0) return 1;
  return Math.max(...apps.map(a => a.id)) + 1;
}

function formatDateFr(isoString) {
  if (!isoString) return '—';
  return new Date(isoString).toLocaleDateString('fr-FR', {
    day: 'numeric', month: 'short', year: 'numeric'
  });
}

function showToast(msg, type = 'success') {
  const existing = document.getElementById('toast');
  if (existing) existing.remove();
  const div = document.createElement('div');
  div.id = 'toast';
  div.className = `toast toast-${type}`;
  div.innerHTML = `<span>${type === 'success' ? '✓' : '✕'}</span> ${msg}`;
  document.querySelector('.main-content').prepend(div);
  setTimeout(() => {
    div.style.transition = 'opacity .4s';
    div.style.opacity = '0';
    setTimeout(() => div.remove(), 400);
  }, 4000);
}

function renderStatusOptions(selectedStatus) {
  return Object.entries(STATUS_LABEL)
    .map(([val, label]) =>
      `<option value="${val}"${parseInt(val) === selectedStatus ? ' selected' : ''}>${label}</option>`
    ).join('');
}

function initPlacesAutocomplete() {
  const input = document.getElementById('locationInput');
  if (!input || !window.google) return;
  const ac = new google.maps.places.Autocomplete(input, {
    types: ['geocode', 'establishment'],
    fields: ['formatted_address', 'name', 'address_components']
  });
  ac.addListener('place_changed', () => {
    const place = ac.getPlace();
    if (!place) return;
    const components = place.address_components || [];
    const city    = components.find(c => c.types.includes('locality'))?.long_name;
    const region  = components.find(c => c.types.includes('administrative_area_level_1'))?.long_name;
    const country = components.find(c => c.types.includes('country'))?.long_name;
    if (city && country)        input.value = `${city}, ${country}`;
    else if (region && country) input.value = `${region}, ${country}`;
    else                        input.value = place.formatted_address || place.name || '';
  });
}

// Inject Google Maps script if API key configured
function maybeLoadGoogleMaps() {
  const key = localStorage.getItem('gmaps_key');
  if (!key) return;
  window.initPlacesAutocomplete = initPlacesAutocomplete;
  const s = document.createElement('script');
  s.src = `https://maps.googleapis.com/maps/api/js?key=${key}&libraries=places&callback=initPlacesAutocomplete`;
  s.async = true; s.defer = true;
  document.head.appendChild(s);
}

// CommonJS export guard for Node.js unit tests
if (typeof module !== 'undefined') {
  module.exports = {
    STATUS, STATUS_LABEL, STATUS_CLASS,
    today, applyAutoTransitions, statusSortOrder,
    sortApplications, applyQuickStatusSideEffects,
    generateId, formatDateFr
  };
}
```

- [ ] **Step 2: Run tests — expect all to pass**

```powershell
node docs/js/app.test.js
```
Expected: all lines starting with `✓`, ending with `N passed, 0 failed`

- [ ] **Step 3: Commit**

```powershell
git add docs/js/app.js docs/js/app.test.js
git commit -m "feat: add app.js pure logic and passing unit tests"
```

---

## Task 4: Write `docs/js/gist.js`

**Files:**
- Create: `docs/js/gist.js`

- [ ] **Step 1: Write `docs/js/gist.js`**

```js
const GIST_FILE = 'applications.json';
const GITHUB_API = 'https://api.github.com';

function getSettings() {
  return {
    pat:      localStorage.getItem('gist_pat')    || '',
    gistId:   localStorage.getItem('gist_id')     || '',
    gmapsKey: localStorage.getItem('gmaps_key')   || ''
  };
}

function saveSettings(pat, gistId, gmapsKey) {
  localStorage.setItem('gist_pat',   pat);
  localStorage.setItem('gist_id',    gistId);
  localStorage.setItem('gmaps_key',  gmapsKey || '');
}

function hasSettings() {
  const { pat, gistId } = getSettings();
  return pat.length > 0 && gistId.length > 0;
}

function redirectIfNoSettings() {
  if (!hasSettings()) {
    const page = window.location.pathname.split('/').pop() || 'index.html';
    if (page !== 'settings.html') window.location.href = './settings.html';
  }
}

function _authHeaders() {
  const { pat } = getSettings();
  return {
    'Authorization': `Bearer ${pat}`,
    'Accept': 'application/vnd.github+json',
    'X-GitHub-Api-Version': '2022-11-28'
  };
}

async function readApplications() {
  const { gistId } = getSettings();
  const resp = await fetch(`${GITHUB_API}/gists/${gistId}`, { headers: _authHeaders() });
  if (!resp.ok) {
    const err = await resp.json().catch(() => ({}));
    throw new Error(err.message || `GitHub API ${resp.status}`);
  }
  const data = await resp.json();
  const content = data.files?.[GIST_FILE]?.content;
  if (!content) return [];
  return JSON.parse(content);
}

async function writeApplications(apps) {
  const { gistId } = getSettings();
  const resp = await fetch(`${GITHUB_API}/gists/${gistId}`, {
    method: 'PATCH',
    headers: { ..._authHeaders(), 'Content-Type': 'application/json' },
    body: JSON.stringify({ files: { [GIST_FILE]: { content: JSON.stringify(apps, null, 2) } } })
  });
  if (!resp.ok) {
    const err = await resp.json().catch(() => ({}));
    throw new Error(err.message || `GitHub API ${resp.status}`);
  }
}

async function createGist() {
  const resp = await fetch(`${GITHUB_API}/gists`, {
    method: 'POST',
    headers: { ..._authHeaders(), 'Content-Type': 'application/json' },
    body: JSON.stringify({
      description: 'JobTracker data',
      public: false,
      files: { [GIST_FILE]: { content: '[]' } }
    })
  });
  if (!resp.ok) {
    const err = await resp.json().catch(() => ({}));
    throw new Error(err.message || `GitHub API ${resp.status}`);
  }
  return (await resp.json()).id;
}

async function testConnection() {
  try {
    const { gistId } = getSettings();
    const resp = await fetch(`${GITHUB_API}/gists/${gistId}`, { headers: _authHeaders() });
    if (!resp.ok) {
      const err = await resp.json().catch(() => ({}));
      return { ok: false, error: err.message || `Erreur ${resp.status}` };
    }
    return { ok: true };
  } catch (e) {
    return { ok: false, error: e.message };
  }
}
```

- [ ] **Step 2: Verify file exists**

```powershell
Test-Path docs/js/gist.js
```
Expected: `True`

- [ ] **Step 3: Commit**

```powershell
git add docs/js/gist.js
git commit -m "feat: add gist.js GitHub API wrapper"
```

---

## Task 5: Write `docs/settings.html`

**Files:**
- Create: `docs/settings.html`

- [ ] **Step 1: Write `docs/settings.html`**

```html
<!DOCTYPE html>
<html lang="fr">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Paramètres — JobTracker</title>
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link href="https://fonts.googleapis.com/css2?family=DM+Serif+Display:ital@0;1&family=DM+Sans:wght@300;400;500;600&display=swap" rel="stylesheet">
  <link rel="icon" type="image/svg+xml" href="data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAzMiAzMiIgZmlsbD0ibm9uZSI+CiAgPHJlY3QgeD0iMyIgeT0iMTEiIHdpZHRoPSIyNiIgaGVpZ2h0PSIxNyIgcng9IjMiIGZpbGw9IiNmMGE4MzIiIG9wYWNpdHk9IjAuMiIvPgogIDxyZWN0IHg9IjMiIHk9IjExIiB3aWR0aD0iMjYiIGhlaWdodD0iMTciIHJ4PSIzIiBzdHJva2U9IiNmMGE4MzIiIHN0cm9rZS13aWR0aD0iMiIvPgogIDxwYXRoIGQ9Ik0xMSAxMVY5QzExIDcuMzQgMTIuMzQgNiAxNCA2SDE4QzE5LjY2IDYgMjEgNy4zNCAyMSA5VjExIiBzdHJva2U9IiNmMGE4MzIiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIi8+CiAgPGxpbmUgeDE9IjMiIHkxPSIyMCIgeDI9IjI5IiB5Mj0iMjAiIHN0cm9rZT0iI2YwYTgzMiIgc3Ryb2tlLXdpZHRoPSIxLjUiIG9wYWNpdHk9IjAuNCIvPgogIDxyZWN0IHg9IjE0IiB5PSIxOC41IiB3aWR0aD0iNCIgaGVpZ2h0PSIzIiByeD0iMSIgZmlsbD0iI2YwYTgzMiIvPgo8L3N2Zz4=" />
  <link rel="stylesheet" href="./css/site.css" />
</head>
<body>
<nav class="navbar">
  <a class="navbar-brand" href="./index.html">
    <svg class="brand-logo" width="34" height="34" viewBox="0 0 32 32" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
      <rect x="3" y="11" width="26" height="17" rx="3" fill="#f0a832" opacity="0.15"/>
      <rect x="3" y="11" width="26" height="17" rx="3" stroke="#f0a832" stroke-width="1.75"/>
      <path d="M11 11V9C11 7.34 12.34 6 14 6H18C19.66 6 21 7.34 21 9V11" stroke="#f0a832" stroke-width="1.75" stroke-linecap="round"/>
      <line x1="3" y1="20" x2="29" y2="20" stroke="#f0a832" stroke-width="1.5" opacity="0.35"/>
      <rect x="14" y="18.5" width="4" height="3" rx="1" fill="#f0a832"/>
    </svg>
    <span class="brand-text">JobTracker</span>
  </a>
  <div class="nav-links">
    <a href="./index.html" class="nav-link">Tableau de bord</a>
    <a href="./applications.html" class="nav-link">Candidatures</a>
    <a href="./settings.html" class="nav-link active">Paramètres</a>
    <a href="./create.html" class="nav-btn">+ Nouvelle</a>
  </div>
</nav>
<main class="main-content">
  <div class="page-container page-narrow">
    <div class="page-header">
      <div>
        <h1 class="page-title">Paramètres</h1>
        <p class="page-subtitle">Connexion GitHub Gist</p>
      </div>
    </div>
    <div class="panel" style="margin-bottom:2rem;">
      <h2 class="panel-title">Comment configurer</h2>
      <ol style="color:var(--text-muted);font-size:.9rem;line-height:2;padding-left:1.25rem;">
        <li>Allez sur <a href="https://github.com/settings/tokens/new?scopes=gist&description=JobTracker" target="_blank">github.com/settings/tokens</a> et créez un token avec le scope <strong>gist</strong> uniquement.</li>
        <li>Collez le token ci-dessous, puis cliquez <em>Créer un nouveau Gist</em> (ou entrez un ID existant).</li>
        <li>Cliquez <em>Enregistrer</em>.</li>
      </ol>
    </div>
    <form id="settingsForm">
      <div class="form-grid">
        <div class="form-group form-col-full">
          <label class="form-label">GitHub Personal Access Token *</label>
          <input type="password" id="patInput" class="form-input" placeholder="ghp_..." autocomplete="off" />
        </div>
        <div class="form-group form-col-2">
          <label class="form-label">Gist ID *</label>
          <input type="text" id="gistIdInput" class="form-input" placeholder="abc123..." />
        </div>
        <div class="form-group form-col-2" style="display:flex;align-items:flex-end;">
          <button type="button" id="createGistBtn" class="btn btn-secondary" style="width:100%;">Créer un nouveau Gist</button>
        </div>
        <div class="form-group form-col-full">
          <label class="form-label">Clé API Google Maps (optionnel)</label>
          <input type="text" id="gmapsKeyInput" class="form-input" placeholder="AIza..." />
          <small style="color:var(--text-muted);font-size:.8rem;">Autocomplétion de ville dans les formulaires.</small>
        </div>
      </div>
      <div class="form-actions">
        <button type="submit" class="btn btn-primary">Enregistrer</button>
        <button type="button" id="testConnBtn" class="btn btn-secondary">Tester la connexion</button>
        <a href="./index.html" class="btn btn-ghost">Annuler</a>
      </div>
      <div id="connStatus" style="margin-top:1rem;"></div>
    </form>
  </div>
</main>
<script src="./js/gist.js"></script>
<script src="./js/app.js"></script>
<script>
(async () => {
  const { pat, gistId, gmapsKey } = getSettings();
  document.getElementById('patInput').value    = pat;
  document.getElementById('gistIdInput').value = gistId;
  document.getElementById('gmapsKeyInput').value = gmapsKey;

  document.getElementById('createGistBtn').addEventListener('click', async () => {
    const pat = document.getElementById('patInput').value.trim();
    if (!pat) { alert('Entrez d\'abord le token GitHub.'); return; }
    saveSettings(pat, '', document.getElementById('gmapsKeyInput').value.trim());
    const btn = document.getElementById('createGistBtn');
    btn.disabled = true; btn.textContent = 'Création…';
    try {
      const id = await createGist();
      document.getElementById('gistIdInput').value = id;
      showStatus('Gist créé : ' + id, true);
    } catch (e) {
      showStatus('Erreur : ' + e.message, false);
    } finally {
      btn.disabled = false; btn.textContent = 'Créer un nouveau Gist';
    }
  });

  document.getElementById('testConnBtn').addEventListener('click', async () => {
    const pat    = document.getElementById('patInput').value.trim();
    const gistId = document.getElementById('gistIdInput').value.trim();
    saveSettings(pat, gistId, document.getElementById('gmapsKeyInput').value.trim());
    const result = await testConnection();
    showStatus(result.ok ? 'Connexion réussie ✓' : 'Échec : ' + result.error, result.ok);
  });

  document.getElementById('settingsForm').addEventListener('submit', e => {
    e.preventDefault();
    const pat    = document.getElementById('patInput').value.trim();
    const gistId = document.getElementById('gistIdInput').value.trim();
    const gmaps  = document.getElementById('gmapsKeyInput').value.trim();
    if (!pat || !gistId) { alert('Le token et l\'ID du Gist sont obligatoires.'); return; }
    saveSettings(pat, gistId, gmaps);
    window.location.href = './index.html';
  });

  function showStatus(msg, ok) {
    const el = document.getElementById('connStatus');
    el.className = `import-status ${ok ? 'import-success' : 'import-error'}`;
    el.textContent = msg;
  }
})();
</script>
</body>
</html>
```

- [ ] **Step 2: Manual verification**

Open `docs/settings.html` in a browser (open the file directly via `file://` or a local server). Verify:
- Form loads without JS errors
- Inputs are empty on first load
- "Créer un nouveau Gist" button is visible

- [ ] **Step 3: Commit**

```powershell
git add docs/settings.html
git commit -m "feat: add settings.html for PAT and Gist ID setup"
```

---

## Task 6: Write `docs/index.html` (Dashboard)

**Files:**
- Create: `docs/index.html`

- [ ] **Step 1: Write `docs/index.html`**

```html
<!DOCTYPE html>
<html lang="fr">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Tableau de bord — JobTracker</title>
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link href="https://fonts.googleapis.com/css2?family=DM+Serif+Display:ital@0;1&family=DM+Sans:wght@300;400;500;600&display=swap" rel="stylesheet">
  <link rel="icon" type="image/svg+xml" href="data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAzMiAzMiIgZmlsbD0ibm9uZSI+CiAgPHJlY3QgeD0iMyIgeT0iMTEiIHdpZHRoPSIyNiIgaGVpZ2h0PSIxNyIgcng9IjMiIGZpbGw9IiNmMGE4MzIiIG9wYWNpdHk9IjAuMiIvPgogIDxyZWN0IHg9IjMiIHk9IjExIiB3aWR0aD0iMjYiIGhlaWdodD0iMTciIHJ4PSIzIiBzdHJva2U9IiNmMGE4MzIiIHN0cm9rZS13aWR0aD0iMiIvPgogIDxwYXRoIGQ9Ik0xMSAxMVY5QzExIDcuMzQgMTIuMzQgNiAxNCA2SDE4QzE5LjY2IDYgMjEgNy4zNCAyMSA5VjExIiBzdHJva2U9IiNmMGE4MzIiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIi8+CiAgPGxpbmUgeDE9IjMiIHkxPSIyMCIgeDI9IjI5IiB5Mj0iMjAiIHN0cm9rZT0iI2YwYTgzMiIgc3Ryb2tlLXdpZHRoPSIxLjUiIG9wYWNpdHk9IjAuNCIvPgogIDxyZWN0IHg9IjE0IiB5PSIxOC41IiB3aWR0aD0iNCIgaGVpZ2h0PSIzIiByeD0iMSIgZmlsbD0iI2YwYTgzMiIvPgo8L3N2Zz4=" />
  <link rel="stylesheet" href="./css/site.css" />
</head>
<body>
<nav class="navbar">
  <a class="navbar-brand" href="./index.html">
    <svg class="brand-logo" width="34" height="34" viewBox="0 0 32 32" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
      <rect x="3" y="11" width="26" height="17" rx="3" fill="#f0a832" opacity="0.15"/>
      <rect x="3" y="11" width="26" height="17" rx="3" stroke="#f0a832" stroke-width="1.75"/>
      <path d="M11 11V9C11 7.34 12.34 6 14 6H18C19.66 6 21 7.34 21 9V11" stroke="#f0a832" stroke-width="1.75" stroke-linecap="round"/>
      <line x1="3" y1="20" x2="29" y2="20" stroke="#f0a832" stroke-width="1.5" opacity="0.35"/>
      <rect x="14" y="18.5" width="4" height="3" rx="1" fill="#f0a832"/>
    </svg>
    <span class="brand-text">JobTracker</span>
  </a>
  <div class="nav-links">
    <a href="./index.html" class="nav-link active">Tableau de bord</a>
    <a href="./applications.html" class="nav-link">Candidatures</a>
    <a href="./settings.html" class="nav-link">Paramètres</a>
    <a href="./create.html" class="nav-btn">+ Nouvelle</a>
  </div>
</nav>
<main class="main-content">
  <div id="errorBanner" style="display:none;" class="toast toast-error"></div>
  <div class="dashboard">
    <div class="page-header">
      <h1 class="page-title">Tableau de bord</h1>
      <p class="page-subtitle">Votre recherche d'emploi en un coup d'œil</p>
    </div>
    <div class="stats-grid">
      <div class="stat-card stat-total"><div class="stat-value" id="s-total">…</div><div class="stat-label">Total</div></div>
      <div class="stat-card stat-progress"><div class="stat-value" id="s-progress">…</div><div class="stat-label">En cours</div></div>
      <div class="stat-card stat-offer"><div class="stat-value" id="s-offers">…</div><div class="stat-label">Offres</div></div>
      <div class="stat-card stat-rejected"><div class="stat-value" id="s-refused">…</div><div class="stat-label">Refusés</div></div>
      <div class="stat-card stat-silent"><div class="stat-value" id="s-noresponse">…</div><div class="stat-label">Sans réponse</div></div>
    </div>
    <div class="dashboard-bottom">
      <div class="panel">
        <h2 class="panel-title">Répartition par statut</h2>
        <div id="breakdownList" class="breakdown-list"></div>
      </div>
      <div class="panel">
        <h2 class="panel-title">Candidatures récentes</h2>
        <div id="recentList"></div>
      </div>
    </div>
  </div>
</main>
<script src="./js/gist.js"></script>
<script src="./js/app.js"></script>
<script>
(async () => {
  redirectIfNoSettings();

  const IN_PROGRESS = new Set([
    STATUS.AppelRecruteur, STATUS.EntretienRecruteur, STATUS.AttenteRecruteur,
    STATUS.EntretienTech, STATUS.AttenteTech, STATUS.TestTechnique,
    STATUS.AttenteTest, STATUS.EntretienRH, STATUS.AttenteRH
  ]);

  let apps;
  try {
    apps = await readApplications();
  } catch (e) {
    const b = document.getElementById('errorBanner');
    b.style.display = '';
    b.innerHTML = `✕ Impossible de lire le Gist : ${e.message}. <a href="./settings.html">Vérifier les paramètres →</a>`;
    ['s-total','s-progress','s-offers','s-refused','s-noresponse'].forEach(id => {
      document.getElementById(id).textContent = '–';
    });
    return;
  }

  const { apps: transitioned, changed } = applyAutoTransitions(apps);
  if (changed) {
    try { await writeApplications(transitioned); } catch (_) { /* best-effort */ }
  }

  const total      = transitioned.length;
  const inProgress = transitioned.filter(a => IN_PROGRESS.has(a.status)).length;
  const offers     = transitioned.filter(a => a.status === STATUS.Offre).length;
  const refused    = transitioned.filter(a => a.status === STATUS.Refuse).length;
  const noResponse = transitioned.filter(a => a.status === STATUS.SansReponse).length;

  document.getElementById('s-total').textContent      = total;
  document.getElementById('s-progress').textContent   = inProgress;
  document.getElementById('s-offers').textContent     = offers;
  document.getElementById('s-refused').textContent    = refused;
  document.getElementById('s-noresponse').textContent = noResponse;

  // Breakdown
  const counts = {};
  for (const app of transitioned) counts[app.status] = (counts[app.status] || 0) + 1;
  const sorted = Object.entries(counts).sort((a, b) => b[1] - a[1]);
  const breakdownList = document.getElementById('breakdownList');
  if (sorted.length === 0) {
    breakdownList.innerHTML = '<p class="empty-msg">Aucune candidature pour le moment.</p>';
  } else {
    breakdownList.innerHTML = sorted.map(([s, count]) => {
      const pct = total > 0 ? Math.round(count * 100 / total) : 0;
      const cls = STATUS_CLASS[s];
      return `<div class="breakdown-row">
        <div class="breakdown-info">
          <span class="status-dot status-${cls}"></span>
          <span class="breakdown-name">${STATUS_LABEL[s]}</span>
          <span class="breakdown-count">${count}</span>
        </div>
        <div class="breakdown-bar-wrap">
          <div class="breakdown-bar status-bar-${cls}" style="width:${pct}%"></div>
        </div>
      </div>`;
    }).join('');
  }

  // Recent (5 most recent by appliedDate)
  const recent = [...transitioned].sort((a, b) => new Date(b.appliedDate) - new Date(a.appliedDate)).slice(0, 5);
  const recentList = document.getElementById('recentList');
  if (recent.length === 0) {
    recentList.innerHTML = '<p class="empty-msg">Aucune candidature. <a href="./create.html">Ajoutez la première →</a></p>';
  } else {
    recentList.innerHTML = `<div class="recent-list">${recent.map(app => `
      <a href="./edit.html?id=${app.id}" class="recent-item">
        <div class="recent-info">
          <span class="recent-company">${app.companyName}</span>
          <span class="recent-role">${app.jobTitle}</span>
        </div>
        <div class="recent-meta">
          <span class="status-badge status-${STATUS_CLASS[app.status]}">${STATUS_LABEL[app.status]}</span>
          <span class="recent-date">${formatDateFr(app.appliedDate)}</span>
        </div>
      </a>`).join('')}</div>
      <a href="./applications.html" class="panel-link">Voir toutes les candidatures →</a>`;
  }
})();
</script>
</body>
</html>
```

- [ ] **Step 2: Manual verification**

Open a local dev server in `docs/` (e.g. `npx serve docs` or VS Code Live Server pointing at `docs/`). Configure real PAT + Gist ID in settings, then open `index.html`. Verify:
- Stats render (all show numbers, not `…`)
- Breakdown list renders
- Recent list renders
- No console errors

- [ ] **Step 3: Commit**

```powershell
git add docs/index.html
git commit -m "feat: add dashboard index.html"
```

---

## Task 7: Write `docs/applications.html` (List)

**Files:**
- Create: `docs/applications.html`

- [ ] **Step 1: Write `docs/applications.html`**

```html
<!DOCTYPE html>
<html lang="fr">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Candidatures — JobTracker</title>
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link href="https://fonts.googleapis.com/css2?family=DM+Serif+Display:ital@0;1&family=DM+Sans:wght@300;400;500;600&display=swap" rel="stylesheet">
  <link rel="icon" type="image/svg+xml" href="data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAzMiAzMiIgZmlsbD0ibm9uZSI+CiAgPHJlY3QgeD0iMyIgeT0iMTEiIHdpZHRoPSIyNiIgaGVpZ2h0PSIxNyIgcng9IjMiIGZpbGw9IiNmMGE4MzIiIG9wYWNpdHk9IjAuMiIvPgogIDxyZWN0IHg9IjMiIHk9IjExIiB3aWR0aD0iMjYiIGhlaWdodD0iMTciIHJ4PSIzIiBzdHJva2U9IiNmMGE4MzIiIHN0cm9rZS13aWR0aD0iMiIvPgogIDxwYXRoIGQ9Ik0xMSAxMVY5QzExIDcuMzQgMTIuMzQgNiAxNCA2SDE4QzE5LjY2IDYgMjEgNy4zNCAyMSA5VjExIiBzdHJva2U9IiNmMGE4MzIiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIi8+CiAgPGxpbmUgeDE9IjMiIHkxPSIyMCIgeDI9IjI5IiB5Mj0iMjAiIHN0cm9rZT0iI2YwYTgzMiIgc3Ryb2tlLXdpZHRoPSIxLjUiIG9wYWNpdHk9IjAuNCIvPgogIDxyZWN0IHg9IjE0IiB5PSIxOC41IiB3aWR0aD0iNCIgaGVpZ2h0PSIzIiByeD0iMSIgZmlsbD0iI2YwYTgzMiIvPgo8L3N2Zz4=" />
  <link rel="stylesheet" href="./css/site.css" />
</head>
<body>
<nav class="navbar">
  <a class="navbar-brand" href="./index.html">
    <svg class="brand-logo" width="34" height="34" viewBox="0 0 32 32" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
      <rect x="3" y="11" width="26" height="17" rx="3" fill="#f0a832" opacity="0.15"/>
      <rect x="3" y="11" width="26" height="17" rx="3" stroke="#f0a832" stroke-width="1.75"/>
      <path d="M11 11V9C11 7.34 12.34 6 14 6H18C19.66 6 21 7.34 21 9V11" stroke="#f0a832" stroke-width="1.75" stroke-linecap="round"/>
      <line x1="3" y1="20" x2="29" y2="20" stroke="#f0a832" stroke-width="1.5" opacity="0.35"/>
      <rect x="14" y="18.5" width="4" height="3" rx="1" fill="#f0a832"/>
    </svg>
    <span class="brand-text">JobTracker</span>
  </a>
  <div class="nav-links">
    <a href="./index.html" class="nav-link">Tableau de bord</a>
    <a href="./applications.html" class="nav-link active">Candidatures</a>
    <a href="./settings.html" class="nav-link">Paramètres</a>
    <a href="./create.html" class="nav-btn">+ Nouvelle</a>
  </div>
</nav>
<main class="main-content">
  <div class="page-container">
    <div class="page-header">
      <div>
        <h1 class="page-title">Candidatures</h1>
        <p class="page-subtitle" id="subtitle">…</p>
      </div>
      <div class="page-header-actions">
        <button id="exportBtn" class="btn btn-ghost">↓ Exporter</button>
        <label class="btn btn-ghost" style="cursor:pointer">
          ↑ Importer
          <input type="file" id="importFile" accept=".json" style="display:none" />
        </label>
        <a href="./create.html" class="btn btn-primary">+ Nouvelle candidature</a>
      </div>
    </div>

    <div id="errorBanner" style="display:none;" class="toast toast-error"></div>

    <div class="filters-bar">
      <input type="text" id="searchInput" placeholder="Rechercher entreprise ou poste…" class="filter-input" />
      <select id="statusFilter" class="filter-select">
        <option value="">Tous les statuts</option>
      </select>
      <select id="sortSelect" class="filter-select">
        <option value="date">Date ↓</option>
        <option value="company">Entreprise A–Z</option>
        <option value="status">Statut</option>
      </select>
      <button id="filterBtn" class="btn btn-secondary">Filtrer</button>
      <button id="clearBtn" class="btn btn-ghost" style="display:none">Effacer</button>
    </div>

    <div id="tableWrap"></div>
  </div>
</main>
<script src="./js/gist.js"></script>
<script src="./js/app.js"></script>
<script>
let ALL_APPS = [];

function renderTable(apps) {
  const wrap = document.getElementById('tableWrap');
  const total = apps.length;
  document.getElementById('subtitle').textContent =
    `${total} candidature${total !== 1 ? 's' : ''}`;

  if (total === 0) {
    wrap.innerHTML = `<div class="empty-state">
      <div class="empty-icon">◈</div>
      <p>Aucune candidature trouvée.</p>
      <a href="./create.html" class="btn btn-primary">Ajouter une candidature</a>
    </div>`;
    return;
  }

  const todayDate = today();
  const rows = apps.map(app => {
    const cls = STATUS_CLASS[app.status];
    const isInterview = new Set([STATUS.EntretienRecruteur, STATUS.EntretienTech,
      STATUS.TestTechnique, STATUS.EntretienRH]).has(app.status);
    const isWaiting = new Set([STATUS.AttenteRecruteur, STATUS.AttenteTech,
      STATUS.AttenteTest, STATUS.AttenteRH]).has(app.status);
    const isTerminal = app.status === STATUS.Refuse || app.status === STATUS.SansReponse;

    const companyCell = app.jobUrl
      ? `<a href="${app.jobUrl}" target="_blank" class="company-link">${app.companyName} <span class="ext-icon">↗</span></a>`
      : app.companyName;

    const salaryCell = app.salaryExpectation
      ? `<span class="td-salary">${Number(app.salaryExpectation).toLocaleString('fr-FR')} €</span>`
      : `<span class="text-muted">—</span>`;

    const rating = app.rating || 0;
    const ratingCell = rating > 0
      ? `<span class="star-display">${'★'.repeat(rating)}<span class="s-off">${'★'.repeat(5 - rating)}</span></span>`
      : `<span class="text-muted">—</span>`;

    let eventCell = '';
    if (isTerminal) {
      const d = app.lastResponseDate ? `<span class="event-date-terminal">${formatDateFr(app.lastResponseDate)}</span>` : '';
      eventCell = `<td class="td-interview td-interview-terminal" colspan="2">${d}</td>`;
    } else {
      const ivVal = app.interviewDate ? app.interviewDate.slice(0, 16) : '';
      const showInput = isInterview;
      const showStatic = (isWaiting || app.status === STATUS.AppelRecruteur) && app.interviewDate;
      eventCell = `<td class="td-interview" colspan="${app.teamsUrl ? 1 : 2}">
        <input type="datetime-local" class="interview-date-input ${showInput ? '' : 'interview-hidden'}"
          value="${ivVal}" onchange="saveDate(${app.id}, this.value)" />
        <span class="interview-date-past ${showStatic ? '' : 'interview-hidden'}">${app.interviewDate ? formatDateFr(app.interviewDate) : ''}</span>
      </td>`;
      if (app.teamsUrl) {
        eventCell += `<td class="td-teams"><a href="${app.teamsUrl}" target="_blank" class="teams-link" title="Rejoindre Teams">📹</a></td>`;
      }
    }

    const refusalBtn = app.refusalMailUrl
      ? `<a href="${app.refusalMailUrl}" target="_blank" class="action-btn action-refusal" title="Voir le mail de refus">✉</a>`
      : '';

    return `<tr class="app-row" data-id="${app.id}">
      <td class="td-company">${companyCell}</td>
      <td class="td-role">${app.jobTitle}</td>
      <td class="td-location">${app.location || '—'}</td>
      <td class="td-date">${formatDateFr(app.appliedDate)}</td>
      <td>${salaryCell}</td>
      <td class="td-rating">${ratingCell}</td>
      <td class="td-status">
        <select class="status-select status-${cls}"
          onchange="quickStatusChange(${app.id}, parseInt(this.value), this)"
          data-original="${app.status}">
          ${renderStatusOptions(app.status)}
        </select>
      </td>
      ${eventCell}
      <td class="td-actions">
        ${refusalBtn}
        <a href="./edit.html?id=${app.id}" class="action-btn" title="Modifier">✎</a>
        <button class="action-btn action-delete" onclick="deleteApp(${app.id})" title="Supprimer">✕</button>
      </td>
    </tr>`;
  }).join('');

  wrap.innerHTML = `<div class="app-table-wrap"><table class="app-table">
    <thead><tr>
      <th>Entreprise</th><th>Poste</th><th>Lieu</th><th>Date</th>
      <th>Salaire</th><th>Note</th><th>Statut</th><th>Événement</th><th>Actions</th>
    </tr></thead>
    <tbody>${rows}</tbody>
  </table></div>`;
}

function applyFilters() {
  const search = document.getElementById('searchInput').value.toLowerCase();
  const statusF = document.getElementById('statusFilter').value;
  const sort    = document.getElementById('sortSelect').value;
  const clearBtn = document.getElementById('clearBtn');
  clearBtn.style.display = (search || statusF) ? '' : 'none';

  let filtered = ALL_APPS;
  if (search) filtered = filtered.filter(a =>
    a.companyName.toLowerCase().includes(search) ||
    a.jobTitle.toLowerCase().includes(search));
  if (statusF !== '') filtered = filtered.filter(a => a.status === parseInt(statusF));
  renderTable(sortApplications(filtered, sort));
}

async function quickStatusChange(id, newStatus, select) {
  const original = parseInt(select.dataset.original);
  select.className = `status-select status-${STATUS_CLASS[newStatus] ?? ''}`;
  const app = ALL_APPS.find(a => a.id === id);
  if (!app) return;
  applyQuickStatusSideEffects(app, newStatus);
  // Update date input/display in the same row
  const row = select.closest('tr');
  const dateInput = row?.querySelector('.interview-date-input');
  const pastSpan  = row?.querySelector('.interview-date-past');
  const isInterview = new Set([STATUS.EntretienRecruteur, STATUS.EntretienTech,
    STATUS.TestTechnique, STATUS.EntretienRH]).has(newStatus);
  if (dateInput) {
    if (newStatus === STATUS.AppelRecruteur) {
      dateInput.classList.add('interview-hidden');
      if (pastSpan) { pastSpan.textContent = formatDateFr(app.interviewDate); pastSpan.classList.remove('interview-hidden'); }
    } else if (isInterview) {
      dateInput.value = ''; dateInput.classList.remove('interview-hidden');
      if (pastSpan) pastSpan.classList.add('interview-hidden');
    } else {
      dateInput.classList.add('interview-hidden');
      if (pastSpan) pastSpan.classList.add('interview-hidden');
    }
  }
  try {
    await writeApplications(ALL_APPS);
    select.dataset.original = newStatus;
    setTimeout(() => location.reload(), 400);
  } catch (e) {
    showToast('Erreur lors de la mise à jour : ' + e.message, 'error');
    app.status = original;
    select.value = original;
    select.className = `status-select status-${STATUS_CLASS[original] ?? ''}`;
  }
}

async function saveDate(id, value) {
  const app = ALL_APPS.find(a => a.id === id);
  if (!app) return;
  app.interviewDate = value || null;
  app.updatedAt = new Date().toISOString();
  try {
    await writeApplications(ALL_APPS);
    setTimeout(() => location.reload(), 400);
  } catch (e) {
    showToast('Erreur lors de la mise à jour : ' + e.message, 'error');
  }
}

async function deleteApp(id) {
  if (!confirm('Supprimer cette candidature ?')) return;
  ALL_APPS = ALL_APPS.filter(a => a.id !== id);
  try {
    await writeApplications(ALL_APPS);
    applyFilters();
  } catch (e) {
    showToast('Erreur lors de la suppression : ' + e.message, 'error');
  }
}

function exportApps() {
  const json = JSON.stringify(ALL_APPS, null, 2);
  const blob = new Blob([json], { type: 'application/json' });
  const url  = URL.createObjectURL(blob);
  const a    = document.createElement('a');
  a.href = url;
  a.download = `jobtracker-export-${new Date().toISOString().slice(0,10)}.json`;
  a.click();
  URL.revokeObjectURL(url);
}

document.getElementById('importFile').addEventListener('change', async function () {
  if (!this.files.length) return;
  const file = this.files[0];
  if (!confirm(`Importer "${file.name}" ? Les candidatures déjà présentes seront ignorées.`)) {
    this.value = ''; return;
  }
  try {
    const text = await file.text();
    const imported = JSON.parse(text);
    if (!Array.isArray(imported)) throw new Error('Format invalide');
    const existingIds = new Set(ALL_APPS.map(a => a.id));
    let added = 0, skipped = 0;
    for (const app of imported) {
      if (existingIds.has(app.id)) { skipped++; continue; }
      app.updatedAt = new Date().toISOString();
      ALL_APPS.push(app);
      added++;
    }
    await writeApplications(ALL_APPS);
    showToast(skipped > 0
      ? `${added} importée(s), ${skipped} ignorée(s) (déjà présentes).`
      : `${added} candidature(s) importée(s) avec succès.`);
    applyFilters();
  } catch (e) {
    showToast('Fichier JSON invalide ou erreur : ' + e.message, 'error');
  }
  this.value = '';
});

// Populate status filter dropdown
const statusFilterEl = document.getElementById('statusFilter');
Object.entries(STATUS_LABEL).forEach(([val, label]) => {
  const opt = document.createElement('option');
  opt.value = val; opt.textContent = label;
  statusFilterEl.appendChild(opt);
});

document.getElementById('filterBtn').addEventListener('click', applyFilters);
document.getElementById('clearBtn').addEventListener('click', () => {
  document.getElementById('searchInput').value = '';
  document.getElementById('statusFilter').value = '';
  applyFilters();
});
document.getElementById('searchInput').addEventListener('keydown', e => { if (e.key === 'Enter') applyFilters(); });
document.getElementById('exportBtn').addEventListener('click', exportApps);

(async () => {
  redirectIfNoSettings();
  try {
    ALL_APPS = await readApplications();
  } catch (e) {
    const b = document.getElementById('errorBanner');
    b.style.display = ''; b.innerHTML =
      `✕ Impossible de lire le Gist : ${e.message}. <a href="./settings.html">Vérifier les paramètres →</a>`;
    document.getElementById('subtitle').textContent = '–';
    return;
  }
  const { apps, changed } = applyAutoTransitions(ALL_APPS);
  ALL_APPS = apps;
  if (changed) { try { await writeApplications(ALL_APPS); } catch (_) {} }
  applyFilters();
})();
</script>
</body>
</html>
```

- [ ] **Step 2: Manual verification**

Open in browser. Verify:
- List renders with correct data from Gist
- Search filters results client-side without a page reload
- Status select updates Gist and reloads
- Delete removes from Gist
- Export downloads JSON file
- Import uploads a previously exported JSON and skips duplicates

- [ ] **Step 3: Commit**

```powershell
git add docs/applications.html
git commit -m "feat: add applications list with inline editing, export, import"
```

---

## Task 8: Write `docs/create.html`

**Files:**
- Create: `docs/create.html`

- [ ] **Step 1: Write `docs/create.html`**

```html
<!DOCTYPE html>
<html lang="fr">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Nouvelle candidature — JobTracker</title>
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link href="https://fonts.googleapis.com/css2?family=DM+Serif+Display:ital@0;1&family=DM+Sans:wght@300;400;500;600&display=swap" rel="stylesheet">
  <link rel="icon" type="image/svg+xml" href="data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAzMiAzMiIgZmlsbD0ibm9uZSI+CiAgPHJlY3QgeD0iMyIgeT0iMTEiIHdpZHRoPSIyNiIgaGVpZ2h0PSIxNyIgcng9IjMiIGZpbGw9IiNmMGE4MzIiIG9wYWNpdHk9IjAuMiIvPgogIDxyZWN0IHg9IjMiIHk9IjExIiB3aWR0aD0iMjYiIGhlaWdodD0iMTciIHJ4PSIzIiBzdHJva2U9IiNmMGE4MzIiIHN0cm9rZS13aWR0aD0iMiIvPgogIDxwYXRoIGQ9Ik0xMSAxMVY5QzExIDcuMzQgMTIuMzQgNiAxNCA2SDE4QzE5LjY2IDYgMjEgNy4zNCAyMSA5VjExIiBzdHJva2U9IiNmMGE4MzIiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIi8+CiAgPGxpbmUgeDE9IjMiIHkxPSIyMCIgeDI9IjI5IiB5Mj0iMjAiIHN0cm9rZT0iI2YwYTgzMiIgc3Ryb2tlLXdpZHRoPSIxLjUiIG9wYWNpdHk9IjAuNCIvPgogIDxyZWN0IHg9IjE0IiB5PSIxOC41IiB3aWR0aD0iNCIgaGVpZ2h0PSIzIiByeD0iMSIgZmlsbD0iI2YwYTgzMiIvPgo8L3N2Zz4=" />
  <link rel="stylesheet" href="./css/site.css" />
</head>
<body>
<nav class="navbar">
  <a class="navbar-brand" href="./index.html">
    <svg class="brand-logo" width="34" height="34" viewBox="0 0 32 32" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
      <rect x="3" y="11" width="26" height="17" rx="3" fill="#f0a832" opacity="0.15"/>
      <rect x="3" y="11" width="26" height="17" rx="3" stroke="#f0a832" stroke-width="1.75"/>
      <path d="M11 11V9C11 7.34 12.34 6 14 6H18C19.66 6 21 7.34 21 9V11" stroke="#f0a832" stroke-width="1.75" stroke-linecap="round"/>
      <line x1="3" y1="20" x2="29" y2="20" stroke="#f0a832" stroke-width="1.5" opacity="0.35"/>
      <rect x="14" y="18.5" width="4" height="3" rx="1" fill="#f0a832"/>
    </svg>
    <span class="brand-text">JobTracker</span>
  </a>
  <div class="nav-links">
    <a href="./index.html" class="nav-link">Tableau de bord</a>
    <a href="./applications.html" class="nav-link">Candidatures</a>
    <a href="./settings.html" class="nav-link">Paramètres</a>
    <a href="./create.html" class="nav-btn">+ Nouvelle</a>
  </div>
</nav>
<main class="main-content">
  <div class="page-container page-narrow">
    <div class="page-header">
      <h1 class="page-title">Nouvelle candidature</h1>
    </div>

    <div class="panel url-import-panel" style="margin-bottom:1.25rem;">
      <p class="import-label">Remplissage automatique depuis une URL</p>
      <div class="url-import-row">
        <input type="url" id="urlInput" class="form-input" placeholder="https://fr.indeed.com/..." />
        <button type="button" id="fetchBtn" class="btn btn-secondary">Importer</button>
      </div>
      <div id="importStatus" style="display:none;" class="import-status"></div>
    </div>

    <form id="appForm">
      <div class="form-grid">
        <div class="form-group form-col-2">
          <label class="form-label">Entreprise *</label>
          <input type="text" id="companyName" class="form-input" placeholder="ex. Acme Corp" required maxlength="200" />
        </div>
        <div class="form-group form-col-2">
          <label class="form-label">Poste *</label>
          <input type="text" id="jobTitle" class="form-input" placeholder="ex. Développeur Senior" required maxlength="200" />
        </div>
        <div class="form-group form-col-2">
          <label class="form-label">Lieu</label>
          <div class="location-wrap">
            <input type="text" id="locationInput" class="form-input" placeholder="ex. Paris, Île-de-France" autocomplete="off" maxlength="200" />
            <span class="location-pin">📍</span>
          </div>
        </div>
        <div class="form-group form-col-2">
          <label class="form-label">Date de candidature *</label>
          <input type="date" id="appliedDate" class="form-input" required />
        </div>
        <div class="form-group form-col-full">
          <label class="form-label">URL de l'offre</label>
          <input type="url" id="jobUrl" class="form-input" placeholder="https://..." maxlength="500" />
        </div>
        <div class="form-group form-col-2">
          <label class="form-label">Statut</label>
          <select id="statusSelect" class="form-select"></select>
        </div>
        <div class="form-group form-col-2" id="interviewDateGroup">
          <label class="form-label">Date d'entretien / deadline</label>
          <input type="datetime-local" id="interviewDate" class="form-input" />
        </div>
        <div class="form-group form-col-2">
          <label class="form-label">Date de dernière réponse</label>
          <input type="date" id="lastResponseDate" class="form-input" />
        </div>
        <div class="form-group form-col-2">
          <label class="form-label">Salaire souhaité (€)</label>
          <input type="number" id="salaryExpectation" class="form-input" placeholder="ex. 45000" min="0" max="9999999" />
        </div>
        <div class="form-group form-col-2">
          <label class="form-label">Note recruteur / entreprise</label>
          <div class="star-picker">
            <input type="hidden" id="ratingInput" value="" />
            <button type="button" class="star-btn">★</button>
            <button type="button" class="star-btn">★</button>
            <button type="button" class="star-btn">★</button>
            <button type="button" class="star-btn">★</button>
            <button type="button" class="star-btn">★</button>
          </div>
        </div>
        <div class="form-group form-col-2">
          <label class="form-label">URL du mail de refus</label>
          <input type="url" id="refusalMailUrl" class="form-input" placeholder="https://mail.google.com/..." maxlength="500" />
        </div>
        <div class="form-group form-col-2">
          <label class="form-label">Lien Teams</label>
          <input type="url" id="teamsUrl" class="form-input" placeholder="https://teams.microsoft.com/..." maxlength="500" />
        </div>
        <div class="form-group form-col-full">
          <label class="form-label">Notes</label>
          <textarea id="notes" class="form-input form-textarea" placeholder="Nom du recruteur, impressions, prochaines étapes…" rows="4" maxlength="2000"></textarea>
        </div>
      </div>
      <div class="form-actions">
        <button type="submit" class="btn btn-primary">Ajouter</button>
        <a href="./applications.html" class="btn btn-ghost">Annuler</a>
      </div>
    </form>
  </div>
</main>
<script src="./js/gist.js"></script>
<script src="./js/app.js"></script>
<script>
redirectIfNoSettings();

// Set today as default applied date
document.getElementById('appliedDate').value = new Date().toISOString().split('T')[0];

// Populate status select
const statusSelectEl = document.getElementById('statusSelect');
statusSelectEl.innerHTML = renderStatusOptions(STATUS.Candidature);

// Show/hide interview date based on status
const INTERVIEW_STATUSES_SET = new Set([STATUS.EntretienRecruteur, STATUS.EntretienTech,
  STATUS.TestTechnique, STATUS.EntretienRH]);
function toggleInterviewDate() {
  const s = parseInt(statusSelectEl.value);
  document.getElementById('interviewDateGroup').style.display =
    INTERVIEW_STATUSES_SET.has(s) ? '' : 'none';
}
statusSelectEl.addEventListener('change', toggleInterviewDate);
toggleInterviewDate();

// Star rating widget
(function () {
  const input = document.getElementById('ratingInput');
  const btns  = Array.from(document.querySelectorAll('.star-btn'));
  function highlight(n) { btns.forEach((b, i) => b.classList.toggle('lit', i < n)); }
  highlight(0);
  btns.forEach((btn, idx) => {
    btn.addEventListener('mouseenter', () => highlight(idx + 1));
    btn.addEventListener('mouseleave', () => highlight(parseInt(input.value) || 0));
    btn.addEventListener('click', () => {
      const cur = parseInt(input.value) || 0;
      const next = cur === idx + 1 ? 0 : idx + 1;
      input.value = next || '';
      highlight(next);
    });
  });
})();

// URL auto-fill via allorigins.win CORS proxy
document.getElementById('fetchBtn').addEventListener('click', async () => {
  const url = document.getElementById('urlInput').value.trim();
  if (!url) return;
  const statusEl = document.getElementById('importStatus');
  statusEl.style.display = '';
  statusEl.className = 'import-status import-loading';
  statusEl.textContent = 'Récupération en cours…';

  try {
    const proxyUrl = 'https://api.allorigins.win/raw?url=' + encodeURIComponent(url);
    const resp = await fetch(proxyUrl, { signal: AbortSignal.timeout(10000) });
    if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
    const html = await resp.text();
    const doc = new DOMParser().parseFromString(html, 'text/html');

    const ogTitle   = doc.querySelector('meta[property="og:title"]')?.content?.trim();
    const ogSite    = doc.querySelector('meta[property="og:site_name"]')?.content?.trim();
    const pageTitle = doc.querySelector('title')?.textContent?.trim();

    const title = ogTitle || pageTitle || '';
    const site  = ogSite || '';

    if (title) document.getElementById('jobTitle').value = title;
    if (site)  document.getElementById('companyName').value = site;
    if (!url.startsWith('http')) document.getElementById('jobUrl').value = url;
    else document.getElementById('jobUrl').value = url;

    statusEl.className = 'import-status ' + (title ? 'import-success' : 'import-warn');
    statusEl.textContent = title
      ? 'Informations récupérées. Vérifiez et complétez les champs.'
      : 'Impossible d\'extraire les informations. Remplissez manuellement.';
  } catch (e) {
    statusEl.className = 'import-status import-warn';
    statusEl.textContent = 'Récupération impossible (site protégé ou réseau). Remplissez manuellement.';
  }
});

// Form submit
document.getElementById('appForm').addEventListener('submit', async e => {
  e.preventDefault();
  const submitBtn = e.target.querySelector('[type=submit]');
  submitBtn.disabled = true; submitBtn.textContent = 'Enregistrement…';

  try {
    const apps = await readApplications();
    const now = new Date().toISOString();
    const newApp = {
      id:                generateId(apps),
      companyName:       document.getElementById('companyName').value.trim(),
      jobTitle:          document.getElementById('jobTitle').value.trim(),
      jobUrl:            document.getElementById('jobUrl').value.trim() || null,
      location:          document.getElementById('locationInput').value.trim() || null,
      appliedDate:       document.getElementById('appliedDate').value + 'T00:00:00',
      lastResponseDate:  document.getElementById('lastResponseDate').value || null,
      status:            parseInt(statusSelectEl.value),
      interviewDate:     document.getElementById('interviewDate').value || null,
      notes:             document.getElementById('notes').value.trim() || null,
      salaryExpectation: document.getElementById('salaryExpectation').value
                           ? parseInt(document.getElementById('salaryExpectation').value) : null,
      rating:            document.getElementById('ratingInput').value
                           ? parseInt(document.getElementById('ratingInput').value) : null,
      refusalMailUrl:    document.getElementById('refusalMailUrl').value.trim() || null,
      teamsUrl:          document.getElementById('teamsUrl').value.trim() || null,
      createdAt:         now,
      updatedAt:         now,
    };
    apps.push(newApp);
    await writeApplications(apps);
    window.location.href = './applications.html';
  } catch (e) {
    showToast('Erreur lors de l\'enregistrement : ' + e.message, 'error');
    submitBtn.disabled = false; submitBtn.textContent = 'Ajouter';
  }
});

maybeLoadGoogleMaps();
</script>
</body>
</html>
```

- [ ] **Step 2: Manual verification**

Open `create.html`. Verify:
- Status select shows all 13 statuses
- Interview date field hides/shows based on status
- Star rating widget works (hover, click, click again to clear)
- URL auto-fill: paste any non-Cloudflare URL (e.g. a simple blog post) and check fields populate; paste an Indeed URL and confirm graceful fallback warning
- Submit creates a new entry visible in `applications.html`

- [ ] **Step 3: Commit**

```powershell
git add docs/create.html
git commit -m "feat: add create.html with URL auto-fill fallback and full form"
```

---

## Task 9: Write `docs/edit.html`

**Files:**
- Create: `docs/edit.html`

- [ ] **Step 1: Write `docs/edit.html`**

```html
<!DOCTYPE html>
<html lang="fr">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>Modifier la candidature — JobTracker</title>
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link href="https://fonts.googleapis.com/css2?family=DM+Serif+Display:ital@0;1&family=DM+Sans:wght@300;400;500;600&display=swap" rel="stylesheet">
  <link rel="icon" type="image/svg+xml" href="data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAzMiAzMiIgZmlsbD0ibm9uZSI+CiAgPHJlY3QgeD0iMyIgeT0iMTEiIHdpZHRoPSIyNiIgaGVpZ2h0PSIxNyIgcng9IjMiIGZpbGw9IiNmMGE4MzIiIG9wYWNpdHk9IjAuMiIvPgogIDxyZWN0IHg9IjMiIHk9IjExIiB3aWR0aD0iMjYiIGhlaWdodD0iMTciIHJ4PSIzIiBzdHJva2U9IiNmMGE4MzIiIHN0cm9rZS13aWR0aD0iMiIvPgogIDxwYXRoIGQ9Ik0xMSAxMVY5QzExIDcuMzQgMTIuMzQgNiAxNCA2SDE4QzE5LjY2IDYgMjEgNy4zNCAyMSA5VjExIiBzdHJva2U9IiNmMGE4MzIiIHN0cm9rZS13aWR0aD0iMiIgc3Ryb2tlLWxpbmVjYXA9InJvdW5kIi8+CiAgPGxpbmUgeDE9IjMiIHkxPSIyMCIgeDI9IjI5IiB5Mj0iMjAiIHN0cm9rZT0iI2YwYTgzMiIgc3Ryb2tlLXdpZHRoPSIxLjUiIG9wYWNpdHk9IjAuNCIvPgogIDxyZWN0IHg9IjE0IiB5PSIxOC41IiB3aWR0aD0iNCIgaGVpZ2h0PSIzIiByeD0iMSIgZmlsbD0iI2YwYTgzMiIvPgo8L3N2Zz4=" />
  <link rel="stylesheet" href="./css/site.css" />
</head>
<body>
<nav class="navbar">
  <a class="navbar-brand" href="./index.html">
    <svg class="brand-logo" width="34" height="34" viewBox="0 0 32 32" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
      <rect x="3" y="11" width="26" height="17" rx="3" fill="#f0a832" opacity="0.15"/>
      <rect x="3" y="11" width="26" height="17" rx="3" stroke="#f0a832" stroke-width="1.75"/>
      <path d="M11 11V9C11 7.34 12.34 6 14 6H18C19.66 6 21 7.34 21 9V11" stroke="#f0a832" stroke-width="1.75" stroke-linecap="round"/>
      <line x1="3" y1="20" x2="29" y2="20" stroke="#f0a832" stroke-width="1.5" opacity="0.35"/>
      <rect x="14" y="18.5" width="4" height="3" rx="1" fill="#f0a832"/>
    </svg>
    <span class="brand-text">JobTracker</span>
  </a>
  <div class="nav-links">
    <a href="./index.html" class="nav-link">Tableau de bord</a>
    <a href="./applications.html" class="nav-link">Candidatures</a>
    <a href="./settings.html" class="nav-link">Paramètres</a>
    <a href="./create.html" class="nav-btn">+ Nouvelle</a>
  </div>
</nav>
<main class="main-content">
  <div class="page-container page-narrow">
    <div class="page-header">
      <h1 class="page-title" id="pageTitle">Modifier la candidature</h1>
    </div>
    <form id="appForm">
      <div class="form-grid">
        <div class="form-group form-col-2">
          <label class="form-label">Entreprise *</label>
          <input type="text" id="companyName" class="form-input" required maxlength="200" />
        </div>
        <div class="form-group form-col-2">
          <label class="form-label">Poste *</label>
          <input type="text" id="jobTitle" class="form-input" required maxlength="200" />
        </div>
        <div class="form-group form-col-2">
          <label class="form-label">Lieu</label>
          <div class="location-wrap">
            <input type="text" id="locationInput" class="form-input" autocomplete="off" maxlength="200" />
            <span class="location-pin">📍</span>
          </div>
        </div>
        <div class="form-group form-col-2">
          <label class="form-label">Date de candidature *</label>
          <input type="date" id="appliedDate" class="form-input" required />
        </div>
        <div class="form-group form-col-full">
          <label class="form-label">URL de l'offre</label>
          <input type="url" id="jobUrl" class="form-input" maxlength="500" />
        </div>
        <div class="form-group form-col-2">
          <label class="form-label">Statut</label>
          <select id="statusSelect" class="form-select"></select>
        </div>
        <div class="form-group form-col-2" id="interviewDateGroup">
          <label class="form-label">Date d'entretien / deadline</label>
          <input type="datetime-local" id="interviewDate" class="form-input" />
        </div>
        <div class="form-group form-col-2">
          <label class="form-label">Date de dernière réponse</label>
          <input type="date" id="lastResponseDate" class="form-input" />
        </div>
        <div class="form-group form-col-2">
          <label class="form-label">Salaire souhaité (€)</label>
          <input type="number" id="salaryExpectation" class="form-input" min="0" max="9999999" />
        </div>
        <div class="form-group form-col-2">
          <label class="form-label">Note recruteur / entreprise</label>
          <div class="star-picker">
            <input type="hidden" id="ratingInput" value="" />
            <button type="button" class="star-btn">★</button>
            <button type="button" class="star-btn">★</button>
            <button type="button" class="star-btn">★</button>
            <button type="button" class="star-btn">★</button>
            <button type="button" class="star-btn">★</button>
          </div>
        </div>
        <div class="form-group form-col-2">
          <label class="form-label">URL du mail de refus</label>
          <input type="url" id="refusalMailUrl" class="form-input" maxlength="500" />
        </div>
        <div class="form-group form-col-2">
          <label class="form-label">Lien Teams</label>
          <input type="url" id="teamsUrl" class="form-input" maxlength="500" />
        </div>
        <div class="form-group form-col-full">
          <label class="form-label">Notes</label>
          <textarea id="notes" class="form-input form-textarea" rows="4" maxlength="2000"></textarea>
        </div>
      </div>
      <div class="form-actions">
        <button type="submit" class="btn btn-primary">Enregistrer</button>
        <a href="./applications.html" class="btn btn-ghost">Annuler</a>
        <button type="button" id="deleteBtn" class="btn btn-danger" style="margin-left:auto;">Supprimer</button>
      </div>
    </form>
  </div>
</main>
<script src="./js/gist.js"></script>
<script src="./js/app.js"></script>
<script>
redirectIfNoSettings();

const appId = parseInt(new URLSearchParams(window.location.search).get('id'));
if (!appId) window.location.href = './applications.html';

const INTERVIEW_STATUSES_SET = new Set([STATUS.EntretienRecruteur, STATUS.EntretienTech,
  STATUS.TestTechnique, STATUS.EntretienRH]);

function toggleInterviewDate() {
  const s = parseInt(document.getElementById('statusSelect').value);
  document.getElementById('interviewDateGroup').style.display =
    INTERVIEW_STATUSES_SET.has(s) ? '' : 'none';
}

// Star rating widget (same as create.html)
(function () {
  const input = document.getElementById('ratingInput');
  const btns  = Array.from(document.querySelectorAll('.star-btn'));
  function highlight(n) { btns.forEach((b, i) => b.classList.toggle('lit', i < n)); }
  highlight(0);
  btns.forEach((btn, idx) => {
    btn.addEventListener('mouseenter', () => highlight(idx + 1));
    btn.addEventListener('mouseleave', () => highlight(parseInt(input.value) || 0));
    btn.addEventListener('click', () => {
      const cur = parseInt(input.value) || 0;
      const next = cur === idx + 1 ? 0 : idx + 1;
      input.value = next || '';
      highlight(next);
    });
  });
})();

let ALL_APPS = [];

(async () => {
  try {
    ALL_APPS = await readApplications();
  } catch (e) {
    showToast('Impossible de charger : ' + e.message, 'error');
    return;
  }

  const app = ALL_APPS.find(a => a.id === appId);
  if (!app) { showToast('Candidature introuvable.', 'error'); return; }

  document.getElementById('pageTitle').textContent = `Modifier — ${app.companyName}`;

  // Populate status select
  const statusSelectEl = document.getElementById('statusSelect');
  statusSelectEl.innerHTML = renderStatusOptions(app.status);
  statusSelectEl.addEventListener('change', toggleInterviewDate);

  // Fill form fields
  document.getElementById('companyName').value       = app.companyName || '';
  document.getElementById('jobTitle').value          = app.jobTitle || '';
  document.getElementById('locationInput').value     = app.location || '';
  document.getElementById('appliedDate').value       = (app.appliedDate || '').split('T')[0];
  document.getElementById('jobUrl').value            = app.jobUrl || '';
  document.getElementById('interviewDate').value     = app.interviewDate ? app.interviewDate.slice(0, 16) : '';
  document.getElementById('lastResponseDate').value  = app.lastResponseDate ? app.lastResponseDate.split('T')[0] : '';
  document.getElementById('salaryExpectation').value = app.salaryExpectation ?? '';
  document.getElementById('refusalMailUrl').value    = app.refusalMailUrl || '';
  document.getElementById('teamsUrl').value          = app.teamsUrl || '';
  document.getElementById('notes').value             = app.notes || '';

  const ratingInput = document.getElementById('ratingInput');
  ratingInput.value = app.rating ?? '';
  const starBtns = Array.from(document.querySelectorAll('.star-btn'));
  const n = parseInt(ratingInput.value) || 0;
  starBtns.forEach((b, i) => b.classList.toggle('lit', i < n));

  toggleInterviewDate();

  document.getElementById('appForm').addEventListener('submit', async e => {
    e.preventDefault();
    const submitBtn = e.target.querySelector('[type=submit]');
    submitBtn.disabled = true; submitBtn.textContent = 'Enregistrement…';
    try {
      const idx = ALL_APPS.findIndex(a => a.id === appId);
      ALL_APPS[idx] = {
        ...app,
        companyName:       document.getElementById('companyName').value.trim(),
        jobTitle:          document.getElementById('jobTitle').value.trim(),
        location:          document.getElementById('locationInput').value.trim() || null,
        appliedDate:       document.getElementById('appliedDate').value + 'T00:00:00',
        jobUrl:            document.getElementById('jobUrl').value.trim() || null,
        status:            parseInt(statusSelectEl.value),
        interviewDate:     document.getElementById('interviewDate').value || null,
        lastResponseDate:  document.getElementById('lastResponseDate').value || null,
        salaryExpectation: document.getElementById('salaryExpectation').value
                             ? parseInt(document.getElementById('salaryExpectation').value) : null,
        rating:            document.getElementById('ratingInput').value
                             ? parseInt(document.getElementById('ratingInput').value) : null,
        refusalMailUrl:    document.getElementById('refusalMailUrl').value.trim() || null,
        teamsUrl:          document.getElementById('teamsUrl').value.trim() || null,
        notes:             document.getElementById('notes').value.trim() || null,
        updatedAt:         new Date().toISOString(),
      };
      await writeApplications(ALL_APPS);
      window.location.href = './applications.html';
    } catch (err) {
      showToast('Erreur lors de l\'enregistrement : ' + err.message, 'error');
      submitBtn.disabled = false; submitBtn.textContent = 'Enregistrer';
    }
  });

  document.getElementById('deleteBtn').addEventListener('click', async () => {
    if (!confirm(`Supprimer la candidature chez ${app.companyName} ?`)) return;
    try {
      await writeApplications(ALL_APPS.filter(a => a.id !== appId));
      window.location.href = './applications.html';
    } catch (err) {
      showToast('Erreur lors de la suppression : ' + err.message, 'error');
    }
  });

  maybeLoadGoogleMaps();
})();
</script>
</body>
</html>
```

- [ ] **Step 2: Manual verification**

Navigate from `applications.html` → click edit on a row → verify:
- Form pre-fills with existing data
- Star rating shows correct stars
- Interview date group hides/shows correctly
- Save updates the record in `applications.html`
- Delete removes the record

- [ ] **Step 3: Commit**

```powershell
git add docs/edit.html
git commit -m "feat: add edit.html with pre-filled form and delete"
```

---

## Task 10: Configure GitHub Pages

- [ ] **Step 1: Push all commits to remote**

```powershell
git push origin main
```

- [ ] **Step 2: Enable GitHub Pages in repository settings**

1. On GitHub, go to your repository → **Settings** → **Pages**
2. Under **Source**, select **Deploy from a branch**
3. Branch: `main`, Folder: `/docs`
4. Click **Save**

GitHub will show a URL like `https://<username>.github.io/<repo-name>/` within ~60 seconds.

- [ ] **Step 3: Open the live URL and run the full smoke test**

Navigate to the GitHub Pages URL. Verify end-to-end:
1. Redirected to `settings.html` (no PAT configured yet)
2. Enter PAT + click "Créer un nouveau Gist" → Gist ID fills in
3. Click "Tester la connexion" → success message
4. Click "Enregistrer" → redirected to dashboard
5. Dashboard shows "0 candidatures"
6. Create one application → appears in list
7. Edit that application → save → changes persist after page reload
8. Open the app in a **different browser / incognito** → go to settings → enter same PAT + Gist ID → see the same application

- [ ] **Step 4: Final commit (CLAUDE.md update)**

Update `CLAUDE.md` to document the GitHub Pages deployment:

In the `## Commands` section, add:
```
## GitHub Pages (static version)
Served from `docs/` — configure GitHub Pages in repo settings to use branch `main`, folder `/docs`.
One-time setup per device: visit `/settings.html`, enter your GitHub PAT (gist scope) and Gist ID.
Data migration from .NET app: Export → JSON → Import in the static app.
Tests: node docs/js/app.test.js
```

Then commit:
```powershell
git add CLAUDE.md
git commit -m "docs: document GitHub Pages deployment in CLAUDE.md"
git push origin main
```

---

## Self-review

**Spec coverage check:**

| Spec requirement | Covered by |
|---|---|
| GitHub Pages from `docs/` | Task 10 |
| Private Gist as database | Task 4 (`gist.js`) |
| PAT + Gist ID in localStorage | Task 4 + Task 5 |
| Redirect to settings if not configured | Task 4 (`redirectIfNoSettings`) |
| Dashboard stats + breakdown + recent | Task 6 |
| Auto-transitions on every page load | Task 3 (`applyAutoTransitions`) + Tasks 6, 7 |
| List with search/filter/sort | Task 7 |
| Inline status update + side-effects | Task 7 (`quickStatusChange`) |
| Inline interview date picker | Task 7 (`saveDate`) |
| Delete from list | Task 7 (`deleteApp`) |
| Export JSON | Task 7 (`exportApps`) |
| Import JSON, skip duplicates | Task 7 (import handler) |
| Create form, all fields | Task 8 |
| URL auto-fill (fetch + fallback) | Task 8 |
| Google Maps autocomplete (optional) | Task 3 (`maybeLoadGoogleMaps`) + Tasks 8, 9 |
| Star rating widget | Tasks 8, 9 |
| Edit form pre-filled | Task 9 |
| Delete from edit form | Task 9 |
| Error banner on Gist failure | Tasks 6, 7 |
| Error toast on write failure | Tasks 7, 8, 9 |
| "Create new Gist" button | Task 5 |
| "Test connection" button | Task 5 |
| CSS unchanged from original | Task 1 |
| Unit tests for pure functions | Tasks 2, 3 |
| CV page dropped | (no task — intentional) |
