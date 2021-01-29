var copies = 0;
function doPopover(elementId, content) {
    var element = document.getElementById(elementId);
    if (!element) {
        return;
    }
    var popover = document.createElement("DIV");
    popover.classList.add("popover_box");
    element.appendChild(popover);
    copies++;
    if (copies > 1)
    {
        if (copies > 8)
        {
            popover.innerHTML += copies + "x " + content;
        }
        else
        {
            popover.innerHTML += ['Double', 'Triple', 'Quaduple', 'Super', 'Omega', 'Extreme', 'Godly'][copies - 2] + ' ' + content;
        }
    }
    else
    {
        popover.innerHTML = content;
    }
    setTimeout(function() { popover.remove(); copies--; }, 2000);
}
