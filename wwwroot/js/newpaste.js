// Paste type related stuff
function setAddr(addr) {
    window.history.pushState(addr, document.title, addr);
    document.getElementById('submitpost').action = addr;
}
var privacyFilterDiv = document.getElementById("log_privacy_filter_options");
var lastSelection = "other-csharp";
function setPasteType(name) {
    lastSelection = name;
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
function autoSetOtherType() {
    var selectedOption = document.getElementById('other_type_selection').selectedOptions[0];
    var selectedName = selectedOption.getAttribute("name");
    lastSelection = selectedName;
    var selectedDisplayName = selectedOption.text;
    document.title = "New Other: " + selectedDisplayName + " Paste | Denizen Pastebin";
    if (selectedName != "other-none") {
        setAddr('/New/Other?selected=' + selectedName);
    }
}
function otherTypeEntryClick() {
    var selectedOption = document.getElementById('other_type_selection').selectedOptions[0];
    var selectedName = selectedOption.getAttribute("name");
    if (selectedName != lastSelection) {
        document.getElementById('pastetype_other').click();
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
for (var option of document.getElementById('other_type_selection').children) {
    option.addEventListener('click', otherTypeEntryClick);
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
function tryConfirmSubmit(message) {
    document.getElementById('submit_modal_text').textContent = message;
    $('#submit_confirm_modal').modal('show');
}
var manualModalYes = false;
document.getElementById('modal_no_button').addEventListener('click', function(e) {
    $('#submit_confirm_modal').modal('hide');
    e.preventDefault();
    e.stopPropagation();
    return false;
}, true);
document.getElementById('modal_yes_button').addEventListener('click', function(e) {
    manualModalYes = true;
    document.getElementById('newpaste_submit_button').click();
    e.preventDefault();
    e.stopPropagation();
    return false;
}, true);
function giveErrorReason(type, cleanName, content, requiredToFail) {
    if (type != "Log" && (content.includes("[Server thread/INFO]: ") || content.includes("Starting minecraft server version "))) {
        tryConfirmSubmit(`Are you sure this is a ${cleanName} paste? It looks like a server log. You should probably click 'Cancel' and select 'Server log' and submit it properly as a log.`);
    }
    else if (type != "other-xml" && (content.includes("<?xml version=") || content.includes("<project xmlns="))) {
        tryConfirmSubmit(`Are you sure this is a ${cleanName} paste? It looks like a Maven pom file. You should probably click 'Cancel' and select 'Other' then 'HTML or XML' and submit it properly as XML.`);
    }
    else if (type != "Script" && (content.includes("  type: task") || content.includes("  type: world") || content.includes("  type: assignment"))) {
        tryConfirmSubmit(`Are you sure this is a ${cleanName} paste? It looks like a Denizen Script. You should probably click 'Cancel' and select 'Denizen Script' and submit it properly as a script.`);
    }
    else if (type != "other-properties" && (content.includes("max-players=") || content.includes("level-type=") || content.includes("allow-flight="))) {
        tryConfirmSubmit(`Are you sure this is a ${cleanName} paste? It looks like a Minecraft server.properties file. You should probably click 'Cancel' and select 'Other' then 'Properties file' and submit it properly as a properties file.`);
    }
    else if (requiredToFail) {
        tryConfirmSubmit(`Are you sure this is a ${cleanName} paste? It doesn't look like one. Consider clicking 'Cancel' and selecting a more appropriate type.`);
    }
    else {
        return false;
    }
    return true;
}
document.getElementById('newpaste_submit_button').addEventListener('click', function(e) {
    console.log(`Trying to submit a ${lastSelection}`);
    if (manualModalYes) {
        return;
    }
    if (lastSelection == "other-none") {
        alert("Invalid type selected. Please select an actual paste type.");
    }
    else if (lastSelection == "Script") {
        if (pasteArea.value.includes("  type: ")) {
            return;
        }
        giveErrorReason(lastSelection, "Denizen Script", pasteArea.value, true);
    }
    else if (lastSelection == "Log") {
        if (pasteArea.value.split('\n').length < 20) {
            tryConfirmSubmit(`Are you sure this is a server log? It looks a bit short. Are you sure you copied the *FULL* 'latest.log' file, not just a snippet?`);
        }
        else if (pasteArea.value.includes("[Server thread/INFO]: ") || pasteArea.value.includes("Starting minecraft server version ")) {
            return;
        }
        else if (!giveErrorReason(lastSelection, "Minecraft Server Log", pasteArea.value, false)) {
            tryConfirmSubmit(`Are you sure this is a server log? It doesn't look like one. Consider clicking 'Cancel' and selecting a more appropriate type. Or, you might not have included the full log content.`);
        }
    }
    else if (lastSelection == "other-java") {
        if (pasteArea.value.includes("package ") || pasteArea.value.includes("class ") || pasteArea.value.includes(" void ")) {
            return;
        }
        giveErrorReason(lastSelection, "Java Source Code", pasteArea.value, true);
    }
    else if (lastSelection == "Text" || lastSelection == "BBCode") {
        if (!giveErrorReason(lastSelection, "Text", pasteArea.value, false)) {
            return;
        }
    }
    else {
        if (!giveErrorReason(lastSelection, lastSelection, pasteArea.value, false)) {
            return;
        }
    }
    e.preventDefault();
    e.stopPropagation();
    return false;
}, true);
