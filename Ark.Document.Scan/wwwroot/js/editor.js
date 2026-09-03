document.addEventListener('DOMContentLoaded', function () {
    var dataEl = document.getElementById('editor-data');
    var data = JSON.parse(dataEl.textContent);

    var svg = document.getElementById('corner-overlay');
    var polygon = document.getElementById('corner-polygon');
    var processedPreview = document.getElementById('processed-preview');
    var statusEl = document.getElementById('processing-status');

    var originalCorners = data.corners.map(function (p) { return { x: p.x, y: p.y }; });
    var corners = originalCorners.map(function (p) { return { x: p.x, y: p.y }; });
    var rotationDegrees = data.rotationDegrees;
    var filterMode = data.filterMode;
    var brightness = data.brightness;
    var contrast = data.contrast;

    var handleRadius = Math.max(data.imageWidth, data.imageHeight) * 0.015;
    var handles = [];

    function createHandle(index) {
        var circle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
        circle.setAttribute('r', handleRadius);
        circle.setAttribute('fill', '#0d6efd');
        circle.setAttribute('stroke', '#ffffff');
        circle.setAttribute('stroke-width', handleRadius * 0.25);
        circle.style.cursor = 'grab';
        circle.style.touchAction = 'none';
        svg.appendChild(circle);
        return circle;
    }

    for (var i = 0; i < 4; i++) {
        handles.push(createHandle(i));
    }

    function render() {
        handles.forEach(function (circle, i) {
            circle.setAttribute('cx', corners[i].x);
            circle.setAttribute('cy', corners[i].y);
        });
        polygon.setAttribute('points', corners.map(function (p) { return p.x + ',' + p.y; }).join(' '));
    }

    render();

    function clientToSvgPoint(clientX, clientY) {
        var pt = svg.createSVGPoint();
        pt.x = clientX;
        pt.y = clientY;
        var ctm = svg.getScreenCTM();
        return pt.matrixTransform(ctm.inverse());
    }

    var draggingIndex = null;

    handles.forEach(function (circle, index) {
        circle.addEventListener('pointerdown', function (e) {
            draggingIndex = index;
            circle.setPointerCapture(e.pointerId);
            circle.style.cursor = 'grabbing';
        });
    });

    svg.addEventListener('pointermove', function (e) {
        if (draggingIndex === null) return;
        var p = clientToSvgPoint(e.clientX, e.clientY);
        var x = Math.min(Math.max(p.x, 0), data.imageWidth);
        var y = Math.min(Math.max(p.y, 0), data.imageHeight);
        corners[draggingIndex] = { x: x, y: y };
        render();
    });

    function endDrag() {
        if (draggingIndex === null) return;
        draggingIndex = null;
        handles.forEach(function (c) { c.style.cursor = 'grab'; });
        reprocess();
    }

    svg.addEventListener('pointerup', endDrag);
    svg.addEventListener('pointercancel', endDrag);

    document.getElementById('reset-corners').addEventListener('click', function () {
        corners = originalCorners.map(function (p) { return { x: p.x, y: p.y }; });
        render();
        reprocess();
    });

    document.getElementById('rotate-button').addEventListener('click', function () {
        rotationDegrees = (rotationDegrees + 90) % 360;
        reprocess();
    });

    var filterSelect = document.getElementById('filter-select');
    filterSelect.value = filterMode;
    filterSelect.addEventListener('change', function (e) {
        filterMode = e.target.value;
        reprocess();
    });

    var brightnessRange = document.getElementById('brightness-range');
    var contrastRange = document.getElementById('contrast-range');
    var brightnessValue = document.getElementById('brightness-value');
    var contrastValue = document.getElementById('contrast-value');
    brightnessRange.value = brightness;
    contrastRange.value = contrast;
    brightnessValue.textContent = brightness;
    contrastValue.textContent = contrast;

    var debounceTimer = null;
    function debounceReprocess() {
        if (debounceTimer) clearTimeout(debounceTimer);
        debounceTimer = setTimeout(reprocess, 350);
    }

    brightnessRange.addEventListener('input', function (e) {
        brightness = Number(e.target.value);
        brightnessValue.textContent = brightness;
        debounceReprocess();
    });

    contrastRange.addEventListener('input', function (e) {
        contrast = Number(e.target.value);
        contrastValue.textContent = contrast;
        debounceReprocess();
    });

    var reprocessInFlight = false;
    var reprocessQueued = false;

    function reprocess() {
        if (reprocessInFlight) {
            reprocessQueued = true;
            return;
        }
        reprocessInFlight = true;
        statusEl.textContent = 'Processing…';

        fetch(data.reprocessUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                corners: corners,
                rotationDegrees: rotationDegrees,
                filterMode: filterMode,
                brightness: brightness,
                contrast: contrast
            })
        })
            .then(function (response) {
                if (!response.ok) {
                    return response.json().then(function (err) { throw new Error(err.error || 'Processing failed.'); });
                }
                return response.json();
            })
            .then(function (result) {
                processedPreview.src = result.processedImageUrl;
                statusEl.textContent = '';
            })
            .catch(function (err) {
                statusEl.textContent = err.message;
            })
            .finally(function () {
                reprocessInFlight = false;
                if (reprocessQueued) {
                    reprocessQueued = false;
                    reprocess();
                }
            });
    }

    document.getElementById('share-button').addEventListener('click', function () {
        var button = this;
        var resultEl = document.getElementById('share-result');
        button.disabled = true;
        button.textContent = 'Generating…';

        fetch(data.shareUrl, { method: 'POST' })
            .then(function (response) { return response.json(); })
            .then(function (result) {
                resultEl.innerHTML = '<a href="' + result.shareUrl + '" target="_blank" rel="noopener">' + result.shareUrl + '</a>';
                window.open(result.shareUrl, '_blank');
            })
            .catch(function () {
                resultEl.textContent = 'Could not generate a share link. Please try again.';
            })
            .finally(function () {
                button.disabled = false;
                button.textContent = 'Generate share link';
            });
    });
});
