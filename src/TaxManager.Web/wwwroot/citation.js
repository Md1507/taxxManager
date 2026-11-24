document.addEventListener('keydown', event => {
    if (event.key !== 'Escape') return;
    const panel = event.target.closest('details.basis[open]');
    if (!panel) return;
    panel.open = false;
    panel.querySelector('summary')?.focus();
    event.preventDefault();
});
