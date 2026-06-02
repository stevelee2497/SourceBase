window.setCursorToEnd = function (el) {
    if (!el) return;
    requestAnimationFrame(function () {
        var len = el.value.length;
        el.setSelectionRange(len, len);
    });
};

window.focusElement = function (el) {
    if (el) el.focus();
};

window.initOtpPaste = function (container, dotNetRef) {
    if (!container) return;
    container.addEventListener('paste', function (e) {
        var text = e.clipboardData ? e.clipboardData.getData('text') : '';
        var digits = text.replace(/\D/g, '').slice(0, 6);
        if (digits.length > 0) {
            e.preventDefault();
            dotNetRef.invokeMethodAsync('OnPaste', digits);
        }
    });
};
