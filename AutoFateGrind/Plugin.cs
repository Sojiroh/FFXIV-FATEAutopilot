using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using AutoFateGrind.Core;
using AutoFateGrind.Core.Debug;
using AutoFateGrind.Core.Game.SharedFates;
using AutoFateGrind.Core.Game.Watchers;
using AutoFateGrind.Core.Ipc;
using AutoFateGrind.Core.Localization;
using AutoFateGrind.Core.Stats;
using AutoFateGrind.Core.Tasks;
using AutoFateGrind.Core.Zones;
using AutoFateGrind.Windows;
using AutoFateGrind.Windows.Shell;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.DalamudServices;
using clib;

namespace AutoFateGrind;

public sealed class Plugin : IDalamudPlugin, IDisposable
{
    private readonly GmAlertWatcher gmAlertWatcher;

    private readonly PartyInviteWatcher partyInviteWatcher;

    private readonly DutyWatcher dutyWatcher;

    private readonly AppWindow appWindow;

    private readonly CommandInfo primaryCommand;

    private readonly CommandInfo aliasCommand;

    private readonly EventHandler<UnobservedTaskExceptionEventArgs> unobservedTaskHandler;

    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    internal static IPluginLog Log { get; private set; } = null!;

    internal static Plugin Instance { get; private set; } = null!;

    internal Configuration Configuration { get; }

    internal static Configuration Cfg { get; private set; } = null!;

    internal WindowSystem WindowSystem { get; } = new WindowSystem("FateAutopilot");

    internal RunHistory History { get; }

    internal AutoFateController Controller { get; }

    internal LiveFateWindow LiveFateWindow { get; }

    public Plugin()
    {
        Instance = this;
        ECommonsMain.Init(PluginInterface, this);
        CLibMain.Init(PluginInterface, this, CLibModule.Automation);
        SharedFateProgress.Initialize();
        TeleportRefusals.Initialize();
        unobservedTaskHandler = OnUnobservedTaskException;
        TaskScheduler.UnobservedTaskException += unobservedTaskHandler;
        Configuration = (PluginInterface.GetPluginConfig() as Configuration) ?? new Configuration();
        Cfg = Configuration;
        if (Configuration.MigrateGoal())
        {
            Configuration.Save();
        }
        if (Configuration.MigrateTradeList())
        {
            Configuration.Save();
        }
        if (CityCatalog.MigrateSelection(Configuration.HumanizerCities))
        {
            Configuration.Save();
        }
        History = new RunHistory();
        Controller = new AutoFateController();
        gmAlertWatcher = new GmAlertWatcher();
        partyInviteWatcher = new PartyInviteWatcher();
        dutyWatcher = new DutyWatcher();
        InitializeLocalization();
        GameTextGlyphs.Collect(Configuration, History);
        Fonts.Initialize(PluginInterface.UiBuilder, PluginDirectory);
        appWindow = new AppWindow(this);
        LiveFateWindow = new LiveFateWindow(this)
        {
            IsOpen = Configuration.ShowLivePopout
        };
        WindowSystem.AddWindow(appWindow);
        WindowSystem.AddWindow(LiveFateWindow);
        primaryCommand = new CommandInfo(OnCommand)
        {
            HelpMessage = Loc.T(L.Plugin.CommandHelp)
        };
        aliasCommand = new CommandInfo(OnCommand)
        {
            HelpMessage = Loc.T(L.Plugin.CommandHelpAlias)
        };
        CommandManager.AddHandler(AfgConstants.PrimaryCommand, primaryCommand);
        CommandManager.AddHandler(AfgConstants.AliasCommand, aliasCommand);
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        Svc.ClientState.Login += OnLogin;
        if (Svc.ClientState.IsLoggedIn)
        {
            OnLogin();
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (e.Observed)
        {
            return;
        }
        if (e.Exception.ToString().Contains("Navmesh.IPCProvider"))
        {
            e.SetObserved();
            RunLog.Debug("Observed vnavmesh IPC task fault: " + e.Exception.GetBaseException().Message);
        }
    }

    public void Dispose()
    {
        TaskScheduler.UnobservedTaskException -= unobservedTaskHandler;
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        Svc.ClientState.Login -= OnLogin;
        BossModFateHelper.ReleaseChocobo();
        BossModMovementTuning.Release();
        WindowSystem.RemoveAllWindows();
        appWindow.Dispose();
        LiveFateWindow.Dispose();
        Fonts.Dispose();
        CommandManager.RemoveHandler(AfgConstants.PrimaryCommand);
        CommandManager.RemoveHandler(AfgConstants.AliasCommand);
        gmAlertWatcher.Dispose();
        partyInviteWatcher.Dispose();
        dutyWatcher.Dispose();
        TeleportRefusals.Shutdown();
        SharedFateProgress.Shutdown();
        CLibMain.Dispose();
        ECommonsMain.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        var trimmed = args.Trim();
        var spaceIndex = trimmed.IndexOf(' ');
        var subcommand = ((spaceIndex < 0) ? trimmed : trimmed.Substring(0, spaceIndex)).ToLowerInvariant();
        var subArgs = ((spaceIndex < 0) ? string.Empty : trimmed.Substring(spaceIndex + 1).Trim());
        switch (subcommand)
        {
            case "config":
                ToggleConfigUi();
                break;
            case "about":
                ToggleAboutUi();
                break;
            case "deps":
            case "dependencies":
                ToggleDependenciesUi();
                break;
            case "stats":
            case "history":
                ToggleHistoryUi();
                break;
            case "log":
                ToggleLogUi();
                break;
            case "changelog":
                ToggleChangelogUi();
                break;
            case "resume":
            case "pause":
                Controller.TogglePause();
                break;
            case "target":
                TargetDumper.Dump();
                break;
            case "start":
                StartFromCommand();
                break;
            case "stop":
                if (subArgs.Equals("soft", StringComparison.OrdinalIgnoreCase))
                {
                    Controller.StopWhenSafe();
                }
                else
                {
                    Controller.Stop();
                }
                break;
            case "softstop":
                Controller.StopWhenSafe();
                break;
            case "run":
                RunCountFromCommand(subArgs);
                break;
            default:
                ToggleMainUi();
                break;
        }
    }

    private void StartFromCommand()
    {
        if (Controller.Running)
        {
            Svc.Chat.Print("[FTA] A run is already going. Use /fta stop or /fta stop soft first.");
            return;
        }
        var readOnlyList = ZoneSelection.ResolveStartList(Configuration);
        if (readOnlyList.Count == 0)
        {
            Svc.Chat.PrintError("[FTA] Nothing to start: pick zones in the window first.");
        }
        else
        {
            Controller.RunAll(readOnlyList);
        }
    }

    private void RunCountFromCommand(string countText)
    {
        if (!int.TryParse(countText, out var count) || count <= 0)
        {
            Svc.Chat.PrintError("[FTA] Usage: /fta run <count>, for example /fta run 30.");
            return;
        }
        Configuration.StopAfterFatesEnabled = true;
        Configuration.TargetFateCount = Math.Clamp(count, 1, 9999);
        Configuration.Save();
        if (Controller.Running)
        {
            Svc.Chat.Print($"[FTA] Limit set: this run stops after {Configuration.TargetFateCount} FATEs.");
        }
        else
        {
            StartFromCommand();
        }
    }

    public void OnLanguageChanged()
    {
        primaryCommand.HelpMessage = Loc.T(L.Plugin.CommandHelp);
        aliasCommand.HelpMessage = Loc.T(L.Plugin.CommandHelpAlias);
    }

    private static string PluginDirectory => PluginInterface.AssemblyLocation.DirectoryName ?? string.Empty;

    private static void InitializeLocalization()
    {
        var localizationDirectory = Path.Combine(PluginDirectory, "Localization");
        if (string.IsNullOrEmpty(Cfg.Language))
        {
            Cfg.Language = DetectLanguage();
            Cfg.Save();
        }
        Loc.Initialize(Cfg.Language, localizationDirectory);
    }

    private static string DetectLanguage()
    {
        var uiLanguage = PluginInterface.UiLanguage;
        if (Languages.IsKnown(uiLanguage))
        {
            return Languages.Resolve(uiLanguage).Code;
        }
        switch (Svc.ClientState.ClientLanguage)
        {
            case ClientLanguage.German:
                return Languages.German.Code;
            case ClientLanguage.French:
                return Languages.French.Code;
            case ClientLanguage.Japanese:
                return Languages.Japanese.Code;
            default:
                var twoLetterISOLanguageName = CultureInfo.InstalledUICulture.TwoLetterISOLanguageName;
                if (!Languages.IsKnown(twoLetterISOLanguageName))
                {
                    return Languages.English.Code;
                }
                return Languages.Resolve(twoLetterISOLanguageName).Code;
        }
    }

    private void OnLogin()
    {
        if (!Configuration.AutoShowOnLogin)
        {
            return;
        }
        appWindow.Show(AppWindow.Page.Grind);
    }

    public void ToggleMainUi() => appWindow.Toggle();

    public void ToggleConfigUi() => appWindow.TogglePage(AppWindow.Page.Settings);

    public void ToggleAboutUi() => appWindow.TogglePage(AppWindow.Page.About);

    public void ToggleDependenciesUi() => appWindow.TogglePage(AppWindow.Page.Plugins);

    public void ToggleHistoryUi() => appWindow.TogglePage(AppWindow.Page.History);

    public void ToggleLogUi() => appWindow.TogglePage(AppWindow.Page.Log);

    public void ToggleChangelogUi() => appWindow.TogglePage(AppWindow.Page.Changelog);
}
