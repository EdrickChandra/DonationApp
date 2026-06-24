

window.renderCategoryFields = function (containerId, idPrefix, val, existingDetails) {
    var container = document.getElementById(containerId);
    if (!container) return;
    container.innerHTML = '';
    var fields = (window.categoryFields || {})[val];
    if (!fields) return;
    existingDetails = existingDetails || {};
    fields.forEach(function (f) {
        var div = document.createElement('div');
        div.className = 'form-group';
        var label = document.createElement('label');
        label.className = 'form-label';
        label.textContent = f.label;
        div.appendChild(label);
        var input;
        if (f.type === 'select') {
            input = document.createElement('select');
            input.className = 'form-input';
            input.id = idPrefix + f.key;
            input.dataset.key = f.key;
            var empty = document.createElement('option');
            empty.value = '';
            empty.textContent = 'Pilih ' + f.label;
            input.appendChild(empty);
            f.options.forEach(function (o) {
                var opt = document.createElement('option');
                opt.value = o;
                opt.textContent = o;
                if (existingDetails[f.key] === o) opt.selected = true;
                input.appendChild(opt);
            });
        } else {
            input = document.createElement('input');
            input.className = 'form-input';
            input.type = 'text';
            input.placeholder = f.placeholder || '';
            input.id = idPrefix + f.key;
            input.dataset.key = f.key;
            if (existingDetails[f.key]) input.value = existingDetails[f.key];
        }
        div.appendChild(input);
        container.appendChild(div);
    });
};

window.initConditionButtons = function (inputId) {
    document.querySelectorAll('.btn-condition').forEach(function (btn) {
        btn.addEventListener('click', function () {
            document.querySelectorAll('.btn-condition').forEach(function (b) {
                b.classList.remove('selected');
            });
            this.classList.add('selected');
            var input = document.getElementById(inputId);
            if (input) input.value = this.dataset.value;
        });
    });
};

window.initSimpleImagePreview = function (inputId, containerId, max) {
    var input = document.getElementById(inputId);
    if (!input) return;
    max = max || 5;
    input.addEventListener('change', function () {
        var container = document.getElementById(containerId);
        if (!container) return;
        container.innerHTML = '';
        Array.from(this.files).slice(0, max).forEach(function (file) {
            var reader = new FileReader();
            reader.onload = function (e) {
                var div = document.createElement('div');
                div.className = 'simg-tile';
                var img = document.createElement('img');
                img.src = e.target.result;
                img.className = 'simg-img';
                div.appendChild(img);
                container.appendChild(div);
            };
            reader.readAsDataURL(file);
        });
    });
};

window.validateRequiredImages = function (inputId, errorId) {
    var input = document.getElementById(inputId);
    var error = document.getElementById(errorId);
    if (input && (!input.files || input.files.length === 0)) {
        if (error) error.textContent = 'Minimal 1 foto harus diunggah.';
        return false;
    }
    if (error) error.textContent = '';
    return true;
};

window.initImageUploader = function (cfg) {
    var input = document.getElementById(cfg.inputId);
    if (!input) return null;
    var store = [];
    var max = cfg.max;
    var variant = cfg.variant || 'grid';

    function sync() {
        var dt = new DataTransfer();
        store.forEach(function (f) { dt.items.add(f); });
        input.files = dt.files;
    }

    function appendAddTile(container) {
        var existing = container.querySelector('.' + cfg.addTileClass);
        if (existing) existing.remove();
        var tile = document.createElement('div');
        tile.className = cfg.addTileClass + ' up-add';
        tile.innerHTML = '<i class="ti ti-plus"></i><span>Tambah</span>';
        tile.onclick = function () { input.click(); };
        container.appendChild(tile);
    }

    function render() {
        var container = document.getElementById(cfg.previewId);
        if (!container) return;
        container.innerHTML = '';
        store.forEach(function (file, i) {
            var reader = new FileReader();
            reader.onload = function (e) {
                var wrap = document.createElement('div');
                var img = document.createElement('img');
                img.src = e.target.result;
                var del = document.createElement('button');
                del.type = 'button';
                if (variant === 'compact') {
                    wrap.className = 'up-tile--compact';
                    img.className = 'up-img--compact';
                    del.className = 'up-del--compact';
                    del.textContent = '✕';
                } else {
                    wrap.className = 'up-tile';
                    img.className = 'up-img';
                    del.className = 'up-del';
                    del.innerHTML = '✕';
                }
                del.onclick = (function (idx) {
                    return function () { store.splice(idx, 1); render(); sync(); };
                })(i);
                wrap.appendChild(img);
                wrap.appendChild(del);
                container.appendChild(wrap);
                if (variant === 'grid' && store.length < max) appendAddTile(container);
            };
            reader.readAsDataURL(file);
        });
        if (variant === 'grid' && store.length === 0) appendAddTile(container);
    }

    input.addEventListener('change', function () {
        Array.from(this.files).forEach(function (f) { if (store.length < max) store.push(f); });
        this.value = '';
        render();
        sync();
    });

    if (cfg.renderOnInit) render();

    return { getCount: function () { return store.length; }, render: render };
};
