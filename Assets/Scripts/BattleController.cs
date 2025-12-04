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

    private bool playerTurn = true;
    private bool battleOver = false;    

    private void Start()
    {
        UpdateUI();
        if (battleLogText != null)
        {
            battleLogText.text = "Battle start!";
        }

        // start-of-battle: player turn setup
        player.TickCooldowns();
        UpdateAbilityButtons();
    }

    // basic attack
    public void OnBasicAttackPressed()
    {
        if (!CanPlayerAct()) return;

        float elemMul;
        int damage = player.CalculateDamageAgainst(enemy, 1.0f, 0, out elemMul);
        enemy.TakeDamage(damage);

        if (battleLogText != null)
        {
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"You strike the enemy for {damage} damage.{elemText}";
        }

        EndPlayerTurn(afterDealingDamage: true);
    }

    // skill (more damage, on cooldown)
    public void OnSkillPressed()
    {
        if (!CanPlayerAct()) return;

        if (!player.CanUseSkill())
        {
            if (battleLogText != null)
                battleLogText.text = "Skill is on cooldown!";
            return;
        }

        float elemMul;
        int damage = player.CalculateDamageAgainst(enemy, 1.2f, player.skillPower, out elemMul);
        enemy.TakeDamage(damage);
        player.PutSkillOnCooldown();

        if (battleLogText != null)
        {
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"You use your skill for {damage} damage!{elemText}";
        }

        EndPlayerTurn(afterDealingDamage: true);
    }

    // ultimate (huge hit, long cooldown)
    public void OnUltimatePressed()
    {
        if (!CanPlayerAct()) return;

        if (!player.CanUseUltimate())
        {
            if (battleLogText != null)
                battleLogText.text = "Ultimate is on cooldown!";
            return;
        }

        float elemMul;
        int damage = player.CalculateDamageAgainst(enemy, 1.5f, player.ultimatePower, out elemMul);
        enemy.TakeDamage(damage);
        player.PutUltimateOnCooldown();

        if (battleLogText != null)
        {
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"ULTIMATE! You deal {damage} massive damage!{elemText}";
        }

        EndPlayerTurn(afterDealingDamage: true);
    }

    // enemy turn
    private void EnemyTurn()
    {
        if (battleOver) return;

        // Enemy just uses basic attack for now
        float elemMul;
        int damage = enemy.CalculateDamageAgainst(player, 1.0f, 0, out elemMul);
        player.TakeDamage(damage);

        if (battleLogText != null)
        {
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"Enemy hits you for {damage} damage.{elemText}";
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

        // back to player's turn
        playerTurn = true;
        // tick player's cooldowns at start of their turn
        player.TickCooldowns();
        UpdateAbilityButtons();

        if (battleLogText != null && !battleOver)
        {
            battleLogText.text += "\nYour turn.";
        }
    }

    // turn handling / utilities

    private bool CanPlayerAct()
    {
        if (battleOver) return false;
        if (!playerTurn) return false;
        return true;
    }

    private void EndPlayerTurn(bool afterDealingDamage)
    {
        UpdateUI();

        if (afterDealingDamage && enemy.IsDead())
        {
            if (battleLogText != null)
                battleLogText.text += "\nEnemy defeated! You win.";
            battleOver = true;
            UpdateAbilityButtons();
            return;
        }

        playerTurn = false;
        UpdateAbilityButtons();
        Invoke(nameof(EnemyTurn), 0.8f);
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
        bool canAct = playerTurn && !battleOver;

        if (basicAttackButton != null)
            basicAttackButton.interactable = canAct;

        if (skillButton != null)
            skillButton.interactable = canAct && player.CanUseSkill();

        if (ultimateButton != null)
            ultimateButton.interactable = canAct && player.CanUseUltimate();
    }

    // small helper to write text for element advantage
    private string BuildElementText(float elemMul)
    {
        if (elemMul > 1.01f)
            return " (Element Advantage)";
        if (elemMul < 0.99f)
            return " (Element Disadvantage)";
        return "";
    }
}