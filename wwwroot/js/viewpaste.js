function loadprivateinfo(url) {
    fetch(url, { method: 'POST' }).then(res => res.json()).then(obj => {
        console.log(obj);
        var parsed = JSON.parse(obj); // This should already be a JSON parsed object (because res.json()) but something borks and it's a string until parsed
        document.getElementById('staff_private_submitter').textContent = (parsed.paste.submitter ?? "Unknown");
        var selector = document.getElementById('staff_submitter_status_selector');
        for (var i = 0; i < selector.options.length; i++) {
            if (selector.options[i].value.toLowerCase() == parsed.userStatus.toLowerCase()) {
                selector.options[i].selected = true;
                break;
            }
        }
        document.getElementById('staff_private_info').style.display = "block";
        var parts = parsed.paste.filtered;
        if (parts) {
            for (var i = 0; i < parts.length; i++) {
                var elem = document.getElementById("filtered_block_" + (i + 1));
                if (elem) {
                    elem.className = "private_block";
                    elem.innerText = parts[i];
                }
            }
        }
    });
}
