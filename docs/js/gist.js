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
