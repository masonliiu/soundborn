using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HomeUIController : MonoBehaviour
{
    public TextMeshProUGUI softCurrencyText;
    public TextMeshProUGUI premiumCurrencyText;
    public Image playerPortrait;

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        var gm = GameManager.Instance;
        var playerData = gm.playerData;

        // update currencies (for now just leave them 0)
        softCurrencyText.text = playerData.softCurrency.ToString();
        premiumCurrencyText.text = playerData.premiumCurrency.ToString();

        // player portrait (use CharacterData.silhouetteSprite)
        var active = gm.GetActiveCharacterInstance();
        if (active != null && active.data.silhouetteSprite != null)
        {
            playerPortrait.sprite = active.data.silhouetteSprite;
        }
    }

    public void OnClick_ClimbTower()
    {
        SceneManager.LoadScene("BattleScene");
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