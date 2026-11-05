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
const speakerResults = document.querySelector("[data-speaker-results]");
let discoveryPending = false;

async function loadSpeakers(event) {
  event?.preventDefault();
  if (discoveryPending) return;
  discoveryPending = true;
  const button = discoveryForm.querySelector("button");
  const progress = discoveryForm.querySelector("[data-progress]");
  button.disabled = true;
  discoveryForm.setAttribute("aria-busy", "true");
  speakerResults.setAttribute("aria-busy", "true");
  progress.textContent = button.dataset.busyLabel;
  try {
    const response = await fetch(discoveryForm.action, {
      method: "POST",
      body: new FormData(discoveryForm),
      credentials: "same-origin"
    });
    if (!response.ok) throw new Error("Speaker request failed");
    const documentResult = new DOMParser().parseFromString(await response.text(), "text/html");
    const results = documentResult.querySelector("[data-speaker-results]");
    if (!results) throw new Error("Speaker response missing");
    speakerResults.replaceChildren(...results.childNodes);
  } catch {
    speakerResults.querySelector("[data-load-error]")?.remove();
    const error = document.createElement("p");
    error.dataset.loadError = "true";
    error.setAttribute("role", "alert");
    error.textContent = discoveryForm.dataset.errorLabel;
    speakerResults.prepend(error);
  } finally {
    discoveryPending = false;
    button.disabled = false;
    discoveryForm.removeAttribute("aria-busy");
    speakerResults.removeAttribute("aria-busy");
    progress.textContent = "";
  }
}

discoveryForm?.addEventListener("submit", loadSpeakers);
if (discoveryForm?.dataset.autoLoad === "true") loadSpeakers();
