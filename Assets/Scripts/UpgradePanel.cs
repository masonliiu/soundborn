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

    private CharacterInstance targetInstance;
    private int targetCharacterIndex = -1;  

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void Show(HomeUIController home)
    {
        ShowForCharacter(home, -1);
    }

    /// <summary>
    /// Show upgrade panel for a specific character.
    /// characterIndex = -1 is for the active character.
    /// </summary>
    public void ShowForCharacter(HomeUIController home, int characterIndex)
    {
        homeUI = home;
        targetCharacterIndex = characterIndex;

        var gm = GameManager.Instance;
        if (gm == null) return;

        if (characterIndex >= 0 &&
            gm.playerData.ownedCharacters != null &&
            characterIndex < gm.playerData.ownedCharacters.Count)
        {
            targetInstance = gm.playerData.ownedCharacters[characterIndex];
        }
        else
        {
            targetInstance = gm.GetActiveCharacterInstance();
        }

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

        if (targetInstance == null)
        {
            if (targetCharacterIndex >= 0 &&
                gm.playerData.ownedCharacters != null &&
                targetCharacterIndex < gm.playerData.ownedCharacters.Count)
            {
                targetInstance = gm.playerData.ownedCharacters[targetCharacterIndex];
            }
            else
            {
                targetInstance = gm.GetActiveCharacterInstance();
            }
        }

        if (targetInstance == null || targetInstance.data == null)
        {
            if (nameText != null) nameText.text = "No character";
            if (levelText != null) levelText.text = "";
            if (costText != null) costText.text = "";
            if (softCurrencyText != null) softCurrencyText.text = "";
            if (levelUpButton != null) levelUpButton.interactable = false;
            return;
        }

        int cost = gm.GetLevelUpCost(targetInstance);

        if (nameText != null)
            nameText.text = targetInstance.data.displayName;

        if (levelText != null)
            levelText.text = "Level " + targetInstance.level;

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
        if (gm == null || targetInstance == null) return;

        if (gm.TryLevelUpCharacter(targetInstance))
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