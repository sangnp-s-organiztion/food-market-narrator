(() => {
  const toggles = document.querySelectorAll(".toggle-group .toggle");
  toggles.forEach((toggle) => {
    toggle.addEventListener("click", () => {
      toggles.forEach((item) => item.classList.remove("active"));
      toggle.classList.add("active");
    });
  });
})();
