using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using TimersOnSkills.Models;

namespace TimersOnSkills
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public List<TimerConfig> TimerConfigs { get; set; } = new List<TimerConfig>();

        
        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;
        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}
