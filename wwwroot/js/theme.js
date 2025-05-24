// theme.js

(() => {
    const toggleBtn = document.getElementById('theme-toggle');
    const root = document.documentElement;
    const storageKey = 'theme';

    // Apply saved theme or OS preference on load
    const saved = localStorage.getItem(storageKey);
    if (saved) {
        root.classList.toggle('dark', saved === 'dark');
    } else if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
        root.classList.add('dark');
    }

    // Toggle theme on button click and persist choice
    toggleBtn.addEventListener('click', () => {
        const isDark = root.classList.toggle('dark');
        localStorage.setItem(storageKey, isDark ? 'dark' : 'light');
    });

    // Optional: observe for new cards added dynamically and force repaint
    const observer = new MutationObserver(() => {
        // simply forcing a style recalc can help in some rare cases:
        document.querySelectorAll('.card').forEach(card => {
            card.style.transition = 'none';
      /* triggering reflow */ void card.offsetWidth;
            card.style.transition = '';
        });
    });
    observer.observe(document.body, { childList: true, subtree: true });
})();
