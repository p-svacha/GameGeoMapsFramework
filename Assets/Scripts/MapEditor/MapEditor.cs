using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class MapEditor : MonoBehaviour
{
    public Map Map { get; private set; }

    [Header("Prefabs")]
    public UI_EditorToolButton EditorToolButtonPrefab;
    public GameObject ArrowPrefab;

    [Header("Elements")]
    public GameObject ToolButtonContainer;
    public GameObject ToolNamePanel;
    public TextMeshProUGUI ToolNameText;
    public Dictionary<EditorToolId, UI_EditorToolButton> ToolButtons;
    public TextMeshProUGUI HoverInfoText;
    public UI_ToolWindow ToolWindow;

    [Header("Tools")]
    public SaveLoadTool SaveLoadTool;
    public ViewModeTool ViewModeTool;
    public SelectFeatureTool SelectFeatureTool;
    public CreatePointFeatureTool CreatePointFeatureTool;
    public CreateLineFeatureTool CreateLineFeatureTool;
    public CreateAreaFeatureTool CreateAreaFeatureTool;
    public EditPointFeatureTool EditPointFeatureTool;
    public EditLineFeatureTool EditLineFeatureTool;
    public EditAreaFeatureTool EditAreaFeatureTool;

    // Editor
    private bool isInitialized = false;
    float deltaTime; // for fps
    private Dictionary<EditorToolId, EditorTool> Tools;
    public EditorTool CurrentTool;

    // Colors
    public static Color ButtonSelectedColor = new Color(0.9f, 0.6f, 0.22f);
    public static Color ButtonUnselectedColor = new Color(0.55f, 0.55f, 0.55f);
    

    private void Awake()
    {
        MouseHoverInfo.AwakeReset();

        ResourceManager.ClearCache();
        DefDatabaseRegistry.AddAllDefs();
        DefDatabaseRegistry.ResolveAllReferences();
        DefDatabaseRegistry.OnLoadingDone();
    }

    private void Start()
    {
        Map = new Map();

        // Init tools
        Tools = new Dictionary<EditorToolId, EditorTool>()
            {
                { EditorToolId.SaveLoadTool, SaveLoadTool },
                { EditorToolId.ViewModeTool, ViewModeTool },
                { EditorToolId.SelectFeatureTool, SelectFeatureTool },
               // { EditorToolId.CreatePointFeatureTool, CreatePointFeatureTool },
                { EditorToolId.CreateLineFeatureTool, CreateLineFeatureTool },
                { EditorToolId.CreateAreaFeatureTool, CreateAreaFeatureTool },
                //{ EditorToolId.EditPointFeatureTool, EditPointFeatureTool },
                { EditorToolId.EditLineFeatureTool, EditLineFeatureTool },
                { EditorToolId.EditAreaFeatureTool, EditAreaFeatureTool },
            };
        foreach (EditorTool tool in Tools.Values) tool.Init(this);

        // Init tool buttons
        ToolButtons = new Dictionary<EditorToolId, UI_EditorToolButton>();
        foreach (EditorTool tool in Tools.Values)
        {
            UI_EditorToolButton btn = Instantiate(EditorToolButtonPrefab, ToolButtonContainer.transform);
            btn.Init(this, tool);
            ToolButtons.Add(tool.Id, btn);
        }

        SelectTool(EditorToolId.SaveLoadTool);

        // Set initialized to true if everything here did run through without throwing an error
        isInitialized = true;
    }

    public void SetMap(Map map)
    {
        Map.DestroyAllVisuals();
        Map = map;
    }


    private void Update()
    {
        MouseHoverInfo.Update(Map);
        UpdateHoverInfoText();

        HandleInputs();

        CurrentTool.UpdateTool();
    }

    private void UpdateHoverInfoText()
    {
        HoverInfoText.text = "";

        HoverInfoText.text += $"\n{MouseHoverInfo.WorldPosition}";
        if (MouseHoverInfo.HoveredPoint != null)
        {
            HoverInfoText.text += $"\nSnapped Point: [{MouseHoverInfo.HoveredPoint.Id}] {MouseHoverInfo.HoveredPoint.Position}";
            HoverInfoText.text += $"\nconnected to {MouseHoverInfo.HoveredPoint.ConnectedFeatures.Count} features.";
        }
        if (MouseHoverInfo.HoveredMapFeature != null) HoverInfoText.text += $"\nHovered Feature: {MouseHoverInfo.HoveredMapFeature.Id}";

        HoverInfoText.text = HoverInfoText.text.Trim();
    }

    private void HandleInputs()
    {
        // Click
        bool isMouseOverUi = HelperFunctions.IsMouseOverUi();
        HelperFunctions.UnfocusNonInputUiElements();
        bool isUiElementFocussed = HelperFunctions.IsUiFocussed();

        if (Input.GetMouseButtonDown(0) && !isMouseOverUi) CurrentTool.HandleLeftClick();
        if (Input.GetMouseButton(0) && !isMouseOverUi) CurrentTool.HandleLeftDrag();
        if (Input.GetMouseButtonUp(0)) CurrentTool.HandleStopLeftDrag();

        if (Input.GetMouseButtonDown(1) && !isMouseOverUi) CurrentTool.HandleRightClick();
        if (Input.GetMouseButton(1) && !isMouseOverUi) CurrentTool.HandleRightDrag();
        if (Input.GetMouseButtonUp(1)) CurrentTool.HandleStopRightDrag();

        if (Input.GetMouseButtonDown(2) && !isMouseOverUi) CurrentTool.HandleMiddleClick();

        if (isUiElementFocussed) return; // Don't check for keyboard inputs when a ui element is focussed

        CurrentTool.HandleKeyboardInputs();
    }

    #region Tools

    public void SelectTool(EditorToolId id)
    {
        EditorTool oldTool = CurrentTool;
        EditorTool newTool = Tools[id];

        // Handle de-delection of previous tool
        if (oldTool != null)
        {
            ToolButtons[oldTool.Id].SetSelected(false);
            oldTool.OnDeselect();
        }

        // Handle selection of new tool
        ToolButtons[newTool.Id].SetSelected(true);
        ToolWindow.SelectTool(newTool);
        newTool.OnSelect();

        // Set new tool as current
        CurrentTool = newTool;
    }

    public void SetInstructionsText(string text)
    {
        ToolWindow.InstructionsText.text = text;
    }

    #endregion
}
