// MDI pencere surukleme - WebView2 uzerinden native C# ile iletisim
(function () {
    document.addEventListener('mousedown', function (e) {
        // Sadece sol tik
        if (e.button !== 0) return;

        var titlebar = e.target.closest('.window-titlebar');
        if (!titlebar) return;

        // Butonlara tiklanmissa surukleme yapma
        if (e.target.closest('.window-titlebar-controls')) return;

        // Cift tik = maximize/restore
        if (e.detail >= 2) {
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage('MAXIMIZE_TOGGLE');
            }
            e.preventDefault();
            return;
        }

        // Tek tik = surukle
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage('DRAG_START');
        }
        e.preventDefault();
    });
})();
