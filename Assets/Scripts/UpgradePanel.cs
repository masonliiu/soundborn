using System.Collections;
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
    public TextMeshProUGUI hpText;     
    public TextMeshProUGUI atkText;     
    [Header("Buttons")]
    public Button levelUpButton;

    private HomeUIController homeUI;

    private CharacterInstance targetInstance;
    private int targetCharacterIndex = -1;

    private Coroutine levelUpFeedbackRoutine;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void Show(HomeUIController home)
    {
        ShowForCharacter(home, -1);
    }

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
            if (hpText != null) hpText.text = "";
            if (atkText != null) atkText.text = "";
            if (levelUpButton != null) levelUpButton.interactable = false;
            return;
        }

        int cost = gm.GetLevelUpCost(targetInstance);
        GetLeveledStats(targetInstance, out int hp, out int atk);

        if (nameText != null)
            nameText.text = targetInstance.data.displayName;

        if (levelText != null)
            levelText.text = "Level " + targetInstance.level;

        if (costText != null)
            costText.text = "Cost: " + cost;

        if (softCurrencyText != null)
            softCurrencyText.text = "Gold: " + gm.playerData.softCurrency;

        if (hpText != null)
            hpText.text = "HP: " + hp;

        if (atkText != null)
            atkText.text = "ATK: " + atk;

        if (levelUpButton != null)
            levelUpButton.interactable = gm.playerData.softCurrency >= cost;
    }

    public void OnClick_LevelUp()
    {
        var gm = GameManager.Instance;
        if (gm == null || targetInstance == null) return;

        int oldLevel = targetInstance.level;

        if (gm.TryLevelUpCharacter(targetInstance))
        {
            Refresh();

            if (levelUpFeedbackRoutine != null)
                StopCoroutine(levelUpFeedbackRoutine);

            levelUpFeedbackRoutine = StartCoroutine(LevelUpFeedback());

            if (homeUI != null)
                homeUI.Refresh();
        }
    }

    private IEnumerator LevelUpFeedback()
    {
        if (levelText == null)
            yield break;

        Vector3 baseScale = levelText.transform.localScale;
        float duration = 0.35f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            float pulse = 1f + 0.25f * Mathf.Sin(p * Mathf.PI); // quick pop
            levelText.transform.localScale = baseScale * pulse;
            yield return null;
        }

        levelText.transform.localScale = baseScale;
    }

    private void GetLeveledStats(CharacterInstance inst, out int hp, out int atk)
    {
        hp = 0;
        atk = 0;

        if (inst == null || inst.data == null)
            return;

        int baseHP = inst.data.maxHP;
        int baseATK = inst.data.attack;

        int extraLevels = Mathf.Max(0, inst.level - 1);
        hp = baseHP + extraLevels * 10;
        atk = baseATK + extraLevels * 2;
    }

    public void OnClick_Close()
    {
        Hide();
    }
}