using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Simple multi-step home onboarding. Attach to a UI panel with a text and a next button.
/// </summary>
public class HomeOnboardingController : MonoBehaviour
{
    [TextArea]
    public string[] steps = new string[]
    {
        "Welcome to Soundborn! This is your Home base.",
        "Tap Team to set your lineup before battles.",
        "Tap Battle to climb floors and earn rewards.",
        "Use Inventory/Upgrade to power up your characters."
    };

    public TextMeshProUGUI messageText;
    public Button nextButton;

    private int stepIndex = 0;

    private void Awake()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNext);
    }

    private void OnEnable()
    {
        stepIndex = 0;
        UpdateText();
    }

    private void OnNext()
    {
        stepIndex++;
        if (stepIndex >= steps.Length)
        {
            CompleteOnboarding();
            return;
        }
        UpdateText();
    }

    private void UpdateText()
    {
        if (messageText != null && steps != null && steps.Length > 0)
        {
            int idx = Mathf.Clamp(stepIndex, 0, steps.Length - 1);
            messageText.text = steps[idx];
        }
    }

    private void CompleteOnboarding()
    {
        gameObject.SetActive(false);
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.playerData.homeTipsSeen = true;
            gm.playerData.onboardingCompleted = true;
            gm.NotifyPlayerDataChanged();
            gm.SavePlayerData();
        }
    }
}


