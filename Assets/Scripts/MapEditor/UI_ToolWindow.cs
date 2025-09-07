using TMPro;
using UnityEngine;

public class UI_ToolWindow : MonoBehaviour
{
    [Header("Elements")]
    public GameObject ToolsContainer;
    public TextMeshProUGUI TitleText;
    public TextMeshProUGUI InstructionsText;

    public void SelectTool(EditorTool tool)
    {
        TitleText.text = tool.Name;
        InstructionsText.text = "";
        for (int i = 0; i < ToolsContainer.transform.childCount; i++) ToolsContainer.transform.GetChild(i).gameObject.SetActive(false);
        tool.gameObject.SetActive(true);
    }
}