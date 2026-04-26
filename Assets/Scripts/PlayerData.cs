using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterInstance
{
    private const int HpPerLevel = 25;
    private const int AttackPerLevel = 5;
    private const int DefensePerLevel = 3;

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

    public IEnumerable<ItemInstance> GetEquippedItems()
    {
        if (equippedItems == null)
            yield break;

        for (int i = 0; i < equippedItems.Count; i++)
        {
            var item = equippedItems[i];
            if (item != null)
                yield return item;
        }
    }

    public ItemInstance GetEquippedItem(ItemType type)
    {
        if (equippedItems == null) return null;
        for (int i = 0; i < equippedItems.Count; i++)
        {
            var item = equippedItems[i];
            if (item != null && item.data != null && item.data.itemType == type)
                return item;
        }
        return null;
    }

    public bool IsItemEquipped(ItemInstance item)
    {
        if (item == null || string.IsNullOrEmpty(item.instanceId) || equippedItems == null) return false;
        for (int i = 0; i < equippedItems.Count; i++)
        {
            var equipped = equippedItems[i];
            if (equipped != null && equipped.instanceId == item.instanceId)
                return true;
        }
        return false;
    }

    public bool TryEquip(ItemInstance item, out ItemInstance replaced)
    {
        replaced = null;
        if (item == null || item.data == null || item.data.itemType == ItemType.Consumable)
            return false;

        if (equippedItems == null)
            equippedItems = new List<ItemInstance>();

        var existing = GetEquippedItem(item.data.itemType);
        if (existing != null)
        {
            equippedItems.Remove(existing);
            replaced = existing;
        }

        if (!IsItemEquipped(item))
            equippedItems.Add(item);
        return true;
    }

    public bool TryUnequip(ItemType type, out ItemInstance unequipped)
    {
        unequipped = null;
        if (equippedItems == null) return false;

        var existing = GetEquippedItem(type);
        if (existing == null) return false;

        equippedItems.Remove(existing);
        unequipped = existing;
        return true;
    }

    public bool RemoveEquippedItem(ItemInstance item)
    {
        if (item == null || string.IsNullOrEmpty(item.instanceId) || equippedItems == null)
            return false;

        for (int i = equippedItems.Count - 1; i >= 0; i--)
        {
            var equipped = equippedItems[i];
            if (equipped != null && equipped.instanceId == item.instanceId)
            {
                equippedItems.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public void GetEquipmentBonuses(out int hpBonus, out int attackBonus, out int defenseBonus, out int speedBonus)
    {
        hpBonus = 0;
        attackBonus = 0;
        defenseBonus = 0;
        speedBonus = 0;

        if (equippedItems == null) return;

        foreach (var item in equippedItems)
        {
            if (item == null || item.data == null) continue;
            if (item.data.itemType == ItemType.Consumable) continue;

            float multiplier = 1f + 0.1f * Mathf.Max(0, item.level - 1);
            hpBonus += Mathf.RoundToInt(item.data.hpBonus * multiplier);
            attackBonus += Mathf.RoundToInt(item.data.attackBonus * multiplier);
            defenseBonus += Mathf.RoundToInt(item.data.defenseBonus * multiplier);
            speedBonus += Mathf.RoundToInt(item.data.speedBonus * multiplier);
        }
    }

    public void GetTotalStats(out int hp, out int attack, out int defense, out int speed)
    {
        hp = 0;
        attack = 0;
        defense = 0;
        speed = 0;

        if (data == null)
            return;

        int extraLevels = Mathf.Max(0, level - 1);

        hp = data.maxHP + extraLevels * HpPerLevel;
        attack = data.attack + extraLevels * AttackPerLevel;
        defense = data.defense + extraLevels * DefensePerLevel;
        speed = data.speed;

        GetEquipmentBonuses(out int hpBonus, out int attackBonus, out int defenseBonus, out int speedBonus);
        hp += hpBonus;
        attack += attackBonus;
        defense += defenseBonus;
        speed += speedBonus;
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

    // Onboarding flags
    public bool onboardingCompleted = false;
    public bool homeTipsSeen = false;

    // Quest progression
    public List<QuestState> questStates = new List<QuestState>();
}

[Serializable]
public class QuestState
{
    public string questId;
    public int currentCount;
    public bool isCompleted;
    public bool isClaimed;
}

[Serializable]
public class ItemInstance
{
    public ItemData data;
    public int level;
    public string instanceId;

    public ItemInstance(ItemData data, int level = 1, string instanceId = null)
    {
        this.data = data;
        this.level = level;
        this.instanceId = string.IsNullOrEmpty(instanceId) ? Guid.NewGuid().ToString("N") : instanceId;
    }
}
