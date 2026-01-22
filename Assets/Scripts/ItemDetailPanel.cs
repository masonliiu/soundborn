using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDetailPanel : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("Texts")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI rarityText;
    public TextMeshProUGUI typeText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI equippedText;

    [Header("Buttons")]
    public Button equipButton;
    public Button unequipButton;

    [Header("Equip Flow")]
    public ItemEquipPanel equipPanel;

    private ItemInstance currentItem;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);

        if (equipButton != null)
            equipButton.onClick.AddListener(OnClickEquip);

        if (unequipButton != null)
            unequipButton.onClick.AddListener(OnClickUnequip);
    }

    public void Show(ItemInstance item)
    {
        currentItem = item;
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
        if (gm == null || currentItem == null || currentItem.data == null)
            return;

        var data = currentItem.data;

        if (nameText != null)
            nameText.text = data.displayName;

        if (rarityText != null)
            rarityText.text = data.rarity.ToString();

        if (typeText != null)
            typeText.text = data.itemType.ToString();

        if (levelText != null)
            levelText.text = $"Lv {currentItem.level}";

        if (statsText != null)
            statsText.text = $"HP +{data.hpBonus}  ATK +{data.attackBonus}  DEF +{data.defenseBonus}  SPD +{data.speedBonus}";

        var equippedOwner = gm.GetCharacterEquippingItem(currentItem, out int ownerIndex);
        if (equippedText != null)
            equippedText.text = equippedOwner != null && equippedOwner.data != null
                ? $"Equipped by {equippedOwner.data.displayName}"
                : "Not equipped";

        bool canEquip = data.itemType != ItemType.Consumable;
        bool isEquipped = equippedOwner != null;

        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(!isEquipped && canEquip);
            equipButton.interactable = canEquip;
        }

        if (unequipButton != null)
        {
            unequipButton.gameObject.SetActive(isEquipped);
            unequipButton.interactable = isEquipped;
        }
    }

    public void OnClick_Close()
    {
        Hide();
    }

    private void OnClickEquip()
    {
        if (currentItem == null || currentItem.data == null)
            return;

        if (equipPanel != null)
            equipPanel.Show(currentItem, this);
    }

    private void OnClickUnequip()
    {
        var gm = GameManager.Instance;
        if (gm == null || currentItem == null || currentItem.data == null)
            return;

        if (gm.TryUnequipItemFromOwner(currentItem))
            Refresh();
    }
}
