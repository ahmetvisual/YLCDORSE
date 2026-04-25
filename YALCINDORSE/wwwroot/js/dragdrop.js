// Drag-and-drop dataTransfer guvencesi.
// Bazi MAUI/WebView2 senaryolarinda dataTransfer.setData cagrilmadiginda
// dragstart sonrasi drag iptal olabiliyor. Capture-phase global handler ile
// "draggable" elementlerin dataTransfer'ini her zaman initialize ediyoruz.
(function () {
    if (window.__yldragInit) return;
    window.__yldragInit = true;

    document.addEventListener('dragstart', function (e) {
        try {
            const t = e.target;
            if (!t || !e.dataTransfer) return;

            const isDraggable =
                t.draggable === true ||
                (t.getAttribute && t.getAttribute('draggable') === 'true') ||
                (t.closest && t.closest('[draggable="true"]'));

            if (isDraggable) {
                e.dataTransfer.effectAllowed = 'move';
                if (!e.dataTransfer.types || e.dataTransfer.types.length === 0) {
                    e.dataTransfer.setData('text/plain', 'yl-drag');
                }
            }
        } catch { /* yutkun: drag yine de calismaya devam etsin */ }
    }, true); // capture phase
})();
