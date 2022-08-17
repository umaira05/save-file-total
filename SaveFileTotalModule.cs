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
            string saveFilePath = UserIO.GetSaveFilePath(slot.ToString());
            long time = -1;
            foreach (string line in File.ReadLines(saveFilePath)) {
                if (line.Contains("<Time>")) {
                    time = long.Parse(line.Substring(line.IndexOf(">") + 1, line.IndexOf("</") - line.IndexOf(">") - 1));
                    break;
                }
            }
            if (orig(slot)) {
                Settings.DeletedSaveTime += time;
                Logger.Log(LogLevel.Info, "SaveFileTotal", "Added deleted save time to total");
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
    public class SaveFileTotalModuleSettings : EverestModuleSettings {

        [SettingIgnore]
        public long DeletedSaveTime { get; set; } = 0;

        [YamlIgnore]
        public int SaveFileSubmenu { get; set; } = 0;

        public void CreateSaveFileSubmenuEntry(TextMenu menu, bool inGame) {
            if (!inGame) {
                menu.Add(new TextMenu.Button("Save File Total")
                    .Pressed(() => OuiGenericMenu.Goto<OuiSaveFileSubmenu>(overworld => overworld.Goto<OuiModOptions>(), new object[0])));
            }
        }
    }

    class OuiSaveFileSubmenu : OuiGenericMenu, OuiModOptions.ISubmenu {
        public override string MenuName => "Save File Total";

        private void readSaveFiles(ref TextMenu menu, ref List<int> saveFileIndexes, int index, long sum_of_times) {
            if (index == -1) {
                TimeSpan total_ts = TimeSpan.FromMilliseconds(sum_of_times / 10000);
                string total_time_str = (int)total_ts.TotalHours + total_ts.ToString(@"\:mm\:ss\.fff");
                menu.Add(new TextMenu.Button("Total save time: " + total_time_str));
                TimeSpan del_ts = TimeSpan.FromMilliseconds(SaveFileTotalModule.Settings.DeletedSaveTime / 10000);
                string del_str = (int)del_ts.TotalHours + del_ts.ToString(@"\:mm\:ss\.fff");
                menu.Add(new TextMenu.Button("Deleted save time: " + del_str));
                return;
            }
            string filename = null;
            long time = -1;
            string fileNum = saveFileIndexes[index].ToString();
            foreach (string line in File.ReadLines(UserIO.GetSaveFilePath(fileNum))) {
                if (line.Contains("<Name>")) {
                    filename = line.Substring(line.IndexOf(">") + 1, line.IndexOf("</") - line.IndexOf(">") - 1);
                } else if (line.Contains("<Time>")) {
                    time = long.Parse(line.Substring(line.IndexOf(">") + 1, line.IndexOf("</") - line.IndexOf(">") - 1));
                } else if (filename != null && time != -1) break;
            }
            readSaveFiles(ref menu, ref saveFileIndexes, index - 1, sum_of_times + time);
            TimeSpan ts = TimeSpan.FromMilliseconds(time / 10000);
            string time_str = (int)ts.TotalHours + ts.ToString(@"\:mm\:ss\.fff");
            menu.Add(new TextMenu.Button("File " + fileNum + ": " + filename + ", " + time_str));
        }

        protected override void addOptionsToMenu(TextMenu menu) {
            string saveFilePath = UserIO.GetSaveFilePath();
            List<int> saveFileIndexes = new List<int>();
            if (Directory.Exists(saveFilePath)) {
                foreach (string filePath in Directory.GetFiles(saveFilePath, "*.celeste")) {
                    string fileName = Path.GetFileName(filePath);
                    if (int.TryParse(fileName.Substring(0, fileName.Length - 8), out int fileIndex)) {
                        saveFileIndexes.Add(fileIndex);
                    }
                }
            }
            saveFileIndexes.Sort();
            readSaveFiles(ref menu, ref saveFileIndexes, saveFileIndexes.Count - 1, SaveFileTotalModule.Settings.DeletedSaveTime);
        }
    }
}
