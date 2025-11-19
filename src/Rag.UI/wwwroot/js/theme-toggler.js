// Theme toggler with persistence, observer, and helper methods
(function (root) {
    const key = 'color-theme';

    function computeShouldDark() {
        const stored = localStorage.getItem(key);
        const prefersDark = globalThis.matchMedia && globalThis.matchMedia('(prefers-color-scheme: dark)').matches;
        return stored === 'dark' || (!stored && prefersDark);
    }

    function applySavedTheme() {
        const shouldDark = computeShouldDark();
        const hasDark = document.documentElement.classList.contains('dark');
        if (shouldDark && !hasDark) document.documentElement.classList.add('dark');
        else if (!shouldDark && hasDark) document.documentElement.classList.remove('dark');
    }

    function toggle() {
        const isDark = document.documentElement.classList.contains('dark');
        if (isDark) {
            document.documentElement.classList.remove('dark');
            localStorage.setItem(key, 'light');
            return false;
        } else {
            document.documentElement.classList.add('dark');
            localStorage.setItem(key, 'dark');
            return true;
        }
    }

    function set(dark) {
        const isDark = document.documentElement.classList.contains('dark');
        if (dark && !isDark) {
            document.documentElement.classList.add('dark');
            localStorage.setItem(key, 'dark');
        } else if (!dark && isDark) {
            document.documentElement.classList.remove('dark');
            localStorage.setItem(key, 'light');
        }
        return document.documentElement.classList.contains('dark');
    }

    function isDark() { return document.documentElement.classList.contains('dark'); }
    function current() { return isDark() ? 'dark' : 'light'; }

    let observerAttached = false;
    function ensureObserver() {
        if (observerAttached) return;
        const observer = new MutationObserver(() => {
            if (computeShouldDark() && !document.documentElement.classList.contains('dark')) {
                document.documentElement.classList.add('dark');
            }
        });
        observer.observe(document.documentElement, { attributes: true, attributeFilter: ['class'] });
        observerAttached = true;
    }

    // Public API
    root.themeToggler = {
        initialize: function () { applySavedTheme(); ensureObserver(); },
        applySavedTheme,
        toggle,
        set,
        isDark,
        current,
        ensureObserver
    };

})(globalThis);
