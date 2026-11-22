// One Google map per page. Click-to-pin only; no Places or Geocoder calls.
// Loaded with v=weekly and loading=async, which does not need unsafe-eval.
(function () {
    if (window.uomGoogleMapsBooted) {
        return;
    }
    window.uomGoogleMapsBooted = true;

    var root = document.querySelector("[data-uom-map]");
    if (!root) {
        return;
    }

    var key = root.getAttribute("data-api-key");
    if (!key) {
        showNote("Google Haritalar anahtarı tanımlı değil. Harita gösterilemiyor.");
        return;
    }

    // Manisa, centered on western Turkey. Used before any pin exists.
    var manisa = { lat: 38.6191, lng: 27.4289 };
    var defaultZoom = 8;
    var mapReady = false;
    var loadTimer = 0;

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

    function formatCoord(value) {
        return value.toFixed(6).replace(".", ",");
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
        var label = root.parentElement && root.parentElement.querySelector("[data-uom-coord]");
        if (label) {
            label.textContent = "Seçilen konum: " + formatCoord(lat) + ", " + formatCoord(lng);
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
        if (root.getBoundingClientRect().height >= 80) {
            return;
        }
        if (root.classList.contains("uom-map--mini")) {
            root.style.height = "16rem";
        } else if (root.classList.contains("uom-map--tall")) {
            root.style.height = "25rem";
        } else {
            root.style.height = "22.5rem";
        }
    }

    function librariesReady() {
        return window.google && google.maps && typeof google.maps.Map === "function";
    }

    window.gm_authFailure = function () {
        showNote("Google Haritalar yüklenemedi. Anahtar geçersiz veya kısıtlı olabilir.");
    };

    function startMap() {
        if (mapReady || !root.isConnected || !librariesReady()) {
            if (!librariesReady()) {
                showNote("Google Haritalar yüklenemedi.");
            }
            return;
        }

        try {
            ensureHeight();
            var lat = readNumber(root.getAttribute("data-lat"));
            var lng = readNumber(root.getAttribute("data-lng"));
            var hasCenter = lat !== null && lng !== null;
            var map = new google.maps.Map(root, {
                center: hasCenter ? { lat: lat, lng: lng } : manisa,
                zoom: hasCenter ? 14 : defaultZoom,
                mapTypeControl: false,
                streetViewControl: false,
                fullscreenControl: false,
                clickableIcons: false,
                gestureHandling: "cooperative"
            });
            mapReady = true;
            window.clearTimeout(loadTimer);

            var redraw = function () {
                ensureHeight();
                if (google.maps.event) {
                    google.maps.event.trigger(map, "resize");
                }
            };
            if (window.requestAnimationFrame) {
                window.requestAnimationFrame(redraw);
            }
            if (google.maps.event) {
                google.maps.event.addListenerOnce(map, "idle", redraw);
            }

            var paint = function () {
                if (typeof google.maps.Marker !== "function") {
                    return;
                }
                if (root.getAttribute("data-mode") === "picker") {
                    var marker = null;
                    var place = function (position, persist) {
                        if (!marker) {
                            marker = new google.maps.Marker({ map: map, position: position, draggable: true });
                            marker.addListener("dragend", function () {
                                var pos = marker.getPosition();
                                if (pos) {
                                    writePicker(pos.lat(), pos.lng());
                                }
                            });
                        } else {
                            marker.setPosition(position);
                        }
                        if (persist) {
                            writePicker(position.lat, position.lng);
                        }
                    };
                    if (hasCenter) {
                        place({ lat: lat, lng: lng }, false);
                    }
                    map.addListener("click", function (event) {
                        if (!event.latLng) {
                            return;
                        }
                        place({ lat: event.latLng.lat(), lng: event.latLng.lng() }, true);
                    });
                    return;
                }

                var pins = [];
                try {
                    pins = JSON.parse(root.getAttribute("data-pins") || "[]");
                } catch (e) {
                    pins = [];
                }

                var info = new google.maps.InfoWindow();
                var bounds = new google.maps.LatLngBounds();
                var placed = [];
                pins.forEach(function (pin) {
                    if (typeof pin.lat !== "number" || typeof pin.lng !== "number") {
                        return;
                    }
                    var position = { lat: pin.lat, lng: pin.lng };
                    var pinMarker = new google.maps.Marker({ map: map, position: position, title: pin.title || "" });
                    pinMarker.addListener("click", function () {
                        info.setContent(popup(pin));
                        info.open(map, pinMarker);
                    });
                    bounds.extend(position);
                    placed.push(position);
                });

                if (placed.length === 1) {
                    map.setCenter(placed[0]);
                    map.setZoom(14);
                } else if (placed.length > 1) {
                    map.fitBounds(bounds, 32);
                }
            };

            if (typeof google.maps.Marker === "function" || typeof google.maps.importLibrary !== "function") {
                paint();
            } else {
                google.maps.importLibrary("marker").then(function (lib) {
                    if (lib && typeof lib.Marker === "function" && typeof google.maps.Marker !== "function") {
                        google.maps.Marker = lib.Marker;
                    }
                    paint();
                }).catch(function () {
                    paint();
                });
            }
        } catch (e) {
            mapReady = false;
            showNote("Google Haritalar yüklenemedi.");
        }
    }

    window.uomGoogleMapsReady = function () {
        if (librariesReady()) {
            startMap();
            return;
        }
        if (window.google && google.maps && typeof google.maps.importLibrary === "function") {
            google.maps.importLibrary("maps").then(function () {
                startMap();
            }).catch(function () {
                showNote("Google Haritalar yüklenemedi.");
            });
            return;
        }
        showNote("Google Haritalar yüklenemedi.");
    };

    loadTimer = window.setTimeout(function () {
        if (!mapReady) {
            showNote("Google Haritalar yüklenemedi.");
        }
    }, 12000);

    var script = document.createElement("script");
    script.async = true;
    script.src = "https://maps.googleapis.com/maps/api/js?key=" + encodeURIComponent(key) + "&v=weekly&loading=async&callback=uomGoogleMapsReady";
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
