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
    public ItemDetailPanel itemDetailPanel;

    private readonly List<InventoryItemUI> spawnedItems = new List<InventoryItemUI>();

    private void Awake()
    {
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

        for (int i = 0; i < inventory.Count; i++)
        {
            var inst = inventory[i];
            var ui = Instantiate(itemPrefab, contentRoot);
            ui.gameObject.SetActive(true);
            ui.Init(
                inst,
                playerLevel,
                () =>
                {
                    if (itemDetailPanel != null)
                        itemDetailPanel.Show(inst);
                }
            );
            spawnedItems.Add(ui);
        }

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }
}


