// Test-only helper, not part of iHUD. Skips the menus and loads straight into a
// character/world, for quick in-game testing. Copied from Ashlands Reborn's
// DevAutoLoadPatches. It only runs on a launch armed by local/dev-test.ps1 (a
// one-shot flag file it deletes on startup), and dev-test.ps1 -Cleanup removes
// the plugin from the profile again.
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace iHUDDevAutoLoad;

[BepInPlugin("ihud.devautoload", "iHUD DevAutoLoad (test only)", "1.0.0")]
public class DevAutoLoadPlugin : BaseUnityPlugin
{
    private const string FlagFile = "ihud-devautoload.flag";
    private static readonly TimeSpan FlagMaxAge = TimeSpan.FromMinutes(10);

    private enum State { WaitForMainMenu, WaitForCharacterScreen, WaitForWorldScreen, Done }

    private State state = State.Done;
    private string character = "Dove";
    private string world = "Reborn";

    private void Awake()
    {
        // Consume the flag right away so only this launch autoloads.
        try
        {
            string path = Path.Combine(Paths.BepInExRootPath, FlagFile);
            if (!File.Exists(path))
            {
                Logger.LogInfo("No launch flag - not autoloading");
                return;
            }

            TimeSpan age = DateTime.UtcNow - File.GetLastWriteTimeUtc(path);
            string[] parts = File.ReadAllText(path).Trim().Split('|');
            File.Delete(path);

            if (age > FlagMaxAge)
            {
                Logger.LogInfo($"Ignoring stale launch flag ({age.TotalMinutes:F0} min old)");
                return;
            }

            if (parts.Length == 2 && parts[0].Length > 0 && parts[1].Length > 0)
            {
                character = parts[0];
                world = parts[1];
            }

            state = State.WaitForMainMenu;
            Logger.LogInfo($"Launch flag found - autoloading {character} / {world}");
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Launch flag check failed: " + ex.Message);
        }
    }

    private void Update()
    {
        if (state == State.Done)
            return;

        FejdStartup instance = FejdStartup.instance;
        if (instance == null)
            return;

        try
        {
            switch (state)
            {
                case State.WaitForMainMenu: MainMenu(instance); break;
                case State.WaitForCharacterScreen: CharacterScreen(instance); break;
                case State.WaitForWorldScreen: WorldScreen(instance); break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Autoload failed: " + ex);
            state = State.Done;
        }
    }

    private void MainMenu(FejdStartup instance)
    {
        // 1.0.20 plays an intro cinematic with the main menu hidden.
        if (CinematicsManager.s_instance != null && CinematicsManager.IsStartedPlaying())
        {
            CinematicsManager.Stop();
            return;
        }

        if (instance.m_mainMenu == null || !instance.m_mainMenu.activeInHierarchy)
            return;

        Call(instance, "OnStartGame");
        state = State.WaitForCharacterScreen;
    }

    private void CharacterScreen(FejdStartup instance)
    {
        if (instance.m_characterSelectScreen == null ||
            !instance.m_characterSelectScreen.activeInHierarchy)
        {
            return;
        }

        if (!(AccessTools.Field(typeof(FejdStartup), "m_profiles").GetValue(instance) is IList profiles) ||
            profiles.Count == 0)
        {
            return;
        }

        for (int i = 0; i < profiles.Count; i++)
        {
            object profile = profiles[i];
            string? name = profile.GetType()
                .GetMethod("GetName", BindingFlags.Public | BindingFlags.Instance)?
                .Invoke(profile, null) as string;

            if (string.Equals(name, character, StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogInfo($"Selecting character '{character}'");
                AccessTools.Field(typeof(FejdStartup), "m_profileIndex").SetValue(instance, i);
                Call(instance, "OnCharacterStart");
                state = State.WaitForWorldScreen;
                return;
            }
        }

        Logger.LogWarning($"Character '{character}' not found - stopping");
        state = State.Done;
    }

    private void WorldScreen(FejdStartup instance)
    {
        if (instance.m_startGamePanel == null || !instance.m_startGamePanel.activeInHierarchy)
            return;

        if (!(AccessTools.Field(typeof(FejdStartup), "m_worlds").GetValue(instance) is IList worlds) ||
            worlds.Count == 0)
        {
            return;
        }

        foreach (object w in worlds)
        {
            string? name = w.GetType()
                .GetField("m_name", BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(w) as string;

            if (string.Equals(name, world, StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogInfo($"Selecting world '{world}' - starting game");
                AccessTools.Field(typeof(FejdStartup), "m_world").SetValue(instance, w);
                Call(instance, "OnWorldStart");
                state = State.Done;
                return;
            }
        }

        Logger.LogWarning($"World '{world}' not found - stopping");
        state = State.Done;
    }

    private static void Call(FejdStartup instance, string method) =>
        AccessTools.Method(typeof(FejdStartup), method).Invoke(instance, null);
}
