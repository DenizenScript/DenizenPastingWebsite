function flashElement(elementName) {
    var element = document.getElementById(elementName);
    element.classList.add("flash");
    setTimeout(function() { element.classList.remove("flash"); }, 2100);
}
function autoflash() {
    var target = window.location.hash.substring(1);
    if (!target) {
        return;
    }
    var flashable = document.getElementById(target);
    if (!flashable) {
        return;
    }
    var flasher = document.createElement("SPAN");
    flasher.id = "flasher_tool_generated";
    flasher.classList.add("flasher_tool");
    flashable.appendChild(flasher);
    flashElement("flasher_tool_generated");
    window.scroll(0, flashable.getBoundingClientRect().top + window.pageYOffset - window.innerHeight / 2);
}
