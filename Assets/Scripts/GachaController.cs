using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GachaController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI premiumCurrencyText;
    public TextMeshProUGUI resultText;
    public Button pullButton;

    [Header("Gacha Settings")]
    public int pullCost = 100;
    public CharacterData[] gachaPool; 

    private void Start()
    {
        if (pullButton != null)
            pullButton.onClick.AddListener(OnClick_Pull);

        RefreshCurrency();
        ShowWelcomeText();
    }

    private void ShowWelcomeText()
    {
        if (resultText != null)
        {
            resultText.text = "Tap Pull to summon a new Soundborn!";
        }
    }

    private void RefreshCurrency()
    {
        var gm = GameManager.Instance;
        if (gm == null || premiumCurrencyText == null)
            return;

        premiumCurrencyText.text = gm.playerData.premiumCurrency.ToString();
    }

    public void OnClick_Pull()
    {
        var gm = GameManager.Instance;
        if (gm == null)
            return;

        var data = gm.playerData;

        if (data.premiumCurrency < pullCost)
        {
            if (resultText != null)
                resultText.text = "Not enough gems!";
            return;
        }

        data.premiumCurrency -= pullCost;

        if (gachaPool == null || gachaPool.Length == 0)
        {
            if (resultText != null)
                resultText.text = "Gacha pool is empty!";
            RefreshCurrency();
            return;
        }

        int roll = Random.Range(0, gachaPool.Length);
        CharacterData picked = gachaPool[roll];

        CharacterInstance newInstance = new CharacterInstance(picked);
        data.ownedCharacters.Add(newInstance);

        RefreshCurrency();

        if (resultText != null && picked != null)
        {
            string namePart = picked.displayName;
            string elementPart = picked.element.ToString();
            resultText.text = $"You pulled {namePart} ({elementPart})!";
        }
    }

    public void OnClick_Back()
    {
        SceneManager.LoadScene("HomeScene");
    }
}