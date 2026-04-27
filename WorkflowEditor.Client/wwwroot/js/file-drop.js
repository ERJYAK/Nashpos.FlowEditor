// Минимальная обёртка над HTML5 drag-drop. .NET-сторона:
//   await JS.InvokeVoidAsync("fileDrop.attach", elementId, DotNetObjectReference.Create(this));
// Метод [JSInvokable("OnFileDropped")] async Task OnFileDropped(string name, string text)
// получает имя и содержимое файла. Очистка — fileDrop.detach(elementId).
window.fileDrop = {
    _handlers: new Map(),

    attach(elementId, dotnetRef) {
        const el = document.getElementById(elementId);
        if (!el) return;

        const onDragOver = e => { e.preventDefault(); };
        const onDrop = async e => {
            e.preventDefault();
            const file = e.dataTransfer && e.dataTransfer.files && e.dataTransfer.files[0];
            if (!file) return;
            try {
                const text = await file.text();
                await dotnetRef.invokeMethodAsync('OnFileDropped', file.name, text);
            } catch (err) {
                await dotnetRef.invokeMethodAsync('OnFileDropFailed', file.name, String(err));
            }
        };

        el.addEventListener('dragover', onDragOver);
        el.addEventListener('drop', onDrop);
        this._handlers.set(elementId, { el, onDragOver, onDrop, dotnetRef });
    },

    detach(elementId) {
        const h = this._handlers.get(elementId);
        if (!h) return;
        h.el.removeEventListener('dragover', h.onDragOver);
        h.el.removeEventListener('drop', h.onDrop);
        this._handlers.delete(elementId);
    }
};
