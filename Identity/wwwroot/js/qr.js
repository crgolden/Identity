window.addEventListener("load", () => {
    const uri = document.getElementById("qrCodeData").dataset.url;
    new QRCode(document.getElementById("qrCode"),
        {
            text: uri,
            width: 150,
            height: 150
        });
});