(function () {
    var bands = document.querySelectorAll('[data-quote-band]');
    if (!bands.length) return;

    var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    bands.forEach(function (band) {
        var items = band.querySelectorAll('[data-quote-item]');
        if (items.length < 2) return;

        var index = 0;
        items.forEach(function (el, i) {
            if (i === 0) {
                el.hidden = false;
                el.classList.add('is-active');
            } else {
                el.hidden = true;
                el.classList.remove('is-active');
            }
        });

        if (reduceMotion) return;

        window.setInterval(function () {
            items[index].hidden = true;
            items[index].classList.remove('is-active');
            index = (index + 1) % items.length;
            items[index].hidden = false;
            items[index].classList.add('is-active');
        }, 8000);
    });
})();