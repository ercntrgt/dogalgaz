// Çoklu imza pad (canvas). id ile ayrı ayrı yönetilir. Onay linki + servis formunda kullanılır.
window.signaturePad = (function () {
    const pads = {};

    function attach(id) {
        const canvas = document.getElementById(id);
        if (!canvas) return null;
        const ratio = window.devicePixelRatio || 1;
        const w = canvas.clientWidth, h = canvas.clientHeight;
        canvas.width = w * ratio; canvas.height = h * ratio;
        const ctx = canvas.getContext('2d');
        ctx.scale(ratio, ratio);
        ctx.lineWidth = 2.2; ctx.lineCap = 'round'; ctx.strokeStyle = '#12233f';
        const pad = { canvas, ctx, drawing: false, dirty: false };

        function pos(e) {
            const r = canvas.getBoundingClientRect();
            const t = e.touches ? e.touches[0] : e;
            return { x: t.clientX - r.left, y: t.clientY - r.top };
        }
        canvas.addEventListener('mousedown', e => { pad.drawing = true; const p = pos(e); ctx.beginPath(); ctx.moveTo(p.x, p.y); e.preventDefault(); });
        canvas.addEventListener('mousemove', e => { if (!pad.drawing) return; const p = pos(e); ctx.lineTo(p.x, p.y); ctx.stroke(); pad.dirty = true; e.preventDefault(); });
        window.addEventListener('mouseup', () => pad.drawing = false);
        canvas.addEventListener('touchstart', e => { pad.drawing = true; const p = pos(e); ctx.beginPath(); ctx.moveTo(p.x, p.y); e.preventDefault(); }, { passive: false });
        canvas.addEventListener('touchmove', e => { if (!pad.drawing) return; const p = pos(e); ctx.lineTo(p.x, p.y); ctx.stroke(); pad.dirty = true; e.preventDefault(); }, { passive: false });
        canvas.addEventListener('touchend', () => pad.drawing = false);
        return pad;
    }

    return {
        init: function (id) { pads[id] = attach(id); },
        clear: function (id) { const p = pads[id]; if (p) { p.ctx.clearRect(0, 0, p.canvas.width, p.canvas.height); p.dirty = false; } },
        isEmpty: function (id) { const p = pads[id]; return !p || !p.dirty; },
        getData: function (id) { const p = pads[id]; return (p && p.dirty) ? p.canvas.toDataURL('image/png') : null; }
    };
})();
