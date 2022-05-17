using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Status = Dalamud.Game.ClientState.Statuses.Status;

namespace HotbarTimers
{
    class TimersManager
    {
        private List<ActionBarSkill> ActionBarSkills = new();
        
        private List<TimerConfig> ApplicableTimers = new();
        private List<ActionBarSkill> ApplicableSkills = new();
        private List<Status> ApplicableStatuses = new();

        public void OnActionBarUpdate()
        {
            ActionBarSkills.ForEach(skill => skill.Dispose());
            ActionBarSkills = ActionBarSkillBuilder.Build();

            ApplicableTimers = GetApplicableTimers();
            ApplicableSkills = GetApplicableSkills();
            ManageTimers();        
        }

        private DateTime LastFrameworkUpdate = DateTime.Now;
        public void OnFrameworkUpdate()
        {
            if (HotbarTimers.Player != null)
            {
                TimeSpan timeSinceLastUpdate = DateTime.Now - LastFrameworkUpdate;
                if (timeSinceLastUpdate.TotalMilliseconds < 100) return;
                LastFrameworkUpdate = DateTime.Now;

                ApplicableStatuses = GetApplicableStatuses();
                ManageTimers();
            }
        }

        public void OnConfigSave()
        {
            OnActionBarUpdate();
            ActionBarSkills.ForEach(skill => skill.Hide());
            ManageTimers();
        }

        static List<TimerConfig> GetApplicableTimers()
        {
            string? job = HotbarTimers.Player?.ClassJob?.GameData?.Abbreviation?.RawString;
            if (job == null) return new();
            return HotbarTimers.Configuration!.TimerConfigs.Where(timer => timer.Enabled && timer.Job == job).ToList();
        }

        List<ActionBarSkill> GetApplicableSkills()
        {
            List<ActionBarSkill> applicableSkills = new();
            foreach (TimerConfig timerConfig in ApplicableTimers)
            {
                applicableSkills.AddRange(ActionBarSkills.FindAll(x => x.Name == timerConfig.Skill));
            }
            return applicableSkills;
        }

        private List<Status> GetApplicableStatuses()
        {
            List<Status> currentStatuses = StatusesBuilder.GetCurrentStatuses();

            return currentStatuses.Where(status => ApplicableTimers
                    .Any(timer => timer.Status == status.GameData.Name))
                .ToList();
        }

        public void ManageTimers()
        {
            if (HotbarTimers.Player == null) return;
            Dictionary<ActionBarSkill, Status?> skillsToChange = new();
            
            foreach (TimerConfig timerConfig in ApplicableTimers)
            {
                Status? currentStatus = ApplicableStatuses.Find(status => status.GameData.Name == timerConfig.Status);
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
            foreach(ActionBarSkill skill in ApplicableSkills)
            {
                skill.Dispose();
            }
        }
    }
}
