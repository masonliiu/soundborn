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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (playerData == null)
            playerData = new PlayerData();

        if (playerData.ownedCharacters == null)
            playerData.ownedCharacters = new List<CharacterInstance>();

        // seed starter character
        if (playerData.ownedCharacters.Count == 0 && starterPlayer != null)
        {
            playerData.ownedCharacters.Add(new CharacterInstance(starterPlayer));
            playerData.activeCharacterIndex = 0;
        }
        if (extraStartingCharacters != null)
        {
            foreach (var cd in extraStartingCharacters)
            {
                if (cd != null)
                {
                    playerData.ownedCharacters.Add(new CharacterInstance(cd));
                }
            }
        }

        if (currentEnemyData == null && starterEnemy != null)
        {
            currentEnemyData = starterEnemy;
        }

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

    //helpers
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