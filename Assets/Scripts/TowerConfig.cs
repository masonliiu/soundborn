using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TowerConfig", menuName = "Game/Tower Config")]
public class TowerConfig : ScriptableObject
{
    public List<TowerFloor> floors = new List<TowerFloor>();
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


