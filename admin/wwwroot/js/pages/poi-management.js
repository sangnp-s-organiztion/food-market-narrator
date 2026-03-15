(() => {
  if (document.body.dataset.page !== "poi-management") {
    return;
  }

  const STORAGE_KEY = "admin_pois_v1";

  const defaults = [
    {
      id: "#POI-001",
      name: "Oc Dao Vinh Khanh",
      address: "212 Vinh Khanh Street",
      category: "Restaurant",
      status: "Active",
      lastUpdated: "Oct 24, 2023",
    },
    {
      id: "#POI-002",
      name: "Street BBQ 123",
      address: "123 Vinh Khanh Street",
      category: "Stall",
      status: "Pending",
      lastUpdated: "Oct 25, 2023",
    },
    {
      id: "#POI-003",
      name: "Vinh Khanh Gateway",
      address: "Street Entrance",
      category: "Landmark",
      status: "Active",
      lastUpdated: "Sep 12, 2023",
    },
    {
      id: "#POI-004",
      name: "Banh Xeo Que",
      address: "45 Vinh Khanh Street",
      category: "Stall",
      status: "Flagged",
      lastUpdated: "Oct 26, 2023",
    },
  ];

  const tableBody = document.getElementById("poiTableBody");
  const searchInput = document.getElementById("poiSearch");
  const openModalButton = document.getElementById("openPoiModal");

  const poiModalBackdrop = document.getElementById("poiModalBackdrop");
  const closePoiModalButton = document.getElementById("closePoiModal");
  const cancelPoiModalButton = document.getElementById("cancelPoiModal");
  const poiModalTitle = document.getElementById("poiModalTitle");
  const savePoiButton = document.getElementById("savePoiButton");

  const deleteModalBackdrop = document.getElementById("deleteModalBackdrop");
  const deleteModalMessage = document.getElementById("deleteModalMessage");
  const cancelDeletePoiButton = document.getElementById("cancelDeletePoi");
  const confirmDeletePoiButton = document.getElementById("confirmDeletePoi");

  const poiForm = document.getElementById("poiForm");
  const poiIdField = document.getElementById("poiId");
  const poiNameField = document.getElementById("poiName");
  const poiAddressField = document.getElementById("poiAddress");
  const poiCategoryField = document.getElementById("poiCategory");
  const poiStatusField = document.getElementById("poiStatus");

  const activePoiCount = document.getElementById("activePoiCount");
  const pendingPoiCount = document.getElementById("pendingPoiCount");
  const flaggedPoiCount = document.getElementById("flaggedPoiCount");

  let pendingDeletePoiId = "";
  let pois = loadPois();

  renderTable("");
  updateCounters();

  if (searchInput) {
    searchInput.addEventListener("input", (event) => {
      renderTable(event.target.value.trim().toLowerCase());
    });
  }

  if (openModalButton) {
    openModalButton.addEventListener("click", () => {
      openPoiModal();
    });
  }

  if (closePoiModalButton) {
    closePoiModalButton.addEventListener("click", closePoiModal);
  }
  if (cancelPoiModalButton) {
    cancelPoiModalButton.addEventListener("click", closePoiModal);
  }

  if (cancelDeletePoiButton) {
    cancelDeletePoiButton.addEventListener("click", closeDeleteModal);
  }

  if (confirmDeletePoiButton) {
    confirmDeletePoiButton.addEventListener("click", () => {
      if (!pendingDeletePoiId) return;

      pois = pois.filter((poi) => poi.id !== pendingDeletePoiId);
      pendingDeletePoiId = "";
      persistPois();
      closeDeleteModal();
      renderTable(searchInput?.value.trim().toLowerCase() ?? "");
      updateCounters();
    });
  }

  tableBody?.addEventListener("click", (event) => {
    const target = event.target;
    if (!(target instanceof HTMLElement)) return;

    const action = target.dataset.action;
    const poiId = target.dataset.id;
    if (!action || !poiId) return;

    if (action === "edit") {
      const poi = pois.find((item) => item.id === poiId);
      if (!poi) return;
      openPoiModal(poi);
      return;
    }

    if (action === "delete") {
      const poi = pois.find((item) => item.id === poiId);
      if (!poi) return;
      pendingDeletePoiId = poiId;
      if (deleteModalMessage) {
        deleteModalMessage.textContent = `Are you sure you want to delete ${poi.name}?`;
      }
      openDeleteModal();
    }
  });

  if (poiForm) {
    poiForm.addEventListener("submit", (event) => {
      event.preventDefault();

      clearFormErrors();
      const validation = validateForm();
      if (!validation.isValid) {
        return;
      }

      const existingId = poiIdField?.value;
      const now = formatDate(new Date());
      const payload = {
        id: existingId || buildNextId(),
        name: poiNameField.value.trim(),
        address: poiAddressField.value.trim(),
        category: poiCategoryField.value,
        status: poiStatusField.value,
        lastUpdated: now,
      };

      if (existingId) {
        pois = pois.map((poi) => (poi.id === existingId ? payload : poi));
      } else {
        pois.push(payload);
      }

      persistPois();
      closePoiModal();
      renderTable(searchInput?.value.trim().toLowerCase() ?? "");
      updateCounters();
    });
  }

  function loadPois() {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(defaults));
      return [...defaults];
    }

    try {
      const parsed = JSON.parse(raw);
      if (!Array.isArray(parsed)) return [...defaults];
      return parsed;
    } catch {
      return [...defaults];
    }
  }

  function persistPois() {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(pois));
  }

  function openPoiModal(poi) {
    if (poiModalBackdrop) {
      poiModalBackdrop.hidden = false;
    }

    if (poi) {
      poiModalTitle.textContent = "Edit POI";
      savePoiButton.textContent = "Update POI";
      poiIdField.value = poi.id;
      poiNameField.value = poi.name;
      poiAddressField.value = poi.address;
      poiCategoryField.value = poi.category;
      poiStatusField.value = poi.status;
    } else {
      poiModalTitle.textContent = "Add New POI";
      savePoiButton.textContent = "Save POI";
      poiForm.reset();
      poiIdField.value = "";
    }

    clearFormErrors();
    poiNameField.focus();
  }

  function closePoiModal() {
    if (poiModalBackdrop) {
      poiModalBackdrop.hidden = true;
    }
    clearFormErrors();
  }

  function openDeleteModal() {
    if (deleteModalBackdrop) {
      deleteModalBackdrop.hidden = false;
    }
  }

  function closeDeleteModal() {
    if (deleteModalBackdrop) {
      deleteModalBackdrop.hidden = true;
    }
  }

  function renderTable(searchTerm) {
    if (!tableBody) return;

    const filtered = pois.filter((poi) => {
      const searchBody =
        `${poi.id} ${poi.name} ${poi.address} ${poi.category} ${poi.status}`.toLowerCase();
      return searchBody.includes(searchTerm);
    });

    if (filtered.length === 0) {
      tableBody.innerHTML =
        '<tr><td colspan="6" class="empty-state">No POI found.</td></tr>';
      return;
    }

    tableBody.innerHTML = filtered
      .map((poi) => {
        return `
          <tr>
            <td>${poi.id}</td>
            <td><strong>${escapeHtml(poi.name)}</strong><small>${escapeHtml(poi.address)}</small></td>
            <td><span class="badge ${badgeClass(poi.category)}">${escapeHtml(poi.category)}</span></td>
            <td><span class="status ${statusClass(poi.status)}">${escapeHtml(poi.status)}</span></td>
            <td>${escapeHtml(poi.lastUpdated)}</td>
            <td class="actions">
              <button class="action-btn edit" data-action="edit" data-id="${poi.id}" type="button">Edit</button>
              <button class="action-btn delete" data-action="delete" data-id="${poi.id}" type="button">Delete</button>
            </td>
          </tr>
        `;
      })
      .join("");
  }

  function updateCounters() {
    const activeCount = pois.filter((poi) => poi.status === "Active").length;
    const pendingCount = pois.filter((poi) => poi.status === "Pending").length;
    const flaggedCount = pois.filter((poi) => poi.status === "Flagged").length;

    activePoiCount.textContent = String(activeCount);
    pendingPoiCount.textContent = String(pendingCount);
    flaggedPoiCount.textContent = String(flaggedCount);
  }

  function validateForm() {
    let isValid = true;

    if (!poiNameField.value.trim()) {
      setFieldError("poiName", "POI name is required.");
      isValid = false;
    }

    if (!poiAddressField.value.trim()) {
      setFieldError("poiAddress", "Address is required.");
      isValid = false;
    }

    if (!poiCategoryField.value) {
      setFieldError("poiCategory", "Please choose a category.");
      isValid = false;
    }

    if (!poiStatusField.value) {
      setFieldError("poiStatus", "Please choose a status.");
      isValid = false;
    }

    return { isValid };
  }

  function clearFormErrors() {
    const errorLabels = document.querySelectorAll(".field-error");
    errorLabels.forEach((label) => {
      label.textContent = "";
    });
  }

  function setFieldError(fieldName, message) {
    const errorLabel = document.querySelector(
      `[data-error-for="${fieldName}"]`,
    );
    if (!errorLabel) return;
    errorLabel.textContent = message;
  }

  function buildNextId() {
    const maxId = pois.reduce((max, poi) => {
      const numeric = Number.parseInt(poi.id.replace("#POI-", ""), 10);
      if (Number.isNaN(numeric)) return max;
      return Math.max(max, numeric);
    }, 0);

    const next = String(maxId + 1).padStart(3, "0");
    return `#POI-${next}`;
  }

  function formatDate(date) {
    return date.toLocaleDateString("en-US", {
      month: "short",
      day: "2-digit",
      year: "numeric",
    });
  }

  function badgeClass(category) {
    if (category === "Stall") return "stall";
    if (category === "Landmark") return "landmark";
    return "";
  }

  function statusClass(status) {
    if (status === "Active") return "active";
    if (status === "Pending") return "pending";
    if (status === "Flagged") return "flagged";
    return "";
  }

  function escapeHtml(text) {
    const value = String(text);
    return value
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#39;");
  }
})();
