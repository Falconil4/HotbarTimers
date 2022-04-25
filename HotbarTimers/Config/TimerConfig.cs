using System;

namespace HotbarTimers
{
    [Serializable]
    public class TimerConfig
    {
        public string Job { get; set; }
        public string Status { get; set; }
        public string Skill { get; set; }
        public bool Enabled { get; set; } = true;
        public bool SelfOnly { get; set; } = true;

        public TimerConfig(string job, string buff, string skill, bool enabled, bool selfOnly)
        {
            Status = buff;
            Skill = skill;
            Enabled = enabled;
            Job = job;
            SelfOnly = selfOnly;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || !obj.GetType().Equals(this.GetType())) return false;

            var config = (TimerConfig)obj;
            return Job == config.Job && Status == config.Status && Skill == config.Skill;
        }

        public override int GetHashCode()
        {
            return Job.GetHashCode() + Status.GetHashCode() + Skill.GetHashCode();
        }
    }
}
