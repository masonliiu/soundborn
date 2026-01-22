using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI lockText;
    public Button rowButton;

    private ItemInstance instance;
    private bool isLocked;

    public void Init(ItemInstance inst, int playerLevel, System.Action onRowClick)
    {
        instance = inst;
        ConfigureRow(onRowClick);

        if (inst == null || inst.data == null)
        {
            if (nameText != null) nameText.text = "Empty";
            if (icon != null) icon.sprite = null;
            if (lockText != null) lockText.text = "";
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

    }

    private void ConfigureRow(System.Action onRowClick)
    {
        if (rowButton == null) return;

        rowButton.onClick.RemoveAllListeners();
        if (onRowClick != null)
            rowButton.onClick.AddListener(() => onRowClick());
        rowButton.interactable = onRowClick != null;
    }
}


