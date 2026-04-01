// MDI pencere buton kontrolleri - WebView2 uzerinden native C# ile iletisim
// Surukleme InputNonClientPointerSource ile native olarak yapiliyor (sifir gecikme)
// Bu dosya sadece buton tiklamalarini C# tarafina iletir
(function () {
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('.window-ctrl-btn');
        if (!btn) return;

        if (window.chrome && window.chrome.webview) {
            if (btn.classList.contains('minimize')) {
                window.chrome.webview.postMessage('MINIMIZE');
            } else if (btn.classList.contains('maximize')) {
                window.chrome.webview.postMessage('MAXIMIZE_TOGGLE');
            } else if (btn.classList.contains('close')) {
                window.chrome.webview.postMessage('CLOSE');
            }
        }
        e.preventDefault();
        e.stopPropagation();
    });
})();
