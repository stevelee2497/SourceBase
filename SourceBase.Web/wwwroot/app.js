window.setCursorToEnd = function (el) {
    if (!el) return;
    requestAnimationFrame(function () {
        var len = el.value.length;
        el.setSelectionRange(len, len);
    });
};
