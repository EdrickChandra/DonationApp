
(function () {
    window.toggleCardMenu = function (id, e) {
        e.preventDefault();
        e.stopPropagation();
        document.querySelectorAll('.card-menu-dropdown').forEach(function (el) {
            if (el.id !== id) el.classList.remove('open');
        });
        document.getElementById(id).classList.toggle('open');
    };

    document.addEventListener('click', function () {
        document.querySelectorAll('.card-menu-dropdown').forEach(function (el) {
            el.classList.remove('open');
        });
    });

    window.copyLink = function (path) {
        navigator.clipboard.writeText(window.location.origin + path);
        document.querySelectorAll('.card-menu-dropdown').forEach(function (el) { el.classList.remove('open'); });
    };
})();
