// Native form submission stays usable without JavaScript.
(() => {
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
