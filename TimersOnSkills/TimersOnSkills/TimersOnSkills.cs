using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Data;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Hooking;

namespace TimersOnSkills
{
    public sealed unsafe class TimersOnSkills : IDalamudPlugin
    {
        public string Name => "Timers on Skills";

        private const string commandName = "/timersonskills";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private ConfigurationUI ConfigurationUi { get; init; }
        private TimersManager TimersManager { get; init; }
        private Framework Framework { get; init; }

        private delegate byte ActionBarUpdate(AddonActionBarBase* atkUnitBase, NumberArrayData** numberArrayData, StringArrayData** stringArrayData);
        private string Signature = "E8 ?? ?? ?? ?? 83 BB ?? ?? ?? ?? ?? 75 09";
        private Hook<ActionBarUpdate> ActionBarHook;

        public TimersOnSkills(
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
            this.TimersManager = new TimersManager(clientState, targetManager, dataManager);
            
            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            this.ConfigurationUi = new ConfigurationUI(this.Configuration, dataManager, clientState, OnConfigSave);
            
            this.CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Access settings"
            });

            this.PluginInterface.UiBuilder.Draw += this.ConfigurationUi.Draw;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            
            var scanner = new SigScanner(true);
            var address = scanner.ScanText(Signature);
            ActionBarHook = new Hook<ActionBarUpdate>(address, ActionBarUpdateDetour);
            ActionBarHook.Enable();

            Framework.Update += OnFrameworkUpdate;
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
            TimersManager.OnConfigSave(configuration);
        }

        private void DrawConfigUI()
        {
            this.ConfigurationUi.SettingsVisible = true;
        }
    }
}
