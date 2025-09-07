using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadTool : EditorTool
{
    public override EditorToolId Id => EditorToolId.SaveLoadTool;
    public override string Name => "Save / Load Map";

    private List<string> SavedWorlds;

    [Header("Elements")]
    /*
    public TMP_InputField NumChunksInput;

    public TMP_InputField SeedInput;
    public Toggle SeedRandomizeToggle;

    public TMP_Dropdown GeneratorDropdown;
    public Button GenerateButton;
    */

    public TMP_Dropdown LoadDropdown;
    public Button LoadButton;

    public TMP_InputField SaveNameInput;
    public Button SaveButton;

    public override void Init(MapEditor editor)
    {
        base.Init(editor);
        LoadButton.onClick.AddListener(LoadButton_OnClick);
        SaveButton.onClick.AddListener(SaveButton_OnClick);

        UpdateLoadWorldDropdown();
    }

    private void UpdateLoadWorldDropdown(string initValue = "")
    {
        LoadDropdown.ClearOptions();

        string[] fullPaths = Directory.GetFiles(JsonUtilities.SAVE_DATA_PATH, "*.json");
        SavedWorlds = fullPaths.Select(x => System.IO.Path.GetFileNameWithoutExtension(x)).ToList();
        LoadDropdown.AddOptions(SavedWorlds);

        if (initValue != "") LoadDropdown.value = LoadDropdown.options.IndexOf(LoadDropdown.options.First(x => x.text == initValue));
    }

    private void SaveButton_OnClick()
    {
        if (SaveNameInput.text == "") return;

        Map.SetName(SaveNameInput.text);
        Map.Save();
        UpdateLoadWorldDropdown(initValue: SaveNameInput.text);
    }

    private void LoadButton_OnClick()
    {

        if (SavedWorlds.Count == 0) return;

        string mapToLoad = SavedWorlds[LoadDropdown.value];
        MapData loadedData = JsonUtilities.LoadData<MapData>(mapToLoad);
        Map loadedMap = new Map(loadedData);

        Editor.SetMap(loadedMap);

        SaveNameInput.text = mapToLoad;

        OnSelect(); // to refresh display options
    }

    public override void OnSelect()
    {
        // Display options
        Map.Renderer.HideAllPoints();
        MouseHoverInfo.SetShowPointSnapIndicator(false);
        MouseHoverInfo.SetCheckFeatureSelection(false);
    }
}
