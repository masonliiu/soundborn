using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Starting Setup")]
    public CharacterData[] extraStartingCharacters;
    public CharacterData starterPlayer;
    public CharacterData starterEnemy;
    public TowerConfig towerConfig;
    public CharacterDatabase characterDatabase;
    public ItemDatabase itemDatabase;
    public QuestManager questManager;

    [Header("Runtime Data")]
    public PlayerData playerData = new PlayerData();
    public RewardManager rewardManager;
    public TowerProgression towerProgression;

    [Header("Battle Runtime")]
    public CharacterData currentEnemyData;

    public event Action OnPlayerDataChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameManager] Awake: Another GameManager instance exists, destroying this one");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (playerData == null)
            playerData = new PlayerData();

        if (playerData.ownedCharacters == null)
            playerData.ownedCharacters = new List<CharacterInstance>();
        if (playerData.inventory == null)
            playerData.inventory = new List<ItemInstance>();

        bool loaded = SaveSystem.TryLoad(playerData, characterDatabase, itemDatabase);

        if (!loaded)
        {
            InitializeNewPlayerData();
            SavePlayerData();
        }

        if (questManager != null)
            questManager.EnsureInitialQuests();

        if (currentEnemyData == null && starterEnemy != null)
            currentEnemyData = starterEnemy;

        NotifyPlayerDataChanged();
    }

    private void InitializeNewPlayerData()
    {
        playerData.softCurrency = 0;
        playerData.premiumCurrency = 0;
        playerData.characterExp = 0;
        playerData.resonanceMaterialTier1 = 0;
        playerData.resonanceMaterialTier2 = 0;
        playerData.resonanceMaterialTier3 = 0;
        playerData.resonanceMaterialTier4 = 0;
        playerData.towerHighestFloorCleared = 0;
        playerData.towerCurrentFloor = 0;
        playerData.playerLevel = 1;
        playerData.playerExp = 0;
        playerData.onboardingCompleted = false;
        playerData.homeTipsSeen = false;

        if (playerData.questStates == null)
            playerData.questStates = new List<QuestState>();
        else
            playerData.questStates.Clear();

        if (playerData.ownedCharacters == null)
            playerData.ownedCharacters = new List<CharacterInstance>();
        else
            playerData.ownedCharacters.Clear();

        if (playerData.inventory == null)
            playerData.inventory = new List<ItemInstance>();
        else
            playerData.inventory.Clear();

        if (starterPlayer != null)
        {
            playerData.ownedCharacters.Add(new CharacterInstance(starterPlayer));
            playerData.activeCharacterIndex = 0;
        }

        if (extraStartingCharacters != null)
        {
            foreach (var cd in extraStartingCharacters)
            {
                if (cd != null)
                    playerData.ownedCharacters.Add(new CharacterInstance(cd));
            }
        }

        if (playerData.activeLineupIndices == null || playerData.activeLineupIndices.Length != 4)
        {
            playerData.activeLineupIndices = new int[4] { -1, -1, -1, -1 };
        }

        if (playerData.activeLineupIndices[0] == -1 && playerData.ownedCharacters.Count > 0)
            playerData.activeLineupIndices[0] = playerData.activeCharacterIndex;
    }

    public void SavePlayerData()
    {
        SaveSystem.Save(playerData);
    }

    /// <summary>
    /// Completely reset the player's progress and reinitialize starting data.
    /// </summary>
    [ContextMenu("Developer/Reset Player Progress")]
    public void DebugResetPlayerProgress()
    {
        playerData = new PlayerData();
        InitializeNewPlayerData();
        SavePlayerData();
        NotifyPlayerDataChanged();
    }

    public void NotifyPlayerDataChanged()
    {
        OnPlayerDataChanged?.Invoke();
    }

    public CharacterInstance GetActiveCharacterInstance()
    {
        if (playerData.ownedCharacters == null || playerData.ownedCharacters.Count == 0)
            return null;

        if (playerData.activeCharacterIndex < 0 ||
            playerData.activeCharacterIndex >= playerData.ownedCharacters.Count)
        {
            playerData.activeCharacterIndex = 0;
        }

        return playerData.ownedCharacters[playerData.activeCharacterIndex];
    }

    public void SetActiveCharacterIndex(int index)
    {
        if (playerData.ownedCharacters == null || playerData.ownedCharacters.Count == 0)
            return;

        index = Mathf.Clamp(index, 0, playerData.ownedCharacters.Count - 1);

        if (playerData.activeCharacterIndex != index)
        {
            playerData.activeCharacterIndex = index;
            NotifyPlayerDataChanged();
            SavePlayerData();
        }
    }

    public CharacterData GetCurrentEnemyData()
    {
        return currentEnemyData;
    }

    public TowerFloor GetCurrentTowerFloor()
    {
        // Prefer TowerProgression (which can generate procedural floors) when available.
        if (towerProgression != null)
        {
            return towerProgression.GetCurrentFloor(playerData);
        }

        if (towerConfig == null || towerConfig.floors == null || towerConfig.floors.Count == 0) return null;
        int index = Mathf.Clamp(playerData.towerCurrentFloor, 0, towerConfig.floors.Count - 1);
        return towerConfig.floors[index];
    }

    public string GetFloorLabel(int floorIndex)
    {
        if (towerProgression != null)
        {
            return towerProgression.GetFloorLabel(floorIndex);
        }

        return $"Floor {floorIndex + 1}";
    }

    public void SetEnemyFromTowerFloor()
    {
        var floor = GetCurrentTowerFloor();
        if (floor != null && floor.enemyData != null)
        {
            currentEnemyData = floor.enemyData;
        }
    }

    public void SetCurrentEnemy(CharacterData enemy)
    {
        currentEnemyData = enemy;
    }

    public int GetLevelUpCost(CharacterInstance inst)
    {
        return GetLevelUpNotesCost(inst);
    }

    public int GetLevelUpNotesCost(CharacterInstance inst)
    {
        if (inst == null)
            return 0;

        return CharacterInstance.GetNotesCostForLevel(inst.level);
    }

    public int GetLevelUpExpCost(CharacterInstance inst)
    {
        if (inst == null)
            return 0;

        return CharacterInstance.GetExpCostForLevel(inst.level);
    }

    public bool IsCharacterAtLevelCap(CharacterInstance inst)
    {
        if (inst == null)
            return false;

        EnsureCharacterLevelCap(inst);
        return inst.level >= inst.levelCap;
    }

    public void EnsureCharacterLevelCap(CharacterInstance inst)
    {
        if (inst == null)
            return;

        if (inst.levelCap <= 0)
            inst.levelCap = 10;

        if (inst.level > inst.levelCap)
            inst.levelCap = Mathf.CeilToInt(inst.level / 10f) * 10;
    }

    public bool GetNextLevelCapRequirement(CharacterInstance inst, out int materialTier, out int materialAmount, out int nextCap)
    {
        materialTier = 0;
        materialAmount = 0;
        nextCap = 0;

        if (inst == null)
            return false;

        EnsureCharacterLevelCap(inst);

        if (inst.level < inst.levelCap)
            return false;

        nextCap = inst.levelCap + 10;

        switch (nextCap)
        {
            case 20:
                materialTier = 1;
                materialAmount = 2;
                return true;
            case 30:
                materialTier = 1;
                materialAmount = 5;
                return true;
            case 40:
                materialTier = 2;
                materialAmount = 3;
                return true;
            case 50:
                materialTier = 2;
                materialAmount = 8;
                return true;
            default:
                return false;
        }
    }

    public int GetResonanceMaterialCount(int tier)
    {
        switch (tier)
        {
            case 1: return playerData.resonanceMaterialTier1;
            case 2: return playerData.resonanceMaterialTier2;
            case 3: return playerData.resonanceMaterialTier3;
            case 4: return playerData.resonanceMaterialTier4;
            default: return 0;
        }
    }

    private void SpendResonanceMaterial(int tier, int amount)
    {
        switch (tier)
        {
            case 1:
                playerData.resonanceMaterialTier1 -= amount;
                break;
            case 2:
                playerData.resonanceMaterialTier2 -= amount;
                break;
            case 3:
                playerData.resonanceMaterialTier3 -= amount;
                break;
            case 4:
                playerData.resonanceMaterialTier4 -= amount;
                break;
        }
    }

    public bool TryBreakCharacterLevelCap(CharacterInstance inst)
    {
        if (!GetNextLevelCapRequirement(inst, out int tier, out int amount, out int nextCap))
            return false;

        if (GetResonanceMaterialCount(tier) < amount)
            return false;

        SpendResonanceMaterial(tier, amount);
        inst.levelCap = nextCap;

        NotifyPlayerDataChanged();
        SavePlayerData();
        return true;
    }

    public bool TryLevelUpCharacter(CharacterInstance inst)
    {
        if (inst == null)
            return false;

        EnsureCharacterLevelCap(inst);
        if (inst.level >= inst.levelCap)
            return false;

        int notesCost = GetLevelUpNotesCost(inst);
        int expCost = GetLevelUpExpCost(inst);
        if (playerData.softCurrency < notesCost || playerData.characterExp < expCost)
            return false;

        playerData.softCurrency -= notesCost;
        playerData.characterExp -= expCost;
        inst.level++;

        NotifyPlayerDataChanged();
        SavePlayerData();

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnCharacterLeveledUp();
        }
        return true;
    }

    public bool IsItemEquipped(ItemInstance item)
    {
        if (item == null || playerData == null || playerData.ownedCharacters == null) return false;

        foreach (var character in playerData.ownedCharacters)
        {
            if (character != null && character.IsItemEquipped(item))
                return true;
        }

        return false;
    }

    public bool IsItemEquippedByActive(ItemInstance item)
    {
        var active = GetActiveCharacterInstance();
        if (active == null) return false;
        return active.IsItemEquipped(item);
    }

    public bool TryEquipItemToActive(ItemInstance item)
    {
        var active = GetActiveCharacterInstance();
        if (active == null || item == null) return false;

        if (playerData.inventory == null)
            playerData.inventory = new List<ItemInstance>();

        if (IsItemEquipped(item) && !IsItemEquippedByActive(item))
            return false;

        bool success = active.TryEquip(item, out ItemInstance replaced);
        if (!success) return false;

        NotifyPlayerDataChanged();
        SavePlayerData();
        return true;
    }

    public bool TryUnequipItemFromActive(ItemType type)
    {
        var active = GetActiveCharacterInstance();
        if (active == null) return false;

        if (playerData.inventory == null)
            playerData.inventory = new List<ItemInstance>();

        bool success = active.TryUnequip(type, out ItemInstance unequipped);
        if (!success) return false;

        NotifyPlayerDataChanged();
        SavePlayerData();
        return true;
    }

    public CharacterInstance GetCharacterEquippingItem(ItemInstance item, out int characterIndex)
    {
        characterIndex = -1;
        if (item == null || playerData == null || playerData.ownedCharacters == null)
            return null;

        for (int i = 0; i < playerData.ownedCharacters.Count; i++)
        {
            var character = playerData.ownedCharacters[i];
            if (character != null && character.IsItemEquipped(item))
            {
                characterIndex = i;
                return character;
            }
        }

        return null;
    }

    public bool TryEquipItemToCharacter(ItemInstance item, int characterIndex, bool allowSwap, out CharacterInstance previousOwner)
    {
        previousOwner = GetCharacterEquippingItem(item, out int previousIndex);

        if (item == null || playerData == null || playerData.ownedCharacters == null)
            return false;

        if (characterIndex < 0 || characterIndex >= playerData.ownedCharacters.Count)
            return false;

        if (previousOwner != null && previousIndex != characterIndex && !allowSwap)
            return false;

        var target = playerData.ownedCharacters[characterIndex];
        if (target == null)
            return false;

        if (previousOwner != null && previousIndex != characterIndex)
            previousOwner.RemoveEquippedItem(item);

        bool success = target.TryEquip(item, out ItemInstance replaced);
        if (!success)
            return false;

        NotifyPlayerDataChanged();
        SavePlayerData();
        return true;
    }

    public bool TryUnequipItemFromCharacter(ItemType type, int characterIndex)
    {
        if (playerData == null || playerData.ownedCharacters == null)
            return false;

        if (characterIndex < 0 || characterIndex >= playerData.ownedCharacters.Count)
            return false;

        var target = playerData.ownedCharacters[characterIndex];
        if (target == null)
            return false;

        bool success = target.TryUnequip(type, out ItemInstance unequipped);
        if (!success)
            return false;

        NotifyPlayerDataChanged();
        SavePlayerData();
        return true;
    }

    public bool TryUnequipItemFromOwner(ItemInstance item)
    {
        if (item == null || item.data == null)
            return false;

        var owner = GetCharacterEquippingItem(item, out int ownerIndex);
        if (owner == null)
            return false;

        return TryUnequipItemFromCharacter(item.data.itemType, ownerIndex);
    }

    public void AddSoftCurrency(int amount)
    {
        if (amount <= 0) return;
        playerData.softCurrency += amount;
        NotifyPlayerDataChanged();
        SavePlayerData();
    }

    public void AddPremiumCurrency(int amount)
    {
        if (amount <= 0) return;
        playerData.premiumCurrency += amount;
        NotifyPlayerDataChanged();
        SavePlayerData();
    }

    public void AddCharacterExp(int amount)
    {
        if (amount <= 0) return;
        playerData.characterExp += amount;
        NotifyPlayerDataChanged();
        SavePlayerData();
    }
}
