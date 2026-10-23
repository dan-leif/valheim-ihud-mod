# CLAUDE.md

The user's fork of **iHUD** (Immersive HUD) by MMaxG, MIT license: github.com/dan-leif/valheim-ihud-mod
(public, as forks of public repos are). Remotes: `origin` = the fork, `upstream` = MMaxG/valheim-ihud-mod.
Pull upstream changes with `git fetch upstream && git merge upstream/master` (or GitHub's
"Sync fork"). Keep upstream's `LICENSE`. If a modified version is ever published, give it a
different name and credit MMaxG.

The whole mod is one file: `iHUD/iHUD.cs` (~3,700 lines). It only changes the alpha of vanilla
HUD elements via Harmony patches (`Hud.UpdateHealth/UpdateFood/UpdateStatusEffects/UpdateCrosshair/
UpdateGuardianPower`, `HotkeyBar.UpdateIcons`, `Minimap.Update`, `Hud.Awake`, ...).

## Why the fork exists (the pending change)

The user finds it hard to tell whether they're **Resting** or **Rested**, and wants to change
that. They haven't decided how yet, so ask before picking a design. What the code does today
(`StatusEffectController`, the `[HarmonyPatch(typeof(Hud), "UpdateStatusEffects")]` class):

- **Resting** has no duration (`m_ttl` 0), so it's treated like any infinite effect: shown when
  gained, then faded after `Status Effects → Hide Delay` (5 s default, applies to ALL effects).
  `UpdateComfort()` re-shows it only when `Player.GetComfortLevel()` changes. While sitting by the
  fire, the icon is hidden: you can't see the countdown to Rested, or tell you're still resting.
- **Rested** is timed: shown when first gained, popups at 20/15/10/5 min left
  (`Popup Intervals Seconds`), stays visible under `Critical Time` (60 s). When resting again
  refreshes its timer, `UpdateEffectState` just clears `Critical`; it does NOT re-show the icon.
- Effects are matched by `StatusEffect.name` (`"Resting"`; `"Rested"` presumably, verify in-game).
- Workarounds without code: the inventory (Tab) shows everything (`Show All On Inventory`), and
  H toggles iHUD off and on.

Ideas discussed, none chosen: keep Resting fully visible while it exists; re-show Rested whenever
its remaining time goes UP (a refresh).

## Build and test

- Upstream's `iHUD/iHUD.csproj` doesn't build here (needs the BepInEx NuGet feed + a `D:\` path).
  Use the local one, which compiles the same `iHUD/iHUD.cs`:
  ```
  dotnet build -c Release local/iHUD.local.csproj
  ```
  It references the live game DLLs (`C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed`)
  and BepInEx from the r2modman profile `%APPDATA%\r2modmanPlus-local\Valheim\profiles\Ashlands Reborn`,
  then copies `iHUD.dll` over `BepInEx\plugins\MMaxG-iHUD\iHUD\iHUD.dll` in that profile while
  "MMaxG-iHUD" is switched on in r2modman. **Close the game first**: a running game locks the DLL.
- The profile's iHUD was imported from a package built from upstream `ea5a69f` (copy kept in
  `C:\DEV\ashlands-reborn\third-party\ihud\`). Overwriting the DLL is fine for testing; the r2modman
  switch keeps working. For a keeper version, bump the version and re-import a package (zip with
  forward-slash paths: `manifest.json`, `icon.png`, `README.md`, `plugins/iHUD/iHUD.dll`).
- Config: `...\Ashlands Reborn\BepInEx\config\iHUD.cfg`. The user tunes iHUD settings
  themselves in game (F1); don't change their values.
- Log: `...\Ashlands Reborn\BepInEx\LogOutput.log` (iHUD logs `[iHUD]` lines; a missing reflection
  target logs a warning).
- Verified on Valheim 1.0.20 / BepInExPack 5.4.2351 (upstream tested 1.0.15). Neither of the
  user's other mods (Ashlands Reborn, Runestone Compass) touches the HUD, so there's no overlap.

## Quick in-game test (skips the menus)

`powershell -ExecutionPolicy Bypass -File local\dev-test.ps1` builds and installs iHUD, installs a
test-only plugin (`local/DevAutoLoad`, copied from Ashlands Reborn's DevAutoLoad), arms a one-shot
flag file, and launches through Steam straight into Dove / Reborn (log line "Selecting world"
appears ~10-20 s after launch). **When done testing, close the game and run it with `-Cleanup`**:
the user wants the menu skip gone from the profile afterwards. Ashlands Reborn's own autoload
only works while that mod is switched on in r2modman, so don't rely on it.

## Launching Valheim (shared with the user's other mods)

- `<game>\BepInEx` is a directory junction into the r2modman profile (created by
  `C:\DEV\ashlands-reborn\dev.ps1`). Remove it only with `cmd /c rmdir`, never a recursive delete.
- Launch with `steam://rungameid/892970` (Valheim needs Steam). **Never use computer-use's
  open-application for Valheim**: it starts an extra copy outside Steam. Bring the existing window
  forward instead (SetForegroundWindow), then click.
- 1.0.20 plays an intro video before the menu; Esc skips it.
- Test character/world: **Dove** / **Reborn**. "Eagle" is the user's real character, so don't use it for tests.
- Resting needs a fire + roof nearby; in the Reborn dev world, `devcommands` then build a quick
  shelter with `nocost`, or test from a base.
