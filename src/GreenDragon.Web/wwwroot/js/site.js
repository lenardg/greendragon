const themeSelect = document.querySelector("#theme");
const themeKey = "greendragon.theme";
const themes = new Set(["system", "light", "dark"]);

function applyTheme(theme) {
  document.documentElement.dataset.theme = theme;
  if (themeSelect) themeSelect.value = theme;
}

try {
  const saved = localStorage.getItem(themeKey);
  applyTheme(themes.has(saved) ? saved : "system");
} catch {
  applyTheme("system");
}

themeSelect?.addEventListener("change", () => {
  applyTheme(themeSelect.value);
  try {
    localStorage.setItem(themeKey, themeSelect.value);
  } catch {
    // Theme selection still works when browser storage is unavailable.
  }
});

const discoveryForm = document.querySelector("[data-discovery-form]");
const loadingCard = document.querySelector("[data-loading-card]");
const speakerResults = document.querySelector("[data-speaker-results]");
const dirtyVolumes = new Set();
let refreshTimer;
let activeRequest = Promise.resolve();
let requestRunning = false;
let commandPending = false;
let revision = 0;
let controller;
let hasCatalog = Boolean(speakerResults?.querySelector(".rooms"));
let initialDiscoveryAttempted = hasCatalog;
const refreshDelay = Number(discoveryForm?.dataset.refreshSeconds) * 1000;
document.documentElement.classList.add("js-enabled");

function cardId(element) {
  return element.closest("[data-speaker-id]")?.dataset.speakerId;
}

function scheduleRefresh() {
  clearTimeout(refreshTimer);
  if (!discoveryForm || document.hidden || requestRunning || commandPending) return;
  refreshTimer = setTimeout(() => {
    if (dirtyVolumes.size) scheduleRefresh();
    else refreshSpeakers();
  }, refreshDelay);
}

function showError(message) {
  speakerResults.querySelector("[data-load-error]")?.remove();
  const error = document.createElement("p");
  error.dataset.loadError = "true";
  error.setAttribute("role", "alert");
  error.textContent = message;
  speakerResults.prepend(error);
}

function setCommandBusy(busy) {
  for (const control of speakerResults.querySelectorAll("[data-control]")) {
    control.dataset.serverDisabled ??= String(control.disabled);
    control.disabled = busy || control.dataset.serverDisabled === "true";
  }
}

function updateCard(current, incoming) {
  // Keep form controls themselves alive so keyboard focus and slider interaction survive a refresh.
  current.querySelector("[data-card-display]").replaceChildren(...incoming.querySelector("[data-card-display]").childNodes);
  for (const fresh of incoming.querySelectorAll("[data-control]")) {
    const control = [...current.querySelectorAll("[data-control]")].find(item => item.dataset.control === fresh.dataset.control);
    if (!control) continue;
    const editingVolume = dirtyVolumes.has(current.dataset.speakerId) && control.closest("[data-volume-form]");
    if (editingVolume) continue;
    for (const attribute of ["aria-label", "aria-pressed", "title"]) {
      if (fresh.hasAttribute(attribute)) control.setAttribute(attribute, fresh.getAttribute(attribute));
      else control.removeAttribute(attribute);
    }
    control.dataset.serverDisabled = String(fresh.disabled);
    control.disabled = commandPending || fresh.disabled;
    if (control.matches("input[type=range]")) control.value = fresh.value;
    else control.replaceChildren(...fresh.childNodes);
  }
  if (!dirtyVolumes.has(current.dataset.speakerId)) {
    current.querySelector("[data-volume-value]").textContent = incoming.querySelector("[data-volume-value]").textContent;
    current.querySelector("[data-volume-form]").removeAttribute("data-retry");
  }
  current.querySelector(".mute-state").textContent = incoming.querySelector(".mute-state").textContent;
}

function mergeResults(fragment) {
  speakerResults.querySelector("[data-load-error]")?.remove();
  const rooms = speakerResults.querySelector(".rooms");
  const freshRooms = fragment.querySelector(".rooms");
  if (!rooms || !freshRooms) {
    speakerResults.replaceChildren(...fragment.childNodes);
    return;
  }
  const freshCards = [...freshRooms.querySelectorAll("[data-speaker-id]")];
  for (const current of rooms.querySelectorAll("[data-speaker-id]")) {
    if (!freshCards.some(card => card.dataset.speakerId === current.dataset.speakerId)) current.remove();
  }
  for (const fresh of freshCards) {
    const current = [...rooms.querySelectorAll("[data-speaker-id]")].find(card => card.dataset.speakerId === fresh.dataset.speakerId);
    if (current) updateCard(current, fresh);
    else rooms.append(fresh);
  }
  const meta = speakerResults.querySelector("[data-result-meta]");
  meta?.replaceWith(fragment.querySelector("[data-result-meta]"));
}

async function performRequest(url, body, requestRevision, volumeId, showLoading = false) {
  requestRunning = true;
  controller = new AbortController();
  const progress = loadingCard.querySelector("[data-progress]");
  const button = discoveryForm.querySelector("[data-refresh-action]");
  button.disabled = showLoading || commandPending;
  if (showLoading) {
    loadingCard.hidden = false;
    discoveryForm.setAttribute("aria-busy", "true");
    progress.textContent = discoveryForm.dataset.busyLabel;
  }
  let success = false;
  try {
    const response = await fetch(url, {
      method: "POST", body, credentials: "same-origin", signal: controller.signal,
      headers: { "X-Requested-With": "fetch" }
    });
    if (!response.ok) throw new Error("Speaker request failed");
    const documentResult = new DOMParser().parseFromString(await response.text(), "text/html");
    const meta = documentResult.querySelector("[data-result-meta]");
    if (!meta) throw new Error("Speaker response missing");
    if (requestRevision !== revision) return false;
    if (meta.dataset.operationSucceeded !== "true") throw new Error("Speaker operation failed");
    if (volumeId) dirtyVolumes.delete(volumeId);
    mergeResults(documentResult.body);
    hasCatalog = meta.dataset.hasDiscovered === "true";
    success = true;
  } catch (error) {
    if (requestRevision === revision && error.name !== "AbortError") {
      showError(discoveryForm.dataset.errorLabel);
      if (volumeId) {
        const card = [...speakerResults.querySelectorAll("[data-speaker-id]")].find(item => item.dataset.speakerId === volumeId);
        card?.querySelector("[data-volume-form]").setAttribute("data-retry", "true");
      }
    }
  } finally {
    requestRunning = false;
    button.disabled = false;
    discoveryForm.removeAttribute("aria-busy");
    button.hidden = false;
    if (showLoading) {
      loadingCard.hidden = true;
      progress.textContent = "";
    }
  }
  return success;
}

function refreshSpeakers() {
  clearTimeout(refreshTimer);
  if (!discoveryForm || requestRunning || commandPending || document.hidden) return;
  // Cache expiry requires the explicit rediscovery action, never repeated discovery scans.
  if (!hasCatalog && initialDiscoveryAttempted) return;
  initialDiscoveryAttempted = true;
  const url = hasCatalog ? discoveryForm.dataset.refreshUrl : discoveryForm.action;
  activeRequest = performRequest(url, new FormData(discoveryForm), revision, undefined, !hasCatalog).finally(scheduleRefresh);
}

async function submitOperation(form, submitter, kind) {
  if (commandPending) return;
  clearTimeout(refreshTimer);
  commandPending = true;
  revision += 1;
  const body = new FormData(form);
  if (submitter?.name) body.set(submitter.name, submitter.value);
  if (kind) body.set("Kind", kind);
  const volumeId = body.get("Kind") === "SetVolume" ? cardId(form) : undefined;
  const focused = document.activeElement;
  setCommandBusy(true);
  try {
    await activeRequest;
    if (form === discoveryForm) dirtyVolumes.clear();
    activeRequest = performRequest(form.action, body, revision, volumeId, form === discoveryForm);
    await activeRequest;
  } finally {
    commandPending = false;
    setCommandBusy(false);
    if (focused?.isConnected && !focused.disabled && focused !== discoveryForm.querySelector("button")) focused.focus({ preventScroll: true });
    scheduleRefresh();
  }
}

discoveryForm?.addEventListener("submit", event => {
  event.preventDefault();
  submitOperation(discoveryForm, event.submitter);
});

speakerResults?.addEventListener("submit", event => {
  const form = event.target.closest("[data-command-form]");
  if (!form) return;
  event.preventDefault();
  submitOperation(form, event.submitter);
});

speakerResults?.addEventListener("input", event => {
  if (!event.target.matches("input[type=range]")) return;
  dirtyVolumes.add(cardId(event.target));
  event.target.closest("[data-volume-form]").querySelector("[data-volume-value]").textContent = `${event.target.value}%`;
});
speakerResults?.addEventListener("pointerdown", event => {
  if (event.target.matches("input[type=range]")) {
    event.target.dataset.dragStart = event.target.value;
    dirtyVolumes.add(cardId(event.target));
  }
});
document.addEventListener("pointerup", () => {
  for (const range of speakerResults?.querySelectorAll("input[data-drag-start]") ?? []) {
    if (range.value === range.dataset.dragStart && !range.form.hasAttribute("data-retry")) dirtyVolumes.delete(cardId(range));
    delete range.dataset.dragStart;
  }
});
speakerResults?.addEventListener("change", event => {
  if (event.target.matches("input[type=range]")) submitOperation(event.target.form, null, "SetVolume");
});

document.addEventListener("visibilitychange", () => {
  clearTimeout(refreshTimer);
  if (!document.hidden) refreshSpeakers();
});
window.addEventListener("pagehide", () => { clearTimeout(refreshTimer); controller?.abort(); });
window.addEventListener("pageshow", event => { if (event.persisted) refreshSpeakers(); });
if (discoveryForm?.dataset.autoLoad === "true") refreshSpeakers();
else scheduleRefresh();
