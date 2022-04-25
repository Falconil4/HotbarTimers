﻿using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HotbarTimers
{
    public sealed unsafe class HotbarTimers : IDalamudPlugin
    {
        public string Name => "Hotbar Timers";

        private const string commandName = "/hotbartimers";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private ConfigurationUI ConfigurationUi { get; init; }
        private TimersManager TimersManager { get; init; }
        private Framework Framework { get; init; }

        private delegate byte ActionBarUpdate(AddonActionBarBase* atkUnitBase, NumberArrayData** numberArrayData, StringArrayData** stringArrayData);
        private readonly string Signature = "E8 ?? ?? ?? ?? 83 BB ?? ?? ?? ?? ?? 75 09";
        private readonly Hook<ActionBarUpdate> ActionBarHook;

        public HotbarTimers(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            ClientState clientState,
            TargetManager targetManager,
            DataManager dataManager,
            Framework framework)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.Framework = framework;
            if (!FFXIVClientStructs.Resolver.Initialized) FFXIVClientStructs.Resolver.Initialize();

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);
            this.ConfigurationUi = new ConfigurationUI(this.Configuration, dataManager, clientState, OnConfigSave);
            this.TimersManager = new TimersManager(clientState, targetManager, dataManager);

            this.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Access settings"
            });

            this.PluginInterface.UiBuilder.Draw += this.ConfigurationUi.Draw;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            Framework.Update += OnFrameworkUpdate;

            var scanner = new SigScanner(true);
            var address = scanner.ScanText(Signature);
            ActionBarHook = new Hook<ActionBarUpdate>(address, ActionBarUpdateDetour);
            ActionBarHook.Enable();
        }

        private void OnFrameworkUpdate(Framework framework)
        {
            TimersManager.OnFrameworkUpdate(Configuration);
        }

        private byte ActionBarUpdateDetour(AddonActionBarBase* atkUnitBase, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
        {
            TimersManager.OnActionBarUpdate(Configuration);
            var res = ActionBarHook.Original(atkUnitBase, numberArrayData, stringArrayData);
            return res;
        }

        public void Dispose()
        {
            this.ConfigurationUi.Dispose();
            this.CommandManager.RemoveHandler(commandName);
            Framework.Update -= OnFrameworkUpdate;

            ActionBarHook.Disable();
            ActionBarHook.Dispose();
        }

        private void OnCommand(string command, string args) => DrawConfigUI();
        
        private void OnConfigSave(Configuration configuration)
        {
            TimersManager.OnConfigSave();
        }

        private void DrawConfigUI()
        {
            this.ConfigurationUi.SettingsVisible = true;
        }
    }
}