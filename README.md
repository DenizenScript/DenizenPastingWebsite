DenizenPastingWebsite
---------------------

Paste website (like pastebin), primarily for Denizen scripts and server logs.

### How To Install/Run

Designed for and tested on Debian Linux.

- Make sure you have `screen` and `dotnet-6-sdk` available
- Add a user for the service (via `adduser` generally, then `su` to that user)
- Clone the git repo (`git clone https://github.com/DenizenScript/DenizenPastingWebsite`) and enter the folder
- Make a folder labeled `config`, inside it make a text file labeled `config.fds`, and fill it with the config sample below (and change values to fit your configuration needs).
- Call `./update.sh`
- Will by default open on port 8096. To change this, edit `start.sh`
- It is strongly recommended you run this webserver behind a reverse proxy like Apache2.

### Configuration

```yml
# Maximum paste size (in number of characters). Pastes longer than this will be trimmed.
max-paste-size: 5000000
# Set to 'true' if running behind a reverse-proxy, 'false' if directly exposed.
trust-x-forwarded-for: true
# Set to the base URL for the paste service.
url-base: https://paste.denizenscript.com
# How many pastes from a single origin can come through per minute (a simple flood protection tool). If set to 0, the paste website is effectively read-only.
max-pastes-per-minute: 3
# Optionally specify a list of webhooks to run when new pastes are sent. Webhook content will be a simple JSON-formatted payload with key "content" sent to simple displayable text.
webhooks:
    new-paste:
    - https://example.com/webhook
# Optionally specify a list of (case-insensitive) text to check new pastes for to trigger automatic spam blocking.
spam-block-keyphrases:
- some naughty thing
# Optionally specify a list of (case-insensitive) text only short (less than 20 line) pastes for.
spam-block-short-keyphrases:
- some phrase
# Optionally specify a list of (case-insensitive) title text (in full) to auto-block if detected.
spam-block-titles:
- some bad title
# Optionally specify a list of (case-insensitive) title text to auto-block if detected contained in a paste title.
spam-block-partial-titles:
- some bad title
```

### Licensing pre-note:

This is an open source project, provided entirely freely, for everyone to use and contribute to.

If you make any changes that could benefit the community as a whole, please contribute upstream.

### The short of the license is:

You can do basically whatever you want, except you may not hold any developer liable for what you do with the software.

### The long version of the license follows:

The MIT License (MIT)

Copyright (c) 2021-2022 The Denizen Script Team

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
