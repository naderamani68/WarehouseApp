// ====================================================
//  فیلتر ورودی: جلوگیری از تایپ حروف غیرانگلیسی
//
//  data-latin-only     → نام کاربری: حروف، عدد، نقطه، خط تیره، زیرخط، @
//  data-latin-password → رمز عبور: همهٔ کاراکترهای قابل‌چاپ انگلیسی (بدون فاصله)
// ====================================================
(function () {
    var allowedUser = /[^A-Za-z0-9._\-@]/g;
    var allowedPassword = /[^\x21-\x7E]/g;

    document.addEventListener("input", function (e) {
        var el = e.target;
        if (!el || !el.hasAttribute) { return; }

        var pattern = null;

        if (el.hasAttribute("data-latin-only")) {
            pattern = allowedUser;
        } else if (el.hasAttribute("data-latin-password")) {
            pattern = allowedPassword;
        }

        if (pattern === null) { return; }

        var cleaned = el.value.replace(pattern, "");
        if (cleaned !== el.value) {
            el.value = cleaned;
        }
    }, true);
})();