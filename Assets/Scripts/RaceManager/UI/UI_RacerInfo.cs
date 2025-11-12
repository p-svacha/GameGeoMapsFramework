using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_RacerInfo : MonoBehaviour
{
    private Racer Racer;

    [Header("Elements")]
    public TextMeshProUGUI Label;
    public TextMeshProUGUI SpeedText;
    public TextMeshProUGUI RankText;
    public UI_ProgressBar StaminaBar;
    public Button FollowButton;

    // Debug
    private NavigationPath PreviewPath;

    private void Awake()
    {
        FollowButton.onClick.AddListener(FollowButton_OnClick);
    }

    private void FollowButton_OnClick()
    {
        if (Racer == null) return;
        CameraHandler.Instance.PanTo(0.5f, Racer.CurrentWorldPosition, postPanFollowEntity: Racer);
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
        SpeedText.text = (Racer.CurrentSpeed * 3.6f).ToString("F2") + " km/h";
        string rev = Racer.BestGeneralPathIsFromTransitionStart ? " <rev> " : "";
        RankText.text = $"Current Rank: {Racer.CurrentRank} (distance to fin: {Racer.CurrentDistanceToFinish.ToString("F1")}{rev})";
        StaminaBar.SetValue(Racer.Stamina, Racer.MAX_STAMINA, ProgressBarTextType.Percent);
    }

    public void Hide()
    {
        PreviewPath?.HidePreview();
        PreviewPath = null;

        gameObject.SetActive(false);
    }
}
