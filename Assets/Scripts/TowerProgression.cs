using UnityEngine;

public class TowerProgression : MonoBehaviour
{
    [Header("Config")]
    public TowerConfig config;

    [Header("Generated Floors Settings")]
    public int generatedMaxFloors = 100;
    public int bossInterval = 5;

    [Header("Enemy Templates")]
    public CharacterData defaultNormalEnemy;
    public CharacterData defaultBossEnemy;

    [Header("Reward Scaling")]
    public int baseSoftReward = 200;
    public int softRewardPerFloor = 35;
    public int bossSoftBonus = 250;
    public int basePremiumBossReward = 10;
    public int premiumBossStep = 1;

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

    private TowerFloor GenerateFloor(int index)
    {
        var floor = new TowerFloor();
        int floorNumber = index + 1;
        floor.floorNumber = floorNumber;
        floor.floorName = $"Floor {floorNumber}";

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


