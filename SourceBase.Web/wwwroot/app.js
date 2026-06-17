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

window.goldPriceChart = null;

window.renderGoldPriceChart = function (elementId, seriesData) {
    var el = document.getElementById(elementId);
    if (!el) return;
    if (window.goldPriceChart) {
        window.goldPriceChart.destroy();
        window.goldPriceChart = null;
    }
    if (!seriesData || seriesData.length === 0) return;
    var options = {
        chart: { type: 'line', height: 300, toolbar: { show: false }, zoom: { enabled: false } },
        series: seriesData,
        xaxis: { type: 'datetime', labels: { datetimeUTC: false } },
        yaxis: { labels: { formatter: function (v) { return (v / 1000000).toFixed(2) + 'M'; } } },
        stroke: { curve: 'smooth', width: 2 },
        legend: { position: 'top' },
        tooltip: { x: { format: 'dd MMM HH:mm' }, y: { formatter: function (v) { return v.toLocaleString('vi-VN') + ' ₫'; } } },
        colors: ['#6366f1', '#818cf8', '#f59e0b', '#fbbf24', '#10b981', '#34d399', '#f43f5e', '#fb7185'],
    };
    window.goldPriceChart = new ApexCharts(el, options);
    window.goldPriceChart.render();
};
