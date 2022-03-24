// Paste type related stuff
function setAddr(addr) {
    window.history.pushState(addr, document.title, addr);
    document.getElementById('submitpost').action = addr;
}
var privacyFilterDiv = document.getElementById("log_privacy_filter_options");
function setPasteType(name) {
    document.title = "New " + name + " Paste | Denizen Pastebin";
    if (name.toLowerCase() == "other") {
        autoSetOtherType();
    }
    else {
        setAddr('/New/' + name);
    }
    if (name.toLowerCase() == "log") {
        privacyFilterDiv.style.visibility = "visible";
    }
    else {
        privacyFilterDiv.style.visibility = "hidden";
        Array.from(privacyFilterDiv.getElementsByClassName("btn-check")).forEach(element => {
            element.checked = false;
        });
    }
    resize();
}
var lastSelection = "other-csharp";
function autoSetOtherType() {
    var selectedOption = document.getElementById('other_type_selection').selectedOptions[0];
    var selectedName = selectedOption.getAttribute("name");
    lastSelection = selectedName;
    var selectedDisplayName = selectedOption.text;
    document.title = "New Other: " + selectedDisplayName + " Paste | Denizen Pastebin";
    setAddr('/New/Other?selected=' + selectedName);
}
function otherTypeEntryClick() {
    var selectedOption = document.getElementById('other_type_selection').selectedOptions[0];
    var selectedName = selectedOption.getAttribute("name");
    if (selectedName != lastSelection) {
        document.getElementById('other_button').click();
    }
}
const urlParams = new URLSearchParams(window.location.search);
var selectedOtherType = urlParams.get('selected');
if (selectedOtherType === null) {
    var origType = document.getElementById('orig_type').value;
    if (origType.startsWith('other-')) {
        selectedOtherType = origType;
    }
}
if (selectedOtherType !== null) {
    console.log(selectedOtherType);
    document.getElementById('other_type_selection').children[selectedOtherType].selected = 'selected';
    autoSetOtherType();
}
// Paste area related stuff
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
