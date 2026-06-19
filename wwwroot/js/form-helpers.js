// Shared helpers for the donasi/request create forms and offer form.

// Render category-specific detail fields into `containerId`, giving each input
// the id `idPrefix + field.key`. Reads the field map from window.categoryFields
// (emitted by the _CategoryFieldsScript partial).
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

// Wire up a .btn-condition toggle group to write the selected value into #inputId.
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

// Multi-image upload with in-memory store, live previews, delete, and
// DataTransfer sync back into the file input so the form submits the kept files.
// cfg: { inputId, previewId, max, variant: 'grid'|'compact', addTileClass, renderOnInit }
//   variant 'grid'    -> 88px tiles + a dashed "Tambah" add-tile (create forms)
//   variant 'compact' -> 72px tiles, no add-tile (offer form)
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
        tile.className = cfg.addTileClass;
        tile.style.cssText = 'width:88px;height:88px;border-radius:10px;border:2px dashed #cbd5e1;display:flex;flex-direction:column;align-items:center;justify-content:center;cursor:pointer;color:#94a3b8;gap:4px;background:#f8fafc;flex-shrink:0;transition:all 0.15s;';
        tile.innerHTML = '<i class="ti ti-plus" style="font-size:22px;"></i><span style="font-size:12px;font-family:Plus Jakarta Sans,sans-serif;font-weight:600;">Tambah</span>';
        tile.onmouseenter = function () { tile.style.borderColor = 'var(--blue)'; tile.style.color = 'var(--blue)'; tile.style.background = '#eff6ff'; };
        tile.onmouseleave = function () { tile.style.borderColor = '#cbd5e1'; tile.style.color = '#94a3b8'; tile.style.background = '#f8fafc'; };
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
                    wrap.style.cssText = 'position:relative;width:72px;height:72px;';
                    img.style.cssText = 'width:72px;height:72px;border-radius:8px;object-fit:cover;';
                    del.style.cssText = 'position:absolute;top:-6px;right:-6px;width:18px;height:18px;border-radius:50%;background:#ef4444;color:#fff;border:none;cursor:pointer;font-size:12px;';
                    del.textContent = '✕';
                } else {
                    wrap.style.cssText = 'position:relative;width:88px;height:88px;border-radius:10px;overflow:hidden;flex-shrink:0;';
                    img.style.cssText = 'width:100%;height:100%;object-fit:cover;display:block;';
                    del.style.cssText = 'position:absolute;top:4px;right:4px;width:22px;height:22px;border-radius:50%;background:rgba(0,0,0,0.55);color:#fff;border:none;cursor:pointer;font-size:14px;display:flex;align-items:center;justify-content:center;line-height:1;';
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
