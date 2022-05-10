using Dalamud.Data;
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
        private Framework Framework { get; init; }
        private Configuration Configuration { get; init; }
        private ConfigurationUI ConfigurationUi { get; init; }
        private TimersManager TimersManager { get; init; }
        

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
            PluginInterface = pluginInterface;
            CommandManager = commandManager;
            Framework = framework;
            if (FFXIVClientStructs.Resolver.Initialized == false) FFXIVClientStructs.Resolver.Initialize();

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);
            ConfigurationUi = new ConfigurationUI(Configuration, dataManager, clientState, OnConfigSave);
            TimersManager = new TimersManager(clientState, targetManager, dataManager);

            CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Access settings"
            });

            PluginInterface.UiBuilder.Draw += this.ConfigurationUi.Draw;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
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
            return ActionBarHook.Original(atkUnitBase, numberArrayData, stringArrayData);
        }

        private void OnCommand(string command, string args) => DrawConfigUI();
        private void OnConfigSave(Configuration configuration) => TimersManager.OnConfigSave(configuration);

        private void DrawConfigUI()
        {
            this.ConfigurationUi.SettingsVisible = true;
        }

        public void Dispose()
        {
            ConfigurationUi.Dispose();
            CommandManager.RemoveHandler(commandName);
            Framework.Update -= OnFrameworkUpdate;

            ActionBarHook.Disable();
            ActionBarHook.Dispose();

            TimersManager.Dispose();
        }
    }
}
