using System.Collections.Generic;
using System.IO;
using Celeste.Mod.UI;

namespace Celeste.Mod.SaveFileTotal;

internal class OuiDebugSaveStatsSubmenu : OuiGenericMenu, OuiModOptions.ISubmenu
{
    public override string MenuName => "Debug Stats";
    protected override void addOptionsToMenu(TextMenu menu)
    {
        long time = 0;
        int deaths = 0;
        if (File.Exists(UserIO.GetSaveFilePath(SaveData.GetFilename(-1))))
        {
            List<string> stats = SaveFileTotalModule.Settings.FetchSaveFileStats(-1);
            time = long.Parse(stats[1]);
            deaths = int.Parse(stats[2]);
        }
        menu.Add(new TextMenu.Button("Total debug save time: " + SaveFileTotalModule.Settings.SfTimeToStr(time + SaveFileTotalModule.Settings.DeletedDebugTime)));
        menu.Add(new TextMenu.Button("Deleted debug save time: " + SaveFileTotalModule.Settings.SfTimeToStr(SaveFileTotalModule.Settings.DeletedDebugTime)));
        menu.Add(new TextMenu.Button("Debug save time: " + SaveFileTotalModule.Settings.SfTimeToStr(time)));
        menu.Add(new TextMenu.Button("Total debug save deaths: " + (deaths + SaveFileTotalModule.Settings.DeletedDebugDeaths).ToString("N0")));
        menu.Add(new TextMenu.Button("Deleted debug save deaths: " + SaveFileTotalModule.Settings.DeletedDebugDeaths.ToString("N0")));
        menu.Add(new TextMenu.Button("Debug save deaths: " + deaths.ToString("N0")));
    }
}