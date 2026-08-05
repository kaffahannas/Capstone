// Global success/error/info popup — vanilla JS, no jQuery/Bootstrap dependency
// so it works identically across every area layout (Admin has no jQuery).
(function () {
    var ICONS = { success: '✅', error: '⚠️', info: 'ℹ️' };

    function ensureOverlay() {
        var el = document.getElementById('lu-alert-popup-overlay');
        if (el) return el;

        el = document.createElement('div');
        el.id = 'lu-alert-popup-overlay';
        el.className = 'lu-alert-popup-overlay';
        el.innerHTML =
            '<div class="lu-alert-popup" role="alertdialog" aria-modal="true">' +
                '<div class="lu-alert-popup-icon"></div>' +
                '<p class="lu-alert-popup-msg"></p>' +
                '<button type="button" class="lu-alert-popup-btn">OK</button>' +
            '</div>';
        document.body.appendChild(el);

        el.querySelector('.lu-alert-popup-btn').addEventListener('click', hide);
        el.addEventListener('click', function (ev) {
            if (ev.target === el) hide();
        });
        document.addEventListener('keydown', function (ev) {
            if (ev.key === 'Escape') hide();
        });

        return el;
    }

    function hide() {
        var el = document.getElementById('lu-alert-popup-overlay');
        if (el) el.classList.remove('lu-alert-popup-overlay--open');
    }

    window.showAlertPopup = function (type, message) {
        if (!message) return;
        type = (type === 'error' || type === 'info') ? type : 'success';

        var el = ensureOverlay();
        el.querySelector('.lu-alert-popup').className = 'lu-alert-popup lu-alert-popup--' + type;
        el.querySelector('.lu-alert-popup-icon').textContent = ICONS[type];
        el.querySelector('.lu-alert-popup-msg').textContent = message;
        el.classList.add('lu-alert-popup-overlay--open');
    };
})();
