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

    [Header("Panels")]
    public TeamSelectPanel teamSelectPanel;
    public UpgradePanel upgradePanel;

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDataChanged += Refresh;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDataChanged -= Refresh;
        }
    }

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null)
            return;

        var playerData = gm.playerData;

        if (softCurrencyText != null)
            softCurrencyText.text = playerData.softCurrency.ToString();

        if (premiumCurrencyText != null)
            premiumCurrencyText.text = playerData.premiumCurrency.ToString();

        var active = gm.GetActiveCharacterInstance();
        if (active != null && active.data != null && active.data.silhouetteSprite != null && playerPortrait != null)
        {
            playerPortrait.sprite = active.data.silhouetteSprite;
        }
    }

    public void OnClick_ClimbTower()
    {
        SceneManager.LoadScene("BattleScene");
    }

    public void OnClick_OpenTeamSelect()
    {
        if (teamSelectPanel != null)
            teamSelectPanel.Show(this);
    }

    public void OnClick_OpenUpgrade()
    {
        if (upgradePanel != null)
            upgradePanel.Show(this);
    }

    public void OnClick_Characters()
    {
        SceneManager.LoadScene("CharactersScene");
    }

    public void OnClick_Gacha()
    {
        SceneManager.LoadScene("GachaScene");
    }
}