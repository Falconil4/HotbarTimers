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
    }
}
