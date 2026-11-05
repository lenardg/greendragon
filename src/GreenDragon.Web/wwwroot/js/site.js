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

document.querySelector("[data-discovery-form]")?.addEventListener("submit", (event) => {
  const form = event.currentTarget;
  const button = form.querySelector("button");
  button.disabled = true;
  form.setAttribute("aria-busy", "true");
  form.querySelector("[data-progress]").textContent = button.dataset.busyLabel;
});

window.addEventListener("pageshow", () => {
  const form = document.querySelector("[data-discovery-form]");
  if (!form) return;
  form.querySelector("button").disabled = false;
  form.removeAttribute("aria-busy");
  form.querySelector("[data-progress]").textContent = "";
});
