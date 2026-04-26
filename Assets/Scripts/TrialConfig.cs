using UnityEngine;

public enum TrialType
{
    Resonance,
    Equipment
}

[CreateAssetMenu(fileName = "TrialConfig", menuName = "Game/Trial Config")]
public class TrialConfig : ScriptableObject
{
    public TrialDefinition[] trials;

    public TrialDefinition GetTrial(TrialType type, int tier)
    {
        if (trials == null) return null;

        for (int i = 0; i < trials.Length; i++)
        {
            var trial = trials[i];
            if (trial != null && trial.trialType == type && trial.tier == tier)
                return trial;
        }

        return null;
    }
}

[System.Serializable]
public class TrialDefinition
{
    public string displayName = "Trial";
    public TrialType trialType = TrialType.Resonance;
    public int tier = 1;
    public int unlockTowerFloor = 5;

    [Header("Enemies")]
    public CharacterData[] enemies = new CharacterData[4];
    public int enemyScaleFloor = 5;

    [Header("Base Rewards")]
    public int rewardNotes = 100;
    public int rewardCharacterExp = 50;

    [Header("Resonance Reward")]
    public int resonanceMaterialTier = 1;
    public int resonanceMaterialAmount = 1;
    [Range(0f, 1f)] public float resonanceDropChance = 0.2f;

    [Header("Equipment Reward")]
    public ItemData[] equipmentDrops;
    [Range(0f, 1f)] public float equipmentDropChance = 0.25f;
}
