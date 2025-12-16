using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public ItemData[] allItems;

    public ItemData GetById(string id)
    {
        if (string.IsNullOrEmpty(id) || allItems == null) return null;
        for (int i = 0; i < allItems.Length; i++)
        {
            var item = allItems[i];
            if (item != null && item.itemId == id)
                return item;
        }
        return null;
    }
}


