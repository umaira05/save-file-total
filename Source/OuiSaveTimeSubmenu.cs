using System.Collections.Generic;
using Celeste.Mod.UI;

namespace Celeste.Mod.SaveFileTotal;

internal class OuiSaveTimeSubmenu : OuiGenericMenu, OuiModOptions.ISubmenu
{
    public override string MenuName => "Total Time";
    protected override void addOptionsToMenu(TextMenu menu)
    {
        List<int> saveFileIndexes = SaveFileTotalModule.Settings.FetchSaveFileIndexes();
        ReadSaveFiles(ref menu, ref saveFileIndexes, saveFileIndexes.Count - 1, SaveFileTotalModule.Settings.DeletedSaveTime);
    }

    private void ReadSaveFiles(ref TextMenu menu, ref List<int> saveFileIndexes, int index, long sumOfTimes)
    {
        if (index == -1)
        {
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

