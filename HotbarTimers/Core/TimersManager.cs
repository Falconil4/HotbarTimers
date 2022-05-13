using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using System.Linq;
using Status = Dalamud.Game.ClientState.Statuses.Status;

namespace HotbarTimers
{
    class TimersManager
    {
        private ClientState ClientState { get; init; }
        private TargetManager TargetManager { get; init; }
        private ExcelSheet<Action>? GameActionsList { get; init; }
        private List<ActionBarSkill> ActionBarSkills = new();
        private List<Status> CurrentStatuses = new();

        private List<TimerConfig> ApplicableTimers = new();
        private List<ActionBarSkill> ApplicableSkills = new();

        public TimersManager(ClientState clientState, TargetManager targetManager, DataManager dataManager)
        {
            ClientState = clientState;
            TargetManager = targetManager;
            
            GameActionsList = dataManager.GetExcelSheet<Action>();
        }

        public void OnActionBarUpdate(Configuration configuration, bool rebuild = false)
        {
            ActionBarSkills = ActionBarSkillBuilder.Build(GameActionsList, configuration, rebuild);
            ApplicableTimers = GetApplicableTimers(configuration);
            ApplicableSkills = GetApplicableSkills();

            ManageTimers();        
        }

        public void OnFrameworkUpdate()
        {
            if (ClientState.LocalPlayer != null)
            {
                CurrentStatuses = StatusesBuilder.GetCurrentStatuses(ClientState.LocalPlayer, TargetManager);
                ManageTimers();
            }
        }

        public void OnConfigSave(Configuration configuration)
        {
            OnActionBarUpdate(configuration, true);
            foreach (ActionBarSkill skill in ActionBarSkills)
            {
                skill.Hide();
            }

            ApplicableTimers = GetApplicableTimers(configuration);
            ApplicableSkills = GetApplicableSkills();
        }

        private List<TimerConfig> GetApplicableTimers(Configuration configuration)
        {
            string? job = ClientState.LocalPlayer?.ClassJob?.GameData?.Abbreviation?.RawString;
            if (job == null) return new();
            return configuration.TimerConfigs.Where(timer => timer.Enabled && timer.Job == job).ToList();
        }

        private List<ActionBarSkill> GetApplicableSkills()
        {
            List<ActionBarSkill> skills = new();
            foreach (TimerConfig timerConfig in ApplicableTimers)
            {
                skills.AddRange(ActionBarSkills.FindAll(x => x.Name == timerConfig.Skill));
            }
            return skills;
        }

        public void ManageTimers()
        {
            uint? playerId = ClientState.LocalPlayer?.ObjectId;
            if (playerId == null) return;
            Dictionary<ActionBarSkill, Status?> skillsToChange = new();
            
            foreach (TimerConfig timerConfig in ApplicableTimers)
            {
                Status? currentStatus = CurrentStatuses.Find(
                    status => status.GameData.Name == timerConfig.Status &&
                    (!timerConfig.SelfOnly || status.SourceID == playerId)
                );
                List<ActionBarSkill> currentSkills = ApplicableSkills.FindAll(s => s.Name == timerConfig.Skill);
                
                foreach (ActionBarSkill skill in currentSkills)
                {
                    if (!skillsToChange.ContainsKey(skill)) skillsToChange.Add(skill, currentStatus);
                    else
                    {
                        if (skillsToChange[skill] == null) skillsToChange[skill] = currentStatus;
                    }
                }
            }

            foreach (KeyValuePair<ActionBarSkill, Status?> skillToChange in skillsToChange)
            {
                ActionBarSkill skill = skillToChange.Key;
                Status? status = skillToChange.Value;

                if (status != null) skill.Show(status.RemainingTime, status.StackCount);
                else skill.Hide();
            }
        }

        public void Dispose()
        {
            foreach(ActionBarSkill skill in ActionBarSkills)
            {
                skill.Dispose();
            }
        }
    }
}
