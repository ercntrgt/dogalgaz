// Çoklu imza pad (canvas). id ile ayrı ayrı yönetilir. Onay linki + servis formunda kullanılır.
window.signaturePad = (function () {
    const pads = {};
    const MAX_RATIO = 2;    // yüksek DPI telefonlarda bitmap'i (ve dolayısıyla payload'ı) şişirmemek için
    const MAX_OUT_W = 600;  // dışa aktarımda en fazla genişlik (px)
    const TRIM_PAD = 6;     // kırpma payı (css px)

    function attach(id) {
        const canvas = document.getElementById(id);
        if (!canvas) return null;
        const ratio = Math.min(window.devicePixelRatio || 1, MAX_RATIO);
        const w = canvas.clientWidth, h = canvas.clientHeight;
        canvas.width = w * ratio; canvas.height = h * ratio;
        const ctx = canvas.getContext('2d');
        ctx.scale(ratio, ratio);
        ctx.lineWidth = 2.2; ctx.lineCap = 'round'; ctx.strokeStyle = '#12233f';
        const pad = {
            canvas, ctx, ratio, w, h, drawing: false, dirty: false,
            minX: Infinity, minY: Infinity, maxX: -Infinity, maxY: -Infinity
        };

        function pos(e) {
            const r = canvas.getBoundingClientRect();
            const t = e.touches ? e.touches[0] : e;
            return { x: t.clientX - r.left, y: t.clientY - r.top };
        }
        // Mürekkebin kapladığı alanı takip et (dışa aktarırken kırpmak için).
        function mark(p) {
            if (p.x < pad.minX) pad.minX = p.x;
            if (p.y < pad.minY) pad.minY = p.y;
            if (p.x > pad.maxX) pad.maxX = p.x;
            if (p.y > pad.maxY) pad.maxY = p.y;
        }
        function down(e) { pad.drawing = true; const p = pos(e); mark(p); ctx.beginPath(); ctx.moveTo(p.x, p.y); e.preventDefault(); }
        function move(e) { if (!pad.drawing) return; const p = pos(e); mark(p); ctx.lineTo(p.x, p.y); ctx.stroke(); pad.dirty = true; e.preventDefault(); }
        function up() { pad.drawing = false; }

        canvas.addEventListener('mousedown', down);
        canvas.addEventListener('mousemove', move);
        window.addEventListener('mouseup', up);
        canvas.addEventListener('touchstart', down, { passive: false });
        canvas.addEventListener('touchmove', move, { passive: false });
        canvas.addEventListener('touchend', up);
        return pad;
    }

    function reset(pad) {
        pad.dirty = false;
        pad.minX = Infinity; pad.minY = Infinity; pad.maxX = -Infinity; pad.maxY = -Infinity;
    }

    // İmzayı mürekkep alanına kırpıp en fazla MAX_OUT_W genişliğe küçültür.
    // Boş kenar boşlukları gittiği için hem payload küçülür hem PDF'te imza düzgün görünür.
    function exportTrimmed(pad) {
        const x0 = Math.max(0, pad.minX - TRIM_PAD), y0 = Math.max(0, pad.minY - TRIM_PAD);
        const x1 = Math.min(pad.w, pad.maxX + TRIM_PAD), y1 = Math.min(pad.h, pad.maxY + TRIM_PAD);
        const cw = Math.max(1, x1 - x0), ch = Math.max(1, y1 - y0);
        const scale = Math.min(1, MAX_OUT_W / cw);
        const out = document.createElement('canvas');
        out.width = Math.max(1, Math.round(cw * scale));
        out.height = Math.max(1, Math.round(ch * scale));
        out.getContext('2d').drawImage(
            pad.canvas,
            x0 * pad.ratio, y0 * pad.ratio, cw * pad.ratio, ch * pad.ratio,
            0, 0, out.width, out.height);
        return out.toDataURL('image/png');
    }

    return {
        // Idempotent: aynı canvas için tekrar çağrılırsa çizimi silmez, dinleyici yığmaz.
        init: function (id) {
            const p = pads[id];
            if (p && p.canvas === document.getElementById(id) && p.canvas.isConnected) return;
            pads[id] = attach(id);
        },
        clear: function (id) {
            const p = pads[id];
            if (!p) return;
            p.ctx.clearRect(0, 0, p.canvas.width, p.canvas.height);
            reset(p);
        },
        isEmpty: function (id) { const p = pads[id]; return !p || !p.dirty; },
        getData: function (id) { const p = pads[id]; return (p && p.dirty) ? exportTrimmed(p) : null; }
    };
})();
