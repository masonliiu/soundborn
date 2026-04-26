using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UpgradePanel : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("Character")]
    public Image iconImage;

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

    public void ShowForCharacter(HomeUIController home, CharacterInstance character)
    {
        homeUI = home;
        targetCharacterIndex = -1;
        targetInstance = character;

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
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
            if (levelText != null) levelText.text = "";
            if (costText != null) costText.text = "";
            if (softCurrencyText != null) softCurrencyText.text = "";
            if (hpText != null) hpText.text = "";
            if (atkText != null) atkText.text = "";
            if (levelUpButton != null) levelUpButton.interactable = false;
            return;
        }

        gm.EnsureCharacterLevelCap(targetInstance);
        bool atCap = gm.IsCharacterAtLevelCap(targetInstance);
        int notesCost = gm.GetLevelUpNotesCost(targetInstance);
        int expCost = gm.GetLevelUpExpCost(targetInstance);
        targetInstance.GetTotalStats(out int hp, out int atk, out _, out _);

        if (nameText != null)
            nameText.text = targetInstance.data.displayName;

        if (iconImage != null)
        {
            iconImage.sprite = targetInstance.data.silhouetteSprite;
            iconImage.enabled = targetInstance.data.silhouetteSprite != null;
            iconImage.preserveAspect = true;
        }

        if (levelText != null)
            levelText.text = $"Level {targetInstance.level}/{targetInstance.levelCap}";

        if (costText != null)
        {
            if (atCap && gm.GetNextLevelCapRequirement(targetInstance, out int tier, out int amount, out int nextCap))
            {
                int owned = gm.GetResonanceMaterialCount(tier);
                costText.text = $"Limit Break: {amount} {GetResonanceMaterialName(tier)} ({owned}/{amount})\nUnlocks level {nextCap}";
            }
            else if (atCap)
            {
                costText.text = "Max level reached";
            }
            else
            {
                costText.text = $"Level Up: {expCost} Character EXP, {notesCost} Notes";
            }
        }

        if (softCurrencyText != null)
            softCurrencyText.text = $"Notes: {gm.playerData.softCurrency}  EXP: {gm.playerData.characterExp}";

        if (hpText != null)
            hpText.text = "HP: " + hp;

        if (atkText != null)
            atkText.text = "ATK: " + atk;

        if (levelUpButton != null)
        {
            levelUpButton.interactable = CanPressUpgradeButton(gm, targetInstance, atCap, notesCost, expCost);
            var label = levelUpButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = atCap ? "Limit Break" : "Level Up";
        }
    }

    public void OnClick_LevelUp()
    {
        var gm = GameManager.Instance;
        if (gm == null || targetInstance == null) return;

        bool changed = gm.IsCharacterAtLevelCap(targetInstance)
            ? gm.TryBreakCharacterLevelCap(targetInstance)
            : gm.TryLevelUpCharacter(targetInstance);

        if (changed)
        {
            Refresh();

            if (levelUpFeedbackRoutine != null)
                StopCoroutine(levelUpFeedbackRoutine);

            levelUpFeedbackRoutine = StartCoroutine(LevelUpFeedback());

            if (homeUI != null)
                homeUI.Refresh();
        }
    }

    private bool CanPressUpgradeButton(GameManager gm, CharacterInstance inst, bool atCap, int notesCost, int expCost)
    {
        if (gm == null || gm.playerData == null || inst == null)
            return false;

        if (!atCap)
            return gm.playerData.softCurrency >= notesCost && gm.playerData.characterExp >= expCost;

        if (!gm.GetNextLevelCapRequirement(inst, out int tier, out int amount, out _))
            return false;

        return gm.GetResonanceMaterialCount(tier) >= amount;
    }

    private string GetResonanceMaterialName(int tier)
    {
        return "Resonance " + tier;
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

    public void OnClick_Close()
    {
        Hide();
    }
}
