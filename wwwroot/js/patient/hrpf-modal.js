// #Bagian Kontrol Modal Pasien (hrpf-modal)#
(function () {
    'use strict';

    function updateScrollLock() {
        var anyOpen = document.querySelector('.hrpf-modal-overlay.is-open') !== null;
        document.body.classList.toggle('hrpf-modal-open', anyOpen);
        document.body.classList.toggle('lu-scroll-lock', anyOpen);
    }

    function applyModalOverlayLayout(overlay) {
        if (window.getComputedStyle(overlay).position === 'fixed') return false;
        overlay.style.setProperty('position', 'fixed', 'important');
        overlay.style.setProperty('top', '0', 'important');
        overlay.style.setProperty('left', '0', 'important');
        overlay.style.setProperty('right', '0', 'important');
        overlay.style.setProperty('bottom', '0', 'important');
        overlay.style.setProperty('z-index', '10000', 'important');
        overlay.style.setProperty('display', 'flex', 'important');
        overlay.style.setProperty('align-items', 'center', 'important');
        overlay.style.setProperty('justify-content', 'center', 'important');
        return true;
    }

    function clearModalOverlayLayout(overlay) {
        ['position', 'top', 'left', 'right', 'bottom', 'z-index', 'display', 'align-items', 'justify-content'].forEach(function (prop) {
            overlay.style.removeProperty(prop);
        });
    }

    window.openHrpfModal = function (id) {
        var overlay = document.getElementById(id);
        if (!overlay) return;
        overlay.classList.add('is-open');
        overlay.removeAttribute('hidden');
        overlay.setAttribute('aria-hidden', 'false');
        applyModalOverlayLayout(overlay);
        updateScrollLock();
    };

    window.closeHrpfModal = function (id) {
        var overlay = document.getElementById(id);
        if (!overlay) return;
        overlay.classList.remove('is-open');
        overlay.setAttribute('hidden', '');
        overlay.setAttribute('aria-hidden', 'true');
        clearModalOverlayLayout(overlay);
        updateScrollLock();
    };

    document.addEventListener('click', function (e) {
        var opener = e.target.closest('[data-hrpf-modal-open]');
        if (opener) {
            e.preventDefault();
            var targetId = opener.getAttribute('data-hrpf-modal-open');
            window.openHrpfModal(targetId);
            return;
        }

        var closer = e.target.closest('[data-hrpf-modal-close]');
        if (closer) {
            e.preventDefault();
            var overlay = closer.closest('.hrpf-modal-overlay');
            if (overlay && overlay.id) {
                window.closeHrpfModal(overlay.id);
            }
            return;
        }

        if (e.target.classList.contains('hrpf-modal-overlay') && e.target.classList.contains('is-open')) {
            window.closeHrpfModal(e.target.id);
        }
    });

    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Escape') return;
        var open = document.querySelector('.hrpf-modal-overlay.is-open');
        if (open && open.id) {
            window.closeHrpfModal(open.id);
        }
    });
})();
