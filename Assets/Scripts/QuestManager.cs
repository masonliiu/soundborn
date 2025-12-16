using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages active quests and updates progress based on gameplay events.
/// Lives alongside GameManager and uses PlayerData for persistence.
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Quest Definitions")]
    public QuestData[] initialQuests; // quests that are active for new players

    public event Action OnQuestsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private PlayerData Player => GameManager.Instance != null ? GameManager.Instance.playerData : null;

    public void EnsureInitialQuests()
    {
        var pd = Player;
        if (pd == null) return;

        if (pd.questStates == null)
            pd.questStates = new List<QuestState>();

        if (pd.questStates.Count == 0 && initialQuests != null)
        {
            foreach (var q in initialQuests)
            {
                if (q == null || string.IsNullOrEmpty(q.questId)) continue;
                if (!HasQuest(q.questId))
                {
                    pd.questStates.Add(new QuestState { questId = q.questId, currentCount = 0, isCompleted = false, isClaimed = false });
                }
            }
            OnQuestsChanged?.Invoke();
        }
    }

    private bool HasQuest(string questId)
    {
        var pd = Player;
        if (pd == null || pd.questStates == null) return false;
        for (int i = 0; i < pd.questStates.Count; i++)
        {
            if (pd.questStates[i].questId == questId)
                return true;
        }
        return false;
    }

    private QuestState FindState(string questId)
    {
        var pd = Player;
        if (pd == null || pd.questStates == null) return null;
        for (int i = 0; i < pd.questStates.Count; i++)
        {
            if (pd.questStates[i].questId == questId)
                return pd.questStates[i];
        }
        return null;
    }

    private QuestData FindDefinition(string questId)
    {
        if (string.IsNullOrEmpty(questId) || initialQuests == null) return null;
        for (int i = 0; i < initialQuests.Length; i++)
        {
            var q = initialQuests[i];
            if (q != null && q.questId == questId)
                return q;
        }
        return null;
    }

    #region Event hooks

    public void OnBattleWon(int floorIndex)
    {
        IncrementQuests(QuestType.WinBattles, 1, floorIndex);
        IncrementQuests(QuestType.ClearFloor, 1, floorIndex);
    }

    public void OnCharacterLeveledUp()
    {
        IncrementQuests(QuestType.LevelUpCharacter, 1, 0);
    }

    public void OnTeamChanged()
    {
        IncrementQuests(QuestType.ChangeTeam, 1, 0);
    }

    public void OnGachaPulled()
    {
        IncrementQuests(QuestType.DoGachaPull, 1, 0);
    }

    public void OnSkillUsed()
    {
        IncrementQuests(QuestType.UseSkill, 1, 0);
    }

    #endregion

    private void IncrementQuests(QuestType type, int amount, int floorIndex)
    {
        var pd = Player;
        if (pd == null || pd.questStates == null || amount <= 0) return;

        bool changed = false;

        foreach (var state in pd.questStates)
        {
            if (state == null || state.isCompleted) continue;

            var def = FindDefinition(state.questId);
            if (def == null || def.type != type) continue;

            if (def.type == QuestType.ClearFloor && def.requiredFloor > 0 && floorIndex + 1 != def.requiredFloor)
                continue;

            state.currentCount += amount;
            if (state.currentCount >= def.targetCount)
            {
                state.currentCount = def.targetCount;
                state.isCompleted = true;
            }
            changed = true;
        }

        if (changed)
        {
            OnQuestsChanged?.Invoke();
            if (GameManager.Instance != null)
                GameManager.Instance.SavePlayerData();
        }
    }

    public void ClaimReward(string questId)
    {
        var pd = Player;
        if (pd == null || pd.questStates == null) return;

        var state = FindState(questId);
        var def = FindDefinition(questId);
        if (state == null || def == null || !state.isCompleted || state.isClaimed) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddSoftCurrency(def.rewardSoftCurrency);
            GameManager.Instance.AddPremiumCurrency(def.rewardPremiumCurrency);
        }

        if (def.rewardItem != null)
        {
            if (pd.inventory == null)
                pd.inventory = new List<ItemInstance>();
            pd.inventory.Add(new ItemInstance(def.rewardItem));
        }

        state.isClaimed = true;
        OnQuestsChanged?.Invoke();
        if (GameManager.Instance != null)
            GameManager.Instance.SavePlayerData();
    }
}


