// ====================================================
//  فیلتر ورودی: جلوگیری از تایپ حروف غیرمجاز و رقم فارسی
//
//  data-latin-only     → نام کاربری: حروف، عدد، نقطه، خط تیره، زیرخط، @
//  data-latin-password → رمز عبور: همهٔ کاراکترهای قابل‌چاپ انگلیسی (بدون فاصله)
//  data-digits-only    → فقط عدد صحیح: تعداد، کد ملی، کد پستی، ترتیب
//  data-number-only    → عدد با جداکننده یا اعشار: مبلغ، درصد، شمارهٔ سند
// ====================================================
(function () {
    var allowedUser = /[^A-Za-z0-9._\-@]/g;
    var allowedPassword = /[^\x21-\x7E]/g;
    var allowedDigits = /[^0-9]/g;
    var allowedNumber = /[^0-9.,٫]/g;

    var persianDigits = /[\u06F0-\u06F9]/g;
    var arabicDigits = /[\u0660-\u0669]/g;

    // رقم فارسی و عربی به رقم لاتین تبدیل می‌شود تا خواندن و نوشتن یکسان باشد
    function toLatinDigits(text) {
        return text
            .replace(persianDigits, function (c) { return String.fromCharCode(c.charCodeAt(0) - 0x06F0); })
            .replace(arabicDigits, function (c) { return String.fromCharCode(c.charCodeAt(0) - 0x0660); });
    }

    document.addEventListener("input", function (e) {
        var el = e.target;
        if (!el || !el.hasAttribute) { return; }

        var pattern = null;
        var number = false;

        if (el.hasAttribute("data-latin-only")) {
            pattern = allowedUser;
        } else if (el.hasAttribute("data-latin-password")) {
            pattern = allowedPassword;
        } else if (el.hasAttribute("data-digits-only")) {
            pattern = allowedDigits;
        } else if (el.hasAttribute("data-number-only")) {
            pattern = allowedNumber;
            number = true;
        }

        if (pattern === null) { return; }

        var cleaned = toLatinDigits(el.value).replace(pattern, "");

        if (number) {
            // فقط یک نقطهٔ اعشار می‌ماند
            var parts = cleaned.split(".");
            if (parts.length > 2) {
                cleaned = parts[0] + "." + parts.slice(1).join("");
            }
        }

        if (cleaned !== el.value) {
            el.value = cleaned;
        }
    }, true);
})();
