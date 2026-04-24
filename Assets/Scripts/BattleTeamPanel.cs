using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class BattleTeamPanel : MonoBehaviour
{
    [Header("Scroll")]
    public ScrollRect scrollRect;
    public RectTransform contentRoot;
    public CharacterCatalogItem itemPrefab;

    private readonly List<CharacterCatalogItem> spawnedItems = new List<CharacterCatalogItem>();

    private Action<int> onClickIndex;
    private HashSet<int> selected = new HashSet<int>();

    public void SetClickHandler(Action<int> handler)
    {
        onClickIndex = handler;
        Refresh();
    }

    public void SetSelected(HashSet<int> selectedSet)
    {
        selected = selectedSet ?? new HashSet<int>();
        Refresh();
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDataChanged += HandlePlayerDataChanged;
        }
        Refresh();
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
        Refresh();
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

        for (int i = 0; i < owned.Count; i++)
        {
            var inst = owned[i];

            GetLeveledStats(inst, out int hp, out int atk);

            var item = UnityEngine.Object.Instantiate(itemPrefab, contentRoot);
            item.gameObject.SetActive(true);

            bool isSelected = selected.Contains(i);

            item.Setup(
                owner: null,
                characterIndex: i,
                instance: inst,
                hp: hp,
                atk: atk,
                onClickOverride: onClickIndex,
                greyedOut: isSelected
            );
            spawnedItems.Add(item);
        }

        if (scrollRect != null)
            scrollRect.horizontalNormalizedPosition = 0f;
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
}