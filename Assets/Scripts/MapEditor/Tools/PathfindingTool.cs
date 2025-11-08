using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PathfindingTool : EditorTool
{
    public override EditorToolId Id => EditorToolId.PathfindingTool;
    public override string Name => "Pathfinding";

    private enum ToolState
    {
        Idle,
        FindPath_SelectFirstPoint,
        FindPath_SelectSecondPoint,
        FindPath_ShowResult,
    }

    private ToolState State;

    private List<Point> HighlightedPathPoints = new List<Point>();

    [Header("Elements")]
    public TextMeshProUGUI ActionsInstructionsText;
    public Button FindPathButton;

    public GameObject EntitiesContainer;

    [Header("Prefabs")]
    public UI_PathfindingEntityToggle EntityPrefab;

    private List<Entity> Entities;
    private Dictionary<Entity, UI_PathfindingEntityToggle> EntityRows;
    private Dictionary<Entity, NavigationPath> EntityPaths;

    public override void Init(MapEditor editor)
    {
        base.Init(editor);

        EntityPaths = new Dictionary<Entity, NavigationPath>();

        // Buttons
        FindPathButton.onClick.AddListener(FindPathButton_OnClick);

        // Entities
        Entities = new List<Entity>()
        {
            new Entity()
            {
                Name = "Default",
                Color = Color.yellow,
            },
        };

        HelperFunctions.DestroyAllChildredImmediately(EntitiesContainer);
        EntityRows = new Dictionary<Entity, UI_PathfindingEntityToggle>();
        foreach(Entity e in Entities)
        {
            UI_PathfindingEntityToggle elem = GameObject.Instantiate(EntityPrefab, EntitiesContainer.transform);
            elem.Init(e);
            elem.Toggle.onValueChanged.AddListener(_ => RefreshPathPreviews());
            EntityRows.Add(e, elem);
        }
    }

    public override void OnSelect()
    {
        // Set all line points as hoverable
        List<Point> hoverablePoints = Map.Points.Values.Where(p => p.HasLineFeature).ToList();
        MouseHoverInfo.SetPointSelectionOptions(hoverablePoints);
        MouseHoverInfo.SetShowPointSnapIndicator(true);

        Map.Renderer2D.HideAllPoints();
        foreach (Point p in hoverablePoints) p.Show();

        // Disable feature selection
        MouseHoverInfo.SetCheckFeatureSelection(false);

        // Idle state
        SetState(ToolState.Idle);
    }

    public override void OnDeselect()
    {
        UnhighlightAllPoints();
        MouseHoverInfo.ResetPointSelectionOptions();
        ResetPaths();
    }

    private void FindPathButton_OnClick()
    {
        SetState(ToolState.FindPath_SelectFirstPoint);
    }

    private void RefreshPathPreviews()
    {
        foreach (Entity e in Entities)
        {
            if (EntityPaths[e] != null)
            {
                if (EntityRows[e].Toggle.isOn) ShowPathPreviewFor(e);
                else EntityPaths[e].HidePreview();
            }
        }
    }

    #region Input Handling

    public override void HandleLeftClick()
    {
        switch(State)
        {
            case ToolState.FindPath_SelectFirstPoint:
                if (MouseHoverInfo.HoveredPoint != null)
                {
                    HighlightedPathPoints.Add(MouseHoverInfo.HoveredPoint);
                    MouseHoverInfo.HoveredPoint.SetDisplayColor(Color.green);
                    SetState(ToolState.FindPath_SelectSecondPoint);
                }
                break;

            case ToolState.FindPath_SelectSecondPoint:
                if (MouseHoverInfo.HoveredPoint != null)
                {
                    if (MouseHoverInfo.HoveredPoint == HighlightedPathPoints.Last())
                    {
                        return; // Clicked on previous point again
                    }
                    HighlightedPathPoints.Add(MouseHoverInfo.HoveredPoint);
                    MouseHoverInfo.HoveredPoint.SetDisplayColor(Color.green);
                    SetState(ToolState.FindPath_ShowResult);
                }
                break;
        }
    }

    public override void HandleRightClick()
    {
        SetState(ToolState.Idle);
    }


    #endregion

    private void UnhighlightAllPoints()
    {
        foreach (Point p in HighlightedPathPoints) p.SetDisplayColor(Color.white);
        HighlightedPathPoints.Clear();
    }

    private void SetState(ToolState state)
    {
        // Select new
        State = state;

        switch(State)
        {
            case ToolState.Idle:
                FindPathButton.GetComponent<Image>().color = Color.white;
                ActionsInstructionsText.text = "Click on 'Find Path' to start pathfinding.";
                UnhighlightAllPoints();
                ResetPaths();
                break;

            case ToolState.FindPath_SelectFirstPoint:
                FindPathButton.GetComponent<Image>().color = Color.yellow;
                ActionsInstructionsText.text = "Select any point on a LineFeature to set as the start point.";
                UnhighlightAllPoints();
                ResetPaths();
                break;

            case ToolState.FindPath_SelectSecondPoint:
                FindPathButton.GetComponent<Image>().color = Color.yellow;
                ActionsInstructionsText.text = "Select any other point on a LineFeature to set as the target point.";
                break;

            case ToolState.FindPath_ShowResult:
                FindPathButton.GetComponent<Image>().color = Color.yellow;
                ShowResult();
                ActionsInstructionsText.text = "Showing pathfinding result.";
                break;
        }
    }

    private void ShowResult()
    {
        ResetPaths();

        foreach(Entity e in Entities)
        {
            NavigationPath path = Pathfinder.GetPath(Map, e, HighlightedPathPoints[0], HighlightedPathPoints[1]);
            EntityPaths[e] = path;

            if (path != null)
            {
                EntityRows[e].PathCostText.text = path.GetCostAsTimeString(e);
            }

            ShowPathPreviewFor(e);
        }
    }

    private void ShowPathPreviewFor(Entity e)
    {
        if (EntityPaths[e] != null) EntityPaths[e].ShowPreview(2f, e.Color, LineTexture.DottedRoundWithOutline);
    }

    private void ResetPaths()
    {
        foreach (Entity e in Entities)
        {
            if (EntityPaths.ContainsKey(e) && EntityPaths[e] != null) EntityPaths[e].HidePreview();
            if (EntityRows.ContainsKey(e)) EntityRows[e].PathCostText.text = "";
        }
        EntityPaths.Clear();
    }
}

