// Renumber surviving visible rows so model binding gets CollectionProperty[0].Field, [1].Field, ...
function renumberRows(tbody, prefix) {
    let index = 0;
    tbody.querySelectorAll('tr[data-row]').forEach(row => {
        if (row.style.display === 'none') return;
        row.querySelectorAll('input, select, textarea').forEach(input => {
            input.name = input.name.replace(/\[\d+\]/, '[' + index + ']');
        });
        index++;
    });
}

document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-collection]').forEach(table => {
        const prefix = table.dataset.collection;
        const tbody = table.querySelector('tbody');
        const template = table.closest('form').querySelector('template[data-for="' + prefix + '"]');

        // Add row
        table.closest('form').querySelector('[data-add-for="' + prefix + '"]')?.addEventListener('click', e => {
            e.preventDefault();
            const count = tbody.querySelectorAll('tr[data-row]:not([style*="display: none"])').length;
            const html = template.innerHTML.replace(/\[-1\]/g, '[' + count + ']');
            const tmp = document.createElement('tbody');
            tmp.innerHTML = html;
            const row = tmp.firstElementChild;
            row.setAttribute('data-row', '');
            tbody.appendChild(row);
        });

        // Remove row (delegated)
        tbody.addEventListener('click', e => {
            const btn = e.target.closest('[data-remove-row]');
            if (!btn) return;
            e.preventDefault();
            const row = btn.closest('tr[data-row]');
            row.style.display = 'none';
            row.querySelectorAll('input, select, textarea').forEach(i => { i.value = ''; i.disabled = true; });
        });
    });

    // Renumber on submit
    document.querySelectorAll('form[data-collection-form]').forEach(form => {
        form.addEventListener('submit', () => {
            form.querySelectorAll('[data-collection]').forEach(table => {
                renumberRows(table.querySelector('tbody'), table.dataset.collection);
            });
        });
    });
});
