using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleController : MonoBehaviour
{
    [Header("Characters")]
    public CharacterStats player;
    public CharacterStats enemy;

    [Header("UI References")]
    public TextMeshProUGUI playerHpText;
    public TextMeshProUGUI enemyHpText;
    public TextMeshProUGUI battleLogText;

    [Header("Ability Buttons")]
    public Button basicAttackButton;
    public Button skillButton;
    public Button ultimateButton;

    [Header("Enemy Damage")]
    public int enemyAttackDamage = 15;

    private bool playerTurn = true;
    private bool battleOver = false;

    private void Start()
    {
        UpdateUI();
        if (battleLogText != null)
        {
            battleLogText.text = "Battle start!";
        }

        //at the start of the very first turn, tick cooldowns for player
        player.TickCooldowns();
        UpdateAbilityButtons();
    }

    //ability 1: basic attack
    public void OnBasicAttackPressed()
    {
        if (!CanPlayerAct()) return;

        int damage = player.attack;
        enemy.TakeDamage(damage);

        if (battleLogText != null)
        {
            battleLogText.text = $"You strike the enemy for {damage} damage.";
        }

        EndPlayerTurn(afterDealingDamage: true);
    }

    //ability 2: skill
    public void OnSkillPressed()
    {
        if (!CanPlayerAct()) return;

        if (!player.CanUseSkill())
        {
            if (battleLogText != null)
                battleLogText.text = "Skill is on cooldown!";
            return;
        }

        int damage = player.skillPower;
        enemy.TakeDamage(damage);
        player.PutSkillOnCooldown();  // start cooldown

        if (battleLogText != null)
        {
            battleLogText.text = $"You use your skill for {damage} damage!";
        }

        EndPlayerTurn(afterDealingDamage: true);
    }

    //ability 3: ultimate
    public void OnUltimatePressed()
    {
        if (!CanPlayerAct()) return;

        if (!player.CanUseUltimate())
        {
            if (battleLogText != null)
                battleLogText.text = "Ultimate is on cooldown!";
            return;
        }

        int damage = player.ultimatePower;
        enemy.TakeDamage(damage);
        player.PutUltimateOnCooldown();  // start cooldown

        if (battleLogText != null)
        {
            battleLogText.text = $"ULTIMATE! You deal {damage} massive damage!";
        }

        EndPlayerTurn(afterDealingDamage: true);
    }

    //common checks / turn handling

    private bool CanPlayerAct()
    {
        if (battleOver) return false;
        if (!playerTurn) return false;
        return true;
    }

    private void EndPlayerTurn(bool afterDealingDamage)
    {
        UpdateUI();

        //if we hit the enemy with something, check death
        if (afterDealingDamage && enemy.IsDead())
        {
            if (battleLogText != null)
                battleLogText.text += "\nEnemy defeated! You win.";

            battleOver = true;
            UpdateAbilityButtons();
            return;
        }

        //hand turn to enemy
        playerTurn = false;
        UpdateAbilityButtons();
        Invoke(nameof(EnemyTurn), 0.8f);
    }

    private void EnemyTurn()
    {
        if (battleOver) return;

        //simple enemy: always basic attack
        int damage = enemyAttackDamage;
        player.TakeDamage(damage);

        if (battleLogText != null)
        {
            battleLogText.text = $"Enemy hits you for {damage} damage.";
        }

        UpdateUI();

        if (player.IsDead())
        {
            if (battleLogText != null)
                battleLogText.text += "\nYou were defeated...";
            battleOver = true;
            UpdateAbilityButtons();
            return;
        }

        //now it's the player's turn again
        playerTurn = true;

        //at the start of the player's turn, tick their cooldowns
        player.TickCooldowns();
        UpdateAbilityButtons();

        if (battleLogText != null && !battleOver)
        {
            battleLogText.text += "\nYour turn.";
        }
    }

    private void UpdateUI()
    {
        if (playerHpText != null && player != null)
        {
            playerHpText.text = $"Player HP: {player.currentHP}/{player.maxHP}";
        }

        if (enemyHpText != null && enemy != null)
        {
            enemyHpText.text = $"Enemy HP: {enemy.currentHP}/{enemy.maxHP}";
        }
    }

    private void UpdateAbilityButtons()
    {
        //basic attack is available on player's turn if battle not over
        if (basicAttackButton != null)
            basicAttackButton.interactable = playerTurn && !battleOver;

        //skill only if player's turn, not over, and not on cooldown
        if (skillButton != null)
            skillButton.interactable = playerTurn && !battleOver && player.CanUseSkill();

        //ultimate similar
        if (ultimateButton != null)
            ultimateButton.interactable = playerTurn && !battleOver && player.CanUseUltimate();
    }
}