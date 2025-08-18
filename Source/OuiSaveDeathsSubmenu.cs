using System.Collections.Generic;
using Celeste.Mod.UI;

namespace Celeste.Mod.SaveFileTotal;

internal class OuiSaveDeathsSubmenu : OuiGenericMenu, OuiModOptions.ISubmenu
{
    public override string MenuName => "Total Deaths";
    protected override void addOptionsToMenu(TextMenu menu)
    {
        List<int> saveFileIndexes = SaveFileTotalModule.Settings.FetchSaveFileIndexes();
        ReadSaveFiles(ref menu, ref saveFileIndexes, saveFileIndexes.Count - 1, SaveFileTotalModule.Settings.DeletedSaveDeaths);
    }

    private void ReadSaveFiles(ref TextMenu menu, ref List<int> saveFileIndexes, int index, int sumOfDeaths)
    {
        if (index == -1)
        {
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
