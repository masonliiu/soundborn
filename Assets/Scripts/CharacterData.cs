using UnityEngine;

// template data for any character (player or enemy)
[CreateAssetMenu(fileName = "NewCharacterData", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Unnamed";
    public ElementType element = ElementType.None;

    [Header("Core Stats")]
    public int maxHP = 100;
    public int attack = 20;
    public int defense = 5;
    public int speed = 10;

    [Header("Crit Settings")]
    [Range(0f, 1f)] public float critChance = 0.1f;
    public float critDamageMultiplier = 1.5f;

    [Header("Ability Power")]
    public int skillPower = 35;
    public int ultimatePower = 60;

    [Header("Status Tuning")]
    public int bleedDamagePerTurn = 10;
    public int defenseUpAmount = 10;

    [Header("Visuals (later)")]
    public Sprite silhouetteSprite;
    public Sprite elementIcon;

    [Header("Boss Settings")]
    public bool isBoss = false;
    public AudioClip bossIntroClip;

    [Header("Abilities & Synergy")]
    public string[] additionalAbilities;
    public string[] synergyTags;
}