// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function toggleSidebar(sidebarId) {
    const sidebar = document.getElementById(sidebarId);
    if (sidebar) {
        sidebar.classList.toggle('collapsed');
    }
}

document.addEventListener('DOMContentLoaded', () => {
    const input = document.querySelector('#searchInput');
    const results = document.querySelector('#searchResults');
    if (!input || !results) return;

    input.addEventListener('input', async () => {
        const q = input.value.trim();
        if (!q) { results.innerHTML = ''; results.style.display = 'none'; return; }

        try {
            const res = await fetch(`/Search/Live?q=${encodeURIComponent(q)}`);
            if (!res.ok) {
                const text = await res.text();
                console.error('Search error', res.status, text);
                results.innerHTML = '';
                results.style.display = 'none';
                return;
            }
            results.innerHTML = await res.text();
            results.style.display = 'block';
        } catch (err) {
            console.error('Search fetch failed:', err);
            results.innerHTML = '';
            results.style.display = 'none';
        }
    });

    // hide results when clicking outside
    document.addEventListener('click', (e) => {
        if (e.target !== input && !results.contains(e.target)) {
            results.style.display = 'none';
        }
    });
});