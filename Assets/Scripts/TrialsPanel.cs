using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrialsPanel : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("Buttons")]
    public Button resonanceTrialButton;
    public Button equipmentTrialButton;

    [Header("Optional Text")]
    public TextMeshProUGUI resonanceStatusText;
    public TextMeshProUGUI equipmentStatusText;

    private const int DefaultTier = 1;
    private HomeUIController homeUI;

    private void Awake()
    {
        if (resonanceTrialButton != null)
            resonanceTrialButton.onClick.AddListener(OnClick_ResonanceTrial);

        if (equipmentTrialButton != null)
            equipmentTrialButton.onClick.AddListener(OnClick_EquipmentTrial);
    }

    public void Show(HomeUIController home)
    {
        homeUI = home;

        if (root != null)
            root.SetActive(true);

        Refresh();
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null)
            return;

        RefreshTrialButton(
            resonanceTrialButton,
            resonanceStatusText,
            gm,
            TrialType.Resonance,
            "Resonance Trial"
        );

        RefreshTrialButton(
            equipmentTrialButton,
            equipmentStatusText,
            gm,
            TrialType.Equipment,
            "Equipment Trial"
        );
    }

    private void RefreshTrialButton(Button button, TextMeshProUGUI statusText, GameManager gm, TrialType type, string fallbackName)
    {
        var trial = gm.trialConfig != null ? gm.trialConfig.GetTrial(type, DefaultTier) : null;
        bool unlocked = trial != null && gm.IsTrialUnlocked(trial);

        if (button != null)
            button.interactable = unlocked;

        if (statusText == null)
            return;

        if (trial == null)
        {
            statusText.text = fallbackName + "\nNot configured";
        }
        else if (unlocked)
        {
            statusText.text = trial.displayName + "\nUnlocked";
        }
        else
        {
            statusText.text = trial.displayName + $"\nUnlocks after Floor {trial.unlockTowerFloor}";
        }
    }

    public void OnClick_ResonanceTrial()
    {
        StartTrial(TrialType.Resonance);
    }

    public void OnClick_EquipmentTrial()
    {
        StartTrial(TrialType.Equipment);
    }

    private void StartTrial(TrialType type)
    {
        var gm = GameManager.Instance;
        if (gm == null)
            return;

        gm.StartTrialBattle(type, DefaultTier);
    }

    public void OnClick_Close()
    {
        Hide();
    }
}
