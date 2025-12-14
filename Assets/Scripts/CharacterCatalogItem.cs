using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CharacterCatalogItem : MonoBehaviour
{
    public Button button;
    public Image portraitImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI atkText;

    private CharacterCatalogPanel owner;
    private int characterIndex;
    private Action<int> onClickOverride;

    public void Setup(CharacterCatalogPanel owner, int characterIndex,
                      CharacterInstance instance, int hp, int atk,
                      Action<int> onClickOverride = null,
                      bool greyedOut = false)
    {
        this.owner = owner;
        this.characterIndex = characterIndex;
        this.onClickOverride = onClickOverride;

        if (instance != null && instance.data != null)
        {
            if (portraitImage != null)
                portraitImage.sprite = instance.data.silhouetteSprite;

            if (nameText != null)
                nameText.text = instance.data.displayName;

            if (levelText != null)
                levelText.text = "Lv " + instance.level;

            if (hpText != null)
                hpText.text = "HP: " + hp;

            if (atkText != null)
                atkText.text = "ATK: " + atk;
        }

        SetGreyedOut(greyedOut);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    public void SetGreyedOut(bool greyed)
    {
        float a = greyed ? 0.45f : 1f;

        if (portraitImage != null)
        {
            var c = portraitImage.color;
            portraitImage.color = new Color(c.r, c.g, c.b, a);
        }

        if (nameText != null) nameText.alpha = a;
        if (levelText != null) levelText.alpha = a;
        if (hpText != null) hpText.alpha = a;
        if (atkText != null) atkText.alpha = a;
    }

    private void OnClick()
    {
        if (onClickOverride != null)
        {
            onClickOverride(characterIndex);
            return;
        }

        if (owner != null)
            owner.OnClickItem(characterIndex);
    }
}