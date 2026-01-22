using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI lockText;
    public Button actionButton;
    public TextMeshProUGUI actionText;

    private ItemInstance instance;
    private bool isLocked;

    public void Init(ItemInstance inst, int playerLevel, bool equippedByActive, bool equippedByOther, System.Action onAction)
    {
        instance = inst;
        if (inst == null || inst.data == null)
        {
            if (nameText != null) nameText.text = "Empty";
            if (icon != null) icon.sprite = null;
            if (lockText != null) lockText.text = "";
            ConfigureAction(false, "", null);
            return;
        }

        if (nameText != null) nameText.text = inst.data.displayName;
        if (icon != null) icon.sprite = inst.data.icon;

        int requiredLevel = inst.data.unlockLevel;
        isLocked = playerLevel < requiredLevel;

        if (lockText != null)
        {
            lockText.text = isLocked ? $"Unlocks at Lv {requiredLevel}" : "";
        }

        var img = GetComponent<Image>();
        if (img != null)
        {
            img.color = isLocked ? new Color(1f, 1f, 1f, 0.4f) : Color.white;
        }

        if (inst.data.itemType == ItemType.Consumable)
        {
            ConfigureAction(false, "Use", null);
            return;
        }

        if (equippedByOther)
        {
            ConfigureAction(false, "Equipped", null);
        }
        else if (equippedByActive)
        {
            ConfigureAction(!isLocked, "Unequip", onAction);
        }
        else
        {
            ConfigureAction(!isLocked, "Equip", onAction);
        }
    }

    private void ConfigureAction(bool interactable, string label, System.Action onAction)
    {
        if (actionText != null)
            actionText.text = label;

        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            if (onAction != null)
                actionButton.onClick.AddListener(() => onAction());
            actionButton.interactable = interactable;
        }
    }
}


