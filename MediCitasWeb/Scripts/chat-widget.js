(function () {
    const toggle = document.getElementById('chatToggle');
    const panel = document.getElementById('chatPanel');
    const close = document.getElementById('chatClose');

    if (!toggle || !panel || !close) return;

    toggle.addEventListener('click', function () {
        const visible = panel.style.display === 'block';
        panel.style.display = visible ? 'none' : 'block';
    });

    close.addEventListener('click', function () {
        panel.style.display = 'none';
    });
})();
