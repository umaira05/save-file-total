using System;
using System.Collections.Generic;
using System.IO;
using Celeste.Mod.UI;
using YamlDotNet.Serialization;

namespace Celeste.Mod.SaveFileTotal;

[SettingName("modoptions_savefiletotal")]
[SettingInGame(false)]
public class SaveFileTotalModuleSettings : EverestModuleSettings
{

    [SettingIgnore]
    public long DeletedSaveTime { get; set; } = 0;

    [SettingIgnore]
    public int DeletedSaveDeaths { get; set; } = 0;

    [SettingIgnore]
    public long DeletedDebugTime { get; set; } = 0;

    [SettingIgnore]
    public int DeletedDebugDeaths { get; set; } = 0;

    [YamlIgnore]
    public int SaveFileSubmenu { get; set; } = 0;

    public void CreateSaveFileSubmenuEntry(TextMenu menu, bool inGame)
    {
        menu.Add(new TextMenu.Button("Total Time")
            .Pressed(() => OuiGenericMenu.Goto<OuiSaveTimeSubmenu>(overworld => overworld.Goto<OuiModOptions>(), new object[0])));
        menu.Add(new TextMenu.Button("Total Deaths")
            .Pressed(() => OuiGenericMenu.Goto<OuiSaveDeathsSubmenu>(overworld => overworld.Goto<OuiModOptions>(), new object[0])));
        if (DeletedDebugTime != 0 || DeletedDebugDeaths != 0 ||
            (File.Exists(UserIO.GetSaveFilePath(SaveData.GetFilename(-1))) && (FetchSaveFileStats(-1)[1] != "0" || FetchSaveFileStats(-1)[2] != "0")))
        {
            menu.Add(new TextMenu.Button("Debug Stats")
                .Pressed(() => OuiGenericMenu.Goto<OuiDebugSaveStatsSubmenu>(overworld => overworld.Goto<OuiModOptions>(), new object[0])));
        }
    }

    public string SfTimeToStr(long time)
    {
        TimeSpan ts = TimeSpan.FromMilliseconds(time / 10000);
        return ((int)ts.TotalHours).ToString("N0") + ts.ToString(@"\:mm\:ss\.fff");
    }

    public List<string> FetchSaveFileStats(int slot)
    {
        // default empty stats in case the vanilla file no longer exists
        List<string> stats = ["", "", ""];

        string saveFilePath = UserIO.GetSaveFilePath(SaveData.GetFilename(slot));

        // the vanilla file may not exist, return empty stats in this case
        if (!File.Exists(saveFilePath))
        {
            stats[1] = stats[2] = "0";
            Logger.Log(LogLevel.Info, "SaveFileTotal", $"Vanilla file for slot {slot} did not exist when deleting, ignoring stats");
            return stats;
        }

        foreach (string line in File.ReadLines(saveFilePath))
        {
            if (line.Contains("<Name>"))
                stats[0] = line.Substring(line.IndexOf(">") + 1, line.IndexOf("</") - line.IndexOf(">") - 1);
            else if (line.Contains("<Time>"))
                stats[1] = line.Substring(line.IndexOf(">") + 1, line.IndexOf("</") - line.IndexOf(">") - 1);
            else if (line.Contains("<TotalDeaths>"))
                stats[2] = line.Substring(line.IndexOf(">") + 1, line.IndexOf("</") - line.IndexOf(">") - 1);
            else if (!stats.Contains("")) break;
        }
        return stats;
    }

    public List<int> FetchSaveFileIndexes()
    {
        string saveFilePath = UserIO.GetSaveFilePath();
        List<int> saveFileIndexes = new List<int>();
        if (Directory.Exists(saveFilePath))
        {
            foreach (string filePath in Directory.GetFiles(saveFilePath, "*.celeste"))
            {
                string fileName = Path.GetFileName(filePath);
                if (int.TryParse(fileName.Substring(0, fileName.Length - 8), out int fileIndex))
                    saveFileIndexes.Add(fileIndex);
            }
        }
        saveFileIndexes.Sort();
        return saveFileIndexes;
    }
}