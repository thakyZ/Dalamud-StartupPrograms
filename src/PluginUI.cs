using Dalamud;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;

using ImGuiNET;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Logging;

namespace NekoBoiNick.FFXIV.DalamudPlugin.StartupPrograms {
  // It is good to have this be disposable in general, in case you ever need it
  // to do any cleanup
  class PluginUI : IDisposable {
    private Configuration configuration;

    private ImGuiScene.TextureWrap pluginIcon;

    // this extra bool exists for ImGui, since you can't ref a property
    private bool visible = false;
    public bool Visible {
      get => visible;
      set => visible = value;
    }

    private List<ProgramListItem> programsToStart;
    private bool programsToStartChanged;
    private string programsToStartTempPath = string.Empty;
    private string programsToStartAddError = string.Empty;

    // passing in the image here just for simplicity
    public PluginUI(Configuration configuration, ImGuiScene.TextureWrap goatImage) {
      this.configuration = configuration;
      pluginIcon = goatImage;
      programsToStart = this.configuration.ProgramsToStart.Select(x => x.Clone()).ToList();
    }

    public void Dispose() {
      pluginIcon.Dispose();
    }

    public void Draw() {
      // This is our only draw handler attached to UIBuilder, so it needs to be
      // able to draw any windows we might have open.
      // Each method checks its own visibility/state to ensure it only draws when
      // it actually makes sense.
      // There are other ways to do this, but it is generally best to keep the number of
      // draw delegates as low as possible.

      DrawWindow();
    }

    public void DrawWindow() {
      if (!visible) {
        return;
      }
      programsToStartChanged = false;

      ImGui.SetNextWindowSize(new Vector2(740, 550), ImGuiCond.Always);
      if (ImGui.Begin("Startup Programs Settings", ref visible, ImGuiWindowFlags.NoResize)) {
        var windowSize = ImGui.GetWindowSize();
        if (ImGui.BeginChild("scrolling", new Vector2(windowSize.X - 5 - (5 * ImGuiHelpers.GlobalScale), windowSize.Y - 35 - (35 * ImGuiHelpers.GlobalScale)), false)) {
          DrawProgramsTable();
          ImGui.EndChild();
        }

        DrawSaveCloseButtons();
      }
      ImGui.End();
    }

    private void DrawProgramsTable() {
      if (programsToStartChanged) {
        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.HealerGreen);
        ImGui.SameLine();
        ImGui.Text("Programs To Start Changed");
        ImGui.PopStyleColor();
      }

      ImGuiHelpers.ScaledDummy(5);

      ImGui.Columns(6);
      ImGui.SetColumnWidth(0, 18 + (5 * ImGuiHelpers.GlobalScale));
      ImGui.SetColumnWidth(1, ImGui.GetWindowContentRegionMax().X - (18 + 16 + 16 + 16 + 14) - ((5 + 45 + 55 + 55 + 26) * ImGuiHelpers.GlobalScale) - 15);
      ImGui.SetColumnWidth(2, 16 + (45 * ImGuiHelpers.GlobalScale));
      ImGui.SetColumnWidth(3, 16 + (55 * ImGuiHelpers.GlobalScale));
      ImGui.SetColumnWidth(4, 16 + (55 * ImGuiHelpers.GlobalScale));
      ImGui.SetColumnWidth(5, 14 + (26 * ImGuiHelpers.GlobalScale));

      ImGui.Separator();

      ImGui.Text("#");
      ImGui.NextColumn();
      ImGui.Text("Path");
      ImGui.NextColumn();
      ImGui.Text("Enabled");
      ImGui.NextColumn();
      ImGui.Text("At Login");
      ImGui.NextColumn();
      ImGui.Text("As Admin");
      ImGui.NextColumn();
      ImGui.Text(string.Empty);
      ImGui.NextColumn();

      ImGui.Separator();

      ProgramListItem? programToRemove = null;

      var programrNumber = 0;
      foreach (var program in this.programsToStart) {
        var isEnabled = program.IsEnabled;
        var runAtLogin = program.RunAtLogin;
        var runAsAdmin = program.RunAsAdmin;

        ImGui.PushID($"startupProgram_{program.Path}");

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 8 - (ImGui.CalcTextSize(programrNumber.ToString()).X / 2));
        ImGui.Text(programrNumber.ToString());
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(-1);
        var path = program.Path;
        if (ImGui.InputText($"##startupProgramInput", ref path, 65535, ImGuiInputTextFlags.EnterReturnsTrue)) {
          var contains = programsToStart.Select(repo => repo.Path).Contains(path);
          if (program.Path == path) {
            // no change.
          } else if (contains && program.Path != path) {
            programsToStartAddError = "This program already exists.";
            _ = Task.Delay(5000).ContinueWith(t => programsToStartAddError = string.Empty);
          } else {
            program.Path = path;
            programsToStartChanged = path != program.Path;
          }
        }

        ImGui.NextColumn();

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 7 - (12 * ImGuiHelpers.GlobalScale));
        _ = ImGui.Checkbox("##startupProgramCheck", ref isEnabled);
        ImGui.NextColumn();

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 7 - (12 * ImGuiHelpers.GlobalScale));
        _ = ImGui.Checkbox("##startupProgramLoginCheck", ref runAtLogin);
        ImGui.NextColumn();

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 7 - (12 * ImGuiHelpers.GlobalScale));
        _ = ImGui.Checkbox("##startupProgramAdminCheck", ref runAsAdmin);
        ImGui.NextColumn();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash)) {
          programToRemove = program;
        }

        ImGui.PopID();

        ImGui.NextColumn();
        ImGui.Separator();

        program.IsEnabled = isEnabled;
        program.RunAtLogin = runAtLogin;
        program.RunAsAdmin = runAsAdmin;

        programrNumber++;
      }

      if (programToRemove != null) {
        _ = programsToStart.Remove(programToRemove);
        programsToStartChanged = true;
      }

      ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 8 - (ImGui.CalcTextSize(programrNumber.ToString()).X / 2));
      ImGui.Text(programrNumber.ToString());
      ImGui.NextColumn();
      ImGui.SetNextItemWidth(-1);
      _ = ImGui.InputText("##startupProgramPathInput", ref programsToStartTempPath, 300);
      ImGui.NextColumn();
      // Enabled button
      ImGui.NextColumn();
      // At Login button
      ImGui.NextColumn();
      // As Admin button
      ImGui.NextColumn();
      if (!string.IsNullOrEmpty(programsToStartTempPath) && ImGuiComponents.IconButton(FontAwesomeIcon.Plus)) {
        programsToStartTempPath = programsToStartTempPath.TrimEnd();
        if (programsToStart.Any(r => string.Equals(r.Path, programsToStartTempPath, StringComparison.InvariantCultureIgnoreCase))) {
          programsToStartAddError = "This program already exists.";
          _ = Task.Delay(5000).ContinueWith(t => programsToStartAddError = string.Empty);
        } else {
          programsToStart.Add(new ProgramListItem {
            Path = programsToStartTempPath,
            IsEnabled = false,
            RunAtLogin = false,
            RunAsAdmin = false,
          });
          programsToStartChanged = true;
          programsToStartTempPath = string.Empty;
        }
      }

      ImGui.Columns(1);

      if (!string.IsNullOrEmpty(this.programsToStartAddError)) {
        ImGui.TextColored(new Vector4(1, 0, 0, 1), this.programsToStartAddError);
      }
    }

    private void DrawSaveCloseButtons() {
      var buttonSave = false;
      var buttonClose = false;

      if (ImGui.Button("Save")) {
        buttonSave = true;
      }
      ImGui.SameLine();
      if (ImGui.Button("Close")) {
        buttonClose = true;
      }
      ImGui.SameLine();
      if (ImGui.Button("Save and Close")) {
        buttonSave = buttonClose = true;
      }
      if (buttonSave) {
        Save();

        if (programsToStartChanged) {
          programsToStartChanged = false;
        }
      }

      if (buttonClose) {
        Visible = false;
      }
    }

    private void Save() {
      configuration.ProgramsToStart = programsToStart;

      configuration.Save();
    }
  }
}
