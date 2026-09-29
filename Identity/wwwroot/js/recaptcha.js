(function () {
    const token = document.getElementById("recaptcha-token");
    if (!token) {
        return;
    }

    const form = token.closest("form");
    if (!form) {
        return;
    }

    function passesClientValidation() {
        const jQuery = window.jQuery;
        if (!jQuery?.(form).data("validator")) {
            return true;
        }

        return jQuery(form).valid();
    }

    form.addEventListener("submit", function (event) {
        if (event.submitter?.hasAttribute("formnovalidate")) {
            return;
        }

        event.preventDefault();
        if (!passesClientValidation()) {
            return;
        }

        grecaptcha.ready(function () {
            grecaptcha
                .execute(token.dataset.recaptchaSiteKey, { action: token.dataset.recaptchaAction })
                .then(function (value) {
                    token.value = value;
                    form.submit();
                });
        });
    });
})();
