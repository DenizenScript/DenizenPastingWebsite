function loadprivateinfo(url) {
    fetch(url, { method: 'POST' }).then(res => res.json()).then(obj => {
        var parsed = JSON.parse(obj); // This should already be a JSON parsed object (because res.json()) but something borks and it's a string until parsed
        document.getElementById('staff_private_submitter').textContent = parsed.submitter ?? "Unknown";
        document.getElementById('staff_private_info').style.display = "block";
        var parts = parsed.filtered;
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
