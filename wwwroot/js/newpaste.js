function setPasteType(name) {
    document.title = "New " + name + " Paste | Denizen Pastebin";
    window.history.pushState('/New/' + name, document.title, '/New/' + name);
}
document.getElementById('pastecontents').addEventListener('keydown', function(e) {
    if (e.key == 'Tab') {
        e.preventDefault();
        var start = this.selectionStart;
        this.value = this.value.substring(0, start) + "    " + this.value.substring(this.selectionEnd);
        this.selectionStart = start + 4;
        this.selectionEnd = this.selectionStart;
    }
});
