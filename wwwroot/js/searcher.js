
var currentResults = [];

function begin_search() {
    var textElem = document.getElementById('search_text');
    var maxElem = document.getElementById('search_max');
    var submitElem = document.getElementById('search_submit');
    var resultsElem = document.getElementById('search_results_box');
    var searchTerm = textElem.value;
    var searchMax = maxElem.value * 1000;
    for (var elem of [textElem, maxElem, submitElem]) {
        elem.readOnly = true;
        elem.classList.add('disabled');
    }
    resultsElem.innerHTML = "<tr><th>Match #</th><th>Link</th><th>Matched</th><th>Type</th><th>Source</th><th>Date</th><th>Title</th><th>Edited</th></tr>";
    currentResults = [];
    console.log(`Begin search for ${searchTerm} until ${searchMax} pages...`);
    doSearch(searchTerm, searchMax, 0);
}

function setSearchProgress(result) {
    document.getElementById('search_ticker').innerText = result;
}

function searchEnded(text) {
    setSearchProgress(text);
    var textElem = document.getElementById('search_text');
    var maxElem = document.getElementById('search_max');
    var submitElem = document.getElementById('search_submit');
    for (var elem of [textElem, maxElem, submitElem]) {
        elem.readOnly = false;
        elem.classList.remove('disabled');
    }
}

function postJSON(url, callback) {
    var xhr = new XMLHttpRequest();
    xhr.open('POST', url, true);
    xhr.responseType = 'json';
    xhr.onload = function() {
        callback(xhr.status, xhr.response);
    };
    xhr.send();
};

function escapeHtml(raw) {
    return raw.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;').replaceAll("'", '&#039;');
}

function doSearch(searchTerm, searchMax, index) {
    setSearchProgress(`Searching... ${index} / ${(searchMax)}... please wait...`);
    console.log(`Step ${index} on search for ${searchTerm} until ${searchMax} pages...`);
    postJSON(`/Info/PostSearchJson?search-term=${encodeURIComponent(searchTerm)}&search-start-ind=${index}`, function(status, data) {
        if (status != 200) {
            searchEnded(`Search ended early: loading failed or rejected by server. Possibly invalid input?`);
            return;
        }
        if ('error' in data) {
            searchEnded(`Search ended early with message: ${data['error']}`);
            return;
        }
        console.log(data)
        for (var paste of data['result']) {
            if (paste.id == -1) {
                searchEnded(`Searched every paste in database and got ${currentResults.length} result(s)`);
                return;
            }
            currentResults.push(paste.id);
            var termSet = searchTerm.split("|||");
            matchedFor = escapeHtml(termSet[paste.match_id]);
            let text = `<tr><td>#${currentResults.length}</td><td>in <a href="/View/${paste.id}">Paste #${paste.id}</a></td><td>'<code>${matchedFor}</code>'</td>`;
            text += `<td><code>${paste.type}</code></td><td><code>${escapeHtml(paste.source)}</code></td><td><code>${escapeHtml(paste.date)}</code></td><td>'<code>${escapeHtml(paste.title)}</code>'</td>`;
            if (paste.edited > 0) {
                text += `<td><a href="/View/${paste.id}">Paste #${paste.edited}</a></td>`;
            }
            else {
                text += '<td></td>';
            }
            text += '</tr>';
            document.getElementById('search_results_box').innerHTML += text;
        }
        if (currentResults.length > 500) {
            searchEnded(`Search stopped early due to finding 500+ results.`);
            return;
        }
        index += 1000;
        if (index >= searchMax) {
            searchEnded(`Search hit maximum limit with ${currentResults.length} result(s)`);
            return;
        }
        doSearch(searchTerm, searchMax, index);
    });
}
