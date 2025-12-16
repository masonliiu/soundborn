using UnityEngine;

public enum QuestType
{
    WinBattles,
    ClearFloor,
    LevelUpCharacter,
    ChangeTeam,
    DoGachaPull,
    UseSkill,
    Other
}

/// <summary>
/// Static definition of a quest/mission.
/// </summary>
[CreateAssetMenu(fileName = "NewQuestData", menuName = "Game/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("Identity")]
    public string questId = "quest_id";
    public string title = "New Quest";
    [TextArea]
    public string description;

    [Header("Conditions")]
    public QuestType type = QuestType.Other;
    public int targetCount = 1;
    public int requiredFloor = 0; // for ClearFloor-type quests

    [Header("Rewards")]
    public int rewardSoftCurrency = 0;
    public int rewardPremiumCurrency = 0;
    public ItemData rewardItem;
}


