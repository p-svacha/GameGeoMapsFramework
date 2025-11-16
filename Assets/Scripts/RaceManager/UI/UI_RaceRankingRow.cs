using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_RaceRankingRow : MonoBehaviour
{
    private Racer Racer;

    [Header("Elements")]
    public Button Button;
    public Image Background;
    public TextMeshProUGUI RankText;
    public TextMeshProUGUI NameText;
    public Image MovementModeImage;
    public TextMeshProUGUI MovementModeText;
    public UI_ProgressBar StaminaBar;
    public TextMeshProUGUI GapText;

    // Double click config
    private float LastClickTime = -1f;
    private const float DoubleClickMaxDelay = 0.25f;

    public void Init(Racer racer)
    {
        Button.onClick.AddListener(OnClick);
        Racer = racer;
    }

    private void OnClick()
    {
        HelperFunctions.UnfocusNonInputUiElements();

        float now = Time.unscaledTime;

        if (now - LastClickTime <= DoubleClickMaxDelay)
        {
            // Double click
            OnDoubleClick();
            LastClickTime = -1f; // reset
        }
        else
        {
            // Single click
            Racer.Race.SelectRacer(Racer);
            LastClickTime = now;
        }
    }

    private void OnDoubleClick()
    {
        Racer.Race.PanToAndFollowRacer(Racer);
    }

    public void UpdateValues()
    {
        RankText.text = Racer.CurrentRank + ".";
        NameText.text = Racer.Name;
        StaminaBar.SetValue(Racer.Stamina, Racer.MAX_STAMINA, ProgressBarTextType.NoText);

        // Gap
        if(Racer.IsFinished)
        {
            GapText.text = "Finished";
        }
        else
        {
            GapText.text = Racer.CurrentRank == 1 ? "-" : "+" + Racer.CurrentTimeGapToRacerInFront.GetAsDuration(millisecondDigits: 1);
        }
    }

    public void ShowAsSelected(bool value)
    {
        Background.color = new Color(1f, 1f, 1f, value ? 0.1f : 0.01f); 
    }
}
