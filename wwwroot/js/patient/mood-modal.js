// #Bagian Modal Mood Pasien#
(function () {
    function bindMoodModalResize() {
        window.addEventListener('message', function (e) {
            if (e.origin !== window.location.origin) return;
            if (!e.data || e.data.type !== 'moodWizardResize') return;

            var iframe = document.getElementById('moodIframe');
            if (!iframe) return;

            var maxH = Math.floor(window.innerHeight * 0.88);
            var h = Math.min(Math.max(e.data.height || 0, 280), maxH);
            iframe.style.height = h + 'px';
        });
    }

    window.openMoodModal = function () {
        var iframe = document.getElementById('moodIframe');
        // #region agent log
        if (window.luDbg) window.luDbg('mood-modal.js:openMoodModal', 'openMoodModal called', { iframeFound: !!iframe, moodModalFound: !!document.getElementById('moodModal') }, 'H4');
        // #endregion
        if (iframe && !iframe.getAttribute('src')) {
            iframe.setAttribute('src', '/Patient/Mood/Feeling');
        }
        window.openHrpfModal('moodModal');
    };

    window.closeMoodModal = function () {
        window.closeHrpfModal('moodModal');
    };

    bindMoodModalResize();
})();
