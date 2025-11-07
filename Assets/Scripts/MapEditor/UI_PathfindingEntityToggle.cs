using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_PathfindingEntityToggle : MonoBehaviour
{
    [Header("Elements")]
    public Toggle Toggle;
    public Image ColorKnob;
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI PathCostText;

    public void Init(Entity e)
    {
        ColorKnob.color = e.Color;
        NameText.text = e.Name;
        PathCostText.text = "";
    }
}
