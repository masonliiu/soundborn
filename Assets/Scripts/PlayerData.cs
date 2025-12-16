using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterInstance
{
    public CharacterData data;
    public int level;
    public int currentExp;
    public List<ItemInstance> equippedItems = new List<ItemInstance>();

    public CharacterInstance(CharacterData data)
    {
        this.data = data;
        level = 1;
        currentExp = 0;
    }

    public int GetExpToNextLevel()
    {
        // linear curve
        return 10 + (level - 1) * 5;
    }

    public bool AddExp(int amount)
    {
        if (amount <= 0) return false;

        bool leveledUp = false;
        currentExp += amount;

        while (currentExp >= GetExpToNextLevel())
        {
            currentExp -= GetExpToNextLevel();
            level++;
            leveledUp = true;
        }

        return leveledUp;
    }
}

[Serializable]
public class PlayerData
{
    public List<CharacterInstance> ownedCharacters = new List<CharacterInstance>();
    public int activeCharacterIndex = 0;

    public int[] activeLineupIndices = new int[4] { -1, -1, -1 ,-1 };

    public int softCurrency = 0;
    public int premiumCurrency = 0;

    public List<ItemInstance> inventory = new List<ItemInstance>();
    public int towerHighestFloorCleared = 0;
    public int towerCurrentFloor = 0;
    public int playerLevel = 1;
    public int playerExp = 0;
}

[Serializable]
public class ItemInstance
{
    public ItemData data;
    public int level;

    public ItemInstance(ItemData data, int level = 1)
    {
        this.data = data;
        this.level = level;
    }
}