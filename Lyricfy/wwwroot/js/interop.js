window.captureAndCopy = (elementId) => {
    var element = document.getElementById(elementId);
    html2canvas(element, {
        useCORS : true
    }).then(function(canvas) {

        var dataUrl = canvas.toDataURL('image/png');
        var blob = dataURLToBlob(dataUrl);
        var url = URL.createObjectURL(blob);
        window.open(url, '_blank');
    });
};

function dataURLToBlob(dataurl) {
    var parts = dataurl.split(','), mime = parts[0].match(/:(.*?);/)[1];
    if(parts[0].indexOf('base64') !== -1) {
        var byteString = atob(parts[1]);
    } else {
        var byteString = unescape(parts[1]);
    }
    var ab = new ArrayBuffer(byteString.length);
    var ia = new Uint8Array(ab);
    for(var i = 0; i < byteString.length; i++) {
        ia[i] = byteString.charCodeAt(i);
    }
    return new Blob([ab], {type: mime});
}
var brightness = 0;
window.getHexValue = function() {
    return new Promise((resolve, reject) => {
        const image = document.getElementById('album');
        const colorThief = new ColorThief();
        image.crossOrigin = "Anonymous";
        if (image.complete) {
            let color = colorThief.getColor(image);
            brightness = Math.sqrt(0.299 * Math.pow(color[0], 2) + 0.587 * Math.pow(color[1], 2) + 0.114 * Math.pow(color[2], 2));
            var xd = rgbToHsl(color[0], color[1], color[2]);
            var bgvar = Math.floor(xd[0]) + " " + Math.floor(xd[1]) + " " +  Math.floor(xd[2]);
            resolve(bgvar);
        }
        else {
            image.addEventListener('load', function () {
                let color = colorThief.getColor(image);
                brightness = Math.sqrt(0.299 * Math.pow(color[0], 2) + 0.587 * Math.pow(color[1], 2) + 0.114 * Math.pow(color[2], 2));
                var xd = rgbToHsl(color[0], color[1], color[2]);
                var bgvar = Math.floor(xd[0]) + " " + Math.floor(xd[1]) + " " +  Math.floor(xd[2]);
                resolve(bgvar);
            });
        }
    });
};
window.getBrightness = () => {
    return new Promise((resolve, reject) => { resolve(brightness.toString()) });
};
const rgbToHsl = (r, g, b) => {
    r /= 255, g /= 255, b /= 255;
    let max = Math.max(r, g, b), min = Math.min(r, g, b);
    let h, s, l = (max + min) / 2;

    if(max === min){
        h = s = 0; // achromatic
    } else {
        let d = max - min;
        s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
        switch(max){
            case r: h = (g - b) / d + (g < b ? 6 : 0); break;
            case g: h = (b - r) / d + 2; break;
            case b: h = (r - g) / d + 4; break;
        }
        h /= 6;
    }

    return [h * 360, s * 100, l * 100];
};

