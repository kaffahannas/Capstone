(function () {
    window.animateBarFills = function (selector) {
        requestAnimationFrame(function () {
            document.querySelectorAll(selector).forEach(function (el) {
                var target = el.style.getPropertyValue('--bar-w');
                if (!target) return;
                el.style.setProperty('--bar-w', '0%');
                requestAnimationFrame(function () {
                    el.style.setProperty('--bar-w', target);
                });
            });
        });
    };
})();
