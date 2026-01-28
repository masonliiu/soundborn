using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemEquipRowUI : MonoBehaviour
{
    public Button button;
    public Image portraitImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public GameObject equippedOverlay;
    public TextMeshProUGUI equippedText;

    public void Bind(CharacterInstance character, bool equippedByThis, System.Action onClick)
    {
        if (character == null || character.data == null)
            return;

        if (portraitImage != null && character.data.silhouetteSprite != null)
            portraitImage.sprite = character.data.silhouetteSprite;

        if (nameText != null)
            nameText.text = character.data.displayName;

        if (levelText != null)
            levelText.text = $"Lv {character.level}";

        if (equippedOverlay != null)
            equippedOverlay.SetActive(equippedByThis);

        if (equippedText != null)
            equippedText.text = equippedByThis ? "Equipped" : "";

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            if (onClick != null)
                button.onClick.AddListener(() => onClick());
            button.interactable = !equippedByThis;
        }

        float alpha = equippedByThis ? 0.5f : 1f;
        if (portraitImage != null)
        {
            var color = portraitImage.color;
            portraitImage.color = new Color(color.r, color.g, color.b, alpha);
        }

        if (nameText != null)
            nameText.alpha = alpha;
        if (levelText != null)
            levelText.alpha = alpha;
    }
}
