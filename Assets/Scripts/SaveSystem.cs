using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles serialization of PlayerData into a lightweight save format that can
/// be stored in PlayerPrefs. Uses character/item IDs to re-link ScriptableObjects.
/// </summary>
public static class SaveSystem
{
    private const string PlayerPrefsKey = "Soundborn_Player";

    [Serializable]
    private class CharacterSave
    {
        public string characterId;
        public string displayNameFallback;
        public int level;
        public int currentExp;
        public List<ItemSave> equippedItems = new List<ItemSave>();
    }

    [Serializable]
    private class ItemSave
    {
        public string itemId;
        public int level;
    }

    [Serializable]
    private class PlayerSaveData
    {
        public List<CharacterSave> characters = new List<CharacterSave>();
        public int activeCharacterIndex = 0;
        public int[] activeLineupIndices = new int[4] { -1, -1, -1, -1 };

        public int softCurrency = 0;
        public int premiumCurrency = 0;

        public List<ItemSave> inventory = new List<ItemSave>();
        public int towerHighestFloorCleared = 0;
        public int towerCurrentFloor = 0;
        public int playerLevel = 1;
        public int playerExp = 0;
    }

    public static bool HasSave()
    {
        return PlayerPrefs.HasKey(PlayerPrefsKey);
    }

    public static void Save(PlayerData data)
    {
        if (data == null)
            return;

        var save = new PlayerSaveData
        {
            activeCharacterIndex = data.activeCharacterIndex,
            softCurrency = data.softCurrency,
            premiumCurrency = data.premiumCurrency,
            towerHighestFloorCleared = data.towerHighestFloorCleared,
            towerCurrentFloor = data.towerCurrentFloor,
            playerLevel = data.playerLevel,
            playerExp = data.playerExp
        };

        // Active lineup
        if (data.activeLineupIndices != null && data.activeLineupIndices.Length == 4)
        {
            save.activeLineupIndices = (int[])data.activeLineupIndices.Clone();
        }

        // Characters
        if (data.ownedCharacters != null)
        {
            foreach (var inst in data.ownedCharacters)
            {
                if (inst == null || inst.data == null) continue;

                var cs = new CharacterSave
                {
                    characterId = inst.data.characterId,
                    displayNameFallback = inst.data.displayName,
                    level = inst.level,
                    currentExp = inst.currentExp
                };

                if (inst.equippedItems != null)
                {
                    foreach (var eq in inst.equippedItems)
                    {
                        if (eq == null || eq.data == null) continue;
                        cs.equippedItems.Add(new ItemSave
                        {
                            itemId = eq.data.itemId,
                            level = eq.level
                        });
                    }
                }

                save.characters.Add(cs);
            }
        }

        // Inventory
        if (data.inventory != null)
        {
            foreach (var item in data.inventory)
            {
                if (item == null || item.data == null) continue;
                save.inventory.Add(new ItemSave
                {
                    itemId = item.data.itemId,
                    level = item.level
                });
            }
        }

        string json = JsonUtility.ToJson(save);
        PlayerPrefs.SetString(PlayerPrefsKey, json);
        PlayerPrefs.Save();

        Debug.Log($"[SaveSystem] Saved PlayerData. Characters={save.characters.Count}, Inventory={save.inventory.Count}");
    }

    public static bool TryLoad(PlayerData target, CharacterDatabase characterDb, ItemDatabase itemDb)
    {
        if (target == null)
        {
            Debug.LogError("[SaveSystem] TryLoad: target PlayerData is NULL");
            return false;
        }

        if (!PlayerPrefs.HasKey(PlayerPrefsKey))
            return false;

        string json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
        if (string.IsNullOrEmpty(json))
            return false;

        PlayerSaveData save;
        try
        {
            save = JsonUtility.FromJson<PlayerSaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] TryLoad: Failed to parse JSON: {e.Message}");
            return false;
        }

        if (save == null)
            return false;

        // Clear existing
        target.ownedCharacters = new List<CharacterInstance>();
        target.inventory = new List<ItemInstance>();

        // Characters
        if (save.characters != null)
        {
            foreach (var cs in save.characters)
            {
                if (cs == null) continue;

                CharacterData data = null;
                if (characterDb != null)
                {
                    if (!string.IsNullOrEmpty(cs.characterId))
                        data = characterDb.GetById(cs.characterId);
                    if (data == null && !string.IsNullOrEmpty(cs.displayNameFallback))
                        data = characterDb.GetByDisplayName(cs.displayNameFallback);
                }

                if (data == null)
                {
                    Debug.LogWarning($"[SaveSystem] TryLoad: Could not find CharacterData for id='{cs.characterId}' name='{cs.displayNameFallback}'");
                    continue;
                }

                var inst = new CharacterInstance(data)
                {
                    level = cs.level,
                    currentExp = cs.currentExp
                };

                if (cs.equippedItems != null && itemDb != null)
                {
                    foreach (var eq in cs.equippedItems)
                    {
                        if (eq == null || string.IsNullOrEmpty(eq.itemId)) continue;
                        var itemData = itemDb.GetById(eq.itemId);
                        if (itemData == null) continue;
                        inst.equippedItems.Add(new ItemInstance(itemData, eq.level));
                    }
                }

                target.ownedCharacters.Add(inst);
            }
        }

        // Inventory
        if (save.inventory != null && itemDb != null)
        {
            foreach (var isave in save.inventory)
            {
                if (isave == null || string.IsNullOrEmpty(isave.itemId)) continue;
                var itemData = itemDb.GetById(isave.itemId);
                if (itemData == null)
                {
                    Debug.LogWarning($"[SaveSystem] TryLoad: Could not find ItemData for id='{isave.itemId}'");
                    continue;
                }
                target.inventory.Add(new ItemInstance(itemData, isave.level));
            }
        }

        // Simple fields
        target.activeCharacterIndex = save.activeCharacterIndex;
        if (save.activeLineupIndices != null && save.activeLineupIndices.Length == 4)
            target.activeLineupIndices = (int[])save.activeLineupIndices.Clone();

        target.softCurrency = save.softCurrency;
        target.premiumCurrency = save.premiumCurrency;
        target.towerHighestFloorCleared = save.towerHighestFloorCleared;
        target.towerCurrentFloor = save.towerCurrentFloor;
        target.playerLevel = save.playerLevel;
        target.playerExp = save.playerExp;

        Debug.Log($"[SaveSystem] Loaded PlayerData. Characters={target.ownedCharacters.Count}, Inventory={target.inventory.Count}");
        return true;
    }
}


