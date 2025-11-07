using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Renderer responsible for rendering a map in 2D.
/// </summary>
public class MapRenderer2D
{
    public static Sprite DEFAULT_POINT_SPRITE = ResourceManager.LoadSprite("Sprites/Point");

    private static float LINE_SELECTION_INDICATOR_WIDTH = 3f; // additional to line width
    public static float LINE_SELECTION_INDICATOR_ALPHA = 0.3f;

    private static float AREA_SELECTION_INDICATOR_WIDTH = 2f;
    public static Color AREA_SELECTION_INDICATOR_COLOR = new Color(0f, 0f, 0f, 0.3f);

    public Map Map { get; private set; }

    public GameObject MapRoot { get; private set; }
    private GameObject PointsContainer;
    public GameObject PointFeatureContainer; // UI element

    // Higher numbers get rendererd on top
    private Dictionary<MapZLayer, int> SortingOrders = new Dictionary<MapZLayer, int>()
    {
        { MapZLayer.MapOverlay, 2000 },

        { MapZLayer.Point, 1010 },
        { MapZLayer.PointSnapIndicator, 1000 },
        { MapZLayer.AreaFeatureSelectionIndicator, 999 },

        { MapZLayer.LineFeature, 900 }, // 900 - 920 based on layer
        { MapZLayer.LineFeatureSelectionIndicator, 899 },

        { MapZLayer.AreaFeatureOutline, 850 },
        { MapZLayer.AreaFeaturePolygon, 800 }, // 800 - 820 based on layer
    };
    private const string SortingLayer = "Map";

    public MapRenderer2D(Map map)
    {
        Map = map;

        MapRoot = new GameObject("Map2D");
        PointsContainer = new GameObject("Points");
        PointsContainer.transform.SetParent(MapRoot.transform);

        PointFeatureContainer = new GameObject("PointFeatureContainer");
        PointFeatureContainer.transform.SetParent(GameObject.Find("Canvas").transform);
        PointFeatureContainer.transform.SetAsFirstSibling(); // render below all other UI
    }

    public void ApplySortingOrder(Renderer r, MapZLayer layer, int orderOffset = 0)
    {
        r.sortingLayerName = SortingLayer;
        r.sortingOrder = SortingOrders[layer] + orderOffset;
        var t = r.transform;
        if (Mathf.Abs(t.position.z) > 0f) t.position = new Vector3(t.position.x, t.position.y, 0f);
    }

    /// <summary>
    /// Called every frame.
    /// </summary>
    public void Update()
    {
        foreach (PointFeature pf in Map.PointFeatures.Values)
        {
            // Hide feature if zoom level is out of range
            float zoom = CameraHandler.Instance.Camera.orthographicSize;
            if (zoom < pf.Def.MinZoom || zoom > pf.Def.MaxZoom)
            {
                pf.VisualRoot.SetActive(false);
                continue;
            }
            else pf.VisualRoot.SetActive(true);

            // Position feature in screen space
            Vector3 screenSpace = CameraHandler.Instance.Camera.WorldToScreenPoint(pf.Point.Position);
            pf.VisualRoot.transform.position = screenSpace;
        }
    }

    #region Display Options

    public void ShowAllPoints()
    {
        foreach (Point p in Map.Points.Values) p.Show();
    }
    public void HideAllPoints()
    {
        foreach (Point p in Map.Points.Values) p.Hide();
    }

    #endregion

    #region Map Element Renderers

    public GameObject DrawPoint(Point p, Sprite overrideSprite = null)
    {
        // Point sprite
        GameObject obj = new GameObject($"Point {p.Position.x.ToString("#.##")}/{p.Position.y.ToString("#.##")}");
        obj.transform.SetParent(PointsContainer.transform);
        obj.transform.position = p.Position;
        SpriteRenderer spriteRenderer = obj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = overrideSprite != null ? overrideSprite : DEFAULT_POINT_SPRITE;
        ApplySortingOrder(spriteRenderer, MapZLayer.Point);

        return obj;
    }

    public GameObject DrawPointSnapIndicator(Point p)
    {
        // Snap indicator
        GameObject obj = new GameObject("SnapIndicator");
        obj.transform.SetParent(p.RenderedPoint.transform);
        SpriteRenderer spriteRenderer = obj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = ResourceManager.LoadSprite("Sprites/Circle");
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);
        ApplySortingOrder(spriteRenderer, MapZLayer.PointSnapIndicator);
        obj.transform.localScale = new Vector3(2f, 2f, 2f);
        obj.transform.localPosition = Vector3.zero;
        obj.SetActive(false);

        return obj;
    }

    public void RedrawPointAndAllConnectedFeatures(Point p)
    {
        // Point
        p.RenderedPoint.transform.position = p.Position;

        // Connected features
        foreach (MapFeature feat in p.ConnectedFeatures)
        {
            if (feat is AreaFeature area) area.RecalculateClockwise();
            RedrawFeature(feat);
        }
    }



    public void CreatePointFeatureVisuals(PointFeature feature)
    {
        // Root
        feature.VisualRoot = new GameObject($"PointFeature_{feature.Id}");
        feature.VisualRoot.transform.SetParent(PointFeatureContainer.transform);

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(feature.VisualRoot.transform);
        feature.VisualIcon = iconObj.AddComponent<Image>();
        feature.VisualIcon.raycastTarget = false;

        // Text
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(feature.VisualRoot.transform);
        feature.VisualLabel = labelObj.AddComponent<TextMeshProUGUI>();
        feature.VisualLabel.alignment = TextAlignmentOptions.Center;
        feature.VisualLabel.raycastTarget = false;

        // Selection indicator
        feature.SelectionIndicator = new GameObject("Selection Indicator");
        feature.SelectionIndicator.transform.SetParent(feature.VisualRoot.transform);
        Image selectionImage = feature.SelectionIndicator.AddComponent<Image>();
        selectionImage.color = new Color(0f, 0f, 0f, 0.3f);
        selectionImage.raycastTarget = false;
        feature.SelectionIndicator.SetActive(false);

        // Draw
        RedrawPointFeature(feature);

        Debug.Log($"Created PointFeature visuals for feature {feature.Id}");
    }
    public void RedrawPointFeature(PointFeature feature)
    {
        // Icon
        feature.VisualIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(feature.Def.IconSize, feature.Def.IconSize);
        feature.VisualIcon.gameObject.SetActive(feature.Def.Icon != null);
        feature.VisualIcon.sprite = feature.Def.Icon;

        // Label
        feature.VisualLabel.color = Color.black;
        feature.VisualLabel.textWrappingMode = TextWrappingModes.NoWrap; // Single line only
        feature.VisualLabel.fontSize = feature.Def.LabelFontSize;
        feature.VisualLabel.text = feature.Label;

        // Label size
        Canvas.ForceUpdateCanvases();
        feature.VisualLabel.ForceMeshUpdate();
        Vector2 labelDimensions = feature.VisualLabel.GetPreferredValues(feature.VisualLabel.text);
        feature.VisualLabel.GetComponent<RectTransform>().sizeDelta = labelDimensions;

        // Label local position
        Vector2 labelLocalPosition;
        if (feature.Def.Icon == null) labelLocalPosition = Vector3.zero;
        else labelLocalPosition = new Vector3(0f, feature.Def.IconSize / 2 + labelDimensions.y / 2 + 5, 0f);
        feature.VisualLabel.transform.localPosition = labelLocalPosition;

        // Selection indicator
        Vector2 selectionIndicatorMargins = new Vector2(12, 4);
        feature.SelectionIndicator.GetComponent<RectTransform>().sizeDelta = labelDimensions + selectionIndicatorMargins;
        feature.SelectionIndicator.transform.localPosition = labelLocalPosition;
    }


    public void CreateLineFeatureVisuals(LineFeature line)
    {
        // Root
        line.VisualRoot = new GameObject($"Line_{line.Id}");
        line.VisualRoot.transform.SetParent(MapRoot.transform);

        // Line
        CreateLineFeature(line);

        // Selection Indicator
        CreateLineFeatureSelectionIndicator(line);
        line.ResetSelectionIndicatorColor();
    }

    public void CreateLineFeature(LineFeature line)
    {
        line.VisualLine = new GameObject($"Line");
        line.VisualLine.transform.SetParent(line.VisualRoot.transform);
        LineRenderer lr = line.VisualLine.AddComponent<LineRenderer>();
        lr.textureMode = LineTextureMode.Tile;

        RedrawLineFeature(line);
    }
    private void RedrawLineFeature(LineFeature line)
    {
        LineRenderer lr = line.VisualLine.GetComponent<LineRenderer>();
        lr.material = line.Def.Material;
        lr.startColor = line.Def.Color;
        lr.endColor = line.Def.Color;
        lr.startWidth = line.Def.Width;
        lr.endWidth = line.Def.Width;
        lr.numCornerVertices = line.Def.RoundedCorners ? 5 : 0;

        float textureScale = 0.5f / line.Def.Width;
        lr.textureScale = new Vector2(textureScale, 1f);

        lr.positionCount = line.Points.Count;
        for (int i = 0; i < line.Points.Count; i++) lr.SetPosition(i, line.Points[i].Position);

        ApplySortingOrder(lr, MapZLayer.LineFeature, orderOffset: line.RenderLayer);
    }

    public void CreateLineFeatureSelectionIndicator(LineFeature line)
    {
        line.SelectionIndicator = new GameObject($"Selection Indicator");
        line.SelectionIndicator.transform.SetParent(line.VisualRoot.transform);
        LineRenderer lr = line.SelectionIndicator.AddComponent<LineRenderer>();
        lr.material = ResourceManager.LoadMaterial("Materials/LineMaterials/Default");

        RedrawLineSelectionIndicator(line);
        line.SelectionIndicator.SetActive(false);
    }
    private void RedrawLineSelectionIndicator(LineFeature line)
    {
        LineRenderer lr = line.SelectionIndicator.GetComponent<LineRenderer>();
        lr.startColor = new Color(line.Def.Color.r, line.Def.Color.g, line.Def.Color.b, LINE_SELECTION_INDICATOR_ALPHA);
        lr.endColor = new Color(line.Def.Color.r, line.Def.Color.g, line.Def.Color.b, LINE_SELECTION_INDICATOR_ALPHA);
        lr.startWidth = line.Def.Width + LINE_SELECTION_INDICATOR_WIDTH;
        lr.endWidth = line.Def.Width + LINE_SELECTION_INDICATOR_WIDTH;

        lr.positionCount = line.Points.Count;
        for (int i = 0; i < line.Points.Count; i++) lr.SetPosition(i, line.Points[i].Position);

        ApplySortingOrder(lr, MapZLayer.LineFeatureSelectionIndicator);
    }



    public void CreateAreaFeatureVisuals(AreaFeature area)
    {
        area.VisualRoot = new GameObject($"Area_{area.Id}");
        area.VisualRoot.transform.SetParent(MapRoot.transform);

        // Polygon
        RedrawAreaFeaturePolygon(area);

        // Border
        RedrawAreaFeatureOutline(area);

        // Selection Indicator
        RedrawAreaFeatureSelectionIndicator(area);
        area.ResetSelectionIndicatorColor();
    }

    private void RedrawAreaFeaturePolygon(AreaFeature area)
    {
        // Destroy old polygon
        if (area.VisualPolygon != null) GameObject.Destroy(area.VisualPolygon);

        // Create new polygon
        area.VisualPolygon = MeshGenerator.GeneratePolygon(area.PointPositions);
        area.VisualPolygon.transform.SetParent(area.VisualRoot.transform);

        // Set color and render sorting order
        MeshRenderer polygonMeshRenderer = area.VisualPolygon.GetComponent<MeshRenderer>();
        polygonMeshRenderer.material.color = area.Def.Color;
        ApplySortingOrder(polygonMeshRenderer, MapZLayer.AreaFeaturePolygon, orderOffset: area.RenderLayer);
    }

    private void RedrawAreaFeatureOutline(AreaFeature area)
    {
        // Destroy old border
        if (area.VisualOutline != null) GameObject.Destroy(area.VisualOutline);

        if (area.Def.OutlineWidth > 0f)
        {
            // Create new border
            area.VisualOutline = MeshGenerator.CreateSinglePolygonBorder(area.PointPositions, width: area.Def.OutlineWidth, area.Def.OutlineColor, area.IsClockwise);
            area.VisualOutline.transform.SetParent(area.VisualRoot.transform);

            // Set color and render sorting order
            MeshRenderer borderMeshRenderer = area.VisualOutline.GetComponent<MeshRenderer>();
            borderMeshRenderer.material.color = area.Def.OutlineColor;
            ApplySortingOrder(borderMeshRenderer, MapZLayer.AreaFeatureOutline);
        }
    }

    private void RedrawAreaFeatureSelectionIndicator(AreaFeature area)
    {
        // Destroy old border
        bool wasOldBorderActive = false;
        if (area.SelectionIndicator != null)
        {
            GameObject.Destroy(area.SelectionIndicator);
            wasOldBorderActive = area.SelectionIndicator.activeSelf;
        }

        // Create new border
        area.SelectionIndicator = MeshGenerator.CreateSinglePolygonBorder(area.PointPositions, width: AREA_SELECTION_INDICATOR_WIDTH, AREA_SELECTION_INDICATOR_COLOR, area.IsClockwise);
        area.SelectionIndicator.transform.SetParent(area.VisualRoot.transform);

        // Set color and render sorting order
        MeshRenderer borderMeshRenderer = area.SelectionIndicator.GetComponent<MeshRenderer>();
        borderMeshRenderer.material.color = AREA_SELECTION_INDICATOR_COLOR;
        ApplySortingOrder(borderMeshRenderer, MapZLayer.AreaFeatureSelectionIndicator);
        area.SelectionIndicator.SetActive(wasOldBorderActive);
    }

    public void RedrawFeature(MapFeature feat)
    {
        // Note: Point features dont need manual redraws since they are redrawn every frame in Update() because they are screen-space UI elements.
        if (feat is LineFeature line)
        {
            RedrawLineFeature(line);
            RedrawLineSelectionIndicator(line);
        }
        if (feat is AreaFeature area)
        {
            RedrawAreaFeaturePolygon(area);
            RedrawAreaFeatureOutline(area);
            RedrawAreaFeatureSelectionIndicator(area);
        }
    }

    #endregion
}
