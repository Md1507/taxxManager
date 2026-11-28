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
    document.querySelectorAll('[data-password-registration]').forEach(form => {
        const password = form.querySelector('input[name="Password"]');
        const confirmation = form.querySelector('input[name="ConfirmPassword"]');
        const condition = form.querySelector('[data-password-condition]');
        const match = form.querySelector('[data-password-match]');
        if (!password || !confirmation || !condition || !match) return;
        const showCondition = (element, state, message) => {
            element.dataset.conditionState = state;
            element.querySelector('[data-condition-idle]').hidden = state !== 'idle';
            element.querySelector('[data-condition-valid]').hidden = state !== 'valid';
            element.querySelector('[data-condition-invalid]').hidden = state !== 'invalid';
            const text = element.querySelector('[data-condition-message]');
            // Announce changes of condition, not every keystroke or the secret itself.
            if (text.textContent !== message) text.textContent = message;
        };
        const updateConditions = () => {
            const value = password.value;
            // Match .NET Required's whitespace semantics; keep the entered value untouched.
            const onlyWhitespace = /^[\u0009-\u000D\u0020\u0085\u00A0\u1680\u2000-\u200A\u2028\u2029\u202F\u205F\u3000]*$/.test(value);
            const validLength = value.length >= password.minLength && value.length <= password.maxLength;
            const passwordError = !value ? '' : onlyWhitespace ? 'Không chỉ nhập khoảng trắng.'
                : !validLength ? `Dùng mật khẩu từ ${password.minLength} đến ${password.maxLength} ký tự.` : '';
            password.setCustomValidity(passwordError);
            showCondition(condition, !value ? 'idle' : passwordError ? 'invalid' : 'valid',
                !value ? `${password.minLength}–${password.maxLength} ký tự, không chỉ gồm khoảng trắng.` : passwordError || 'Đạt điều kiện mật khẩu.');
            const confirmationError = !confirmation.value ? '' : !value ? 'Nhập mật khẩu ở trên trước.'
                : confirmation.value !== value ? 'Hai mật khẩu chưa khớp.' : '';
            confirmation.setCustomValidity(confirmationError);
            showCondition(match, !confirmation.value ? 'idle' : confirmationError ? 'invalid' : 'valid',
                !confirmation.value ? 'Nhập lại mật khẩu để xác nhận.' : confirmationError || 'Hai mật khẩu đã khớp.');
        };
        [password, confirmation].forEach(input => {
            input.addEventListener('input', updateConditions);
            input.addEventListener('change', updateConditions);
        });
        window.addEventListener('pageshow', updateConditions);
        updateConditions();
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
            const label = expanded ? 'Đóng menu' : 'Mở menu';
            toggle.setAttribute('aria-label', label);
            toggle.title = label;
            toggle.querySelector('[data-navigation-label]').textContent = label;
            toggle.querySelector('[data-navigation-open]').hidden = expanded;
            toggle.querySelector('[data-navigation-close]').hidden = !expanded;
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
    const monthly = document.querySelector('[data-monthly-calculator]');
    if (monthly) {
        const markStale = () => {
            const notice = document.querySelector('[data-monthly-stale]');
            if (notice) notice.hidden = false;
        };
        const groups = [...monthly.querySelectorAll('[data-month-inputs]')];
        const choices = [...monthly.querySelectorAll('[data-monthly-select]')];
        const common = [...monthly.querySelectorAll('[data-monthly-common]')];
        const feedback = monthly.querySelector('[data-monthly-feedback]');
        const confirmation = monthly.querySelector('[data-monthly-confirm]');
        const apply = monthly.querySelector('[data-monthly-apply]');
        const undo = monthly.querySelector('[data-monthly-undo]');
        const from = monthly.querySelector('[data-monthly-from]');
        const to = monthly.querySelector('[data-monthly-to]');
        let pending = [], previous = [], selected = 0;
        const fields = group => [...group.querySelectorAll('input[name]')];
        const update = () => choices.forEach((choice, i) => {
            const count = fields(groups[i]).filter(input => input.value.trim() !== '').length;
            choice.querySelector('[data-monthly-state]').textContent = count === 0 ? 'Chưa nhập' : `${count}/3 ô đã nhập`;
            choice.classList.toggle('has-data', count > 0);
            if (i === selected) choice.setAttribute('aria-current', 'true');
            else choice.removeAttribute('aria-current');
        });
        const select = index => {
            selected = index;
            groups.forEach((group, i) => group.hidden = i !== index);
            update();
        };
        const cancelPending = () => { pending = []; confirmation.hidden = true; };
        const review = monthly.querySelector('[data-monthly-review]');
        choices.forEach((choice, i) => choice.addEventListener('click', () => select(i)));
        review.addEventListener('click', () => {
            cancelPending();
            const start = Number(from.value), end = Number(to.value);
            if (!Number.isInteger(start) || !Number.isInteger(end) || start < 1 || end > 12 || start > end) {
                feedback.textContent = 'Nhập khoảng tháng từ 1 đến 12, tháng cuối không trước tháng đầu.';
                from.focus(); return;
            }
            const entered = common.filter(input => input.value.trim() !== '');
            if (!entered.length) { feedback.textContent = 'Nhập ít nhất một khoản muốn dùng chung.'; common[0].focus(); return; }
            const invalid = entered.find(input => !/^(?:0|[1-9]\d{0,2}(?:\.\d{3})*|[1-9]\d*)(?:,\d{1,28})?$/.test(input.value));
            if (invalid) { feedback.textContent = 'Kiểm tra số tiền, ví dụ 30.000.000 hoặc 1.234,50.'; invalid.focus(); return; }
            for (let i = start - 1; i < end; i++) entered.forEach(source => {
                const field = groups[i].querySelector(`input[name="${source.dataset.monthlyCommon}"]`);
                if (field.value !== source.value) pending.push({ field, before: field.value, after: source.value });
            });
            if (!pending.length) { feedback.textContent = 'Các tháng trong khoảng này đã có đúng số vừa nhập.'; return; }
            const existing = pending.filter(change => change.before !== '').length;
            feedback.textContent = `Sẽ đổi ${pending.length} ô từ tháng ${start} đến ${end}${existing ? `, gồm ${existing} ô đang có số` : ''}. Các khoản để trống giữ nguyên.`;
            apply.textContent = `Áp dụng từ tháng ${start} đến ${end}`;
            confirmation.hidden = false;
        });
        monthly.querySelector('[data-monthly-cancel]').addEventListener('click', () => {
            cancelPending(); feedback.textContent = 'Đã hủy, số từng tháng vẫn được giữ.';
            review.focus();
        });
        apply.addEventListener('click', () => {
            previous = pending;
            previous.forEach(change => change.field.value = change.after);
            if (previous.length) markStale();
            cancelPending(); update(); undo.hidden = false;
            feedback.textContent = `Đã áp dụng ${previous.length} ô. Có thể sửa riêng từng tháng hoặc hoàn tác.`;
            choices[selected].focus();
        });
        undo.addEventListener('click', () => {
            let restored = 0;
            previous.forEach(change => { if (change.field.value === change.after) { change.field.value = change.before; restored++; } });
            if (restored) markStale();
            previous = []; undo.hidden = true; cancelPending(); update();
            feedback.textContent = `Đã hoàn tác ${restored} ô; giữ các ô bạn đã sửa riêng sau khi áp dụng.`;
            choices[selected].focus();
        });
        [...common, from, to].forEach(input => input.addEventListener('keydown', event => {
            if (event.key !== 'Enter') return;
            event.preventDefault();
            review.click();
        }));
        monthly.addEventListener('submit', event => {
            if (!pending.length) return;
            event.preventDefault();
            feedback.textContent = 'Chưa áp dụng thay đổi chung. Chọn Áp dụng hoặc Hủy trước khi tính.';
            apply.focus();
        });
        [...common, from, to].forEach(input => input.addEventListener('input', () => {
            if (!pending.length) return;
            cancelPending(); feedback.textContent = 'Thông số chung đã đổi. Chọn Xem thay đổi để kiểm tra lại.';
        }));
        groups.forEach(group => group.addEventListener('input', () => {
            markStale();
            if (pending.length) feedback.textContent = 'Số từng tháng đã đổi. Chọn Xem thay đổi để kiểm tra lại.';
            cancelPending(); update();
        }));
        monthly.querySelector('[name="year"]').addEventListener('change', markStale);
        monthly.addEventListener('invalid', () => {
            const first = monthly.querySelector('[data-month-inputs] input:invalid');
            const index = groups.indexOf(first?.closest('[data-month-inputs]'));
            if (index >= 0) select(index);
        }, true);
        monthly.querySelector('[data-monthly-bulk]').hidden = false;
        monthly.querySelector('[data-monthly-navigation]').hidden = false;
        monthly.dataset.monthlyReady = '';
        const errorMonth = Number(monthly.dataset.errorMonth);
        select(errorMonth >= 1 && errorMonth <= 12 ? errorMonth - 1 : 0);
    }
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
