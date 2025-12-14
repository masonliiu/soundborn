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

    [Header("Runtime Data")]
    public PlayerData playerData = new PlayerData();

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

        if (playerData.ownedCharacters == null)
            playerData.ownedCharacters = new List<CharacterInstance>();

        if (playerData.ownedCharacters.Count == 0 && starterPlayer != null)
        {
            playerData.ownedCharacters.Add(new CharacterInstance(starterPlayer));
            playerData.activeCharacterIndex = 0;
            Debug.Log($"[GameManager] Awake: Added starter player: {starterPlayer.displayName}");
        }
        
        if (extraStartingCharacters != null)
        {
            foreach (var cd in extraStartingCharacters)
            {
                if (cd != null)
                {
                    playerData.ownedCharacters.Add(new CharacterInstance(cd));
                    Debug.Log($"[GameManager] Awake: Added extra starting character: {cd.displayName}");
                }
            }
        }
        
        if (playerData.activeLineupIndices == null || playerData.activeLineupIndices.Length != 4)
        {
            playerData.activeLineupIndices = new int[4] { -1, -1, -1, -1 };
            Debug.Log("[GameManager] Awake: Initialized activeLineupIndices to all -1");
        }

        if (playerData.activeLineupIndices[0] == -1 && playerData.ownedCharacters.Count > 0)
        {
            playerData.activeLineupIndices[0] = playerData.activeCharacterIndex;
            Debug.Log($"[GameManager] Awake: Set activeLineupIndices[0] to {playerData.activeCharacterIndex}");
        }

        if (currentEnemyData == null && starterEnemy != null)
        {
            currentEnemyData = starterEnemy;
            Debug.Log($"[GameManager] Awake: Set currentEnemyData to {starterEnemy.displayName}");
        }

        Debug.Log($"[GameManager] Awake: Final state - ownedCharacters.Count={playerData.ownedCharacters.Count}, activeLineupIndices=[{playerData.activeLineupIndices[0]}, {playerData.activeLineupIndices[1]}, {playerData.activeLineupIndices[2]}, {playerData.activeLineupIndices[3]}]");

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
        }
    }

    public CharacterData GetCurrentEnemyData()
    {
        return currentEnemyData;
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
        return true;
    }

    public void AddSoftCurrency(int amount)
    {
        if (amount <= 0) return;
        playerData.softCurrency += amount;
        NotifyPlayerDataChanged();
    }

    public void AddPremiumCurrency(int amount)
    {
        if (amount <= 0) return;
        playerData.premiumCurrency += amount;
        NotifyPlayerDataChanged();
    }
}
