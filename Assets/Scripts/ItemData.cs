using UnityEngine;

public enum ItemType
{
    Weapon,
    Armor,
    Accessory,
    Consumable
}

[CreateAssetMenu(fileName = "NewItemData", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemId = "item_id";
    public string displayName = "New Item";
    public ItemType itemType = ItemType.Weapon;
    public Sprite icon;
    public int attackBonus = 0;
    public int defenseBonus = 0;
    public int hpBonus = 0;
    public int speedBonus = 0;
    public string description = "";
}

