console.log('barcode-scanner.js executing (copied to Web)');
var barcodeScanner = {
    _scanner: null,

    start: async function (elementId, dotNetRef) {
        // Ensure the html5-qrcode library is available (check both names)
        if (!window.Html5QrcodeScanner && !window.Html5Qrcode) {
            var script = document.createElement('script');
            script.src = 'https://unpkg.com/html5-qrcode@2.3.8/html5-qrcode.min.js';
            document.head.appendChild(script);
            await new Promise(function (resolve, reject) {
                script.onload = resolve;
                script.onerror = function (e) { reject(new Error('Failed to load html5-qrcode script')); };
            });
        }

        try {
            await dotNetRef.invokeMethodAsync('OnScannerDebug', 'Loaded html5-qrcode library');
        } catch (e) {
            console.error('Debug invoke failed:', e);
        }

        var readerEl = document.getElementById(elementId);
        if (!readerEl) {
            try {
                await dotNetRef.invokeMethodAsync('OnScannerDebug', 'Missing barcode reader element');
            } catch (e) { }
            return;
        }

        readerEl.innerHTML = "";

        try {
            await dotNetRef.invokeMethodAsync('OnScannerDebug', 'Creating Html5QrcodeScanner');
        } catch (e) { }

        // Prefer direct Html5Qrcode for camera rendering, since Html5QrcodeScanner may not attach video properly in this host.
        var scanner;
        if (window.Html5Qrcode) {
            console.log('Creating Html5Qrcode on', elementId);
            var html5Qr = new Html5Qrcode(elementId);
            var scanErrorCount = 0;
            var config = {
                fps: 15,
                qrbox: { width: 320, height: 200 },
                videoConstraints: {
                    width: { ideal: 1280 },
                    height: { ideal: 720 },
                    facingMode: 'environment'
                },
                formatsToSupport: [
                    Html5QrcodeSupportedFormats.QR_CODE,
                    Html5QrcodeSupportedFormats.EAN_13,
                    Html5QrcodeSupportedFormats.EAN_8,
                    Html5QrcodeSupportedFormats.CODE_39,
                    Html5QrcodeSupportedFormats.CODE_128,
                    Html5QrcodeSupportedFormats.UPC_A,
                    Html5QrcodeSupportedFormats.UPC_E,
                    Html5QrcodeSupportedFormats.ITF
                ]
            };
            scanner = {
                _impl: html5Qr,
                render: function (success, error) {
                    html5Qr.start(
                        { facingMode: 'environment' },
                        config,
                        function (decoded) { success(decoded); },
                        function (err) { error(err); }
                    ).catch(function (e) { console.error('Html5Qrcode.start failed', e); });
                },
                clear: function () { return html5Qr.stop(); }
            };
        } else if (window.Html5QrcodeScanner) {
            scanner = new Html5QrcodeScanner(
                elementId,
                { fps: 10, qrbox: { width: 250, height: 150 } },
                false
            );
        }

        scanner.render(
            async function (decodedText) {
                await scanner.clear();
                barcodeScanner._scanner = null;
                try {
                    await dotNetRef.invokeMethodAsync('OnBarcodeFound', decodedText);
                } catch (e) {
                    console.error('Failed to invoke .NET:', e);
                }
            },
            async function (errorMessage) {
                scanErrorCount = (scanErrorCount || 0) + 1;
                if (!errorMessage?.includes('QR code parse error')) {
                    try {
                        await dotNetRef.invokeMethodAsync('OnScannerDebug', 'Scan error: ' + errorMessage);
                    } catch (e) { }
                } else if (scanErrorCount % 200 === 0) {
                    console.log('ignored parse error', scanErrorCount, errorMessage);
                }
            }
        );

        this._scanner = scanner;
        try {
            await dotNetRef.invokeMethodAsync('OnScannerDebug', 'Scanner render complete');
        } catch (e) { }
    },

    stop: async function () {
        try {
            if (this._scanner) {
                await this._scanner.clear();
                this._scanner = null;
            }
        } catch (e) { }

        try {
            var el = document.getElementById('barcode-reader');
            if (el) el.innerHTML = '';
        } catch (e) { }
    }
};
