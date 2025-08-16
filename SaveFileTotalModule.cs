using System;
using System.IO;
using System.Collections.Generic;
using Celeste.Mod.UI;
using YamlDotNet.Serialization;

namespace Celeste.Mod.SaveFileTotal {
    public class SaveFileTotalModule : EverestModule {
        
        public static SaveFileTotalModule Instance;

        public SaveFileTotalModule() {
            Instance = this;
        }

        public override Type SettingsType => typeof(SaveFileTotalModuleSettings);
        public static SaveFileTotalModuleSettings Settings => (SaveFileTotalModuleSettings)Instance._Settings;

        private bool SaveFileTotal_TryDelete(On.Celeste.SaveData.orig_TryDelete orig, int slot) {
            if (slot == -1 && !File.Exists(UserIO.GetSaveFilePath(SaveData.GetFilename(-1)))) {
                long debugTime = SaveData.Instance.Time;
                int debugDeaths = SaveData.Instance.TotalDeaths;
                if (orig(slot)) {
                    Settings.DeletedDebugTime += debugTime;
                    Settings.DeletedDebugDeaths += debugDeaths;
                    Logger.Log(LogLevel.Info, "SaveFileTotal", "Added deleted debug save data to totals");
                    return true;
                }
                return false;
            }
            List<string> stats = Settings.FetchSaveFileStats(slot);
            long time = long.Parse(stats[1]);
            int deaths = int.Parse(stats[2]);
            if (orig(slot)) {
                if (slot == -1) {
                    Settings.DeletedDebugTime += time;
                    Settings.DeletedDebugDeaths += deaths;
                    Logger.Log(LogLevel.Info, "SaveFileTotal", "Added deleted debug save data to totals");
                    return true;
                }
                Settings.DeletedSaveTime += time;
                Settings.DeletedSaveDeaths += deaths;
                Logger.Log(LogLevel.Info, "SaveFileTotal", "Added deleted save data to totals");
                return true;
            }
            return false;
        }

        public override void Load() {
            On.Celeste.SaveData.TryDelete += SaveFileTotal_TryDelete;
        }

        public override void Unload() {
            On.Celeste.SaveData.TryDelete -= SaveFileTotal_TryDelete;
        }
    }

    [SettingName("modoptions_savefiletotal")]
    [SettingInGame(false)]
    public class SaveFileTotalModuleSettings : EverestModuleSettings {

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

        public void CreateSaveFileSubmenuEntry(TextMenu menu, bool inGame) {
            menu.Add(new TextMenu.Button("Total Time")
                .Pressed(() => OuiGenericMenu.Goto<OuiSaveTimeSubmenu>(overworld => overworld.Goto<OuiModOptions>(), new object[0])));
            menu.Add(new TextMenu.Button("Total Deaths")
                .Pressed(() => OuiGenericMenu.Goto<OuiSaveDeathsSubmenu>(overworld => overworld.Goto<OuiModOptions>(), new object[0])));
            if (DeletedDebugTime != 0 || DeletedDebugDeaths != 0 ||
                (File.Exists(UserIO.GetSaveFilePath(SaveData.GetFilename(-1))) && (FetchSaveFileStats(-1)[1] != "0" || FetchSaveFileStats(-1)[2] != "0"))) {
                menu.Add(new TextMenu.Button("Debug Stats")
                    .Pressed(() => OuiGenericMenu.Goto<OuiDebugSaveStatsSubmenu>(overworld => overworld.Goto<OuiModOptions>(), new object[0])));
            }
        }

        public string SfTimeToStr(long time) {
            TimeSpan ts = TimeSpan.FromMilliseconds(time / 10000);
            return ((int)ts.TotalHours).ToString("N0") + ts.ToString(@"\:mm\:ss\.fff");
        }

        public List<string> FetchSaveFileStats(int slot) {
            // default empty stats in case the vanilla file no longer exists
            List<string> stats = new List<string> { "", "0", "0" };

            string saveFilePath = UserIO.GetSaveFilePath(SaveData.GetFilename(slot));

            // the vanilla file may not exist, return empty stats in this case
            if (!File.Exists(saveFilePath)) {
                Logger.Log(LogLevel.Info, "SaveFileTotal", $"Vanilla file for slot {slot} did not exist when deleting, ignoring stats");
                return stats;
            }

            foreach (string line in File.ReadLines(saveFilePath)) {
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

        public List<int> FetchSaveFileIndexes() {
            string saveFilePath = UserIO.GetSaveFilePath();
            List<int> saveFileIndexes = new List<int>();
            if (Directory.Exists(saveFilePath)) {
                foreach (string filePath in Directory.GetFiles(saveFilePath, "*.celeste")) {
                    string fileName = Path.GetFileName(filePath);
                    if (int.TryParse(fileName.Substring(0, fileName.Length - 8), out int fileIndex))
                        saveFileIndexes.Add(fileIndex);
                }
            }
            saveFileIndexes.Sort();
            return saveFileIndexes;
        }
    }

    class OuiSaveTimeSubmenu : OuiGenericMenu, OuiModOptions.ISubmenu {
        public override string MenuName => "Total Time";
        protected override void addOptionsToMenu(TextMenu menu) {
            List<int> saveFileIndexes = SaveFileTotalModule.Settings.FetchSaveFileIndexes();
            ReadSaveFiles(ref menu, ref saveFileIndexes, saveFileIndexes.Count - 1, SaveFileTotalModule.Settings.DeletedSaveTime);
        }

        private void ReadSaveFiles(ref TextMenu menu, ref List<int> saveFileIndexes, int index, long sumOfTimes) {
            if (index == -1) {
                menu.Add(new TextMenu.Button("Total save time: " + SaveFileTotalModule.Settings.SfTimeToStr(sumOfTimes)));
                menu.Add(new TextMenu.Button("Deleted save time: " + SaveFileTotalModule.Settings.SfTimeToStr(SaveFileTotalModule.Settings.DeletedSaveTime)));
                return;
            }
            List<string> stats = SaveFileTotalModule.Settings.FetchSaveFileStats(saveFileIndexes[index]);
            string filename = stats[0];
            long time = long.Parse(stats[1]);
            ReadSaveFiles(ref menu, ref saveFileIndexes, index - 1, sumOfTimes + time);
            string fileNum = SaveData.GetFilename(saveFileIndexes[index]);
            menu.Add(new TextMenu.Button("File " + fileNum + ": " + filename + ", " + SaveFileTotalModule.Settings.SfTimeToStr(time)));
        }
    }

    class OuiSaveDeathsSubmenu : OuiGenericMenu, OuiModOptions.ISubmenu {
        public override string MenuName => "Total Deaths";
        protected override void addOptionsToMenu(TextMenu menu) {
            List<int> saveFileIndexes = SaveFileTotalModule.Settings.FetchSaveFileIndexes();
            ReadSaveFiles(ref menu, ref saveFileIndexes, saveFileIndexes.Count - 1, SaveFileTotalModule.Settings.DeletedSaveDeaths);
        }

        private void ReadSaveFiles(ref TextMenu menu, ref List<int> saveFileIndexes, int index, int sumOfDeaths) {
            if (index == -1) {
                menu.Add(new TextMenu.Button("Total save deaths: " + sumOfDeaths.ToString("N0")));
                menu.Add(new TextMenu.Button("Deleted save deaths: " + SaveFileTotalModule.Settings.DeletedSaveDeaths.ToString("N0")));
                return;
            }
            List<string> stats = SaveFileTotalModule.Settings.FetchSaveFileStats(saveFileIndexes[index]);
            string filename = stats[0];
            int deaths = int.Parse(stats[2]);
            ReadSaveFiles(ref menu, ref saveFileIndexes, index - 1, sumOfDeaths + deaths);
            string fileNum = SaveData.GetFilename(saveFileIndexes[index]);
            menu.Add(new TextMenu.Button("File " + fileNum + ": " + filename + ", " + deaths.ToString("N0")));
        }
    }

    class OuiDebugSaveStatsSubmenu : OuiGenericMenu, OuiModOptions.ISubmenu {
        public override string MenuName => "Debug Stats";
        protected override void addOptionsToMenu(TextMenu menu) {
            long time = 0;
            int deaths = 0;
            if (File.Exists(UserIO.GetSaveFilePath(SaveData.GetFilename(-1)))) {
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
}
