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
        return;
    }

    var manisa = { lat: 38.6191, lng: 27.4289 };

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
        var note = document.createElement("p");
        note.className = "uom-map-notice text-muted small";
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

    window.gm_authFailure = function () {
        showNote("Google Haritalar yüklenemedi. Anahtar geçersiz veya kısıtlı olabilir.");
    };

    window.uomGoogleMapsReady = function () {
        if (!window.google || !google.maps || !google.maps.Map) {
            showNote("Google Haritalar yüklenemedi.");
            return;
        }

        try {
            var lat = readNumber(root.getAttribute("data-lat"));
            var lng = readNumber(root.getAttribute("data-lng"));
            var hasCenter = lat !== null && lng !== null;
            var map = new google.maps.Map(root, {
                center: hasCenter ? { lat: lat, lng: lng } : manisa,
                zoom: hasCenter ? 14 : 8,
                mapTypeControl: false,
                streetViewControl: false,
                fullscreenControl: false,
                clickableIcons: false,
                gestureHandling: "cooperative"
            });

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
            if (!pins.length) {
                return;
            }

            var info = new google.maps.InfoWindow();
            var bounds = new google.maps.LatLngBounds();
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
            });

            if (pins.length === 1) {
                map.setCenter({ lat: pins[0].lat, lng: pins[0].lng });
                map.setZoom(14);
            } else {
                map.fitBounds(bounds, 32);
            }
        } catch (e) {
            showNote("Google Haritalar yüklenemedi.");
        }
    };

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
