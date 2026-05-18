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
