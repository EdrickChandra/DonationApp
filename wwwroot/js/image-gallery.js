

(function () {
    var images = [];
    var currentIndex = 0;

    window.initImageGallery = function (imgs) {
        images = imgs || [];
        currentIndex = 0;
        var zoom = document.getElementById('zoomModal');
        if (zoom) {
            zoom.addEventListener('click', function (e) {
                if (e.target === this) window.closeZoom();
            });
        }
    };

    window.setImage = function (index) {
        currentIndex = index;
        var main = document.getElementById('mainImage');
        if (main) main.src = images[index];
        var counter = document.getElementById('imageCounter');
        if (counter) counter.textContent = (index + 1) + ' / ' + images.length;
        document.querySelectorAll('[id^="thumb-"]').forEach(function (el, i) {
            el.classList.toggle('active', i === index);
        });
    };

    window.slideImage = function (dir) {
        if (!images.length) return;
        window.setImage((currentIndex + dir + images.length) % images.length);
    };

    window.openZoom = function () {
        if (!images.length) return;
        var zi = document.getElementById('zoomImage');
        if (zi) zi.src = images[currentIndex];
        var zm = document.getElementById('zoomModal');
        if (zm) zm.classList.add('open');
    };

    window.closeZoom = function () {
        var zm = document.getElementById('zoomModal');
        if (zm) zm.classList.remove('open');
    };
})();
