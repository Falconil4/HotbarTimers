using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace HotbarTimers
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public List<TimerConfig> TimerConfigs { get; set; } = new List<TimerConfig>();
        public Vector4 StatusTimerTextColor { get; set; } = new Vector4(1, 1, 1, 255);
        public Vector4 StackCountTextColor { get; set; } = new Vector4(1, 0.5f, 0, 255);
        

        
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
