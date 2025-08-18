using System;
using System.IO;
using System.Collections.Generic;

namespace Celeste.Mod.SaveFileTotal;

public class SaveFileTotalModule : EverestModule {
    public static SaveFileTotalModule Instance { get; private set; }

    public override Type SettingsType => typeof(SaveFileTotalModuleSettings);
    public static SaveFileTotalModuleSettings Settings => (SaveFileTotalModuleSettings) Instance._Settings;

    public SaveFileTotalModule() {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(SaveFileTotalModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(SaveFileTotalModule), LogLevel.Info);
#endif
    }

    private bool SaveFileTotal_TryDelete(On.Celeste.SaveData.orig_TryDelete orig, int slot)
    {
        if (slot == -1 && !File.Exists(UserIO.GetSaveFilePath(SaveData.GetFilename(-1))))
        {
            long debugTime = SaveData.Instance.Time;
            int debugDeaths = SaveData.Instance.TotalDeaths;
            if (orig(slot))
            {
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
        if (orig(slot))
        {
            if (slot == -1)
            {
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

    public override void Load()
    {
        On.Celeste.SaveData.TryDelete += SaveFileTotal_TryDelete;
    }

    public override void Unload()
    {
        On.Celeste.SaveData.TryDelete -= SaveFileTotal_TryDelete;
    }
}