(function () {
    'use strict';

    var useNative = 'BarcodeDetector' in window;
    var scanner = null;
    var scanTimer = null;
    var videoStream = null;

    function loadHtml5Qr() {
        return new Promise(function (resolve, reject) {
            if (window.Html5Qrcode) { resolve(); return; }
            var script = document.createElement('script');
            script.src = '_content/StockScanTool.Shared/lib/html5-qrcode.min.js';
            script.onload = resolve;
            script.onerror = function () { reject(new Error('Failed to load html5-qrcode')); };
            document.head.appendChild(script);
        });
    }

    function getVideoElement(elementId) {
        var el = document.getElementById(elementId);
        if (!el) throw new Error('Element not found: ' + elementId);
        return el;
    }

    // ── Native BarcodeDetector API ──────────────────────────
    function startBarcodeDetector(elementId, dotNetRef, continuous) {
        var video = document.createElement('video');
        video.setAttribute('playsinline', '');
        video.setAttribute('autoplay', '');
        video.style.width = '100%';
        video.style.height = 'auto';

        var container = getVideoElement(elementId);
        container.innerHTML = '';
        container.appendChild(video);

        var canvas = document.createElement('canvas');
        var ctx = canvas.getContext('2d');
        var detector = new BarcodeDetector({
            formats: ['ean_13', 'ean_8', 'code_39', 'code_128', 'upc_a', 'upc_e', 'qr_code', 'itf', 'data_matrix']
        });
        var detected = false;

        return navigator.mediaDevices.getUserMedia({
            video: { facingMode: 'environment', width: { ideal: 1280 }, height: { ideal: 720 } }
        }).then(function (stream) {
            videoStream = stream;
            video.srcObject = stream;

            return new Promise(function (resolve) {
                video.onloadedmetadata = function () {
                    video.play();
                    canvas.width = video.videoWidth;
                    canvas.height = video.videoHeight;

                    scanTimer = setInterval(function () {
                        if (detected && !continuous) return;
                        ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
                        detector.detect(canvas).then(function (barcodes) {
                            if (barcodes.length > 0 && !detected) {
                                detected = true;
                                var code = barcodes[0].rawValue;
                                stopBarcodeDetector();
                                dotNetRef.invokeMethodAsync('OnBarcodeFound', code).catch(function () { });
                                if (continuous) {
                                    setTimeout(function () { startScanner(elementId, dotNetRef, true); }, 500);
                                }
                            }
                        }).catch(function () { });
                    }, 800);

                    scanner = { stop: stopBarcodeDetector };
                    resolve();
                };
            });
        });
    }

    function stopBarcodeDetector() {
        if (scanTimer) { clearInterval(scanTimer); scanTimer = null; }
        if (videoStream) { videoStream.getTracks().forEach(function (t) { t.stop(); }); videoStream = null; }
        scanner = null;
    }

    // ── html5-qrcode fallback ───────────────────────────────
    function startHtml5Qr(elementId, dotNetRef, continuous) {
        var container = getVideoElement(elementId);
        container.innerHTML = '';

        var html5Qr = new Html5Qrcode(elementId);
        var detected = false;
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

        return html5Qr.start(
            { facingMode: 'environment' },
            config,
            function (decodedText) {
                if (detected) return;
                detected = true;
                html5Qr.stop().catch(function () { });
                dotNetRef.invokeMethodAsync('OnBarcodeFound', decodedText).catch(function () { });
                if (continuous) {
                    setTimeout(function () { startScanner(elementId, dotNetRef, true); }, 500);
                }
            },
            function () { }
        ).then(function () {
            scanner = {
                clear: function () { return html5Qr.stop().catch(function () { }); }
            };
        });
    }

    function stopHtml5Qr() {
        if (scanner && scanner.clear) {
            scanner.clear().catch(function () { });
        }
        scanner = null;
    }

    // ── Unified API ────────────────────────────────────────
    function startScanner(elementId, dotNetRef, continuous) {
        if (useNative) {
            return startBarcodeDetector(elementId, dotNetRef, continuous);
        }
        return loadHtml5Qr().then(function () {
            return startHtml5Qr(elementId, dotNetRef, continuous);
        });
    }

    // ── Local storage helpers (used for session persistence) ──
    window.getStorageItem = function (key) {
        try {
            return window.localStorage.getItem(key);
        } catch (e) {
            return null;
        }
    };

    window.setStorageItem = function (key, value) {
        try {
            window.localStorage.setItem(key, value);
        } catch (e) {
            // storage unavailable (e.g. private mode) — session works in-memory
        }
    };

    window.removeStorageItem = function (key) {
        try {
            window.localStorage.removeItem(key);
        } catch (e) {
            // ignore
        }
    };

    window.stockScan = {
        startContinuous: function (elementId, dotNetRef) {
            return startScanner(elementId, dotNetRef, true);
        },

        scanOnce: function (elementId, dotNetRef) {
            return startScanner(elementId, dotNetRef, false);
        },

        stop: function () {
            if (useNative) {
                stopBarcodeDetector();
            } else {
                stopHtml5Qr();
            }
            var els = ['barcode-reader', 'admin-barcode-reader'];
            els.forEach(function (id) {
                var el = document.getElementById(id);
                if (el) el.innerHTML = '';
            });
        }
    };
})();
