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

    function portalModalsToBody() {
        document.querySelectorAll(
            '.hrpf-modal-overlay, .prf-modal-overlay, .psy-modal-overlay, .modal-overlay'
        ).forEach(function (el) {
            if (el.parentElement !== document.body) {
                document.body.appendChild(el);
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', portalModalsToBody);
    } else {
        portalModalsToBody();
    }
})();
