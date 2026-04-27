// Минимальная обёртка над HTML5 drag-drop с поддержкой multi-file.
// .NET-сторона:
//   await JS.InvokeVoidAsync("fileDrop.attach", elementId, DotNetObjectReference.Create(this));
// Методы [JSInvokable]:
//   OnBatchStart(int count)   — перед обработкой набора файлов;
//   OnFileDropped(string name, string text) — для каждого файла;
//   OnFileDropFailed(string name, string error) — при ошибке чтения файла.
// Очистка: fileDrop.detach(elementId).
window.fileDrop = {
    _handlers: new Map(),

    attach(elementId, dotnetRef) {
        const el = document.getElementById(elementId);
        if (!el) return;

        const onDragOver = e => { e.preventDefault(); };

        const onDrop = async e => {
            e.preventDefault();
            const files = Array.from((e.dataTransfer && e.dataTransfer.files) || []);
            if (files.length === 0) return;

            try {
                await dotnetRef.invokeMethodAsync('OnBatchStart', files.length);
            } catch { /* окно могло закрыться */ }

            for (const file of files) {
                try {
                    const text = await file.text();
                    await dotnetRef.invokeMethodAsync('OnFileDropped', file.name, text);
                } catch (err) {
                    await dotnetRef.invokeMethodAsync('OnFileDropFailed', file.name, String(err));
                }
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
