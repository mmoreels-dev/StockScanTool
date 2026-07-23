var barcodeScanner = {
    _scanner: null,

    start: async function (videoElementId, dotNetRef) {
        // Dynamically load html5-qrcode from CDN
        if (!window.Html5Qrcode) {
            var script = document.createElement('script');
            script.src = 'https://unpkg.com/html5-qrcode@2.3.8/html5-qrcode.min.js';
            document.head.appendChild(script);
            await new Promise(function (resolve) { script.onload = resolve; });
        }

        this._scanner = new Html5Qrcode(videoElementId);

        await this._scanner.start(
            { facingMode: "environment" },
            {
                fps: 10,
                qrbox: { width: 250, height: 150 },
                aspectRatio: 1.0
            },
            function (decodedText) {
                dotNetRef.invokeMethodAsync('OnBarcodeFound', decodedText);
            },
            function (errorMessage) { /* ignore scan errors */ }
        );
    },

    stop: async function () {
        if (this._scanner) {
            try {
                await this._scanner.stop();
                this._scanner.clear();
            } catch (e) { }
            this._scanner = null;
        }
    }
};
