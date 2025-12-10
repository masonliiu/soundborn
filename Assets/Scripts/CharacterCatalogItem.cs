using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    public void Setup(CharacterCatalogPanel owner, int characterIndex,
                      CharacterInstance instance, int hp, int atk)
    {
        this.owner = owner;
        this.characterIndex = characterIndex;

        if (portraitImage != null &&
            instance.data != null &&
            instance.data.silhouetteSprite != null)
        {
            portraitImage.sprite = instance.data.silhouetteSprite;
        }

        if (nameText != null)
            nameText.text = instance.data != null ? instance.data.displayName : "???";

        if (levelText != null)
            levelText.text = "Lv. " + instance.level;

        if (hpText != null)
            hpText.text = "HP: " + hp;

        if (atkText != null)
            atkText.text = "ATK: " + atk;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (owner != null)
            owner.OnClickItem(characterIndex);
    }
}