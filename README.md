# JobTracker

A personal French-language job application tracker. Runs entirely in the browser — no server, no database, no install. Data is stored in a private GitHub Gist.

**Live:** https://mathieu-chateigner.github.io/JobTracker/

## First-time setup

1. Create a GitHub Personal Access Token with the **`gist`** scope only → [github.com/settings/tokens](https://github.com/settings/tokens/new?scopes=gist&description=JobTracker)
2. Open the app and go to **Paramètres**
3. Paste your token, click **Créer un nouveau Gist**, then **Enregistrer**

That's it. The same PAT + Gist ID on any other device or browser gives you access to the same data.

## Features

- **Tableau de bord** — stats, status breakdown, recent applications
- **Candidatures** — searchable/filterable list with inline status and interview date editing
- **Ajout rapide** — paste a job URL to auto-fill title and company via a CORS proxy
- **Export / Import** — full JSON backup, import skips duplicate IDs
- **Auto-transitions** — past interview dates automatically advance to the corresponding waiting status; applications with no response after 21 days become *Sans réponse*
- **Google Maps autocomplete** — optional, configure API key in Paramètres

## Status flow

```
Candidature → Appel recruteur → Entretien recruteur → En attente (recruteur)
                                                     → Entretien tech      → En attente (tech)
                                                     → Test technique      → En attente (test)
                                                     → Entretien RH        → En attente (RH)
                                                                           → Offre
                                                                           → Refusé
                                                     → Sans réponse
```

## Local development

No build step. Serve the repo root with any static server:

```bash
npx serve .
```

Run unit tests (pure logic, no browser):

```bash
node js/app.test.js
```
