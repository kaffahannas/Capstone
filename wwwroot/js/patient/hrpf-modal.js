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
        // #region agent log
        if (window.luDbg) window.luDbg('hrpf-modal.js:openHrpfModal:entry', 'openHrpfModal called', { id: id, overlayFound: !!overlay }, 'H1');
        // #endregion
        if (!overlay) return;
        overlay.classList.add('is-open');
        overlay.removeAttribute('hidden');
        overlay.setAttribute('aria-hidden', 'false');
        var jsFallbackApplied = applyModalOverlayLayout(overlay);
        updateScrollLock();
        var cs = window.getComputedStyle(overlay);
        var rect = overlay.getBoundingClientRect();
        // #region agent log
        if (window.luDbg) window.luDbg('hrpf-modal.js:openHrpfModal:after', 'modal state after open', { id: id, parent: overlay.parentElement === document.body ? 'BODY' : overlay.parentElement.tagName, hasHidden: overlay.hasAttribute('hidden'), hasIsOpen: overlay.classList.contains('is-open'), jsFallbackApplied: jsFallbackApplied, position: cs.position, display: cs.display, opacity: cs.opacity, visibility: cs.visibility, pointerEvents: cs.pointerEvents, zIndex: cs.zIndex, rect: { top: rect.top, left: rect.left, width: rect.width, height: rect.height } }, 'H2,H3,H5,H6', 'post-fix-2');
        // #endregion
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
            // #region agent log
            if (window.luDbg) window.luDbg('hrpf-modal.js:click', 'data-hrpf-modal-open clicked', { targetId: targetId }, 'H1');
            // #endregion
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
