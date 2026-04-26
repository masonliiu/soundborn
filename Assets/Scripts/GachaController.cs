using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GachaController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI premiumCurrencyText;
    public TextMeshProUGUI resultText;
    public Button pullButton;

    [Header("Gacha Settings")]
    public int pullCost = 400;
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

        var pool = GetAvailablePool(gm);
        if (pool.Count == 0)
        {
            if (resultText != null)
                resultText.text = "No summon pool configured.";
            RefreshCurrency();
            return;
        }

        data.premiumCurrency -= pullCost;

        CharacterData picked = pool[Random.Range(0, pool.Count)];

        CharacterInstance newInstance = new CharacterInstance(picked);
        data.ownedCharacters.Add(newInstance);

        gm.NotifyPlayerDataChanged();
        gm.SavePlayerData();

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnGachaPulled();
        }

        RefreshCurrency();

        if (resultText != null && picked != null)
        {
            string namePart = picked.displayName;
            string elementPart = picked.element.ToString();
            resultText.text = $"You pulled {namePart} ({elementPart})!";
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Play("gacha");
        }
    }

    public void OnClick_Back()
    {
        SceneManager.LoadScene("HomeScene");
    }

    private List<CharacterData> GetAvailablePool(GameManager gm)
    {
        var pool = new List<CharacterData>();

        AddValidCharacters(pool, gachaPool);

        if (pool.Count == 0 && gm != null && gm.characterDatabase != null)
            AddValidCharacters(pool, gm.characterDatabase.allCharacters);

        return pool;
    }

    private void AddValidCharacters(List<CharacterData> pool, CharacterData[] characters)
    {
        if (pool == null || characters == null) return;

        foreach (var character in characters)
        {
            if (character != null && !pool.Contains(character))
                pool.Add(character);
        }
    }
}
