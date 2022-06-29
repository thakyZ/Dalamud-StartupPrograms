using Dalamud.Configuration;
using Dalamud.Plugin;

using System;
using System.Collections.Generic;

namespace NekoBoiNick.FFXIV.DalamudPlugin.StartupPrograms {
  [Serializable]
  public class Configuration : IPluginConfiguration {
    public int Version { get; set; } = 0;

    public List<ProgramListItem> ProgramsToStart { get; set; } = new();

    // the below exist just to make saving less cumbersome

    [NonSerialized]
    private DalamudPluginInterface? pluginInterface;

    public void Initialize(DalamudPluginInterface pluginInterface) {
      this.pluginInterface = pluginInterface;
    }

    public void Save() {
      pluginInterface!.SavePluginConfig(this);
    }
  }

  public sealed class ProgramListItem {
    public bool IsEnabled { get; set; } = false;
    public string Path { get; set; } = string.Empty;
    public bool RunAtLogin { get; set; } = false;
    public bool RunAsAdmin { get; set; } = false;
    public ProgramListItem Clone() => (this.MemberwiseClone() as ProgramListItem)!;
  }
}
