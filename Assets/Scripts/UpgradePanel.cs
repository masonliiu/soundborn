using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UpgradePanel : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("Texts")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI softCurrencyText;

    [Header("Buttons")]
    public Button levelUpButton;

    private HomeUIController homeUI;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);
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
        if (gm == null) return;

        var inst = gm.GetActiveCharacterInstance();
        if (inst == null || inst.data == null)
        {
            if (nameText != null) nameText.text = "No character";
            if (levelText != null) levelText.text = "";
            if (costText != null) costText.text = "";
            if (levelUpButton != null) levelUpButton.interactable = false;
            return;
        }

        int cost = gm.GetLevelUpCost(inst);

        if (nameText != null)
            nameText.text = inst.data.displayName;

        if (levelText != null)
            levelText.text = "Level " + inst.level;

        if (costText != null)
            costText.text = "Cost: " + cost;

        if (softCurrencyText != null)
            softCurrencyText.text = "Gold: " + gm.playerData.softCurrency;

        if (levelUpButton != null)
            levelUpButton.interactable = gm.playerData.softCurrency >= cost;
    }

    public void OnClick_LevelUp()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.TryLevelUpActiveCharacter())
        {
            Refresh();
            if (homeUI != null)
                homeUI.Refresh();
        }
    }

    public void OnClick_Close()
    {
        Hide();
    }
}