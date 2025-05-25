(() => {
    const toggleBtn = document.getElementById('theme-toggle');
    const root = document.documentElement;
    const storageKey = 'theme';

    // Apply saved theme or OS preference on load
    const saved = localStorage.getItem(storageKey);
    if (saved === 'dark' || (!saved && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
        root.classList.add('dark');
    } else {
        root.classList.remove('dark');
    }

    // Toggle theme on button click and persist choice
    toggleBtn.addEventListener('click', () => {
        const isDark = root.classList.toggle('dark');
        localStorage.setItem(storageKey, isDark ? 'dark' : 'light');

        // Force reflow on key components to avoid flicker
        document.querySelectorAll('.card, .navbar, .form-control, .btn').forEach(el => {
            el.style.transition = 'none';
            /* reflow */ void el.offsetWidth;
            el.style.transition = '';
        });
    });

    // Observe dynamic additions and re-apply theme overrides
    const observer = new MutationObserver((mutations) => {
        mutations.forEach(({ addedNodes }) => {
            addedNodes.forEach(node => {
                if (node.matches && node.matches('.card, .navbar, .form-control, .btn, .dropdown-menu')) {
                    node.classList.add(root.classList.contains('dark') ? 'dark' : '');
                }
            });
        });
    });
    observer.observe(document.body, { childList: true, subtree: true });
})();
