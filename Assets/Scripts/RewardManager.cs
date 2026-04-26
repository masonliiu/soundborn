using System.Collections.Generic;
using UnityEngine;

public class RewardManager : MonoBehaviour
{
    public FloorRewardResult GrantFloorRewards(PlayerData data, TowerFloor floor)
    {
        var result = new FloorRewardResult();
        if (data == null || floor == null) return result;

        data.softCurrency += floor.rewardSoftCurrency;
        data.premiumCurrency += floor.rewardPremiumCurrency;
        result.softCurrency = floor.rewardSoftCurrency;
        result.premiumCurrency = floor.rewardPremiumCurrency;
        result.characterExp = GetCharacterExpReward(floor);
        data.characterExp += result.characterExp;

        if (data.inventory == null)
            data.inventory = new List<ItemInstance>();

        if (floor.rewardItem != null)
            result.items.Add(floor.rewardItem);

        if (GameManager.Instance != null && GameManager.Instance.towerProgression != null)
        {
            var rolled = GameManager.Instance.towerProgression.RollBossDrops(floor.floorNumber, floor.isBossFloor);
            if (rolled != null)
                result.items.AddRange(rolled);
        }

        foreach (var item in result.items)
        {
            if (item != null)
                data.inventory.Add(new ItemInstance(item));
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.NotifyPlayerDataChanged();
            GameManager.Instance.SavePlayerData();
        }

        return result;
    }

    public FloorRewardResult GrantTrialRewards(PlayerData data, TrialDefinition trial)
    {
        var result = new FloorRewardResult();
        if (data == null || trial == null) return result;

        data.softCurrency += trial.rewardNotes;
        data.characterExp += trial.rewardCharacterExp;
        result.softCurrency = trial.rewardNotes;
        result.characterExp = trial.rewardCharacterExp;

        if (data.inventory == null)
            data.inventory = new List<ItemInstance>();

        if (trial.trialType == TrialType.Resonance && Random.value < trial.resonanceDropChance)
        {
            AddResonanceMaterial(data, trial.resonanceMaterialTier, trial.resonanceMaterialAmount);
            result.resonanceMaterialTier = trial.resonanceMaterialTier;
            result.resonanceMaterialAmount = trial.resonanceMaterialAmount;
        }

        if (trial.trialType == TrialType.Equipment && Random.value < trial.equipmentDropChance)
        {
            var item = RollEquipmentDrop(trial.equipmentDrops);
            if (item != null)
            {
                data.inventory.Add(new ItemInstance(item));
                result.items.Add(item);
            }
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.NotifyPlayerDataChanged();
            GameManager.Instance.SavePlayerData();
        }

        return result;
    }

    private void AddResonanceMaterial(PlayerData data, int tier, int amount)
    {
        int safeAmount = Mathf.Max(0, amount);
        switch (tier)
        {
            case 1:
                data.resonanceMaterialTier1 += safeAmount;
                break;
            case 2:
                data.resonanceMaterialTier2 += safeAmount;
                break;
            case 3:
                data.resonanceMaterialTier3 += safeAmount;
                break;
            case 4:
                data.resonanceMaterialTier4 += safeAmount;
                break;
        }
    }

    private ItemData RollEquipmentDrop(ItemData[] pool)
    {
        if (pool == null || pool.Length == 0)
            return null;

        var valid = new List<ItemData>();
        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i] != null)
                valid.Add(pool[i]);
        }

        if (valid.Count == 0)
            return null;

        return valid[Random.Range(0, valid.Count)];
    }

    private int GetCharacterExpReward(TowerFloor floor)
    {
        int floorNumber = Mathf.Max(1, floor.floorNumber);
        int reward = 25 + floorNumber * 5;
        if (floor.isBossFloor)
            reward *= 2;
        return reward;
    }
}

public class FloorRewardResult
{
    public int softCurrency;
    public int premiumCurrency;
    public int characterExp;
    public int resonanceMaterialTier;
    public int resonanceMaterialAmount;
    public List<ItemData> items = new List<ItemData>();
}
