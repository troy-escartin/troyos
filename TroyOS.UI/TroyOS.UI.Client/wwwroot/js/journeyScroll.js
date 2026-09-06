// Smooth-scroll helper for the Journey page stage navigation buttons.
window.journeyScroll = {
    scrollToId: function (elementId) {
        const el = document.getElementById(elementId);
        if (!el) {
            return;
        }

        const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        el.scrollIntoView({ behavior: prefersReducedMotion ? 'auto' : 'smooth', block: 'start' });
    }
};
