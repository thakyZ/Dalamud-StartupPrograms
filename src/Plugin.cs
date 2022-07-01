using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui.PartyFinder.Types;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;

using Lumina;

using Microsoft.VisualBasic;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NekoBoiNick.FFXIV.DalamudPlugin.StartupPrograms {
  public sealed class StartupPrograms : IDalamudPlugin {
    public string Name => "Startup Programs";

    private const string commandName = "/startupPrograms";
    private const string shortCmdName = "/srtPgms";

    private DalamudPluginInterface PluginInterface {
      get; init;
    }
    private CommandManager CommandManager {
      get; init;
    }
    private Configuration Configuration {
      get; init;
    }
    private PluginUI PluginUi {
      get; init;
    }
    private ClientState ClientState {
      get; init;
    }

    public StartupPrograms(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] CommandManager commandManager,
        [RequiredVersion("1.0")] ClientState clientState) {
      PluginInterface = pluginInterface;
      CommandManager = commandManager;
      ClientState = clientState;

      Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
      Configuration.Initialize(PluginInterface);

      // you might normally want to embed resources and load them from the manifest stream
      var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "startupPrograms.png");
      var pluginIcon = PluginInterface.UiBuilder.LoadImage(imagePath);
      PluginUi = new PluginUI(Configuration, pluginIcon);

      var commandInfo = new CommandInfo(OnCommand) {
        HelpMessage = "Opens the startup programs config window."
      };

      _ = CommandManager.AddHandler(commandName, commandInfo);
      _ = CommandManager.AddHandler(shortCmdName, commandInfo);

      PluginInterface.UiBuilder.Draw += PluginUi.Draw;
      PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

      StartTasks();
    }

    public void Dispose() {
      PluginUi.Dispose();
      _ = CommandManager.RemoveHandler(commandName);
      _ = CommandManager.RemoveHandler(shortCmdName);
    }

    private void OnCommand(string command, string args) {
      // in response to the slash command, just display our main ui
      PluginUi.Visible = true;
    }

    private void DrawConfigUI() {
      PluginUi.Visible = true;
    }
    private void StartTasks() {
      if (Configuration.ProgramsToStart.Count <= 0) {
        return;
      }
      foreach (ProgramListItem program in Configuration.ProgramsToStart.Where(x => GetIfFileIsAccessable(x.Path))) {
        if (program.RunAtLogin) {
          _ = Task.Run(async () => _ = await DoWaitUntilLogin(program));
        } else {
          _ = Task.Run(async () => _ = await StartNow(program));
        }
      }
    }

    private async Task<bool> DoWaitUntilLogin(ProgramListItem program) {
      var waited = await WaitUntilLogin();
      var finished = false;
      if (waited) {
        finished = StartProgram(program.Path, program.RunAsAdmin).Result;
      }
      return finished;
    }

    private async Task<bool> WaitUntilLogin() {
      while (!ClientState.IsLoggedIn) {
        await Task.Delay(25);
      }
      return true;
    }

    private static Task<bool> StartNow(ProgramListItem program) {
      return StartProgram(program.Path, program.RunAsAdmin);
    }

    private static (string, string) GetProgramAndArguments(string path) {
      (string, string) tempTruple = ("", "");
      var parts = Regex.Matches(path, @"[\""].+?[\""]|[^ ]+").Cast<Match>().Select(m => m.Value).ToList();
      PluginLog.Information($"{parts.Count}");
      foreach (var part in parts) {
        PluginLog.Information($"{part}");
      }
      var program = parts[0].Replace("\"","");
      var arguments = string.Join(" ", parts.ToArray()[1 .. parts.Count]);
      tempTruple.Item1 = program;
      tempTruple.Item2 = arguments;
      return tempTruple;
    }

    private static Task<bool> StartProgram(string path, bool asAdmin) {
      var taskCompletion = new TaskCompletionSource<bool>();
      (string, string) truple = GetProgramAndArguments(path);
      var proc = new ProcessStartInfo {
        UseShellExecute = true,
        WorkingDirectory = Directory.GetParent(path)?.FullName ?? Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XIVLauncher"),
        FileName = truple.Item1,
        Arguments = truple.Item2
      };
      if (asAdmin) {
        proc.Verb = "runas";
      }
      Process? process;
      try {
        process = Process.Start(proc);
        PluginLog.Information("Started process: `{0}` With PID {0}", process?.ProcessName ?? "Unknown Process", process?.Id ?? -1);
        taskCompletion.SetResult(true);
      } catch (Exception e) {
        PluginLog.Error("Program `{0}` failed to start...", path);
        PluginLog.Error("Exception: {0}", e.Message);
        PluginLog.Error("{0}", e.StackTrace ?? "No call stack.");
        taskCompletion.SetResult(false);
      }
      return taskCompletion.Task;
    }

    private static bool GetIfFileIsAccessable(string path) {
      (string, string) truple = GetProgramAndArguments(path);
      if (!File.Exists(truple.Item1)) {
        PluginLog.Warning("The program at the path `{0}` does not exist.", truple.Item1);
        return false;
      }
      try {
        File.Open(truple.Item1, FileMode.Open, FileAccess.Read).Dispose();
        return true;
      } catch (IOException e) {
        PluginLog.Warning("The program at the path `{0}` could not be read.", truple.Item1);
        PluginLog.Warning("IOException: {0}", e.Message);
        PluginLog.Warning("{0}", e.StackTrace ?? "No call stack.");
        return false;
      }
    }
  }
}
