// MAUI WebView2 icinde HTML5 drag-drop tutarsiz oldugundan, sol panel sira
// degistirme islemi pointer (mouse) event'leri ile yapiliyor. Sag panelden
// sol panele tasima icin HTML5 drag hala kullaniliyor; ona da dataTransfer
// guvencesi sagliyoruz.
(function () {
    'use strict';
    if (window.__yldragInit) return;
    window.__yldragInit = true;

    // === HTML5 drag (sag→sol icin) dataTransfer guvencesi ===
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
        } catch { }
    }, true);

    // === Pointer-based reorder (sol panel kalemler arasi) ===
    let dotnetRef = null;
    let isDragging = false;
    let srcId = null;
    let srcEl = null;
    let targetEl = null;
    let startX = 0, startY = 0;
    let dragStarted = false;
    const DRAG_THRESHOLD = 5; // px — kucuk hareketleri click sayma

    window.YLDragReorder = {
        setRef: function (ref) { dotnetRef = ref; },
        clearRef: function () { dotnetRef = null; }
    };

    function clearVisuals() {
        if (srcEl) srcEl.classList.remove('dragging');
        if (targetEl) targetEl.classList.remove('drop-target-row');
        document.body.style.userSelect = '';
        document.body.style.cursor = '';
    }

    function reset() {
        isDragging = false;
        dragStarted = false;
        srcId = null;
        srcEl = null;
        targetEl = null;
    }

    document.addEventListener('mousedown', function (e) {
        if (e.button !== 0) return;
        const grip = e.target.closest && e.target.closest('.item-drag-grip');
        if (!grip) return;
        const row = grip.closest('[data-item-id]');
        if (!row) return;

        // Drag baslat (henuz threshold'u gecmediyse "armed" durumunda)
        e.preventDefault();
        isDragging = true;
        dragStarted = false;
        srcId = parseInt(row.getAttribute('data-item-id'));
        srcEl = row;
        startX = e.clientX;
        startY = e.clientY;
    }, true);

    document.addEventListener('mousemove', function (e) {
        if (!isDragging) return;

        // Threshold kontrolu — kucuk titremeler click sayilsin
        if (!dragStarted) {
            const dx = e.clientX - startX;
            const dy = e.clientY - startY;
            if ((dx * dx + dy * dy) < (DRAG_THRESHOLD * DRAG_THRESHOLD)) return;
            dragStarted = true;
            if (srcEl) srcEl.classList.add('dragging');
            document.body.style.userSelect = 'none';
            document.body.style.cursor = 'grabbing';
        }

        // Hedef satir tespiti — elementFromPoint kursor altindaki elemani verir
        const el = document.elementFromPoint(e.clientX, e.clientY);
        const newTarget = el && el.closest ? el.closest('[data-item-id]') : null;

        if (targetEl && targetEl !== newTarget) {
            targetEl.classList.remove('drop-target-row');
            targetEl = null;
        }
        if (newTarget && newTarget !== srcEl && newTarget !== targetEl) {
            newTarget.classList.add('drop-target-row');
            targetEl = newTarget;
        }
    });

    document.addEventListener('mouseup', async function () {
        if (!isDragging) return;

        const finalSrcId = srcId;
        const finalTargetId = (dragStarted && targetEl)
            ? parseInt(targetEl.getAttribute('data-item-id'))
            : null;

        clearVisuals();
        reset();

        if (finalSrcId != null && finalTargetId != null && finalSrcId !== finalTargetId && dotnetRef) {
            try { await dotnetRef.invokeMethodAsync('OnPointerReorder', finalSrcId, finalTargetId); }
            catch { }
        }
    });

    // ESC ile drag iptal
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && isDragging) {
            clearVisuals();
            reset();
        }
    });
})();
