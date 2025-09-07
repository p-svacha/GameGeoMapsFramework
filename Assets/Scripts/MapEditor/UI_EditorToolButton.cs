using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_EditorToolButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private MapEditor Editor;
    public EditorTool Tool { get; private set; }

    [Header("Elements")]
    public Image Background;
    public Image Icon;

    public void Init(MapEditor editor, EditorTool tool)
    {
        Editor = editor;
        Tool = tool;
        Icon.sprite = tool.Icon;
        GetComponent<Button>().onClick.AddListener(() => editor.SelectTool(Tool.Id));
    }

    public void SetSelected(bool value)
    {
        Background.color = value ? MapEditor.ButtonSelectedColor : MapEditor.ButtonUnselectedColor;
    }

    // Called when the mouse enters the button
    public void OnPointerEnter(PointerEventData eventData)
    {
        Editor.ToolNamePanel.SetActive(true);
        Editor.ToolNameText.text = Tool.Name;
    }
    // Called when the mouse exits the button
    public void OnPointerExit(PointerEventData eventData)
    {
        Editor.ToolNamePanel.SetActive(false);
    }
}
