# Startup Programs

TeamCraft packet capture not working for you upon startup? Try this instead.

A program to start tools upon login or Dalamud startup.

<!--
## Main Points

*Simple functional plugin
  * Slash command
  *Main UI
  * Settings UI
  *Image loading
  * Plugin json
*Simple, slightly-improved plugin configuration handling
* Project organization
  *Copies all necessary plugin files to the output directory
    * Does not copy dependencies that are provided by dalamud
    *Output directory can be zipped directly and have exactly what is required
  * Hides data files from visual studio to reduce clutter
    * Also allows having data files in different paths than VS would usually allow if done in the IDE directly

The intention is less that any of this is used directly in other projects, and more to show how similar things can be done.
-->

## To Use

### Building

1. Open up `StartupPrograms.sln` in your C# editor of choice (likely [Visual Studio 2022](https://visualstudio.microsoft.com) or [JetBrains Rider](https://www.jetbrains.com/rider/)).
2. Build the solution. By default, this will build a `Debug` build, but you can switch to `Release` in your IDE.
3. The resulting plugin can be found at `%AppData%/XIVLauncher/devPlugins/StartupPrograms/StartupPrograms.dll`

### Activating in-game

1. You should now be able to use `/startupPrograms` (chat) or `startupPrograms` (console)!

### Reconfiguring for your own uses

Basically, just replace all references to `StartupPrograms` in all of the files and filenames with your desired name. You'll figure it out 😁

Dalamud will load the JSON file (by default, `StartupPrograms/StartupPrograms.json`) next to your DLL and use it for metadata, including the description for your plugin in the Plugin Installer. Make sure to update this with information relevant to _your_ plugin!
