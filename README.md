DenizenPastingWebsite
---------------------

Paste website (like pastebin), primarily for Denizen scripts and server logs.

Primary server this is intended for: https://paste.denizenscript.com

### Features

- Server-side syntax pre-highlighting for a few core types.
- Clientside (JavaScript) syntax highlighting for a wide range of common languages.
- Highly configurable.
- Hackability: If the config isn't enough, this pastebin is fully open source and welcomes you to do whatever you want with it, including easily adding different paste types or whatever else.
- Spam bot detection and blocking: not perfect but it works, and more importantly provides tools for you to do your own blocking with as-needed.
- Permanent pastes: this paste server doesn't delete your pastes over time as many others do.
- Privacy filter capability: this server can apply privacy filters to some paste types (in the main version, only for Server Logs), hiding private information within a paste for only staff to see.
- Webhook output to log incoming new pastes (intended to output to a Discord channel but works for other things).
- Admin tools (via Discord OAuth login) for blocking spambots, viewing filtered out information, etc.
- Simple but efficient local data storage of pastes ([LiteDB](https://www.litedb.org/))

### How To Install/Run

Designed for and tested on a Debian Linux server.

Usage on other Linux distros is likely very similar. Usage outside Linux may require independent research regarding how to install DotNet 6, and how to run a generic executable service perpetually.

- Make sure you have `screen` and `dotnet-6-sdk` available
- Add a user for the service (via `adduser` generally, then `su` to that user)
- Clone the git repo (`git clone https://github.com/DenizenScript/DenizenPastingWebsite`) and enter the folder
- Make a folder labeled `config`, inside it make a text file labeled `config.fds`, and fill it with the config sample below (and change values to fit your configuration needs).
- Call `./update.sh`
- Will by default open on port 8096. To change this, edit `start.sh`
- It is strongly recommended you run this webserver behind a reverse proxy like Apache2 or Nginx.

For testing on Windows, `start.ps1` is also available to run via powershell.


### Testing on Windows

- Install [DotNET 6](https://dotnet.microsoft.com/download/dotnet/6.0)
- Make a folder labeled `config`, inside it make a text file labeled `config.fds`, and fill it with the config sample below (and change values to fit your configuration needs).
- Open this folder in VS Code
- Use the terminal at the bottom of VS Code, run `.\start.ps1`
- Open `localhost:8096` in your browser

### Console Commands Available

- `stop`: close the server.
- `rebuild`: Internal LiteDB rebuild.
- `flush`: Internal LiteDB checkpoint flush.
- `resubmit_all`: Resubmits all pastes internally. Useful to apply data format changes if relevant.
- `render_type (type)`: rerenders all pastes of a given type. Useful to apply render output changes if relevant.
- `remove_bot_post (id)`: marks a paste as a spambot paste and removes it from public view (the on-page admin button is preferred).

### Configuration

```yml
### GENERAL

# Maximum paste size (in number of characters). Pastes longer than this will be trimmed. Reference value is 5 million, or approximately 5 megabytes.
max-paste-size: 5000000
# Whether to test the "X-Forwarded-For" web header.
# Set to 'true' if running behind a reverse-proxy (like Apache2 or Nginx), 'false' if directly exposed.
trust-x-forwarded-for: true
# Set to the base URL for the paste service.
url-base: https://example.com
# How many pastes from a single origin can come through per minute (a simple flood protection tool). If set to 0, the paste website is effectively read-only. Set to '99999' if you want effectively unlimited pastes.
max-pastes-per-minute: 3
# Optionally specify a list of webhooks to run when new pastes are sent. Webhook content will be a simple JSON-formatted payload with key "content" set to simple displayable text (intended for use as a Discord webhook).
webhooks:
    new-paste:
    - https://example.com/webhook

### SPAM BLOCKING
# New pastes that match these tests will automatically be blocked.

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

### OAUTH
# The paste service can optionally use Discord as a staff login tool, using Discord OAuth, and a role to mark staff in your Discord guild.

discord_oauth:
    # To use Discord OAuth2, you must register an application at https://discord.com/developers/applications
    # Change to 'true' if in use.
    enabled: false
    # Discord client ID. Generated on Discord's OAuth2 page.
    client-id: 123
    # Discord client secret Generated on Discord's OAuth2 page.
    client-secret: abc
    # Discord redirect URL. Must be added under "Redirects" on the OAuth2 page.
    # In most cases: The "/Auth/DiscordAuthConfirm" portion should be left as-is and the base URL should match 'url-base'.
    redirect-url: https://example.com/Auth/DiscordAuthConfirm
    # ID of the Discord guild relevant to this paste server, used for roles check.
    guild-id: 123
    # Guild role ID(s) that identity the user as an admin of the paste site.
    guild-roles-admin:
    - 123

### TERMS OF SERVICE
# This will show up at "/Info/Terms"

# Contact information. HTML allowed.
tos_contact: Ask on Discord @ <a href="https://discord.gg/Q6pZGSR">https://discord.gg/Q6pZGSR</a> or send an email to <code>webmaster@example.com</code>.
# Text body of Terms of Service. HTML allowed.
tos_text: Legal stuff here, etc. Terms regarding takedown policy, etc. Probably include something like:<br>Pastes sent as spam or for advertising or "SEO" reasons will result in the uploader being blocked (if/when discovered).<br>Large numbers of pastes from a single user for any purpose may be ratelimited or blocked either automatically or manually.
# To customize any other part of the terms page, edit the file "Views/Info/Terms.cshtml"
```

### Licensing pre-note:

This is an open source project, provided entirely freely, for everyone to use and contribute to.

If you make any changes that could benefit the community as a whole, please contribute upstream.

### The short of the license is:

You can do basically whatever you want, except you may not hold any developer liable for what you do with the software.

### The long version of the license follows:

The MIT License (MIT)

Copyright (c) 2021-2023 The Denizen Script Team

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
