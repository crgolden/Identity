document.addEventListener("click", function (event) {
    const collapseToggle = event.target.closest("[data-bs-toggle='collapse']");
    if (collapseToggle) {
        const target = document.querySelector(collapseToggle.dataset.bsTarget);
        if (target) {
            target.classList.toggle("show");
        }

        return;
    }

    const alertDismiss = event.target.closest("[data-bs-dismiss='alert']");
    if (alertDismiss) {
        const alert = alertDismiss.closest(".alert");
        if (alert) {
            alert.remove();
        }
    }
});
