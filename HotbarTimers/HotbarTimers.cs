using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System.Diagnostics;

namespace HotbarTimers
{
    public sealed unsafe class HotbarTimers : IDalamudPlugin
    {
        public string Name => "Hotbar Timers";
        private const string commandName = "/hotbartimers";

        private ConfigurationUI ConfigurationUi { get; init; }
        private TimersManager TimersManager { get; init; }

        public static DalamudPluginInterface? PluginInterface { get; private set; }
        public static CommandManager? CommandManager { get; private set; }
        public static Framework? Framework { get; private set; }
        public static ClientState? ClientState { get; private set; }
        public static TargetManager? TargetManager { get; private set; }
        public static ExcelSheet<ClassJob>? GameJobsList { get; private set; }
        public static ExcelSheet<Action>? GameActionsList { get; private set; }
        public static ExcelSheet<Status>? GameStatusList { get; private set; }
        public static Configuration? Configuration { get; set; }
        public static PlayerCharacter? Player { get; private set; }


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
            ClientState = clientState;
            TargetManager = targetManager;
            GameJobsList = dataManager.GetExcelSheet<ClassJob>();
            GameActionsList = dataManager.GetExcelSheet<Action>();
            GameStatusList = dataManager.GetExcelSheet<Status>();

            if (FFXIVClientStructs.Resolver.Initialized == false) FFXIVClientStructs.Resolver.Initialize();

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);
            ConfigurationUi = new ConfigurationUI(Configuration, OnConfigSave);

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

            TimersManager = new TimersManager();
        }

        private void OnFrameworkUpdate(Framework framework)
        {
            if (Player == null) Player = ClientState?.LocalPlayer;
            TimersManager?.OnFrameworkUpdate();
        }

        private byte ActionBarUpdateDetour(AddonActionBarBase* atkUnitBase, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
        {
            //Debug.Print("ActionBarUpdate event");
            TimersManager.OnActionBarUpdate();
            return ActionBarHook.Original(atkUnitBase, numberArrayData, stringArrayData);
        }

        private void OnCommand(string command, string args) => DrawConfigUI();
        private void OnConfigSave(Configuration configuration) => TimersManager.OnConfigSave();

        private void DrawConfigUI()
        {
            this.ConfigurationUi.SettingsVisible = true;
        }

        public void Dispose()
        {
            Framework!.Update -= OnFrameworkUpdate;
            ConfigurationUi.Dispose();
            CommandManager?.RemoveHandler(commandName);

            ActionBarHook.Disable();
            ActionBarHook.Dispose();

            TimersManager.Dispose();
        }
    }
}
