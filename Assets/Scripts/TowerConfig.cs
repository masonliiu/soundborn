using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TowerConfig", menuName = "Game/Tower Config")]
public class TowerConfig : ScriptableObject
{
    public List<TowerFloor> floors = new List<TowerFloor>();
    public List<TowerLootArea> lootAreas = new List<TowerLootArea>();
}

[System.Serializable]
public class TowerFloor
{

    public CharacterData[] enemies = new CharacterData[4];
    public int floorNumber;
    public string floorName;
    public CharacterData enemyData;
    public int rewardSoftCurrency;
    public int rewardPremiumCurrency;
    public ItemData rewardItem;
    public bool isBossFloor;
}

[System.Serializable]
public struct LootRarityWeights
{
    public int common;
    public int rare;
    public int epic;
    public int legendary;

    public int Total => common + rare + epic + legendary;

    public ItemRarity RollRarity()
    {
        int total = Total;
        if (total <= 0) return ItemRarity.Common;

        int roll = Random.Range(0, total);
        if (roll < common) return ItemRarity.Common;
        roll -= common;
        if (roll < rare) return ItemRarity.Rare;
        roll -= rare;
        if (roll < epic) return ItemRarity.Epic;
        return ItemRarity.Legendary;
    }
}

[System.Serializable]
public class TowerLootArea
{
    public string areaName;
    public int startFloor = 1;
    public int endFloor = 10;
    public ItemData[] items;
    public int minBossDrops = 1;
    public int maxBossDrops = 2;
    public LootRarityWeights bossRarityWeights = new LootRarityWeights
    {
        common = 60,
        rare = 28,
        epic = 10,
        legendary = 2
    };
}

