using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [Header("Identity")]
    public string displayName = "Unnamed";

    [Header("Core Stats")]
    public int maxHP = 100;
    public int currentHP;
    public int attack = 20;          // basic attack power

    [Header("Ability Power")]
    public int skillPower = 35;      // special skill damage
    public int ultimatePower = 60;   // ultimate damage

    [Header("Cooldowns (in turns)")]
    public int skillCooldownTurns = 2;     // e.g. usable every 2 of *your* turns
    public int ultimateCooldownTurns = 4;  // e.g. every 4 of your turns

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

    // Called at the start of THIS character's turn
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
}