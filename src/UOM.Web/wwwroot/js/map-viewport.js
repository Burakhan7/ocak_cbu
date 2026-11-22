// Web Mercator for the map picker. Keep in sync with MapViewportMath.
// Screen Y grows south and is scaled by 256 * 2^zoom. Omitting that scale,
// or flipping Y, saturates latitude near ±90 so a drag only changes longitude.
(function (root) {
    var TILE = 256;
    var MERCATOR_LIMIT = 85.0511287798066;
    var MANISA = { lat: 38.6191, lng: 27.4289 };

    function normalizeLng(longitude) {
        if (!Number.isFinite(longitude)) {
            return 0;
        }
        var wrapped = (longitude + 180) % 360;
        if (wrapped < 0) {
            wrapped += 360;
        }
        return wrapped - 180;
    }

    function isUsable(lat, lng) {
        if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
            return false;
        }
        if (Math.abs(lat) >= MERCATOR_LIMIT) {
            return false;
        }
        var wrapped = normalizeLng(lng);
        return wrapped >= -180 && wrapped <= 180;
    }

    function project(lat, lng) {
        var siny = Math.sin(lat * Math.PI / 180);
        siny = Math.min(0.9999, Math.max(-0.9999, siny));
        return {
            x: TILE * (0.5 + lng / 360),
            y: TILE * (0.5 - Math.log((1 + siny) / (1 - siny)) / (4 * Math.PI))
        };
    }

    function pointToLatLng(centerLat, centerLng, zoom, width, height, pixelX, pixelY) {
        if (!isUsable(centerLat, centerLng) || width <= 1 || height <= 1 || !Number.isFinite(pixelX) || !Number.isFinite(pixelY)) {
            return isUsable(centerLat, centerLng)
                ? { lat: centerLat, lng: normalizeLng(centerLng) }
                : { lat: MANISA.lat, lng: MANISA.lng };
        }

        var scale = Math.pow(2, zoom);
        if (!Number.isFinite(scale) || scale <= 0) {
            scale = 1;
        }

        var center = project(centerLat, centerLng);
        var x = (center.x * scale + (pixelX - width / 2)) / scale;
        var y = (center.y * scale + (pixelY - height / 2)) / scale;
        var lng = normalizeLng(x / TILE * 360 - 180);
        var n = Math.PI - 2 * Math.PI * y / TILE;
        var lat = Math.atan(Math.sinh(n)) * 180 / Math.PI;
        if (!Number.isFinite(lat)) {
            lat = centerLat;
        }

        var limit = MERCATOR_LIMIT - 1e-6;
        if (lat > limit) {
            lat = limit;
        }
        if (lat < -limit) {
            lat = -limit;
        }
        return { lat: lat, lng: lng };
    }

    function resolvePicker(native, projected) {
        if (native && projected && isUsable(native.lat, native.lng) && isUsable(projected.lat, projected.lng)) {
            var dLng = normalizeLng(native.lng - projected.lng);
            if (Math.abs(native.lat - projected.lat) <= 1 && Math.abs(dLng) <= 1) {
                return { lat: native.lat, lng: normalizeLng(native.lng) };
            }
        }
        if (projected && isUsable(projected.lat, projected.lng)) {
            return projected;
        }
        if (native && isUsable(native.lat, native.lng)) {
            return { lat: native.lat, lng: normalizeLng(native.lng) };
        }
        return { lat: MANISA.lat, lng: MANISA.lng };
    }

    root.uomMapViewport = {
        manisa: MANISA,
        defaultZoom: 8,
        isUsable: isUsable,
        normalizeLng: normalizeLng,
        pointToLatLng: pointToLatLng,
        resolvePicker: resolvePicker
    };
})(typeof globalThis !== "undefined" ? globalThis : this);
