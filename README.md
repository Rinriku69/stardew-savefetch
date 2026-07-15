# SaveFetch

A [SMAPI](https://smapi.io) mod for Stardew Valley that uploads a summary of your save to a
companion website every time the game saves. Log in once through your browser — after that it
works silently in the background.

## How it works

1. Run `savefetch_login` in the SMAPI console. Your default browser opens the website's login
   page (the same redirect flow as `gh auth login` — your password never touches the game or
   its logs).
2. After you log in, the site hands an access token back to the mod through a temporary
   `127.0.0.1` listener. The token is stored in the game's `.smapi/mod-data/` folder, **not**
   in `config.json`, so it can't leak by sharing your config.
3. From then on, every time the game finishes saving, the mod builds a JSON summary of the
   save and POSTs it to the site's API on a background thread — the game never freezes on a
   slow connection.

If you're not logged in, the mod does nothing except print a reminder at launch.

## Installation

1. Install [SMAPI](https://smapi.io) 4.0+ (game version 1.6+).
2. Unzip the release into your `Stardew Valley/Mods` folder.
3. Launch the game once so `config.json` is generated, then set `BaseUrl` to the website's URL.
4. Run `savefetch_login` in the SMAPI console and log in through your browser.

## Configuration (`config.json`)

| Setting        | Default                | What it does                                                                 |
| -------------- | ---------------------- | ---------------------------------------------------------------------------- |
| `BaseUrl`      | `https://example.test` | Root URL of the companion website. **You must change this** — the default is a placeholder. |
| `CallbackPort` | `0`                    | Fixed TCP port for the login callback listener. `0` picks any free port automatically; set a fixed one only if you need a firewall rule or are testing by hand. |

The access token is deliberately **not** in this file — see step 2 above.

## Console commands

| Command            | What it does                                            |
| ------------------ | ------------------------------------------------------- |
| `savefetch_login`  | Open your browser to log in to the website.             |
| `savefetch_status` | Show who's logged in, the server URL, and the last upload result. |
| `savefetch_logout` | Forget the token and stop uploading saves.              |

## What gets uploaded

A curated summary, not the raw save file: farmer/farm name, save ID, in-game date, days
played, playtime, money (current + lifetime), the five skill levels, and a few stats (items
crafted/cooked, fish caught, monsters killed), plus game/mod version and a timestamp. See
[`SavePayload.cs`](SavePayload.cs) for the exact fields.

## Building from source

Requires the **.NET 6 SDK** (the mod must target `net6.0` — that's what the game and SMAPI
run on, so don't bump it).

```
dotnet build
```

That's the whole workflow: the [`Pathoschild.Stardew.ModBuildConfig`](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig)
package finds your game install, references the game/SMAPI assemblies, copies the build into
your `Mods` folder, and produces a release zip under `bin/Debug/net6.0/`.

## Server API

The website must expose two routes:

- `GET /mod-auth?port=&state=` — web route: log the user in via the normal web session, issue
  a personal access token, then redirect to
  `http://127.0.0.1:{port}/callback?token=...&username=...&state=...` (the `state` value must
  be echoed back unchanged — the mod rejects the callback otherwise).
- `POST /api/saves` — API route accepting the JSON payload with `Authorization: Bearer {token}`.

## Testing without a server

1. Set a fixed `CallbackPort` (e.g. `8722`) in the deployed `config.json`.
2. Run `savefetch_login` — the console prints the login URL it tried to open; copy the `state`
   value from it.
3. Simulate the site's redirect by opening
   `http://127.0.0.1:8722/callback?token=test&username=you&state={that state}` in a browser.
4. Load a save and sleep — the console logs the upload attempt (it fails against a fake
   `BaseUrl`, which still proves the save → payload → HTTP path works).
