// Native form submission stays usable without JavaScript.
(() => {
    const monthOptions = document.querySelector('[data-month-options]');
    const monthFallback = document.querySelector('[data-month-fallback]');
    if (monthOptions && monthFallback) {
        monthOptions.hidden = false;
        monthFallback.hidden = true;
    }
    document.querySelectorAll('[data-password-fields]').forEach(form => {
        form.querySelectorAll('[data-password-toggle]').forEach(toggle => {
            const password = document.getElementById(toggle.getAttribute('aria-controls'));
            if (!password || !form.contains(password)) return;
            toggle.hidden = false;
            toggle.closest('.password-input').dataset.passwordReady = '';
            toggle.addEventListener('click', () => {
                const show = password.type === 'password';
                password.type = show ? 'text' : 'password';
                const label = `${show ? 'Ẩn' : 'Hiện'} ${toggle.dataset.passwordLabel}`;
                toggle.setAttribute('aria-pressed', String(show));
                toggle.setAttribute('aria-label', label);
                toggle.title = label;
                toggle.querySelector('[data-password-show]').hidden = show;
                toggle.querySelector('[data-password-hide]').hidden = !show;
            });
        });
    });
    const navigation = document.getElementById('primary-navigation');
    const toggle = document.querySelector('[data-navigation-toggle]');
    if (navigation && toggle) {
        const compact = window.matchMedia('(max-width: 64rem)');
        const sidebar = navigation.closest('.sidebar');
        const syncOffset = () => {
            if (compact.matches) {
                const style = getComputedStyle(sidebar);
                const height = sidebar.querySelector('.sidebar-heading').getBoundingClientRect().height;
                const padding = parseFloat(style.paddingTop);
                document.documentElement.style.setProperty('--navigation-offset',
                    `${height + padding * 2 + parseFloat(style.paddingBottom)}px`);
            } else document.documentElement.style.removeProperty('--navigation-offset');
        };
        const setNavigation = expanded => {
            navigation.hidden = !expanded;
            toggle.setAttribute('aria-expanded', String(expanded));
            toggle.textContent = expanded ? 'Đóng menu' : 'Menu';
        };
        const syncNavigation = () => {
            const active = document.activeElement;
            toggle.hidden = !compact.matches;
            setNavigation(!compact.matches);
            // The destination must be visible before receiving focus after a resize.
            if (compact.matches && navigation.contains(active)) toggle.focus();
            if (!compact.matches && active === toggle) navigation.querySelector('a')?.focus();
            syncOffset();
        };
        sidebar.dataset.navigationReady = '';
        syncNavigation();
        compact.addEventListener('change', syncNavigation);
        new ResizeObserver(syncOffset).observe(sidebar.querySelector('.sidebar-heading'));
        toggle.addEventListener('click', () => setNavigation(navigation.hidden));
        sidebar.addEventListener('focusout', event => {
            if (compact.matches && !sidebar.contains(event.relatedTarget)) setNavigation(false);
        });
        sidebar.addEventListener('keydown', event => {
            if (compact.matches && event.key === 'Escape' && !navigation.hidden) {
                event.preventDefault();
                setNavigation(false);
                toggle.focus();
            }
        });
        const sectionLinks = [...navigation.querySelectorAll('[data-section-link]')].filter(link => {
            const url = new URL(link.href);
            return url.pathname === location.pathname && url.search === location.search;
        });
        const setCurrentSection = id => {
            navigation.querySelectorAll('[aria-current]').forEach(link => link.removeAttribute('aria-current'));
            sectionLinks.find(link => link.hash === '#' + id)?.setAttribute('aria-current', 'location');
        };
        const sections = [...document.querySelectorAll('main [id], main [data-navigation-section]')]
            .filter(section => sectionLinks.some(link => link.hash === '#' + (section.dataset.navigationSection || section.id)));
        const syncSection = () => {
            if (!sections.length) return;
            const offset = parseFloat(getComputedStyle(document.documentElement).scrollPaddingTop);
            // A section becomes current as its heading enters the upper third of the visible content.
            const readingLine = offset + (innerHeight - offset) / 3;
            let current = sections[0];
            for (const section of sections) {
                const margin = parseFloat(getComputedStyle(section).scrollMarginTop);
                if (section.getBoundingClientRect().top <= readingLine + margin) current = section;
            }
            if (scrollY > 0 && innerHeight + scrollY >= document.documentElement.scrollHeight - 2)
                current = sections.at(-1);
            setCurrentSection(current.dataset.navigationSection || current.id);
        };
        let sectionFrame;
        const scheduleSection = () => {
            cancelAnimationFrame(sectionFrame);
            sectionFrame = requestAnimationFrame(syncSection);
        };
        window.addEventListener('scroll', scheduleSection, { passive: true });
        window.addEventListener('resize', scheduleSection);
        window.addEventListener('hashchange', scheduleSection);
        window.addEventListener('pageshow', scheduleSection);
        scheduleSection();
        navigation.querySelectorAll('a').forEach(link => link.addEventListener('click', event => {
            if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
            const target = new URL(link.href, location.href);
            if (compact.matches) setNavigation(false);
            if (target.pathname === location.pathname && target.search === location.search) {
                // Keep edits in this document; the native fragment link supplies history and scrolling.
                const destination = document.getElementById(target.hash.slice(1));
                if (destination) {
                    destination.setAttribute('tabindex', '-1');
                    destination.focus({ preventScroll: true });
                    if (sectionLinks.includes(link)) setCurrentSection(destination.id);
                }
            }
        }));
    }
    const summary = document.querySelector('.error-summary');
    summary?.focus();
    summary?.querySelectorAll('a[href^="#"]').forEach(link => link.addEventListener('click', () => {
        const field = document.getElementById(link.getAttribute('href').slice(1));
        for (let parent = field?.parentElement; parent; parent = parent.parentElement)
            if (parent instanceof HTMLDetailsElement) parent.open = true;
        field?.focus();
    }));
    const groupedPost = document.body.dataset.amountFormat === 'vi-VN';
    const groupDigits = text => text.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
    document.querySelectorAll('form').forEach(moneyForm => {
        const fields = moneyForm.querySelectorAll('input[inputmode="decimal"], input[data-money]');
        if (!fields.length) return;
        const marker = document.createElement('input');
        marker.type = 'hidden';
        marker.name = 'AmountFormat';
        marker.value = 'vi-VN';
        moneyForm.append(marker);
        fields.forEach(field => {
            // Do not parse through Number: decimal amounts can exceed JS precision.
            if (!groupedPost && /^(0|[1-9]\d*)([.,]\d{1,28})?$/.test(field.value)) {
                const [whole, fraction] = field.value.replace('.', ',').split(',');
                field.value = groupDigits(whole) + (fraction === undefined ? '' : ',' + fraction);
            }
            field.addEventListener('input', event => {
                const original = field.value;
                const caret = field.selectionStart ?? original.length;
                if (event.isComposing) return;
                // A malformed pasted amount stays visible for correction on submit.
                if (event.inputType === 'insertFromPaste' && original.includes('.') &&
                    !/^[1-9]\d{0,2}(\.\d{3})+(,\d*)?$/.test(original)) return;
                const plain = original.replace(/\./g, '');
                if (!/^(0|[1-9]\d*)(,\d*)?$/.test(plain)) return;
                const before = original.slice(0, caret).replace(/\./g, '').length;
                const [whole, fraction] = plain.split(',');
                field.value = groupDigits(whole) + (fraction === undefined ? '' : ',' + fraction);
                let position = 0, seen = 0;
                while (position < field.value.length && seen < before) {
                    if (field.value[position] !== '.') seen++;
                    position++;
                }
                if (field.type !== 'hidden') field.setSelectionRange(position, position);
            });
        });
    });
    document.querySelector('[data-scenario-form]')?.addEventListener('input', () => {
        const stale = document.querySelector('[data-stale-comparison]');
        if (stale) stale.hidden = false;
    });
    const reconciliation = document.querySelector('[data-reconciliation-form]');
    const markReconciliationStale = () => {
        const stale = document.querySelector('[data-stale-reconciliation]');
        if (stale) stale.hidden = false;
    };
    reconciliation?.addEventListener('input', markReconciliationStale);
    reconciliation?.addEventListener('change', markReconciliationStale);
    document.querySelector('[data-reconciliation-result]')?.focus();
    const form = document.querySelector('[data-forecast-form]');
    if (!form) return;
    const state = document.querySelector('[data-save-state]');
    let edited = form.dataset.unsaved === 'true';
    let submitting = false;
    form.addEventListener('input', () => {
        edited = true;
        if (state) state.textContent = 'Chưa lưu thay đổi. Xem trước không lưu hồ sơ.';
        const stale = document.querySelector('[data-stale-result]');
        if (stale) stale.hidden = false;
    });
    window.addEventListener('beforeunload', event => {
        if (edited && !submitting) {
            event.preventDefault();
            event.returnValue = '';
        }
    });
    form.addEventListener('submit', () => { submitting = true; });
})();
