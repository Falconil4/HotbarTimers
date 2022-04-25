using System.Linq;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using TimersOnSkills.Models;
using Status = Dalamud.Game.ClientState.Statuses.Status;

namespace TimersOnSkills
{
    class TimersManager
    {
        private ClientState ClientState { get; init; }
        private TargetManager TargetManager { get; init; }
        private PlayerCharacter? Player { get; init; }
        private ExcelSheet<Action>? GameActionsList { get; init; }
        private List<ActionBarSkill> ActionBarSkills = new List<ActionBarSkill>();
        private List<Status> CurrentStatuses = new List<Status>();

        public TimersManager(ClientState clientState, TargetManager targetManager, DataManager dataManager)
        {
            ClientState = clientState;
            TargetManager = targetManager;
            
            Player = ClientState.LocalPlayer;
            GameActionsList = dataManager.GetExcelSheet<Action>();
        }

        public void OnActionBarUpdate(Configuration configuration)
        {
            ActionBarSkills = ActionBarSkillBuilder.Build(GameActionsList);
            ManageTimers(configuration);        }

        public void OnFrameworkUpdate(Configuration configuration)
        {
            CurrentStatuses = StatusesBuilder.GetCurrentStatuses(Player!, TargetManager);
            ManageTimers(configuration);
        }

        public void OnConfigSave(Configuration configuration)
        {
            foreach (ActionBarSkill skill in ActionBarSkills)
            {
                skill.Hide();
            }
        }

        public void ManageTimers(Configuration configuration)
        {
            string? job = Player?.ClassJob?.GameData?.Abbreviation?.RawString;
            if (job == null) return;
            List<TimerConfig> applicableTimers = configuration.TimerConfigs
                .Where(timer => timer.Enabled && timer.Job == job).ToList();

            Dictionary<ActionBarSkill, Status?> skillsToChange = new Dictionary<ActionBarSkill, Status?>();
            foreach (TimerConfig timerConfig in applicableTimers)
            {
                List<ActionBarSkill> skills = ActionBarSkills.FindAll(x => x.Name == timerConfig.Skill);
                Status? currentStatus = CurrentStatuses.Find(
                    status => status.GameData.Name == timerConfig.Status &&
                    (!timerConfig.SelfOnly || status.SourceID == Player!.ObjectId)
                );
                
                foreach (ActionBarSkill skill in skills)
                {
                    if (!skillsToChange.ContainsKey(skill)) skillsToChange.Add(skill, currentStatus);
                    else
                    {
                        if (skillsToChange[skill] == null) skillsToChange[skill] = currentStatus;
                    }
                }

                foreach(KeyValuePair<ActionBarSkill, Status?> skillToChange in skillsToChange)
                {
                    ActionBarSkill skill = skillToChange.Key;
                    Status? status = skillToChange.Value;

                    if (status != null) skill.Show(status.RemainingTime, status.StackCount);
                    else skill.Hide();
                }
            }
        }
    }
}
