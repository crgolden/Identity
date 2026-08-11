(function () {
    var token = document.getElementById("recaptcha-token");
    if (!token) {
        return;
    }

    var form = token.closest("form");
    if (!form) {
        return;
    }

    form.addEventListener("submit", function (event) {
        if (event.submitter && event.submitter.hasAttribute("formnovalidate")) {
            return;
        }

        event.preventDefault();
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
