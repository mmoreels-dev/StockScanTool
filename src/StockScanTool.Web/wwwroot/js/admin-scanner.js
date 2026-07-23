window.adminScanner = {
    _scanner: null,

    scanOnce: async function (dotNetRef) {
        if (!window.Html5QrcodeScanner) {
            var script = document.createElement('script');
            script.src = 'https://unpkg.com/html5-qrcode@2.3.8/html5-qrcode.min.js';
            document.head.appendChild(script);
            await new Promise(function (resolve, reject) {
                script.onload = resolve;
                script.onerror = reject;
            });
        }

        var readerEl = document.getElementById("admin-barcode-reader");
        if (!readerEl) return;
        readerEl.innerHTML = "";

        var scanner = new Html5QrcodeScanner(
            "admin-barcode-reader",
            { fps: 10, qrbox: { width: 250, height: 150 } },
            false
        );

        scanner.render(
            async function (decodedText) {
                scanner.clear().catch(function () {});
                window.adminScanner._scanner = null;
                try {
                    await dotNetRef.invokeMethodAsync("OnBarcodeScanned", decodedText);
                } catch (e) {
                    console.error("Failed to invoke .NET:", e);
                }
            },
            function (errorMessage) {}
        );

        window.adminScanner._scanner = scanner;
    },

    stop: async function () {
        try {
            if (window.adminScanner._scanner) {
                await window.adminScanner._scanner.clear();
                window.adminScanner._scanner = null;
            }
        } catch (e) {}
        try {
            var el = document.getElementById("admin-barcode-reader");
            if (el) el.innerHTML = "";
        } catch (e) {}
    }
};
