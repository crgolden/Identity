document.addEventListener("click", function (event) {
    var collapseToggle = event.target.closest("[data-bs-toggle='collapse']");
    if (collapseToggle) {
        var target = document.querySelector(collapseToggle.getAttribute("data-bs-target"));
        if (target) {
            target.classList.toggle("show");
        }

        return;
    }

    var alertDismiss = event.target.closest("[data-bs-dismiss='alert']");
    if (alertDismiss) {
        var alert = alertDismiss.closest(".alert");
        if (alert) {
            alert.remove();
        }
    }
});
