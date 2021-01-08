function setPasteType(name) {
    document.title = "New " + name + " Paste | Denizen Pastebin";
    window.history.pushState('/New/' + name, document.title, '/New/' + name);
}
var pasteArea = document.getElementById('pastecontents');
var spacer = document.getElementById('paste_spacer');
function resize() {
    pasteArea.style.height = 'auto';
    pasteArea.style.height = (pasteArea.scrollHeight + 200) + 'px';
    spacer.style.height = pasteArea.style.height;
}
function delayedResize() {
    window.setTimeout(resize, 0);
}
pasteArea.addEventListener('change', resize);
pasteArea.addEventListener('cut', delayedResize);
pasteArea.addEventListener('paste', delayedResize);
pasteArea.addEventListener('drop', delayedResize);
pasteArea.addEventListener('keydown', delayedResize);
pasteArea.addEventListener('keydown', function(e) {
    if (e.key == 'Tab') {
        e.preventDefault();
        var start = this.selectionStart;
        this.value = this.value.substring(0, start) + "    " + this.value.substring(this.selectionEnd);
        this.selectionStart = start + 4;
        this.selectionEnd = this.selectionStart;
    }
});
resize();
