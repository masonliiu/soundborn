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

public class CharacterStats : MonoBehaviour
{
    [Header("Identity")]
    public string displayName = "Unnamed";
    public ElementType element = ElementType.None;

    [Header("Core Stats")]
    public int maxHP = 100;
    public int currentHP;
    public int attack = 20;          // base attack power
    public int defense = 5;          // damage reduction
    public int speed = 10;           // used later for turn order

    [Header("Ability Power")]
    public int skillPower = 35;      // extra flat power for skill
    public int ultimatePower = 60;   // extra flat power for ultimate

    [Header("Cooldowns (in turns)")]
    public int skillCooldownTurns = 2;     // how many of your turns before you can use it again
    public int ultimateCooldownTurns = 4;

    [HideInInspector] public int skillCooldownRemaining = 0;
    [HideInInspector] public int ultimateCooldownRemaining = 0;

    private void Awake()
    {
        currentHP = maxHP;
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

    // cooldowns

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

    /// <summary>
    /// damage formula using attack, defense, a multiplier, flat bonus,
    /// and an element multiplier. returns final damage and outputs the element multiplier used.
    /// </summary>
    public int CalculateDamageAgainst(CharacterStats target, float multiplier, int flatBonus, out float elementMultiplier)
    {
        // base attack vs defense
        int raw = attack - target.defense;
        if (raw < 1) raw = 1;

        float scaled = raw * multiplier + flatBonus;

        // element multiplier based on your world rules
        elementMultiplier = CalculateElementMultiplier(this.element, target.element);
        scaled *= elementMultiplier;

        int finalDamage = Mathf.RoundToInt(scaled);
        if (finalDamage < 1) finalDamage = 1;

        return finalDamage;
    }

    // convenience overload if you ever want to ignore elementMultiplier
    public int CalculateDamageAgainst(CharacterStats target, float multiplier = 1f, int flatBonus = 0)
    {
        float _; // discard element multiplier
        return CalculateDamageAgainst(target, multiplier, flatBonus, out _);
    }

    /// <summary>
    /// element wheel:
    /// Bass > Synth
    /// Synth > Harmony
    /// Harmony > Noise
    /// Noise > Melody
    /// Melody > Percussion
    /// Percussion > Bass
    /// reverse direction = disadvantage.
    /// Neutral otherwise.
    /// </summary>
    private float CalculateElementMultiplier(ElementType attacker, ElementType defender)
    {
        if (attacker == ElementType.None || defender == ElementType.None)
            return 1f;

        // default neutral
        float mul = 1f;

        // strong matchups (1.25x)
        if (attacker == ElementType.Bass      && defender == ElementType.Synth)      mul = 1.25f;
        else if (attacker == ElementType.Synth    && defender == ElementType.Harmony)    mul = 1.25f;
        else if (attacker == ElementType.Harmony  && defender == ElementType.Noise)      mul = 1.25f;
        else if (attacker == ElementType.Noise    && defender == ElementType.Melody)     mul = 1.25f;
        else if (attacker == ElementType.Melody   && defender == ElementType.Percussion) mul = 1.25f;
        else if (attacker == ElementType.Percussion && defender == ElementType.Bass)     mul = 1.25f;

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