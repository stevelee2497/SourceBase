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

// Scroll a row to the top of its scrollable container (by id; no page scroll).
window.scrollRowToTop = function (rowId, containerId) {
  var row = document.getElementById(rowId);
  var container = document.getElementById(containerId);
  if (!row || !container) return;
  container.scrollTop = row.offsetTop - container.offsetTop;
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

function loadApexCharts(callback) {
  if (window.ApexCharts) {
    callback();
    return;
  }
  var s = document.createElement('script');
  s.src = 'https://cdn.jsdelivr.net/npm/apexcharts';
  s.onload = callback;
  document.head.appendChild(s);
}

window.getTheme = function () {
  return localStorage.getItem('theme') === 'dark' ? 'dark' : 'light';
};

window.setTheme = function (theme) {
  var isDark = theme === 'dark';
  localStorage.setItem('theme', isDark ? 'dark' : 'light');
  document.documentElement.classList.toggle('dark', isDark);
};

window.getNavCollapsed = function () {
  return localStorage.getItem('navCollapsed') === 'true';
};

window.setNavCollapsed = function (collapsed) {
  localStorage.setItem('navCollapsed', collapsed ? 'true' : 'false');
};

window.getBrowserTimeZone = function () {
  var tz = localStorage.getItem('userTimeZone');
  if (tz) return tz;
  tz = Intl.DateTimeFormat().resolvedOptions().timeZone;
  localStorage.setItem('userTimeZone', tz);
  return tz;
};

window.renderGoldPriceChart = function (elementId, seriesData, colors, dashArray) {
  loadApexCharts(function () {
    var el = document.getElementById(elementId);
    if (!el) return;
    if (window.goldPriceChart) {
      window.goldPriceChart.destroy();
      window.goldPriceChart = null;
    }
    var oldLegend = el.parentElement && el.parentElement.querySelector('.gold-chart-legend');
    if (oldLegend) oldLegend.remove();
    if (!seriesData || seriesData.length === 0) return;

    // Derive unique sources and their colors from series names ("Source Buy" / "Source Sell")
    var sourceMap = {};
    seriesData.forEach(function (s, i) {
      var m = s.name.match(/^(.+) (Buy|Sell)$/);
      if (m && !sourceMap[m[1]]) sourceMap[m[1]] = colors[i] || '#6b7280';
    });
    var sources = Object.keys(sourceMap);

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
      legend: { show: false },
      tooltip: {
        shared: true,
        custom: function ({ series, seriesIndex, dataPointIndex, w }) {
          var ts = null;
          for (var i = 0; i < w.globals.seriesX.length; i++) {
            if (w.globals.seriesX[i] && w.globals.seriesX[i][dataPointIndex] != null) {
              ts = w.globals.seriesX[i][dataPointIndex];
              break;
            }
          }
          var timeStr = ts ? new Date(ts).toLocaleString('vi-VN', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' }) : '';
          var html = '<div style="padding:8px 12px;font-size:12px;">';
          if (timeStr) html += '<div style="margin-bottom:5px;font-weight:600;color:#374151;">' + timeStr + '</div>';
          sources.forEach(function (src) {
            var buyIdx = w.config.series.findIndex(function (s) {
              return s.name === src + ' Buy';
            });
            var sellIdx = w.config.series.findIndex(function (s) {
              return s.name === src + ' Sell';
            });
            var buyVal = buyIdx >= 0 ? series[buyIdx][dataPointIndex] : null;
            var sellVal = sellIdx >= 0 ? series[sellIdx][dataPointIndex] : null;
            if (buyVal == null && sellVal == null) return;
            var color = sourceMap[src];
            html += '<div style="display:flex;align-items:center;gap:8px;margin-bottom:3px;">';
            html += '<span style="width:8px;height:8px;border-radius:50%;background:' + color + ';flex-shrink:0;display:inline-block;"></span>';
            html += '<span style="font-weight:600;min-width:100px;">' + src + '</span>';
            if (buyVal != null) html += '<span>Buy: ' + buyVal.toLocaleString('vi-VN') + ' ₫</span>';
            if (sellVal != null) html += '<span>Sell: ' + sellVal.toLocaleString('vi-VN') + ' ₫</span>';
            html += '</div>';
          });
          html += '</div>';
          return html;
        },
      },
      colors: colors && colors.length > 0 ? colors : ['#6366f1', '#818cf8', '#f59e0b', '#fbbf24', '#10b981', '#34d399', '#f43f5e', '#fb7185', '#a855f7', '#c084fc'],
    };

    window.goldPriceChart = new ApexCharts(el, options);
    window.goldPriceChart.render().then(function () {
      var legendDiv = document.createElement('div');
      legendDiv.className = 'gold-chart-legend';
      legendDiv.style.cssText = 'display:flex;flex-wrap:wrap;gap:6px;padding-bottom:8px;justify-content:center;';
      var hidden = {};
      sources.forEach(function (src) {
        var item = document.createElement('div');
        item.style.cssText = 'display:inline-flex;align-items:center;gap:5px;cursor:pointer;font-size:12px;padding:3px 8px;border-radius:4px;transition:opacity .15s;';
        var dot = document.createElement('span');
        dot.style.cssText = 'width:8px;height:8px;border-radius:50%;background:' + sourceMap[src] + ';flex-shrink:0;';
        item.appendChild(dot);
        item.appendChild(document.createTextNode(src));
        item.addEventListener('click', function () {
          hidden[src] = !hidden[src];
          item.style.opacity = hidden[src] ? '0.4' : '1';
          window.goldPriceChart.toggleSeries(src + ' Buy');
          window.goldPriceChart.toggleSeries(src + ' Sell');
        });
        legendDiv.appendChild(item);
      });
      el.parentElement.insertBefore(legendDiv, el);
    });
  }); // loadApexCharts
};
