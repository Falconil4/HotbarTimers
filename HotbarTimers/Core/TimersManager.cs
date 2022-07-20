using System;
using System.Collections.Generic;
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

        private DateTime LastActionBarUpdate = DateTime.Now;
        private DateTime LastFrameworkUpdate = DateTime.Now;
        private const int TimeBetweenActionBarUpdates = 2000;
        private const int TimeBetweenFrameworkUpdates = 100;

        public void OnActionBarUpdate(bool force = false)
        {
            if (!force)
            {
                TimeSpan timeSinceLastUpdate = DateTime.Now - LastActionBarUpdate;
                if (timeSinceLastUpdate.TotalMilliseconds < TimeBetweenActionBarUpdates) return;
                LastActionBarUpdate = DateTime.Now;
            }

            var actionBarSkills = ActionBarSkillBuilder.Build();
            if (!force && actionBarSkills.SequenceEqual(ActionBarSkills)) return;

            ActionBarSkills.ForEach(skill => skill.Dispose());
            ActionBarSkills = actionBarSkills;
             
            ApplicableTimers = GetApplicableTimers();
            ApplicableSkills = GetApplicableSkills();
        }

        public void OnFrameworkUpdate()
        {
            if (HotbarTimers.Player != null)
            {
                TimeSpan timeSinceLastUpdate = DateTime.Now - LastFrameworkUpdate;
                if (timeSinceLastUpdate.TotalMilliseconds < TimeBetweenFrameworkUpdates) return;
                LastFrameworkUpdate = DateTime.Now;

                ApplicableStatuses = GetApplicableStatuses();
                UpdateTimers();
            }
        }

        public void OnConfigSave()
        {
            OnActionBarUpdate(true);
            UpdateTimers();
        }

        private List<TimerConfig> GetApplicableTimers()
        {
            string? job = HotbarTimers.Player?.ClassJob?.GameData?.Abbreviation?.RawString;
            if (job == null || HotbarTimers.Configuration == null) return new();
            return HotbarTimers.Configuration.TimerConfigs.Where(timer => timer.Enabled && timer.Job == job).ToList();
        }

        private List<ActionBarSkill> GetApplicableSkills()
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

        private void UpdateTimers()
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
                else if (skill.Visible) skill.Hide();
            }
        }

        public void Dispose()
        {
            ApplicableSkills.ForEach(skill => skill.Dispose());
        }
    }
}
