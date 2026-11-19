(function () {
    document.querySelectorAll('[data-youtube-lite]').forEach(function (root) {
        var videoId = root.getAttribute('data-video-id');
        var trigger = root.querySelector('.youtube-lite__trigger');
        if (!videoId || !trigger) return;

        trigger.addEventListener('click', function () {
            if (root.dataset.loaded === '1') return;
            root.dataset.loaded = '1';
            var iframe = document.createElement('iframe');
            iframe.width = '100%';
            iframe.height = '315';
            iframe.src = 'https://www.youtube-nocookie.com/embed/' + encodeURIComponent(videoId) + '?autoplay=1';
            iframe.title = trigger.getAttribute('aria-label') || 'YouTube videosu';
            iframe.loading = 'lazy';
            iframe.allow = 'accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture';
            iframe.allowFullscreen = true;
            iframe.className = 'youtube-lite__iframe';
            root.innerHTML = '';
            root.appendChild(iframe);
        });
    });
})();