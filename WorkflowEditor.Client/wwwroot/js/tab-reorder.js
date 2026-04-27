// HTML5 drag-and-drop для перемещения вкладок. Нативный dnd работает через атрибут
// draggable="true" на каждом таб-элементе и data-tab-name="<name>". При drop собираем
// порядок DOM-элементов и отдаём в .NET ([JSInvokable] OnTabsReordered(string[])).
window.tabReorder = {
    _handlers: new Map(),

    attach(containerId, dotnetRef) {
        const root = document.getElementById(containerId);
        if (!root) return;

        let draggedName = null;

        const onDragStart = e => {
            const tab = e.target.closest('.workflow-tab');
            if (!tab) return;
            draggedName = tab.dataset.tabName;
            e.dataTransfer.effectAllowed = 'move';
            tab.classList.add('dragging');
        };

        const onDragEnd = e => {
            const tab = e.target.closest('.workflow-tab');
            if (tab) tab.classList.remove('dragging');
            draggedName = null;
        };

        const onDragOver = e => {
            const tab = e.target.closest('.workflow-tab');
            if (!tab || !draggedName) return;
            e.preventDefault();
            e.dataTransfer.dropEffect = 'move';
        };

        const onDrop = async e => {
            e.preventDefault();
            const target = e.target.closest('.workflow-tab');
            if (!draggedName || !target) return;
            const targetName = target.dataset.tabName;
            if (targetName === draggedName) return;

            const tabs = Array.from(root.querySelectorAll('.workflow-tab'));
            const order = tabs.map(t => t.dataset.tabName);
            const from = order.indexOf(draggedName);
            const to = order.indexOf(targetName);
            if (from < 0 || to < 0) return;
            order.splice(to, 0, order.splice(from, 1)[0]);

            try {
                await dotnetRef.invokeMethodAsync('OnTabsReordered', order);
            } catch { /* окно могло закрыться */ }
        };

        root.addEventListener('dragstart', onDragStart);
        root.addEventListener('dragend', onDragEnd);
        root.addEventListener('dragover', onDragOver);
        root.addEventListener('drop', onDrop);
        this._handlers.set(containerId, { root, onDragStart, onDragEnd, onDragOver, onDrop });
    },

    detach(containerId) {
        const h = this._handlers.get(containerId);
        if (!h) return;
        h.root.removeEventListener('dragstart', h.onDragStart);
        h.root.removeEventListener('dragend', h.onDragEnd);
        h.root.removeEventListener('dragover', h.onDragOver);
        h.root.removeEventListener('drop', h.onDrop);
        this._handlers.delete(containerId);
    }
};
