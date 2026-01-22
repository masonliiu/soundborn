using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Starting Setup")]
    [Header("Extra Starting Characters")]
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
        Debug.Log("[GameManager] Awake() called");
        
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameManager] Awake: Another GameManager instance exists, destroying this one");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[GameManager] Awake: Instance set and DontDestroyOnLoad enabled");

        if (playerData == null)
            playerData = new PlayerData();

        // Ensure collections exist before load.
        if (playerData.ownedCharacters == null)
            playerData.ownedCharacters = new List<CharacterInstance>();
        if (playerData.inventory == null)
            playerData.inventory = new List<ItemInstance>();

        bool loaded = SaveSystem.TryLoad(playerData, characterDatabase, itemDatabase);
        Debug.Log($"[GameManager] Awake: TryLoad returned {loaded}");

        if (!loaded)
        {
            InitializeNewPlayerData();
            SavePlayerData();
        }

        // Ensure quests exist for this profile
        if (questManager != null)
        {
            questManager.EnsureInitialQuests();
        }

        if (currentEnemyData == null && starterEnemy != null)
        {
            currentEnemyData = starterEnemy;
            Debug.Log($"[GameManager] Awake: Set currentEnemyData to {starterEnemy.displayName}");
        }

        Debug.Log($"[GameManager] Awake: Final state - ownedCharacters.Count={playerData.ownedCharacters.Count}, activeLineupIndices=[{playerData.activeLineupIndices[0]}, {playerData.activeLineupIndices[1]}, {playerData.activeLineupIndices[2]}, {playerData.activeLineupIndices[3]}]");

        NotifyPlayerDataChanged();
    }

    private void InitializeNewPlayerData()
    {
        Debug.Log("[GameManager] InitializeNewPlayerData() called");

        // Reset all scalar fields to their default starting values.
        playerData.softCurrency = 0;
        playerData.premiumCurrency = 0;
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
            Debug.Log($"[GameManager] InitializeNewPlayerData: Added starter player: {starterPlayer.displayName}");
        }

        if (extraStartingCharacters != null)
        {
            foreach (var cd in extraStartingCharacters)
            {
                if (cd != null)
                {
                    playerData.ownedCharacters.Add(new CharacterInstance(cd));
                    Debug.Log($"[GameManager] InitializeNewPlayerData: Added extra starting character: {cd.displayName}");
                }
            }
        }

        if (playerData.activeLineupIndices == null || playerData.activeLineupIndices.Length != 4)
        {
            playerData.activeLineupIndices = new int[4] { -1, -1, -1, -1 };
            Debug.Log("[GameManager] InitializeNewPlayerData: Initialized activeLineupIndices to all -1");
        }

        if (playerData.activeLineupIndices[0] == -1 && playerData.ownedCharacters.Count > 0)
        {
            playerData.activeLineupIndices[0] = playerData.activeCharacterIndex;
            Debug.Log($"[GameManager] InitializeNewPlayerData: Set activeLineupIndices[0] to {playerData.activeCharacterIndex}");
        }
    }

    public void SavePlayerData()
    {
        SaveSystem.Save(playerData);
    }

    /// <summary>
    /// Completely reset the player's progress and re‑initialise starting data.
    /// This is intended for developer / debug use and can be invoked from the Inspector
    /// via the context menu or from other tools.
    /// </summary>
    [ContextMenu("Developer/Reset Player Progress")]
    public void DebugResetPlayerProgress()
    {
        Debug.Log("[GameManager] DebugResetPlayerProgress() called – clearing PlayerData and reinitialising.");

        // Start from a fresh PlayerData instance.
        playerData = new PlayerData();
        InitializeNewPlayerData();
        // Persist immediately so next launch also starts fresh.
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
        if (inst == null)
            return 0;

        const int baseCost = 100;
        return baseCost * Mathf.Max(1, inst.level);
    }

    public bool TryLevelUpCharacter(CharacterInstance inst)
    {
        if (inst == null)
            return false;

        int cost = GetLevelUpCost(inst);
        if (playerData.softCurrency < cost)
            return false;

        playerData.softCurrency -= cost;
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
}
