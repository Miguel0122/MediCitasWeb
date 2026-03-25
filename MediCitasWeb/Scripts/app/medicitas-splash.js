/**
 * MediCitas — Splash Screen & Page Loader
 * Archivo: Scripts/app/medicitas-splash.js
 */
(function () {
    const SPLASH_DURATION = 2500; // 2.5 segundos

    // ── Barra de progreso animada ────────────────────────────────────────
    const fill = document.getElementById('splashFill');
    const splash = document.getElementById('mediSplash');
    const loader = document.getElementById('pageLoader');

    if (fill) {
        // Simula progreso realista: arranca rápido, frena al final
        const steps = [
            { pct: 20, delay: 100 },
            { pct: 45, delay: 350 },
            { pct: 68, delay: 700 },
            { pct: 85, delay: 1100 },
            { pct: 100, delay: SPLASH_DURATION - 300 }
        ];
        steps.forEach(s => {
            setTimeout(() => { fill.style.width = s.pct + '%'; }, s.delay);
        });
    }

    // ── Ocultar splash después de SPLASH_DURATION ────────────────────────
    if (splash) {
        setTimeout(function () {
            splash.classList.add('hidden');
            // Eliminar del DOM después de la transición para no bloquear clicks
            setTimeout(function () {
                splash.remove();
            }, 600);
        }, SPLASH_DURATION);
    }

    // ── Page Loader: mostrar cuando la página tarda en cargar ────────────
    // Se activa en links de navegación interna (no en downloads ni externos)
    if (loader) {
        document.addEventListener('click', function (e) {
            const link = e.target.closest('a[href]');
            if (!link) return;

            const href = link.getAttribute('href');
            if (!href) return;

            // Ignorar: anclas, javascript:, externos, targets especiales, downloads
            const isAnchor = href.startsWith('#');
            const isJS = href.startsWith('javascript:');
            const isExternal = href.startsWith('http') && !href.includes(window.location.hostname);
            const hasTarget = link.target && link.target !== '_self';
            const isDownload = link.hasAttribute('download');
            const isLogout = href.includes('Login') || href.includes('Logout');

            if (isAnchor || isJS || isExternal || hasTarget || isDownload) return;

            // Solo mostrar loader si la navegación tarda más de 200ms
            // (evita parpadeo en páginas rápidas)
            const loaderTimer = setTimeout(function () {
                loader.style.display = 'flex';
                requestAnimationFrame(function () {
                    loader.classList.add('visible');
                });
            }, 200);

            // Si la página carga antes de 200ms, cancelar
            window.addEventListener('beforeunload', function () {
                clearTimeout(loaderTimer);
            }, { once: true });
        });

        // Ocultar loader cuando la página termina de cargar
        window.addEventListener('pageshow', function () {
            loader.classList.remove('visible');
            setTimeout(function () { loader.style.display = 'none'; }, 300);
        });
    }

    // ── showPageLoader / hidePageLoader: API pública para usar en AJAX ───
    // Uso: MediCitas.showLoader() antes de un fetch, MediCitas.hideLoader() al terminar
    window.MediCitas = {
        showLoader: function () {
            if (!loader) return;
            loader.style.display = 'flex';
            requestAnimationFrame(function () {
                loader.classList.add('visible');
            });
        },
        hideLoader: function () {
            if (!loader) return;
            loader.classList.remove('visible');
            setTimeout(function () { loader.style.display = 'none'; }, 300);
        }
    };

})();