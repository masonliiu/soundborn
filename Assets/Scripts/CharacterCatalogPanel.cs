using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCatalogPanel : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("Scroll")]
    public ScrollRect scrollRect;
    public RectTransform contentRoot;
    public CharacterCatalogItem itemPrefab;

    [Header("Upgrade")]
    public UpgradePanel upgradePanel;

    private HomeUIController homeUI;
    private readonly List<CharacterCatalogItem> spawnedItems = new List<CharacterCatalogItem>();

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDataChanged += HandlePlayerDataChanged;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDataChanged -= HandlePlayerDataChanged;
        }
    }

    private void HandlePlayerDataChanged()
    {
        if (root != null && root.activeSelf)
        {
            Refresh();
        }
    }

    public void Show(HomeUIController home)
    {
        homeUI = home;

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

        var owned = gm.playerData.ownedCharacters;
        if (owned == null)
            return;

        foreach (var item in spawnedItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        spawnedItems.Clear();

        var orderedIndices = Enumerable.Range(0, owned.Count)
            .OrderByDescending(i => owned[i].level)
            .ThenBy(i => owned[i].data != null ? owned[i].data.displayName : "")
            .ToList();

        foreach (int charIndex in orderedIndices)
        {
            var inst = owned[charIndex];

            GetLeveledStats(inst, out int hp, out int atk);

            var item = Instantiate(itemPrefab, contentRoot);
            item.gameObject.SetActive(true);
            item.Setup(this, charIndex, inst, hp, atk);
            spawnedItems.Add(item);
        }

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    private void GetLeveledStats(CharacterInstance inst, out int hp, out int atk)
    {
        hp = 0;
        atk = 0;

        if (inst == null || inst.data == null)
            return;

        int baseHP = inst.data.maxHP;
        int baseATK = inst.data.attack;

        int extraLevels = Mathf.Max(0, inst.level - 1);
        hp = baseHP + extraLevels * 10;
        atk = baseATK + extraLevels * 2;
    }

    public void OnClickItem(int characterIndex)
    {
        if (upgradePanel != null)
        {
            upgradePanel.ShowForCharacter(homeUI, characterIndex);
        }
    }

    public void OnClick_Close()
    {
        Hide();
    }
}