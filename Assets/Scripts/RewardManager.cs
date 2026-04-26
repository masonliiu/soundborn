using System.Collections.Generic;
using UnityEngine;

public class RewardManager : MonoBehaviour
{
    public void GrantFloorRewards(PlayerData data, TowerFloor floor)
    {
        if (data == null || floor == null) return;

        data.softCurrency += floor.rewardSoftCurrency;
        data.premiumCurrency += floor.rewardPremiumCurrency;

        if (data.inventory == null)
            data.inventory = new List<ItemInstance>();

        var grantedItems = new List<ItemData>();
        if (floor.rewardItem != null)
            grantedItems.Add(floor.rewardItem);

        if (GameManager.Instance != null && GameManager.Instance.towerProgression != null)
        {
            var rolled = GameManager.Instance.towerProgression.RollBossDrops(floor.floorNumber, floor.isBossFloor);
            if (rolled != null)
                grantedItems.AddRange(rolled);
        }

        floor.rewardItems = grantedItems;

        foreach (var item in grantedItems)
        {
            if (item != null)
                data.inventory.Add(new ItemInstance(item));
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.NotifyPlayerDataChanged();
            GameManager.Instance.SavePlayerData();
        }
    }
}
