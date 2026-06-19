// #Bagian App Shell Global#
/**
 * LightenUp — global responsive app shell (sidebar offcanvas ≤ 992px)
 */
(function () {
    'use strict';

    const BP = 992;

    // #Function initAppShell#
    function initAppShell() {
        const shell = document.getElementById('appShell') || document.querySelector('.app-shell, .admin-shell');
        const sidebar = document.getElementById('appSidebar')
            || document.querySelector('.app-sidebar, .admin-sidebar, .sidebar');
        const toggle = document.getElementById('sidebarToggle');
        const closeBtn = document.getElementById('sidebarClose');
        const backdrop = document.getElementById('sidebarBackdrop');

        if (!sidebar) return;

        function isMobile() {
            return window.innerWidth < BP;
        }

        function openSidebar() {
            sidebar.classList.add('is-open');
            backdrop?.classList.add('is-visible');
            document.body.classList.add('sidebar-open');
            toggle?.setAttribute('aria-expanded', 'true');
        }

        function closeSidebar() {
            sidebar.classList.remove('is-open');
            backdrop?.classList.remove('is-visible');
            document.body.classList.remove('sidebar-open');
            toggle?.setAttribute('aria-expanded', 'false');
        }

        toggle?.addEventListener('click', function () {
            if (sidebar.classList.contains('is-open')) {
                closeSidebar();
            } else {
                openSidebar();
            }
        });

        closeBtn?.addEventListener('click', closeSidebar);
        backdrop?.addEventListener('click', closeSidebar);

        sidebar.querySelectorAll('a, button[type="submit"]').forEach(function (el) {
            el.addEventListener('click', function () {
                if (isMobile()) closeSidebar();
            });
        });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') closeSidebar();
        });

        window.addEventListener('resize', function () {
            if (!isMobile()) closeSidebar();
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAppShell);
    } else {
        initAppShell();
    }
})();

/* Portal modal overlays to <body> so position:fixed is not clipped by overflow:hidden ancestors */
(function () {
    'use strict';

    // #region agent log
    window.luDbg = function (location, message, data, hypothesisId, runId) {
        var payload = { sessionId: '9d27c6', runId: runId || 'pre-fix', hypothesisId: hypothesisId, location: location, message: message, data: data || {}, timestamp: Date.now() };
        fetch('http://127.0.0.1:7824/ingest/931ee0be-5792-48c4-8700-deede251d66a', { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-Debug-Session-Id': '9d27c6' }, body: JSON.stringify(payload) }).catch(function () {});
        fetch('/debug/log', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) }).catch(function () {});
    };

    document.addEventListener('click', function (e) {
        var top = document.elementFromPoint(e.clientX, e.clientY);
        if (!top) return;
        var overlay = top.closest ? top.closest('.hrpf-modal-overlay, .prf-modal-overlay') : null;
        window.luDbg('site.js:click-capture', 'click target', {
            tag: top.tagName,
            id: top.id || null,
            className: (top.className && top.className.toString) ? top.className.toString().slice(0, 120) : null,
            blockingOverlayId: overlay ? overlay.id : null,
            x: e.clientX,
            y: e.clientY
        }, 'H6');
    }, true);
    // #endregion

    function portalModalsToBody() {
        document.querySelectorAll(
            '.hrpf-modal-overlay, .prf-modal-overlay, .psy-modal-overlay, .modal-overlay'
        ).forEach(function (el) {
            var parentTag = el.parentElement ? el.parentElement.tagName + (el.parentElement.id ? '#' + el.parentElement.id : '') : 'none';
            // #region agent log
            window.luDbg('site.js:portalModalsToBody', 'modal portal check', { id: el.id, parentBefore: parentTag, willPortal: el.parentElement !== document.body }, 'H3');
            // #endregion
            if (el.parentElement !== document.body) {
                document.body.appendChild(el);
            }
            var cs = window.getComputedStyle(el);
            // #region agent log
            window.luDbg('site.js:portalModalsToBody:after', 'modal computed after portal', { id: el.id, parentAfter: el.parentElement === document.body ? 'BODY' : el.parentElement.tagName, position: cs.position, display: cs.display, opacity: cs.opacity, visibility: cs.visibility, pointerEvents: cs.pointerEvents, zIndex: cs.zIndex, hasHidden: el.hasAttribute('hidden'), hasIsOpen: el.classList.contains('is-open') }, 'H2,H3,H5,H6');
            // #endregion
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', portalModalsToBody);
    } else {
        portalModalsToBody();
    }
})();
