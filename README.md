<p align="center">
  <img src="AutoFateGrind/Images/Icon.png" width="180" alt="FATE Autopilot icon" />
</p>

<h1 align="center">FATE Autopilot</h1>

<p align="center">
  <a href="https://github.com/Sojiroh/FFXIV-FATEAutopilot/releases/latest"><img alt="Release" src="https://img.shields.io/github/v/release/Sojiroh/FFXIV-FATEAutopilot?style=flat-square&color=blue"></a>
  <a href="https://github.com/Sojiroh/FFXIV-FATEAutopilot/releases"><img alt="Downloads" src="https://img.shields.io/github/downloads/Sojiroh/FFXIV-FATEAutopilot/total?style=flat-square&color=blue&cacheSeconds=300"></a>
  <a href="https://github.com/Sojiroh/FFXIV-FATEAutopilot/actions/workflows/release.yml"><img alt="Build" src="https://img.shields.io/github/actions/workflow/status/Sojiroh/FFXIV-FATEAutopilot/release.yml?style=flat-square"></a>
  <a href="LICENSE.md"><img alt="License" src="https://img.shields.io/badge/license-AGPL--3.0--or--later-blue?style=flat-square"></a>
</p>

<p align="center">
  <em>FATEs, farmed for you. Built on Dalamud.</em>
</p>

---

> **Fork notice.** FATE Autopilot is a fork maintained by Sojiroh. It is not affiliated with or endorsed by XeldarAlz. Report problems here, not to the original author.
>
> ```text
> Based on Auto FATE Grind by XeldarAlz
> https://github.com/XeldarAlz/FFXIV-AutoFATEGrind
> ```

## What it does

Lists every FATE zone from A Realm Reborn through Dawntrail in one window. Tick the zones you want, pick a goal, press **Start**, and the plugin teleports to each one, scans for active FATEs, flies to them, engages, and rotates to the next selected zone when the current one runs dry.

## Features

- **Zone picker**: any FATE zone from ARR through DT, with live active-FATE counts, a search box and a drag-to-reorder grind order.
- **Goals**: farm Bicolor Gemstones to a target or just grind FATEs; max the Shared FATE rank of every selected Shadowbringers-or-later zone; collect relic and item goals (Atma, Luminous Crystals, Memories of the Dying, Haunting and Vexatious Memories, Demiatma, Crystal Paste); or farm Yo-kai Watch medals.
- **Run limits** that work with any goal: stop after a number of FATEs, a length of time or a number of levels, with an optional number of FATEs per zone, and a choice of what happens afterwards (stay logged in, log out, return to the inn, close the game, plus your own chat commands).
- **FATE filters and priority**: skip by type, time left, progress, level window or long FATEs, blacklist FATEs, reorder how the next FATE is chosen, and hold zone swaps while Twist of Fate is up.
- **Travel**: teleports to the zone or flies within it (Economy mode), picks the mount you choose instead of the roulette, can mount while waiting for FATEs, swaps zones when one is empty, and teleports out when a run is stuck. Optionally pulls as many mobs as possible with a tank.
- **Collect FATEs**: picks the items up and hands them in.
- **Live FATE tracker**: shown inline or as a separate HUD overlay, with the FATEs it passed over and why.
- **Class queue**: cycles gearsets in order with per-class level caps.
- **Auto-trade**: spends Bicolor Gemstones at the trader once you reach your threshold, from a shopping list, spending everything, up to an amount or a fixed quantity.
- **Auto-repair**: Dark Matter first, Grand Company mender (or another NPC) as fallback.
- **Auto-consume**: keeps food and medicine buffs up (Well Fed is a free +3% EXP), HQ first.
- **Humanizer**: takes random city breaks (or idles at a spot you saved) between FATEs and varies its timing and dodging, so long sessions look less mechanical.
- **Party invites**: auto-declines incoming invites during a run after a random delay, with an optional reply message.
- **GM alert**: stops the run when a GM is near, with optional toast, beeps or custom commands.
- **Resilient**: cancellable mid-run (Ctrl+click Stop to finish the current FATE first), and your selection persists across reloads.
- A single app window with Grind, Settings, History, Plugins, Console, Changelog and About pages.
- Nine interface languages, picked automatically on first run and changeable under Settings, General.

## Languages

English, Français, Deutsch, Türkçe, Español, Português (Brasil), Русский, 日本語, 中文.

The plugin picks one on first run from your Dalamud interface language, then your game client, then your OS, falling back to English. Change it any time under **Settings → General → Language**.

## What changed

This fork differs from the original Auto FATE Grind as follows:

- Renamed from Auto FATE Grind to **FATE Autopilot**.
- New internal name `FateAutopilot`, so it installs alongside the original and does not reuse its settings or its lifetime run history. Configure it again after switching.
- Commands are now `/fta` and `/fateautopilot`, and log and chat lines are tagged `[FTA]`.
- Published through Sojiroh's custom repository.
- The original author's Discord, Patreon and Buy Me a Coffee links were removed.
- New icon.

## Install

In-game: `/xlsettings` → **Experimental** → paste into **Custom Plugin Repositories**:

```
https://sojiroh.github.io/SojirohPlugins/pluginmaster.json
```

Tick **Enabled**, click **+**, then **Save and Close**. Open `/xlplugins` → **All Plugins**, search for **FATE Autopilot**, and install.

The plugin needs a few helpers for movement and combat to be installed and loaded: **vnavmesh** (pathfinding and movement to FATEs), **BossMod** or BossMod Reborn (combat rotation, targeting and dodging) and **TextAdvance** (talk-skip during Collect FATE turn-ins). Open `/fta deps` after install to see the list and one-click each missing one.

## Commands

| Command            | Action                                                              |
| ------------------ | ------------------------------------------------------------------- |
| `/fta`             | Toggle the main window                                              |
| `/fateautopilot`   | Alias for `/fta`                                                    |
| `/fta start`       | Start a run with the current plan                                   |
| `/fta stop`        | Stop the run                                                        |
| `/fta stop soft`   | Stop after the current FATE                                         |
| `/fta run <count>` | Run for that many FATEs, then stop                                  |
| `/fta pause`       | Pause or resume the run                                             |
| `/fta config`      | Open settings                                                       |
| `/fta deps`        | Open the dependencies window                                        |
| `/fta stats`       | Open the run history                                                |
| `/fta log`         | Open the Console page                                               |
| `/fta changelog`   | Open the Changelog page                                             |
| `/fta about`       | Open credits and links                                              |
| `/fta target`      | Log the targeted NPC's BaseId (debug helper)                        |

## License

AGPL-3.0-or-later. See [LICENSE.md](LICENSE.md). See [NOTICE](NOTICE) for the original copyright and the additional terms the AGPL allows: a fork, or any project that reuses this code, must credit the original author and must not pass itself off as the original. The license covers the code, not the name or the icon: read the [trademark and naming policy](TRADEMARK.md) before you publish a fork.

## About this source

The original repository (`XeldarAlz/FFXIV-AutoFATEGrind`) and the `XeldarAlz/DalamudPlugins` repository were taken down in late October 2026. This fork starts from a reconstruction of release **2.22.0.0** (upstream commit `ca02237`), recovered from the installed plugin build:

- `AutoFateGrind/` was decompiled from `AutoFateGrind.dll` with ILSpy and rewritten by hand. Type, member, parameter and constant names are the originals. Local variable names that the compiler hoists into closures and async state machines are the originals too; the others were not in the binary and were chosen from usage. Comments and XML docs are lost. Where the original was split into partial classes (`AutoFate`, `AutoCommon`, `AutoFateController`), the files were recovered from the compiler-recorded source paths, and member and file layout follows the compiled metadata order.
- Before the rebrand, built with the official .NET SDK 10.0.402, 3,524 of 3,525 method bodies compiled to the same IL as the shipped 2.22.0.0 DLL (the exception, `ZoneAetherytes.ResolveGateway`, differs only in the numbering of two compiler temporaries). Other SDK builds (e.g. a distro-packaged SDK) can lay out branches differently but behave the same.
- `Localization/` and `Fonts/` come from the shipped build. The localization files have the fork's command names and drop the removed Discord and support strings. The icon is new.
- `ECommons` is pinned to `509c89b`, the commit the shipped `ECommons.dll` was built from. The `clib` helper library comes from NuGet (`croizat.clib` `1.0.104`).
- The README was rebuilt from the Wayback Machine capture of 26 June 2026 (release 1.3.1.0, the last capture of the repository page) and updated from the plugin's own texts and changelog for 2.22.0.0; the demo GIF was not recovered. `NOTICE` and `TRADEMARK.md` were rewritten from the same author's templates in FFXIV-AutoPVPSeriesGrind to carry the attribution and naming terms of the original project; the archived `NOTICE` only states the AGPL notice.

Build with `dotnet build -c Release` from the repository root (needs the .NET 10 SDK and a Dalamud dev install: `~/.xlcore/dalamud/Hooks/dev` on Linux, or set `DALAMUD_HOME`). The packaged plugin lands in `AutoFateGrind/bin/Release/FateAutopilot/latest.zip`.
