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

// ── formatDateFr ──────────────────────────────────────────────────────────────
console.log('\nformatDateFr:');

const { formatDateFr } = require('./app.js');

test('null returns em dash', () => {
  assertEqual(formatDateFr(null), '—');
});
test('empty string returns em dash', () => {
  assertEqual(formatDateFr(''), '—');
});

// ── Summary ───────────────────────────────────────────────────────────────────
console.log(`\n${passed} passed, ${failed} failed`);
if (failed > 0) process.exit(1);
