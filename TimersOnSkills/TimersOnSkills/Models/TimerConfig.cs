using System;

namespace TimersOnSkills.Models
{
    [Serializable]
    public class TimerConfig
    {
        public string Status { get; set; }
        public string Skill { get; set; }
        public bool Enabled { get; set; } = true;
        public string Job { get; set; }

        public TimerConfig(string buff, string skill, bool enabled, string job)
        {
            Status = buff;
            Skill = skill;
            Enabled = enabled;
            Job = job;
        }
    }
}
