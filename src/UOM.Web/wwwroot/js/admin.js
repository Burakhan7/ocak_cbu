(function () {
    document.addEventListener('click', function (event) {
        var target = event.target;
        if (!target || !target.closest) {
            return;
        }

        var button = target.closest('[data-confirm]');
        if (!button) {
            return;
        }

        var message = button.getAttribute('data-confirm');
        if (message && !window.confirm(message)) {
            event.preventDefault();
        }
    });
})();
