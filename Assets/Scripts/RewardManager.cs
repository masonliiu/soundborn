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
}

public class FloorRewardResult
{
    public int softCurrency;
    public int premiumCurrency;
    public List<ItemData> items = new List<ItemData>();
}
