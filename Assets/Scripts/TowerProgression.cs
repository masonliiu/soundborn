using System.Collections.Generic;
using UnityEngine;

public class TowerProgression : MonoBehaviour
{
    [Header("Config")]
    public TowerConfig config;

    [Header("Generated Floors Settings")]
    public int generatedMaxFloors = 100;
    public int bossInterval = 5;

    [Header("Floor Labels")]
    public int firstAreaSize = 10;
    public int midAreaSize = 20;
    public int midAreaCount = 5;
    public int lateAreaSize = 40;

    [Header("Enemy Templates")]
    public CharacterData defaultNormalEnemy;
    public CharacterData defaultBossEnemy;

    [Header("Enemy Scaling")]
    public float hpGrowthPerFloor = 0.035f;
    public float attackGrowthPerFloor = 0.025f;
    public float defenseGrowthPerFloor = 0.02f;
    public float bossHpMultiplier = 1.85f;
    public float bossAttackMultiplier = 1.45f;
    public float bossDefenseMultiplier = 1.25f;

    [Header("Reward Scaling")]
    public int baseSoftReward = 200;
    public int softRewardPerFloor = 35;
    public int bossSoftBonus = 250;
    public int basePremiumBossReward = 10;
    public int premiumBossStep = 1;

    [Header("Loot Drops")]
    public bool dropItemsOnlyOnBossFloors = true;
    public int fallbackMinBossDrops = 1;
    public int fallbackMaxBossDrops = 2;
    public LootRarityWeights fallbackBossRarityWeights = new LootRarityWeights
    {
        common = 60,
        rare = 28,
        epic = 10,
        legendary = 2
    };
    public ItemData[] fallbackLootItems;

    public TowerFloor GetCurrentFloor(PlayerData data)
    {
        if (data == null) return null;

        int maxFloors = Mathf.Max(1, generatedMaxFloors);
        int index = Mathf.Clamp(data.towerCurrentFloor, 0, maxFloors - 1);

        if (config != null && config.floors != null && index < config.floors.Count && config.floors.Count > 0)
        {
            return config.floors[index];
        }

        return GenerateFloor(index);
    }

    public bool TryAdvanceFloor(PlayerData data)
    {
        if (data == null) return false;

        int maxFloors = Mathf.Max(1, generatedMaxFloors);
        if (data.towerCurrentFloor >= maxFloors - 1) return false;

        data.towerCurrentFloor++;
        if (data.towerCurrentFloor > data.towerHighestFloorCleared)
            data.towerHighestFloorCleared = data.towerCurrentFloor;
        return true;
    }

    public string GetFloorLabel(int floorIndex)
    {
        int floorNumber = Mathf.Max(1, floorIndex + 1);

        if (floorNumber <= firstAreaSize)
        {
            return $"1-{floorNumber}";
        }

        int remaining = floorNumber - firstAreaSize;
        int midTotalFloors = midAreaSize * Mathf.Max(0, midAreaCount);

        if (remaining <= midTotalFloors)
        {
            int areaOffset = (remaining - 1) / Mathf.Max(1, midAreaSize);
            int floorInArea = ((remaining - 1) % Mathf.Max(1, midAreaSize)) + 1;
            return $"{2 + areaOffset}-{floorInArea}";
        }

        remaining -= midTotalFloors;
        int lateAreaIndex = 2 + Mathf.Max(0, midAreaCount) + ((remaining - 1) / Mathf.Max(1, lateAreaSize));
        int lateFloorInArea = ((remaining - 1) % Mathf.Max(1, lateAreaSize)) + 1;
        return $"{lateAreaIndex}-{lateFloorInArea}";
    }

    public void ApplyEnemyScaling(CharacterStats stats, int floorNumber, bool isBoss)
    {
        if (stats == null) return;

        int t = Mathf.Max(0, floorNumber - 1);
        float hpMul = Mathf.Pow(1f + Mathf.Max(0f, hpGrowthPerFloor), t);
        float atkMul = Mathf.Pow(1f + Mathf.Max(0f, attackGrowthPerFloor), t);
        float defMul = Mathf.Pow(1f + Mathf.Max(0f, defenseGrowthPerFloor), t);

        if (isBoss)
        {
            hpMul *= bossHpMultiplier;
            atkMul *= bossAttackMultiplier;
            defMul *= bossDefenseMultiplier;
        }

        stats.maxHP = Mathf.RoundToInt(stats.maxHP * hpMul);
        stats.currentHP = stats.maxHP;
        stats.attack = Mathf.RoundToInt(stats.attack * atkMul);
        stats.defense = Mathf.RoundToInt(stats.defense * defMul);
    }

    public List<ItemData> RollBossDrops(int floorNumber, bool isBossFloor)
    {
        var drops = new List<ItemData>();
        if (dropItemsOnlyOnBossFloors && !isBossFloor)
            return drops;

        var area = GetLootAreaForFloor(floorNumber);
        ItemData[] pool = area != null && area.items != null && area.items.Length > 0
            ? area.items
            : fallbackLootItems;

        if (pool == null || pool.Length == 0)
            return drops;

        int minDrops = area != null ? area.minBossDrops : fallbackMinBossDrops;
        int maxDrops = area != null ? area.maxBossDrops : fallbackMaxBossDrops;
        minDrops = Mathf.Max(0, minDrops);
        maxDrops = Mathf.Max(minDrops, maxDrops);

        int dropCount = Random.Range(minDrops, maxDrops + 1);
        if (dropCount <= 0)
            return drops;

        LootRarityWeights weights = area != null ? area.bossRarityWeights : fallbackBossRarityWeights;
        var used = new HashSet<ItemData>();

        for (int i = 0; i < dropCount; i++)
        {
            ItemData rolled = RollItemFromPool(pool, weights, used);
            if (rolled != null)
            {
                drops.Add(rolled);
                used.Add(rolled);
            }
        }

        return drops;
    }

    private ItemData RollItemFromPool(ItemData[] pool, LootRarityWeights weights, HashSet<ItemData> used)
    {
        ItemRarity rarity = weights.RollRarity();
        ItemData item = GetItemForRarity(pool, rarity, used);
        if (item != null)
            return item;

        foreach (var fallback in GetFallbackRarities(rarity))
        {
            item = GetItemForRarity(pool, fallback, used);
            if (item != null)
                return item;
        }

        return GetAnyItem(pool, used);
    }

    private IEnumerable<ItemRarity> GetFallbackRarities(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Legendary:
                yield return ItemRarity.Epic;
                yield return ItemRarity.Rare;
                yield return ItemRarity.Common;
                yield break;
            case ItemRarity.Epic:
                yield return ItemRarity.Rare;
                yield return ItemRarity.Common;
                yield break;
            case ItemRarity.Rare:
                yield return ItemRarity.Common;
                yield break;
            case ItemRarity.Common:
            default:
                yield return ItemRarity.Rare;
                yield return ItemRarity.Epic;
                yield return ItemRarity.Legendary;
                yield break;
        }
    }

    private ItemData GetItemForRarity(ItemData[] pool, ItemRarity rarity, HashSet<ItemData> used)
    {
        ItemData candidate = null;
        int count = 0;

        for (int i = 0; i < pool.Length; i++)
        {
            var item = pool[i];
            if (item == null || item.rarity != rarity) continue;
            if (used != null && used.Contains(item)) continue;

            count++;
            if (Random.Range(0, count) == 0)
                candidate = item;
        }

        return candidate;
    }

    private ItemData GetAnyItem(ItemData[] pool, HashSet<ItemData> used)
    {
        ItemData candidate = null;
        int count = 0;

        for (int i = 0; i < pool.Length; i++)
        {
            var item = pool[i];
            if (item == null) continue;
            if (used != null && used.Contains(item)) continue;

            count++;
            if (Random.Range(0, count) == 0)
                candidate = item;
        }

        return candidate;
    }

    private TowerLootArea GetLootAreaForFloor(int floorNumber)
    {
        if (config == null || config.lootAreas == null || config.lootAreas.Count == 0)
            return null;

        TowerLootArea fallback = null;
        foreach (var area in config.lootAreas)
        {
            if (area == null) continue;
            if (fallback == null || area.endFloor > fallback.endFloor)
                fallback = area;

            if (floorNumber >= area.startFloor && floorNumber <= area.endFloor)
                return area;
        }

        return fallback;
    }

    private TowerFloor GenerateFloor(int index)
    {
        var floor = new TowerFloor();
        int floorNumber = index + 1;
        floor.floorNumber = floorNumber;
        floor.floorName = GetFloorLabel(index);

        bool isBoss = (bossInterval > 0) && (floorNumber % bossInterval == 0);
        floor.isBossFloor = isBoss;

        floor.enemies = new CharacterData[4];

        var normal = defaultNormalEnemy;
        if (normal == null && config != null && config.floors != null && config.floors.Count > 0)
            normal = config.floors[config.floors.Count - 1].enemyData;

        var boss = defaultBossEnemy != null ? defaultBossEnemy : normal;

        if (isBoss)
        {
            floor.enemies[0] = boss;
            floor.enemies[1] = normal;
            floor.enemies[2] = normal;
            floor.enemies[3] = normal;
        }
        else
        {
            floor.enemies[0] = normal;
            floor.enemies[1] = normal;
            floor.enemies[2] = normal;
            floor.enemies[3] = normal;
        }

        floor.enemyData = floor.enemies[0];

        int soft = baseSoftReward + softRewardPerFloor * index;
        if (isBoss)
            soft += bossSoftBonus;
        floor.rewardSoftCurrency = Mathf.Max(0, soft);

        int bossIndex = (bossInterval > 0) ? (floorNumber / bossInterval) : 0;
        floor.rewardPremiumCurrency = isBoss
            ? Mathf.Max(0, basePremiumBossReward + premiumBossStep * Mathf.Max(0, bossIndex - 1))
            : 0;

        floor.rewardItem = null;

        return floor;
    }
}


