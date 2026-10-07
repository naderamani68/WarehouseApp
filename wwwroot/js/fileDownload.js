window.downloadFileFromBase64 = async (fileName, base64) => {
    // تبدیل base64 به Uint8Array
    const binaryString = atob(base64);
    const bytes = new Uint8Array(binaryString.length);
    for (let i = 0; i < binaryString.length; i++) {
        bytes[i] = binaryString.charCodeAt(i);
    }

    const mimeType = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';

    // 🟢 تلاش برای استفاده از File System Access API (انتخاب مسیر)
    if (window.showSaveFilePicker) {
        try {
            const handle = await window.showSaveFilePicker({
                suggestedName: fileName,
                types: [{
                    description: 'Excel Workbook',
                    accept: {
                        [mimeType]: ['.xlsx']
                    }
                }]
            });

            const writable = await handle.createWritable();
            await writable.write(bytes);
            await writable.close();
            return; // موفق - دیگه ادامه نده
        } catch (err) {
            // اگه کاربر Cancel زد، بی‌صدا برگرد
            if (err.name === 'AbortError') {
                return;
            }
            // در غیر این صورت، برو سراغ fallback
            console.warn('showSaveFilePicker failed, falling back:', err);
        }
    }

    // 🟡 Fallback: دانلود معمولی (مرورگرهای قدیمی‌تر)
    const blob = new Blob([bytes], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    setTimeout(() => URL.revokeObjectURL(url), 1000);
};