(function () {
    var article = document.querySelector(".reading-article");
    var track = document.querySelector(".reading-progress");
    var bar = document.querySelector(".reading-progress__bar");
    if (!article || !track || !bar) {
        return;
    }

    function update() {
        var rect = article.getBoundingClientRect();
        var articleTop = window.scrollY + rect.top;
        var articleHeight = article.offsetHeight;
        var view = window.innerHeight;
        if (articleHeight < view * 1.2) {
            track.classList.remove("is-active");
            track.setAttribute("aria-valuenow", "0");
            bar.style.width = "0";
            return;
        }

        track.classList.add("is-active");
        var end = articleTop + articleHeight - view;
        var raw = end <= articleTop ? 1 : (window.scrollY - articleTop) / (end - articleTop);
        var progress = Math.min(1, Math.max(0, raw));
        bar.style.width = (progress * 100) + "%";
        track.setAttribute("aria-valuenow", String(Math.round(progress * 100)));
    }

    update();
    window.addEventListener("scroll", update, { passive: true });
    window.addEventListener("resize", update);
})();
