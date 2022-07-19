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

        public List<TimerConfig> TimerConfigs { get; set; } = new();
        public TextConfig StatusTimerTextConfig { get; set; } = new(FontType.Type1, 16, new Vector4(1, 1, 1, 255));
        public TextConfig StackCountTextConfig { get; set; } = new(FontType.Type1, 13, new Vector4(1, 0.5f, 0, 255));
        
        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;
        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            if (this.pluginInterface == null) return;
            this.pluginInterface.SavePluginConfig(this);
        }
    }

    public enum FontType { Type1, Type2, Type3, Type4, Type5 }
}
