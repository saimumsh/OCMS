// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
    var STORAGE_KEY = 'oc.dashboard.sidebar.collapsed';
    var shell = document.getElementById('dashboardShell');
    var toggle = document.getElementById('dashboardSidebarToggle');
    if (!shell || !toggle) return;

    function setCollapsed(collapsed) {
        shell.classList.toggle('dashboard-collapsed', collapsed);
        toggle.setAttribute('aria-expanded', collapsed ? 'false' : 'true');
        try { localStorage.setItem(STORAGE_KEY, collapsed ? '1' : '0'); } catch (e) { /* ignore */ }
    }

    var stored = null;
    try { stored = localStorage.getItem(STORAGE_KEY); } catch (e) { /* ignore */ }
    if (stored === '1') setCollapsed(true);

    toggle.addEventListener('click', function () {
        setCollapsed(!shell.classList.contains('dashboard-collapsed'));
    });
})();
