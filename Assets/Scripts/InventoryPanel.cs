using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPanel : MonoBehaviour
{
    public GameObject root;
    public ScrollRect scrollRect;
    public RectTransform contentRoot;
    public InventoryItemUI itemPrefab;

    private readonly List<InventoryItemUI> spawnedItems = new List<InventoryItemUI>();

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void Show()
    {
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
        if (gm == null || contentRoot == null || itemPrefab == null)
            return;

        var inventory = gm.playerData.inventory;
        if (inventory == null)
            return;

        foreach (var ui in spawnedItems)
        {
            if (ui != null)
                Destroy(ui.gameObject);
        }
        spawnedItems.Clear();

        int playerLevel = gm.playerData.playerLevel;
        var active = gm.GetActiveCharacterInstance();

        for (int i = 0; i < inventory.Count; i++)
        {
            var inst = inventory[i];
            bool equippedByActive = active != null && active.IsItemEquipped(inst);
            bool equippedByOther = gm.IsItemEquipped(inst) && !equippedByActive;

            var ui = Instantiate(itemPrefab, contentRoot);
            ui.gameObject.SetActive(true);
            ui.Init(
                inst,
                playerLevel,
                equippedByActive,
                equippedByOther,
                () =>
                {
                    if (inst == null || inst.data == null) return;

                    if (equippedByActive)
                        gm.TryUnequipItemFromActive(inst.data.itemType);
                    else
                        gm.TryEquipItemToActive(inst);

                    Refresh();
                }
            );
            spawnedItems.Add(ui);
        }

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }
}


