using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterInstance
{
    public CharacterData data;
    public int level;
    public int currentExp;

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

    public int softCurrency = 0;
    public int premiumCurrency = 0;
}