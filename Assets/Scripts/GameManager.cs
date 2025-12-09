using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Starting Setup")]
    public CharacterData starterPlayer;
    public CharacterData starterEnemy;

    [Header("Economy")]
    public int startingSoftCurrency = 0;       // e.g. gold
    public int startingPremiumCurrency = 0;    // e.g. gems

    [Tooltip("Base cost to go from level 1 → 2")]
    public int baseLevelUpCost = 50;

    [Tooltip("Each extra level multiplies cost by this")]
    public float levelUpCostMultiplier = 1.5f;

    [Header("Runtime Data")]
    public PlayerData playerData = new PlayerData();

    public CharacterData currentEnemyData;

    public Action OnPlayerDataChanged;

    private void Awake()
    {
        // singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (playerData.ownedCharacters.Count == 0 && starterPlayer != null)
        {
            playerData.ownedCharacters.Add(new CharacterInstance(starterPlayer));
            playerData.activeCharacterIndex = 0;
        }

        if (currentEnemyData == null && starterEnemy != null)
        {
            currentEnemyData = starterEnemy;
        }

        if (playerData.softCurrency == 0 && playerData.premiumCurrency == 0)
        {
            playerData.softCurrency = startingSoftCurrency;
            playerData.premiumCurrency = startingPremiumCurrency;
        }

        NotifyDataChanged();
    }

    private void NotifyDataChanged()
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

        playerData.activeCharacterIndex =
            Mathf.Clamp(index, 0, playerData.ownedCharacters.Count - 1);

        NotifyDataChanged();
    }

    public List<CharacterInstance> GetOwnedCharacters()
    {
        return playerData.ownedCharacters;
    }

    public CharacterData GetCurrentEnemyData()
    {
        return currentEnemyData;
    }

    public void AddSoftCurrency(int amount)
    {
        if (amount == 0) return;

        playerData.softCurrency = Mathf.Max(0, playerData.softCurrency + amount);
        NotifyDataChanged();
    }

    public bool TrySpendSoftCurrency(int amount)
    {
        if (amount <= 0)
            return true;

        if (playerData.softCurrency < amount)
            return false;

        playerData.softCurrency -= amount;
        NotifyDataChanged();
        return true;
    }

    public void AddPremiumCurrency(int amount)
    {
        if (amount == 0) return;

        playerData.premiumCurrency = Mathf.Max(0, playerData.premiumCurrency + amount);
        NotifyDataChanged();
    }

    public int GetLevelUpCost(CharacterInstance instance)
    {
        if (instance == null)
            return 0;

        // level 1 → 2 uses baseLevelUpCost
        // each further level multiplies by levelUpCostMultiplier
        int levelIndex = Mathf.Max(0, instance.level - 1);
        float cost = baseLevelUpCost * Mathf.Pow(levelUpCostMultiplier, levelIndex);
        return Mathf.RoundToInt(cost);
    }

    public bool TryLevelUpCharacter(CharacterInstance instance)
    {
        if (instance == null)
            return false;

        int cost = GetLevelUpCost(instance);
        if (!TrySpendSoftCurrency(cost))
            return false;

        instance.level++;
        NotifyDataChanged();
        return true;
    }

    public bool TryLevelUpActiveCharacter()
    {
        return TryLevelUpCharacter(GetActiveCharacterInstance());
    }
}