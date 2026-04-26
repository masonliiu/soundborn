using UnityEngine;

public enum ElementType
{
    None,
    Bass,
    Percussion,
    Harmony,
    Noise,
    Melody,
    Synth
}

public enum StatusType
{
    None,
    BleedEars,
    Stun,
    Sleep,
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
    public int attack = 20;
    public int defense = 5;
    public int speed = 10;

    [Header("Crit Settings")]
    [Range(0f, 1f)] public float critChance = 0.1f;
    public float critDamageMultiplier = 1.5f;

    [Header("Ability Power")]
    public int skillPower = 35;
    public int ultimatePower = 60;

    [Header("Cooldowns (in turns)")]
    public int skillCooldownTurns = 2;
    public int ultimateCooldownTurns = 4;

    [HideInInspector] public int skillCooldownRemaining = 0;
    [HideInInspector] public int ultimateCooldownRemaining = 0;

    [Header("Status")]
    public StatusType currentStatus = StatusType.None;
    public int statusDurationTurns = 0;
    public int bleedDamagePerTurn = 10;
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

    public void ApplyStatus(StatusType status, int durationTurns)
    {
        ClearStatus();

        currentStatus = status;
        statusDurationTurns = durationTurns;

        switch (status)
        {
            case StatusType.DefenseUp:
                defense = baseDefense + defenseUpAmount;
                break;

            case StatusType.BleedEars:
                break;

            case StatusType.Stun:
            case StatusType.Sleep:
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
    /// Returns whether this character should skip their action,
    /// and outputs damage taken from status (if any).
    /// </summary>
    public bool TickStatusAtTurnStart(out int damageFromStatus)
    {
        damageFromStatus = 0;
        bool skipTurn = false;

        if (currentStatus == StatusType.None || statusDurationTurns <= 0)
        {
            ClearStatus();
            return false;
        }

        switch (currentStatus)
        {
            case StatusType.BleedEars:
                damageFromStatus = bleedDamagePerTurn;
                TakeDamage(damageFromStatus);
                break;

            case StatusType.Stun:
            case StatusType.Sleep:
                // they lose this turn
                skipTurn = true;
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

        return skipTurn;
    }

    /// <summary>
    /// damage formula using attack, defense, a multiplier, flat bonus,
    /// and an element multiplier + crit. returns final damage and outputs flags.
    /// </summary>
    public int CalculateDamageAgainst(
        CharacterStats target,
        float multiplier,
        int flatBonus,
        out bool isCrit,
        out float elementMultiplier)
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

    public int CalculateDamagePreviewAgainst(CharacterStats target, float multiplier, int flatBonus)
    {
        int raw = attack - target.defense;
        if (raw < 1) raw = 1;

        float scaled = raw * multiplier + flatBonus;
        scaled *= CalculateElementMultiplier(this.element, target.element);

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

        float mul = 1f;

        // strong matchups (1.25x)
        if (attacker == ElementType.Bass       && defender == ElementType.Synth)      mul = 1.25f;
        else if (attacker == ElementType.Synth     && defender == ElementType.Harmony)    mul = 1.25f;
        else if (attacker == ElementType.Harmony   && defender == ElementType.Noise)      mul = 1.25f;
        else if (attacker == ElementType.Noise     && defender == ElementType.Melody)     mul = 1.25f;
        else if (attacker == ElementType.Melody    && defender == ElementType.Percussion) mul = 1.25f;
        else if (attacker == ElementType.Percussion && defender == ElementType.Bass)      mul = 1.25f;

        // weak matchups (0.75x)
        else if (attacker == ElementType.Synth      && defender == ElementType.Bass)       mul = 0.75f;
        else if (attacker == ElementType.Harmony    && defender == ElementType.Synth)      mul = 0.75f;
        else if (attacker == ElementType.Noise      && defender == ElementType.Harmony)    mul = 0.75f;
        else if (attacker == ElementType.Melody     && defender == ElementType.Noise)      mul = 0.75f;
        else if (attacker == ElementType.Percussion && defender == ElementType.Melody)     mul = 0.75f;
        else if (attacker == ElementType.Bass       && defender == ElementType.Percussion) mul = 0.75f;

        return mul;
    }
    public void InitFrom(CharacterData data)
    {
        if (data == null)
        {
            Debug.LogWarning("InitFrom called with null CharacterData on " + name);
            return;
        }

        displayName = data.displayName;
        element = data.element;

        maxHP = data.maxHP;
        currentHP = maxHP;

        attack = data.attack;
        defense = data.defense;
        speed = data.speed;

        critChance = data.critChance;
        critDamageMultiplier = data.critDamageMultiplier;

        skillPower = data.skillPower;
        ultimatePower = data.ultimatePower;

        bleedDamagePerTurn = data.bleedDamagePerTurn;
        defenseUpAmount = data.defenseUpAmount;

        // reset runtime stuff
        skillCooldownRemaining = 0;
        ultimateCooldownRemaining = 0;
        ClearStatus();
    }

    public void InitFrom(CharacterInstance instance)
    {
        if (instance == null || instance.data == null)
        {
            Debug.LogWarning("InitFrom called with null CharacterInstance on " + name);
            return;
        }

        InitFrom(instance.data);

        instance.GetTotalStats(out maxHP, out attack, out defense, out speed);
        currentHP = maxHP;
    }
}
