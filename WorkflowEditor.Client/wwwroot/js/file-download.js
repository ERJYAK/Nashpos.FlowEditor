// Скачивание файла через Blob/URL.createObjectURL — стандартный браузерный способ.
window.fileDownload = {
    save(filename, content, mime) {
        const blob = new Blob([content], { type: mime || 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }
};
