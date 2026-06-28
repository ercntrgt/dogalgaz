// İmza pad (canvas, parmak/fare). Web ile aynı mantık.
window.signaturePad = {
    _s: {},
    init: function (id) {
        const canvas = document.getElementById(id);
        if (!canvas) return;
        const ctx = canvas.getContext('2d');
        ctx.lineWidth = 2.5; ctx.lineCap = 'round'; ctx.strokeStyle = '#16243f';
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
        this._s[id] = { canvas, ctx };
    },
    clear: function (id) { const s = this._s[id]; if (s) s.ctx.clearRect(0, 0, s.canvas.width, s.canvas.height); },
    getData: function (id) { const s = this._s[id]; return s ? s.canvas.toDataURL('image/png') : null; },
    isEmpty: function (id) {
        const s = this._s[id]; if (!s) return true;
        const b = document.createElement('canvas'); b.width = s.canvas.width; b.height = s.canvas.height;
        return s.canvas.toDataURL() === b.toDataURL();
    }
};
