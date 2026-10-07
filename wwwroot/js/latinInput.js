// ====================================================
//  فیلتر ورودی نام کاربری — فقط حروف و اعداد انگلیسی
//  هر عنصری که ویژگی data-latin-only داشته باشد،
//  کاراکتر غیرمجاز در آن تایپ نمی‌شود (پاک می‌شود).
// ====================================================
(function () {
    var allowed = /[^A-Za-z0-9._\-@]/g;

    document.addEventListener("input", function (e) {
        var el = e.target;
        if (el && el.hasAttribute && el.hasAttribute("data-latin-only")) {
            var cleaned = el.value.replace(allowed, "");
            if (cleaned !== el.value) {
                el.value = cleaned;
            }
        }
    }, true);
})();