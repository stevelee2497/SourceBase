window.hideAppLoading = function () {
  var el = document.getElementById('app-loading');
  if (!el) return;
  el.style.transition = 'opacity 0.25s ease';
  el.style.opacity = '0';
  setTimeout(function () {
    el.remove();
  }, 260);
};

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

window.getBrowserTimeZone = function () {
  var tz = localStorage.getItem('userTimeZone');
  if (tz) return tz;
  tz = Intl.DateTimeFormat().resolvedOptions().timeZone;
  localStorage.setItem('userTimeZone', tz);
  return tz;
};

window.renderGoldPriceChart = function (elementId, seriesData, colors, dashArray) {
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
    yaxis: {
      labels: {
        formatter: function (v) {
          return (v / 1000000).toFixed(2) + 'M';
        },
      },
    },
    stroke: { curve: 'smooth', width: 2, dashArray: dashArray || [] },
    legend: { position: 'top' },
    tooltip: {
      x: { format: 'dd MMM HH:mm' },
      y: {
        formatter: function (v) {
          return v.toLocaleString('vi-VN') + ' ₫';
        },
      },
    },
    colors: colors && colors.length > 0 ? colors : ['#6366f1', '#818cf8', '#f59e0b', '#fbbf24', '#10b981', '#34d399', '#f43f5e', '#fb7185', '#a855f7', '#c084fc'],
  };
  window.goldPriceChart = new ApexCharts(el, options);
  window.goldPriceChart.render();
};
