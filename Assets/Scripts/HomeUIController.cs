using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HomeUIController : MonoBehaviour
{
    [Header("Top Bar")]
    public TextMeshProUGUI softCurrencyText;
    public TextMeshProUGUI premiumCurrencyText;
    public Image playerPortrait;

    [Header("Tower / Player")]
    public TextMeshProUGUI floorText;
    public TextMeshProUGUI playerLevelText;

    [Header("Panels")]
    public TeamSelectPanel teamSelectPanel;
    public CharacterCatalogPanel characterCatalogPanel;
    public UpgradePanel upgradePanel;
    public InventoryPanel inventoryPanel;
    public TrialsPanel trialsPanel;
    public GameObject onboardingPanel; // assign HomeOnboardingController panel

    private void Start()
    {
        Refresh();
        MaybeShowOnboarding();
    }

    public void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        var playerData = gm.playerData;

        if (softCurrencyText != null)
            softCurrencyText.text = playerData.softCurrency.ToString();

        if (premiumCurrencyText != null)
            premiumCurrencyText.text = playerData.premiumCurrency.ToString();

        if (floorText != null)
            floorText.text = $"Floor {gm.GetFloorLabel(playerData.towerCurrentFloor)}";

        if (playerLevelText != null)
            playerLevelText.text = $"Lv {playerData.playerLevel}";

        var active = gm.GetActiveCharacterInstance();
        if (playerPortrait != null &&
            active != null &&
            active.data != null &&
            active.data.silhouetteSprite != null)
        {
            playerPortrait.sprite = active.data.silhouetteSprite;
        }
    }

    private void MaybeShowOnboarding()
    {
        var gm = GameManager.Instance;
        if (gm == null || onboardingPanel == null) return;

        // Show only once per profile
        if (!gm.playerData.homeTipsSeen)
        {
            onboardingPanel.SetActive(true);
        }
    }

    public void OnClick_ClimbTower()
    {
        var gm = GameManager.Instance;
        if (gm != null)
            gm.StartTowerBattle();
        else
            SceneManager.LoadScene("BattleScene");
    }

    public void OnClick_ResonanceTrialTier1()
    {
        StartTrial(TrialType.Resonance, 1);
    }

    public void OnClick_EquipmentTrialTier1()
    {
        StartTrial(TrialType.Equipment, 1);
    }

    private void StartTrial(TrialType type, int tier)
    {
        var gm = GameManager.Instance;
        if (gm != null)
            gm.StartTrialBattle(type, tier);
    }

    public void OnClick_Trials()
    {
        if (trialsPanel != null)
            trialsPanel.Show(this);
    }

    public void OnClick_Characters()
    {
        if (characterCatalogPanel != null)
            characterCatalogPanel.Show(this);
    }

    public void OnClick_Team()
    {
        if (teamSelectPanel != null)
            teamSelectPanel.Show(this);
    }

    public void OnClick_Gacha()
    {
        SceneManager.LoadScene("GachaScene");
    }

    public void OnClick_Inventory()
    {
        if (inventoryPanel != null)
            inventoryPanel.Show();
    }
}
