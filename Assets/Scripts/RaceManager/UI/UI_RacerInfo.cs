using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_RacerInfo : MonoBehaviour
{
    private Racer Racer;

    [Header("Elements")]
    public TextMeshProUGUI Label;
    public TextMeshProUGUI SpeedText;

    public Button RankBeforeButton;
    public TextMeshProUGUI RankBeforeText;
    public TextMeshProUGUI GapToBeforeText;
    public TextMeshProUGUI RankText;
    public TextMeshProUGUI GapToAfterText;
    public TextMeshProUGUI RankAfterText;
    public Button RankAfterButton;


    public UI_ProgressBar StaminaBar;
    public Button FollowButton;

    // Debug
    private NavigationPath PreviewPath;

    private void Awake()
    {
        FollowButton.onClick.AddListener(FollowButton_OnClick);
        RankBeforeButton.onClick.AddListener(RankToBeforeButton_OnClick);
        RankAfterButton.onClick.AddListener(RankToAfterButton_OnClick);
    }

    private void RankToBeforeButton_OnClick()
    {
        if (CameraHandler.Instance.FollowedEntity == Racer) Racer.Race.PanToAndFollowRacer(Racer.CurrentRacerInFront); // Change follow if currently following this
        Racer.Race.SelectRacer(Racer.CurrentRacerInFront);
    }
    private void RankToAfterButton_OnClick()
    {
        if (CameraHandler.Instance.FollowedEntity == Racer) Racer.Race.PanToAndFollowRacer(Racer.CurrentRacerInBack); // Change follow if currently following this
        Racer.Race.SelectRacer(Racer.CurrentRacerInBack);
    }

    private void FollowButton_OnClick()
    {
        if (Racer == null) return;
        Racer.Race.PanToAndFollowRacer(Racer);
        HelperFunctions.UnfocusNonInputUiElements();
    }

    public void Show(Racer racer)
    {
        gameObject.SetActive(true);
        Racer = racer;
        Label.text = racer.Name;
        Label.color = racer.Color;
        UpdateDynamicValues();
    }

    private void Update()
    {
        UpdateDynamicValues();

        if(Input.GetKeyDown(KeyCode.P))
        {
            if(Racer != null)
            {
                Racer?.BestGeneralPathToFin?.ShowPreview(2f, Racer.BestGeneralPathIsFromTransitionStart ? Color.blue : Color.red, LineTexture.DottedRound);
                PreviewPath = Racer.BestGeneralPathToFin;
            }
        }
    }   

    private void UpdateDynamicValues()
    {
        SpeedText.text = $"{(Racer.CurrentSpeed * 3.6f).ToString("F2")}  km/h ({Racer.CurrentMovementMode.Verb})";

        // Before
        if(Racer.IsCurrentlyFirst() || Racer.IsFinished)
        {
            if (RankBeforeButton.gameObject.activeSelf) RankBeforeButton.gameObject.SetActive(false);
            GapToBeforeText.text = "";
        }
        else
        {
            if (!RankBeforeButton.gameObject.activeSelf) RankBeforeButton.gameObject.SetActive(true);
            RankBeforeText.text = $"{Racer.CurrentRank - 1}.";
            GapToBeforeText.text = "-" + (int)Racer.CurrentDistanceToRacerInFront + "m";
        }

        // Own
        RankText.text = $"{Racer.CurrentRank}.";

        // Behind
        if (Racer.IsCurrentlyLast() || Racer.IsFinished)
        {
            if (RankAfterButton.gameObject.activeSelf) RankAfterButton.gameObject.SetActive(false);
            GapToAfterText.text = "";
        }
        else
        {
            if (!RankAfterButton.gameObject.activeSelf) RankAfterButton.gameObject.SetActive(true);
            RankAfterText.text = $"{Racer.CurrentRank + 1}.";
            GapToAfterText.text = "+" + (int)Racer.CurrentDistanceToRacerInBack + "m";
        }

        StaminaBar.SetValue(Racer.Stamina, Racer.MAX_STAMINA, ProgressBarTextType.Percent);
    }

    public void Hide()
    {
        PreviewPath?.HidePreview();
        PreviewPath = null;

        gameObject.SetActive(false);
    }
}
