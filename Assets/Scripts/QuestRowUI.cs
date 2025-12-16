using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Displays a single quest entry with progress and claim button.
/// Uses a Slider for progress (0..1) with an optional numeric label.
/// The rectangular border is provided by the prefab layout (e.g. an Image background).
/// </summary>
public class QuestRowUI : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public Slider progressSlider;          // visual progress bar
    public TextMeshProUGUI progressLabel;  // e.g. "1/3" on top of the slider
    public Button claimButton;

    private string questId;
    private int targetCount;

    private void Awake()
    {
        if (claimButton != null)
            claimButton.onClick.AddListener(OnClickClaim);
    }

    public void Bind(QuestData def, QuestState state)
    {
        questId = def.questId;
        targetCount = Mathf.Max(1, def.targetCount);

        if (titleText != null)
            titleText.text = def.title;


        float ratio = Mathf.Clamp01((float)state.currentCount / targetCount);
        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = ratio;
        }

        if (progressLabel != null)
            progressLabel.text = $"{state.currentCount}/{targetCount}";

        if (claimButton != null)
            claimButton.interactable = state.isCompleted && !state.isClaimed;
    }

    private void OnClickClaim()
    {
        if (QuestManager.Instance != null && !string.IsNullOrEmpty(questId))
        {
            QuestManager.Instance.ClaimReward(questId);
        }
    }
}

