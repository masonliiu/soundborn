using UnityEngine;

// element types for the game's music world
public enum ElementType
{
    None,
    Bass,       // heavy low-end, power
    Percussion, // rhythm / speed
    Harmony,    // chords, support
    Noise,      // distortion / chaos
    Melody,     // hooks, leads
    Synth       // electronic / digital
}

public enum StatusType
{
    None,
    Burn,
    DefenseUp
}

public class CharacterStats : MonoBehaviour
{
    [Header("Identity")]
    public string displayName = "Unnamed";
    public ElementType element = ElementType.None;

    [Header("Core Stats")]
    public int maxHP = 100;
    public int currentHP;
    public int attack = 20;    // base attack power
    public int defense = 5;    // base defense
    public int speed = 10;     // used for who goes first (later for full turn order)

    [Header("Crit Settings")]
    [Range(0f, 1f)] public float critChance = 0.1f;
    public float critDamageMultiplier = 1.5f;   // MUST be float

    [Header("Ability Power")]
    public int skillPower = 35;      // extra flat power for skill
    public int ultimatePower = 60;   // extra flat power for ultimate

    [Header("Cooldowns (in turns)")]
    public int skillCooldownTurns = 2;   // how many of your turns before you can use it again
    public int ultimateCooldownTurns = 4;

    [HideInInspector] public int skillCooldownRemaining = 0;
    [HideInInspector] public int ultimateCooldownRemaining = 0;

    [Header("Status")]
    public StatusType currentStatus = StatusType.None;
    public int statusDurationTurns = 0;
    public int burnDamagePerTurn = 10;
    public int defenseUpAmount = 10;

    private int baseDefense;

    private void Awake()
    {
        currentHP = maxHP;
        baseDefense = defense;
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;
        if (currentHP < 0)
        {
            currentHP = 0;
        }
    }

    public bool IsDead()
    {
        return currentHP <= 0;
    }

    // ===== COOL DOWNS =====

    // called at the start of current character's turn
    public void TickCooldowns()
    {
        if (skillCooldownRemaining > 0)
            skillCooldownRemaining--;

        if (ultimateCooldownRemaining > 0)
            ultimateCooldownRemaining--;
    }

    public void PutSkillOnCooldown()
    {
        skillCooldownRemaining = skillCooldownTurns;
    }

    public void PutUltimateOnCooldown()
    {
        ultimateCooldownRemaining = ultimateCooldownTurns;
    }

    public bool CanUseSkill()
    {
        return skillCooldownRemaining <= 0;
    }

    public bool CanUseUltimate()
    {
        return ultimateCooldownRemaining <= 0;
    }

    // ===== STATUS =====

    public void ApplyStatus(StatusType status, int durationTurns)
    {
        // only one status at a time for now
        ClearStatus();

        currentStatus = status;
        statusDurationTurns = durationTurns;

        switch (status)
        {
            case StatusType.DefenseUp:
                // apply buff
                defense = baseDefense + defenseUpAmount;
                break;

            case StatusType.Burn:
                // damage will be applied at turn start
                break;

            case StatusType.None:
            default:
                break;
        }
    }

    public void ClearStatus()
    {
        defense = baseDefense;
        currentStatus = StatusType.None;
        statusDurationTurns = 0;
    }

    /// <summary>
    /// Called at the START of this character's turn, after cooldown tick.
    /// Returns damage taken from status effects (if any).
    /// </summary>
    public int TickStatusAtTurnStart()
    {
        int damageFromStatus = 0;

        if (currentStatus == StatusType.None || statusDurationTurns <= 0)
        {
            ClearStatus();
            return 0;
        }

        switch (currentStatus)
        {
            case StatusType.Burn:
                damageFromStatus = burnDamagePerTurn;
                TakeDamage(damageFromStatus);
                break;

            case StatusType.DefenseUp:
                // buff already applied, just counting down
                break;
        }

        statusDurationTurns--;

        if (statusDurationTurns <= 0)
        {
            ClearStatus();
        }

        return damageFromStatus;
    }

    /// <summary>
    /// damage formula using attack, defense, a multiplier, flat bonus,
    /// and an element multiplier + crit. returns final damage and outputs flags.
    /// </summary>
    public int CalculateDamageAgainst(CharacterStats target, float multiplier, int flatBonus, out bool isCrit, out float elementMultiplier)
    {
        // base attack vs defense
        int raw = attack - target.defense;
        if (raw < 1) raw = 1;

        float scaled = raw * multiplier + flatBonus;

        // element multiplier based on your world rules
        elementMultiplier = CalculateElementMultiplier(this.element, target.element);
        scaled *= elementMultiplier;

        // crit
        isCrit = false;
        if (Random.value < critChance)
        {
            scaled *= critDamageMultiplier;
            isCrit = true;
        }

        int finalDamage = Mathf.RoundToInt(scaled);
        if (finalDamage < 1) finalDamage = 1;

        return finalDamage;
    }

    /// <summary>
    /// Element wheel:
    /// Bass > Synth
    /// Synth > Harmony
    /// Harmony > Noise
    /// Noise > Melody
    /// Melody > Percussion
    /// Percussion > Bass
    /// Reverse = disadvantage. Neutral otherwise.
    /// </summary>
    private float CalculateElementMultiplier(ElementType attacker, ElementType defender)
    {
        if (attacker == ElementType.None || defender == ElementType.None)
            return 1f;

        // default neutral
        float mul = 1f;

        // strong matchups (1.25x)
        if (attacker == ElementType.Bass       && defender == ElementType.Synth)      mul = 1.25f;
        else if (attacker == ElementType.Synth     && defender == ElementType.Harmony)    mul = 1.25f;
        else if (attacker == ElementType.Harmony   && defender == ElementType.Noise)      mul = 1.25f;
        else if (attacker == ElementType.Noise     && defender == ElementType.Melody)     mul = 1.25f;
        else if (attacker == ElementType.Melody    && defender == ElementType.Percussion) mul = 1.25f;
        else if (attacker == ElementType.Percussion && defender == ElementType.Bass)      mul = 1.25f;

        // weak matchups (0.75x) - reverse of above
        else if (attacker == ElementType.Synth      && defender == ElementType.Bass)       mul = 0.75f;
        else if (attacker == ElementType.Harmony    && defender == ElementType.Synth)      mul = 0.75f;
        else if (attacker == ElementType.Noise      && defender == ElementType.Harmony)    mul = 0.75f;
        else if (attacker == ElementType.Melody     && defender == ElementType.Noise)      mul = 0.75f;
        else if (attacker == ElementType.Percussion && defender == ElementType.Melody)     mul = 0.75f;
        else if (attacker == ElementType.Bass       && defender == ElementType.Percussion) mul = 0.75f;

        return mul;
    }
}