// Base64 içeriği tarayıcıda dosya olarak indirir (rapor Excel çıktısı için).
window.downloadFile = (fileName, contentType, base64) => {
    const link = document.createElement('a');
    link.href = `data:${contentType};base64,${base64}`;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
