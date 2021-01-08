function setPasteType(name) {
    document.title = "New " + name + " Paste | Denizen Pastebin";
    window.history.pushState('/New/' + name, document.title, '/New/' + name);
}
