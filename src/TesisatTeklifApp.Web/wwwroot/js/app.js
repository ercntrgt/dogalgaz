// --- Basit (mobil) görünüm + menü çekmecesi (saf JS; layout etkileşimsiz olduğu için) ---
window.applySimpleMode = function () {
    document.body.classList.toggle('simple-mode', localStorage.getItem('simpleMode') === '1');
};
window.toggleSimpleMode = function () {
    const on = localStorage.getItem('simpleMode') !== '1';
    localStorage.setItem('simpleMode', on ? '1' : '0');
    document.body.classList.toggle('simple-mode', on);
};
window.toggleDrawer = function () { document.body.classList.toggle('drawer-open'); };
window.closeDrawer = function () { document.body.classList.remove('drawer-open'); };

// Tercihi sayfa açılır açılmaz uygula
window.applySimpleMode();
document.addEventListener('DOMContentLoaded', window.applySimpleMode);

// Blazor "enhanced navigation" sayfa değişince body sınıfını sunucu haline
// göre sıfırlıyor; tercih açıkken sınıfı otomatik geri ekle.
const _simpleObserver = new MutationObserver(function () {
    if (localStorage.getItem('simpleMode') === '1' && !document.body.classList.contains('simple-mode')) {
        document.body.classList.add('simple-mode');
    }
});
_simpleObserver.observe(document.body, { attributes: true, attributeFilter: ['class'] });

// Çekmecedeki bir menü bağlantısına tıklanınca çekmeceyi kapat
document.addEventListener('click', function (e) {
    if (e.target.closest('.app-drawer-panel a')) document.body.classList.remove('drawer-open');
});

// --- İmza pad (canvas üzerinde çizim, fare + dokunmatik) ---
window.signaturePad = {
    _state: {},
    init: function (canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;
        const ctx = canvas.getContext('2d');
        ctx.lineWidth = 2;
        ctx.lineCap = 'round';
        ctx.strokeStyle = '#16243f';
        let drawing = false, last = null;

        const pos = (e) => {
            const r = canvas.getBoundingClientRect();
            const p = e.touches ? e.touches[0] : e;
            return { x: (p.clientX - r.left) * (canvas.width / r.width),
                     y: (p.clientY - r.top) * (canvas.height / r.height) };
        };
        const start = (e) => { drawing = true; last = pos(e); e.preventDefault(); };
        const move = (e) => {
            if (!drawing) return;
            const p = pos(e);
            ctx.beginPath(); ctx.moveTo(last.x, last.y); ctx.lineTo(p.x, p.y); ctx.stroke();
            last = p; e.preventDefault();
        };
        const end = () => { drawing = false; };

        canvas.addEventListener('mousedown', start);
        canvas.addEventListener('mousemove', move);
        window.addEventListener('mouseup', end);
        canvas.addEventListener('touchstart', start, { passive: false });
        canvas.addEventListener('touchmove', move, { passive: false });
        canvas.addEventListener('touchend', end);
        this._state[canvasId] = { canvas, ctx };
    },
    clear: function (canvasId) {
        const s = this._state[canvasId];
        if (s) s.ctx.clearRect(0, 0, s.canvas.width, s.canvas.height);
    },
    getData: function (canvasId) {
        const s = this._state[canvasId];
        return s ? s.canvas.toDataURL('image/png') : null;
    },
    isEmpty: function (canvasId) {
        const s = this._state[canvasId];
        if (!s) return true;
        const blank = document.createElement('canvas');
        blank.width = s.canvas.width; blank.height = s.canvas.height;
        return s.canvas.toDataURL() === blank.toDataURL();
    }
};

// --- Toast bildirimi ---
window.showToast = function (message, type) {
    let host = document.getElementById('ds-toast-host');
    if (!host) {
        host = document.createElement('div');
        host.id = 'ds-toast-host';
        host.className = 'ds-toast-host';
        document.body.appendChild(host);
    }
    const icons = { success: 'bi-check-circle', danger: 'bi-exclamation-octagon', warning: 'bi-exclamation-triangle', info: 'bi-info-circle' };
    const t = document.createElement('div');
    t.className = 'ds-toast t-' + (type || 'info');
    t.innerHTML = '<i class="bi ' + (icons[type] || icons.info) + '"></i><span>' + message + '</span>';
    host.appendChild(t);
    setTimeout(() => { t.style.opacity = '0'; t.style.transition = 'opacity .3s'; setTimeout(() => t.remove(), 300); }, 3200);
};

// Base64 içeriği tarayıcıda dosya olarak indirir (rapor Excel çıktısı için).
window.downloadFile = (fileName, contentType, base64) => {
    const link = document.createElement('a');
    link.href = `data:${contentType};base64,${base64}`;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
