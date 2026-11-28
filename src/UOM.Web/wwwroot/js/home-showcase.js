(function () {
    var roots = document.querySelectorAll("[data-home-showcase]");
    if (!roots.length) return;

    var motionQuery = window.matchMedia("(prefers-reduced-motion: reduce)");

    roots.forEach(function (root) {
        if (root.dataset.showcaseReady === "true") return;
        var track = root.querySelector("[data-home-showcase-track]");
        if (!track) return;
        var slides = Array.prototype.slice.call(track.children);
        if (slides.length < 2) return;

        root.dataset.showcaseReady = "true";
        var looping = track.hasAttribute("data-loop") && slides.length >= 3;
        var prevBtn = root.querySelector('[data-direction="previous"]');
        var nextBtn = root.querySelector('[data-direction="next"]');
        var status = root.querySelector("[data-home-showcase-status]");
        var markerNav = root.querySelector("[data-home-showcase-markers]");
        var markerButtons = [];
        var pendingIndex = null;
        var markerIndex = -1;
        var announcedIndex = 0;
        var userActivated = false;
        var booting = true;
        var compensating = false;
        var settling = false;
        var trip = false;
        var heldDirection = null;
        var requestedLeft = 0;
        var startLeft = 0;
        var markerFrame = 0;

        if (looping && window.CSS && CSS.supports && !CSS.supports("order: sibling-count()")) {
            track.style.setProperty("--children-count", String(slides.length));
        }

        function motionBehavior() {
            return motionQuery.matches ? "auto" : "smooth";
        }

        function deltaToCenter(slide) {
            var trackRect = track.getBoundingClientRect();
            var slideRect = slide.getBoundingClientRect();
            return (slideRect.left + slideRect.width / 2) - (trackRect.left + trackRect.width / 2);
        }

        function nearestIndex() {
            var best = 0;
            var bestAbs = Infinity;
            for (var i = 0; i < slides.length; i++) {
                var distance = Math.abs(deltaToCenter(slides[i]));
                if (distance < bestAbs) {
                    bestAbs = distance;
                    best = i;
                }
            }
            return best;
        }

        function visualIndexes() {
            var items = slides.map(function (slide, index) {
                return { index: index, x: slide.getBoundingClientRect().left };
            });
            items.sort(function (a, b) { return a.x - b.x; });
            return items.map(function (item) { return item.index; });
        }

        function setMarker(index) {
            if (index === markerIndex) return;
            markerIndex = index;
            for (var i = 0; i < markerButtons.length; i++) {
                if (i === index) markerButtons[i].setAttribute("aria-current", "true");
                else markerButtons[i].removeAttribute("aria-current");
            }
        }

        function announce(index) {
            if (!status || !userActivated || index === announcedIndex) return;
            announcedIndex = index;
            status.textContent = "Slayt " + (index + 1) + " / " + slides.length;
        }

        function setDisabled(button, disabled, other) {
            if (!button) return;
            var hadFocus = document.activeElement === button;
            button.disabled = disabled;
            if (disabled && hadFocus && other && !other.disabled) other.focus();
        }

        function updateDisabled(index) {
            if (!prevBtn || !nextBtn) return;
            if (looping) {
                prevBtn.disabled = false;
                nextBtn.disabled = false;
                return;
            }
            setDisabled(prevBtn, index <= 0, nextBtn);
            setDisabled(nextBtn, index >= slides.length - 1, prevBtn);
        }

        function shiftForParking(apply, lockOverflow) {
            var index = nearestIndex();
            var before = slides[index].getBoundingClientRect().left;
            var scrollBefore = track.scrollLeft;
            compensating = true;
            track.style.scrollBehavior = "auto";
            track.style.scrollSnapType = "none";
            if (lockOverflow) track.style.overflowX = "hidden";
            apply();
            var delta = slides[index].getBoundingClientRect().left - before;
            track.scrollLeft = scrollBefore + delta;
            track.style.scrollSnapType = "";
            var fix = deltaToCenter(slides[index]);
            if (Math.abs(fix) > 2) track.scrollLeft += fix;
            window.requestAnimationFrame(function () {
                window.requestAnimationFrame(function () {
                    track.style.overflowX = "";
                    track.style.scrollBehavior = "";
                    compensating = false;
                });
            });
        }

        function centerSlide(index) {
            var delta = deltaToCenter(slides[index]);
            if (Math.abs(delta) <= 2) return;
            compensating = true;
            track.style.scrollBehavior = "auto";
            track.style.scrollSnapType = "none";
            track.scrollLeft += delta;
            track.style.scrollSnapType = "";
            window.requestAnimationFrame(function () {
                window.requestAnimationFrame(function () {
                    track.style.scrollBehavior = "";
                    compensating = false;
                });
            });
        }

        function onSettle() {
            if (booting || compensating || settling) return;
            if (pendingIndex !== null && Math.abs(requestedLeft - startLeft) > 2 && Math.abs(track.scrollLeft - startLeft) < 2) {
                return;
            }

            settling = true;
            var index = pendingIndex !== null ? pendingIndex : nearestIndex();
            var replay = trip ? heldDirection : null;
            var skipLock = replay !== null;
            trip = false;
            heldDirection = null;
            if (looping) {
                shiftForParking(function () {
                    track.style.setProperty("--current", String(index));
                }, !skipLock);
                centerSlide(index);
            }
            setMarker(index);
            updateDisabled(index);
            announce(index);
            pendingIndex = null;
            settling = false;
            if (replay) step(replay);
        }

        function scrollToSlide(index, fromMarker) {
            userActivated = true;
            var current = pendingIndex === null ? nearestIndex() : pendingIndex;
            pendingIndex = index;
            setMarker(index);
            if (fromMarker && looping && index !== current) {
                trip = true;
                shiftForParking(function () {
                    track.style.setProperty("--current", "initial");
                }, false);
            }
            var delta = deltaToCenter(slides[index]);
            startLeft = track.scrollLeft;
            requestedLeft = startLeft + delta;
            if (Math.abs(delta) < 1) {
                pendingIndex = null;
                onSettle();
                return;
            }
            track.scrollBy({ left: delta, behavior: motionBehavior() });
        }

        function step(direction) {
            var order = visualIndexes();
            var current = pendingIndex === null ? nearestIndex() : pendingIndex;
            var pos = order.indexOf(current);
            if (pos < 0) pos = 0;
            var nextPos = direction === "next" ? pos + 1 : pos - 1;
            if (nextPos < 0 || nextPos >= order.length) return;
            scrollToSlide(order[nextPos], false);
        }

        function onControl(direction) {
            userActivated = true;
            if (trip) {
                heldDirection = direction;
                return;
            }
            step(direction);
        }

        if (markerNav) {
            slides.forEach(function (slide, index) {
                var button = document.createElement("button");
                button.type = "button";
                button.setAttribute("aria-label", slide.getAttribute("data-marker-name") || ("Slayt " + (index + 1)));
                button.addEventListener("click", function () {
                    userActivated = true;
                    scrollToSlide(index, true);
                });
                markerNav.appendChild(button);
                markerButtons.push(button);
            });
        }

        if (prevBtn) prevBtn.addEventListener("click", function () { onControl("previous"); });
        if (nextBtn) nextBtn.addEventListener("click", function () { onControl("next"); });

        track.addEventListener("pointerdown", function () { userActivated = true; });
        track.addEventListener("wheel", function () { userActivated = true; }, { passive: true });
        track.addEventListener("keydown", function () { userActivated = true; });

        track.addEventListener("scroll", function () {
            if (pendingIndex !== null || markerFrame) return;
            markerFrame = window.requestAnimationFrame(function () {
                markerFrame = 0;
                if (pendingIndex !== null) return;
                setMarker(nearestIndex());
            });
        }, { passive: true });

        if ("onscrollend" in window) {
            track.addEventListener("scrollend", onSettle);
        } else {
            var timer = 0;
            track.addEventListener("scroll", function () {
                window.clearTimeout(timer);
                timer = window.setTimeout(onSettle, 140);
            }, { passive: true });
        }

        if (looping) {
            shiftForParking(function () {
                track.style.setProperty("--current", "0");
            }, true);
        }
        setMarker(0);
        updateDisabled(0);
        booting = false;
    });
})();
