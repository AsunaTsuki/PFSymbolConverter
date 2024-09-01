using Dalamud.Configuration;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using System;
using System.Net.Http;

namespace PFSymbolConverter
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private IDalamudPluginInterface? PluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.PluginInterface = pluginInterface;

        }

        public void Save()
        {
            this.PluginInterface!.SavePluginConfig(this);
        }
    }
}
