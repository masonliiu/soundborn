using UnityEngine;

public class RewardManager : MonoBehaviour
{
    public void GrantFloorRewards(PlayerData data, TowerFloor floor)
    {
        if (data == null || floor == null) return;
        data.softCurrency += floor.rewardSoftCurrency;
        data.premiumCurrency += floor.rewardPremiumCurrency;
        if (floor.rewardItem != null)
        {
            data.inventory.Add(new ItemInstance(floor.rewardItem));
        }
    }

    public void GrantCharacterShard(PlayerData data, CharacterData character, int amount)
    {
        if (data == null || character == null || amount <= 0) return;
        data.softCurrency += amount * 10;
    }
}

