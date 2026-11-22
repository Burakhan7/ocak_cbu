// One Google map per page. Click-to-pin only; no Places or Geocoder calls.
// The script URL uses v=weekly and loading=async, which does not need unsafe-eval.
// importLibrary("maps") must finish before new Map. Constructing the map from the
// bootstrap stub leaves a blank canvas whose projection saturates latitude at the pole.
(function () {
    if (window.uomGoogleMapsBooted) {
        return;
    }
    window.uomGoogleMapsBooted = true;

    var root = document.querySelector("[data-uom-map]");
    if (!root) {
        return;
    }

    var geo = window.uomMapViewport;
    var key = root.getAttribute("data-api-key");
    if (!key) {
        showNote("Google Haritalar anahtarı tanımlı değil. Harita gösterilemiyor.");
        return;
    }
    if (!geo) {
        showNote("Google Haritalar yüklenemedi.");
        return;
    }

    var manisa = geo.manisa;
    var defaultZoom = geo.defaultZoom;
    var mapReady = false;
    var loadTimer = 0;
    var map = null;
    var view = { lat: manisa.lat, lng: manisa.lng, zoom: defaultZoom };
    var cameraTrusted = false;
    var corrections = 0;

    function pageNonce() {
        var scripts = document.scripts;
        for (var i = 0; i < scripts.length; i++) {
            if (scripts[i].nonce) {
                return scripts[i].nonce;
            }
            var attr = scripts[i].getAttribute("nonce");
            if (attr) {
                return attr;
            }
        }
        return "";
    }

    function showNote(message) {
        window.clearTimeout(loadTimer);
        if (!root.isConnected) {
            return;
        }
        var note = document.createElement("div");
        note.className = "alert alert-warning uom-map-notice";
        note.setAttribute("role", "status");
        note.textContent = message;
        if (root.parentNode) {
            root.parentNode.replaceChild(note, root);
        }
    }

    function readNumber(value) {
        if (value === null || value === "") {
            return null;
        }
        var n = Number(value);
        return Number.isFinite(n) ? n : null;
    }

    function parseCoord(value) {
        if (value === null || value === undefined) {
            return null;
        }
        var text = String(value).trim().replace(",", ".");
        if (!text) {
            return null;
        }
        var n = Number(text);
        return Number.isFinite(n) ? n : null;
    }

    function formatCoord(value) {
        return value.toFixed(6).replace(".", ",");
    }

    function coordLabel() {
        return root.parentElement && root.parentElement.querySelector("[data-uom-coord]");
    }

    function writePicker(lat, lng) {
        var latEl = document.getElementById(root.getAttribute("data-lat-field") || "");
        var lngEl = document.getElementById(root.getAttribute("data-lng-field") || "");
        if (latEl) {
            latEl.value = formatCoord(lat);
        }
        if (lngEl) {
            lngEl.value = formatCoord(lng);
        }
        var label = coordLabel();
        if (label) {
            label.textContent = "Seçilen konum: " + formatCoord(lat) + ", " + formatCoord(lng);
        }
    }

    function clearUnusableFields() {
        if (root.getAttribute("data-mode") !== "picker") {
            return;
        }
        var latEl = document.getElementById(root.getAttribute("data-lat-field") || "");
        var lngEl = document.getElementById(root.getAttribute("data-lng-field") || "");
        if (!latEl || !lngEl) {
            return;
        }
        var lat = parseCoord(latEl.value);
        var lng = parseCoord(lngEl.value);
        if (lat === null && lng === null) {
            return;
        }
        if (!geo.isUsable(lat, lng)) {
            latEl.value = "";
            lngEl.value = "";
            var label = coordLabel();
            if (label) {
                label.textContent = "Henüz konum seçilmedi.";
            }
        }
    }

    function safeHref(href) {
        if (!href) {
            return false;
        }
        if (href.charAt(0) === "/" && href.charAt(1) !== "/") {
            return true;
        }
        try {
            var url = new URL(href, window.location.origin);
            return url.protocol === "https:" || url.protocol === "http:";
        } catch (e) {
            return false;
        }
    }

    function popup(pin) {
        var wrap = document.createElement("div");
        var title = document.createElement("strong");
        title.textContent = pin.title || "";
        wrap.appendChild(title);
        if (pin.line1) {
            var line1 = document.createElement("div");
            line1.textContent = pin.line1;
            wrap.appendChild(line1);
        }
        if (pin.line2) {
            var line2 = document.createElement("div");
            line2.textContent = pin.line2;
            wrap.appendChild(line2);
        }
        if (safeHref(pin.href)) {
            var link = document.createElement("a");
            link.href = pin.href;
            link.textContent = pin.linkLabel || "Detay";
            wrap.appendChild(link);
        }
        var directions = document.createElement("a");
        directions.href = "https://www.google.com/maps/dir/?api=1&destination=" + encodeURIComponent(pin.lat + "," + pin.lng);
        directions.target = "_blank";
        directions.rel = "noopener noreferrer";
        directions.textContent = "Yol tarifi al";
        wrap.appendChild(document.createElement("br"));
        wrap.appendChild(directions);
        return wrap;
    }

    function ensureHeight() {
        var rect = root.getBoundingClientRect();
        if (rect.height >= 80 && rect.width >= 80) {
            return;
        }
        var px = 360;
        if (root.classList.contains("uom-map--mini")) {
            px = 256;
        } else if (root.classList.contains("uom-map--tall")) {
            px = 400;
        }
        root.style.height = px + "px";
        root.style.minHeight = px + "px";
        if (rect.width < 80) {
            root.style.width = "100%";
            root.style.minWidth = "16rem";
        }
    }

    function viewport() {
        if (cameraTrusted && map && typeof map.getCenter === "function") {
            var c = map.getCenter();
            var z = typeof map.getZoom === "function" ? map.getZoom() : view.zoom;
            if (c && geo.isUsable(c.lat(), c.lng())) {
                return {
                    lat: c.lat(),
                    lng: c.lng(),
                    zoom: Number.isFinite(z) ? z : view.zoom
                };
            }
        }
        return { lat: view.lat, lng: view.lng, zoom: view.zoom };
    }

    function pointFromDom(domEvent) {
        if (!domEvent || !Number.isFinite(domEvent.clientX) || !Number.isFinite(domEvent.clientY)) {
            return null;
        }
        var rect = root.getBoundingClientRect();
        if (rect.width <= 1 || rect.height <= 1) {
            return null;
        }
        var cam = viewport();
        return geo.pointToLatLng(
            cam.lat,
            cam.lng,
            cam.zoom,
            rect.width,
            rect.height,
            domEvent.clientX - rect.left,
            domEvent.clientY - rect.top);
    }

    function readNative(latLng) {
        if (!latLng || typeof latLng.lat !== "function" || typeof latLng.lng !== "function") {
            return null;
        }
        var lat = latLng.lat();
        var lng = latLng.lng();
        if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
            return null;
        }
        return { lat: lat, lng: lng };
    }

    function choose(native, projected) {
        return geo.resolvePicker(native, projected);
    }

    window.gm_authFailure = function () {
        showNote("Google Haritalar yüklenemedi. Anahtar geçersiz veya kısıtlı olabilir.");
    };

    function startMap(mapsLib, markerLib) {
        if (mapReady || !root.isConnected) {
            return;
        }

        var MapCtor = (mapsLib && mapsLib.Map) || (window.google && google.maps && google.maps.Map);
        var MarkerCtor = (markerLib && markerLib.Marker) || (window.google && google.maps && google.maps.Marker);
        if (typeof MapCtor !== "function" || typeof MarkerCtor !== "function") {
            showNote("Google Haritalar yüklenemedi.");
            return;
        }

        try {
            ensureHeight();
            var lat = readNumber(root.getAttribute("data-lat"));
            var lng = readNumber(root.getAttribute("data-lng"));
            var hasCenter = lat !== null && lng !== null && geo.isUsable(lat, lng);
            if (hasCenter) {
                view = { lat: lat, lng: lng, zoom: 14 };
            }
            clearUnusableFields();

            var options = {
                center: { lat: view.lat, lng: view.lng },
                zoom: view.zoom,
                mapTypeControl: false,
                streetViewControl: false,
                fullscreenControl: false,
                clickableIcons: false,
                gestureHandling: "cooperative",
                tilt: 0,
                heading: 0
            };
            if (window.google && google.maps && google.maps.RenderingType && google.maps.RenderingType.RASTER) {
                options.renderingType = google.maps.RenderingType.RASTER;
            }

            map = new MapCtor(root, options);
            mapReady = true;
            window.clearTimeout(loadTimer);

            if (window.google && google.maps && google.maps.event) {
                google.maps.event.addListenerOnce(map, "idle", function () {
                    ensureHeight();
                    google.maps.event.trigger(map, "resize");
                });
            }
            if (typeof ResizeObserver === "function" && window.google && google.maps && google.maps.event) {
                var observer = new ResizeObserver(function () {
                    google.maps.event.trigger(map, "resize");
                });
                observer.observe(root);
            }

            map.addListener("idle", function () {
                var c = map.getCenter();
                var z = map.getZoom();
                if (!c || !geo.isUsable(c.lat(), c.lng()) || !Number.isFinite(z)) {
                    if (corrections < 1) {
                        corrections += 1;
                        map.setCenter({ lat: view.lat, lng: view.lng });
                        map.setZoom(view.zoom);
                    }
                    return;
                }
                if (!cameraTrusted) {
                    var far = Math.abs(c.lat() - view.lat) > 15 || Math.abs(geo.normalizeLng(c.lng() - view.lng)) > 15;
                    if (far) {
                        if (corrections < 1) {
                            corrections += 1;
                            map.setCenter({ lat: view.lat, lng: view.lng });
                            map.setZoom(view.zoom);
                        }
                        return;
                    }
                    cameraTrusted = true;
                }
                view.lat = c.lat();
                view.lng = c.lng();
                view.zoom = z;
                corrections = 0;
            });

            if (root.getAttribute("data-mode") === "picker") {
                var marker = null;
                var dragging = false;
                var lastPointer = null;

                function commit(position, pan) {
                    view.lat = position.lat;
                    view.lng = position.lng;
                    if (marker) {
                        marker.setPosition(position);
                    }
                    writePicker(position.lat, position.lng);
                    if (pan) {
                        map.panTo(position);
                    }
                }

                function place(position, persist) {
                    if (!marker) {
                        marker = new MarkerCtor({ map: map, position: position, draggable: true });
                        marker.addListener("dragstart", function () {
                            dragging = true;
                            lastPointer = null;
                        });
                        marker.addListener("drag", function (event) {
                            if (event && event.domEvent) {
                                lastPointer = event.domEvent;
                            }
                        });
                        marker.addListener("dragend", function (event) {
                            dragging = false;
                            var dom = (event && event.domEvent) || lastPointer;
                            var chosen = choose(readNative(event && event.latLng), dom ? pointFromDom(dom) : null);
                            commit(chosen, true);
                            lastPointer = null;
                        });
                    } else {
                        marker.setPosition(position);
                    }
                    if (persist) {
                        commit(position, true);
                    }
                }

                window.addEventListener("pointermove", function (event) {
                    if (!dragging) {
                        return;
                    }
                    lastPointer = event;
                    var projected = pointFromDom(event);
                    if (!projected) {
                        return;
                    }
                    var chosen = choose(null, projected);
                    writePicker(chosen.lat, chosen.lng);
                });

                if (hasCenter) {
                    place({ lat: view.lat, lng: view.lng }, false);
                }
                map.addListener("click", function (event) {
                    var chosen = choose(readNative(event && event.latLng), event && event.domEvent ? pointFromDom(event.domEvent) : null);
                    place(chosen, true);
                });
                return;
            }

            var pins = [];
            try {
                pins = JSON.parse(root.getAttribute("data-pins") || "[]");
            } catch (e) {
                pins = [];
            }

            var InfoWindowCtor = (mapsLib && mapsLib.InfoWindow) || google.maps.InfoWindow;
            var BoundsCtor = (mapsLib && mapsLib.LatLngBounds) || google.maps.LatLngBounds;
            if (typeof InfoWindowCtor !== "function" || typeof BoundsCtor !== "function") {
                return;
            }
            var info = new InfoWindowCtor();
            var bounds = new BoundsCtor();
            var placed = [];
            pins.forEach(function (pin) {
                if (typeof pin.lat !== "number" || typeof pin.lng !== "number" || !geo.isUsable(pin.lat, pin.lng)) {
                    return;
                }
                var position = { lat: pin.lat, lng: pin.lng };
                var pinMarker = new MarkerCtor({ map: map, position: position, title: pin.title || "" });
                pinMarker.addListener("click", function () {
                    info.setContent(popup(pin));
                    info.open(map, pinMarker);
                });
                bounds.extend(position);
                placed.push(position);
            });

            if (placed.length === 1) {
                view = { lat: placed[0].lat, lng: placed[0].lng, zoom: 14 };
                map.setCenter(placed[0]);
                map.setZoom(14);
            } else if (placed.length > 1) {
                map.fitBounds(bounds, 32);
            }
        } catch (e) {
            mapReady = false;
            showNote("Google Haritalar yüklenemedi.");
        }
    }

    window.uomGoogleMapsReady = function () {
        if (!window.google || !google.maps || typeof google.maps.importLibrary !== "function") {
            showNote("Google Haritalar yüklenemedi.");
            return;
        }
        Promise.all([
            google.maps.importLibrary("maps"),
            google.maps.importLibrary("marker").catch(function () { return {}; })
        ]).then(function (libs) {
            startMap(libs[0], libs[1]);
        }).catch(function () {
            showNote("Google Haritalar yüklenemedi.");
        });
    };

    loadTimer = window.setTimeout(function () {
        if (!mapReady) {
            showNote("Google Haritalar yüklenemedi.");
        }
    }, 12000);

    var script = document.createElement("script");
    script.async = true;
    script.src = "https://maps.googleapis.com/maps/api/js?key=" + encodeURIComponent(key)
        + "&v=weekly&loading=async&language=tr&region=TR&callback=uomGoogleMapsReady";
    var nonce = pageNonce();
    if (nonce) {
        script.nonce = nonce;
        script.setAttribute("nonce", nonce);
    }
    script.onerror = function () {
        showNote("Google Haritalar yüklenemedi.");
    };
    document.head.appendChild(script);
})();
