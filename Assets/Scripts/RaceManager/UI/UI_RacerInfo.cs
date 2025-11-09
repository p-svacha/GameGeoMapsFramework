using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_RacerInfo : MonoBehaviour
{
    private Racer Racer;

    [Header("Elements")]
    public TextMeshProUGUI Label;
    public TextMeshProUGUI SpeedText;

    public void Show(Racer racer)
    {
        gameObject.SetActive(true);
        Racer = racer;
        Label.text = racer.Name;
        Label.color = racer.Color;
        SpeedText.text = (racer.CurrentSpeed * 3.6f).ToString("F2") + " km/h";
    }

    private void Update()
    {
        SpeedText.text = (Racer.CurrentSpeed * 3.6f).ToString("F2") + " km/h";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
