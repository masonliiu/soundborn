using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemEquipPanel : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("List")]
    public Transform contentRoot;
    public ItemEquipRowUI rowPrefab;

    [Header("Confirm")]
    public GameObject confirmRoot;
    public TextMeshProUGUI confirmText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    [Header("Navigation")]
    public HomeUIController homeUI;

    private readonly List<ItemEquipRowUI> spawnedRows = new List<ItemEquipRowUI>();
    private ItemInstance currentItem;
    private ItemDetailPanel ownerPanel;
    private int pendingCharacterIndex = -1;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);

        if (confirmRoot != null)
            confirmRoot.SetActive(false);

        if (confirmYesButton != null)
            confirmYesButton.onClick.AddListener(ConfirmEquip);

        if (confirmNoButton != null)
            confirmNoButton.onClick.AddListener(CancelConfirm);
    }

    public void Show(ItemInstance item, ItemDetailPanel owner)
    {
        currentItem = item;
        ownerPanel = owner;

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
        if (gm == null || contentRoot == null || rowPrefab == null)
            return;

        foreach (var row in spawnedRows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }
        spawnedRows.Clear();

        var characters = gm.playerData.ownedCharacters;
        if (characters == null)
            return;

        var currentOwner = gm.GetCharacterEquippingItem(currentItem, out int currentOwnerIndex);

        for (int i = 0; i < characters.Count; i++)
        {
            int index = i;
            var character = characters[i];
            var row = Instantiate(rowPrefab, contentRoot);
            row.gameObject.SetActive(true);

            bool equippedByThis = currentOwner != null && currentOwnerIndex == index;
            row.Bind(character, equippedByThis, () => OnSelectCharacter(index, equippedByThis));
            spawnedRows.Add(row);
        }

        if (confirmRoot != null)
            confirmRoot.SetActive(false);
    }

    public void OnClick_Close()
    {
        Hide();
    }

    private void OnSelectCharacter(int index, bool equippedByThis)
    {
        if (equippedByThis)
            return;

        var gm = GameManager.Instance;
        if (gm == null || currentItem == null)
            return;

        var currentOwner = gm.GetCharacterEquippingItem(currentItem, out int ownerIndex);
        if (currentOwner != null && ownerIndex != index)
        {
            pendingCharacterIndex = index;
            if (confirmText != null)
            {
                string itemName = currentItem.data != null ? currentItem.data.displayName : "this item";
                string ownerName = currentOwner.data != null ? currentOwner.data.displayName : "another character";
                confirmText.text = $"{itemName} is currently equipped by {ownerName}. Equip anyway?";
            }

            if (confirmRoot != null)
                confirmRoot.SetActive(true);
            return;
        }

        if (gm.TryEquipItemToCharacter(currentItem, index, true, out CharacterInstance previousOwner))
        {
            Refresh();
            if (ownerPanel != null)
                ownerPanel.Refresh();

            ShowCharacterDetails(index);
        }
    }

    private void ConfirmEquip()
    {
        if (pendingCharacterIndex < 0)
            return;

        var gm = GameManager.Instance;
        if (gm == null || currentItem == null)
            return;

        int targetIndex = pendingCharacterIndex;
        gm.TryEquipItemToCharacter(currentItem, targetIndex, true, out CharacterInstance previousOwner);
        pendingCharacterIndex = -1;

        if (confirmRoot != null)
            confirmRoot.SetActive(false);

        Refresh();
        if (ownerPanel != null)
            ownerPanel.Refresh();

        if (targetIndex >= 0)
            ShowCharacterDetails(targetIndex);
    }

    private void CancelConfirm()
    {
        pendingCharacterIndex = -1;
        if (confirmRoot != null)
            confirmRoot.SetActive(false);
    }

    private void ShowCharacterDetails(int characterIndex)
    {
        var resolvedHome = ResolveHomeUI();
        if (resolvedHome == null)
            return;

        if (resolvedHome.inventoryPanel != null)
            resolvedHome.inventoryPanel.Hide();

        if (resolvedHome.characterCatalogPanel != null)
            resolvedHome.characterCatalogPanel.Show(resolvedHome);

        if (resolvedHome.upgradePanel != null)
            resolvedHome.upgradePanel.ShowForCharacter(resolvedHome, characterIndex);

        if (ownerPanel != null)
            ownerPanel.Hide();

        Hide();
    }

    private HomeUIController ResolveHomeUI()
    {
        if (homeUI != null)
            return homeUI;

        homeUI = FindObjectOfType<HomeUIController>();
        return homeUI;
    }
}
