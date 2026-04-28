// Тонкая обёртка над Monaco Editor, подключаемым по AMD-loader'у с jsdelivr CDN.
// Blazor вызывает create / getValue / setValue / dispose через JSInterop.
// На каждое изменение содержимого editor вызывает .NET-метод OnEditorValueChanged
// у переданного DotNetObjectReference, чтобы Blazor-компонент мог обновить state.
window.monacoInterop = (function () {
    const editors = new Map();
    let monacoReadyPromise = null;

    function ensureMonaco() {
        if (monacoReadyPromise) return monacoReadyPromise;

        monacoReadyPromise = new Promise((resolve, reject) => {
            const baseUrl = 'https://cdn.jsdelivr.net/npm/monaco-editor@0.46.0/min/vs';

            // loader.js регистрирует глобальный `require` (AMD).
            const loaderScript = document.createElement('script');
            loaderScript.src = `${baseUrl}/loader.min.js`;
            loaderScript.onerror = () => reject(new Error('Failed to load Monaco loader.min.js'));
            loaderScript.onload = () => {
                window.require.config({ paths: { vs: baseUrl } });

                // Monaco web-workers через CDN требуют proxy.
                window.MonacoEnvironment = {
                    getWorkerUrl: function () {
                        return `data:text/javascript;charset=utf-8,${encodeURIComponent(`
                            self.MonacoEnvironment = { baseUrl: '${baseUrl}/' };
                            importScripts('${baseUrl}/base/worker/workerMain.js');
                        `)}`;
                    }
                };

                window.require(['vs/editor/editor.main'], () => resolve(window.monaco));
            };
            document.head.appendChild(loaderScript);
        });

        return monacoReadyPromise;
    }

    return {
        async create(elementId, value, language, dotnetRef) {
            const monaco = await ensureMonaco();
            const el = document.getElementById(elementId);
            if (!el) return;
            // Если повторно зовут на тот же id — переиспользуем.
            if (editors.has(elementId)) {
                const existing = editors.get(elementId);
                if (existing.getValue() !== value) existing.setValue(value || '');
                return;
            }

            const editor = monaco.editor.create(el, {
                value: value || '',
                language: language || 'javascript',
                automaticLayout: true,
                minimap: { enabled: false },
                scrollBeyondLastLine: false,
                fontSize: 13,
                theme: 'vs-dark',
                wordWrap: 'on'
            });
            editor.onDidChangeModelContent(() => {
                if (dotnetRef) {
                    dotnetRef.invokeMethodAsync('OnEditorValueChanged', editor.getValue());
                }
            });
            editors.set(elementId, editor);
        },
        getValue(elementId) {
            const e = editors.get(elementId);
            return e ? e.getValue() : '';
        },
        setValue(elementId, value) {
            const e = editors.get(elementId);
            if (e && e.getValue() !== (value || '')) e.setValue(value || '');
        },
        dispose(elementId) {
            const e = editors.get(elementId);
            if (e) {
                e.dispose();
                editors.delete(elementId);
            }
        }
    };
})();
