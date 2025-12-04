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

        // initialize stats using GameManager data
        var gm = GameManager.Instance;
        if (gm != null) {
            var activeInstance = gm.GetActiveCharacterInstance();
            if (activeInstance != null) 
            {
                player.InitFrom(activeInstance);
            }

            var enemyData = gm.GetCurrentEnemyData();
            if (enemyData != null) 
            {
                enemy.InitFrom(enemyData);
            }
        }

        UpdateUI();

        // decide who goes first based on Speed
        if (player.speed >= enemy.speed) {
            if (battleLogText != null) 
            {
                battleLogText.text = "Battle start! You act first.";
            }
            StartPlayerTurn();
        } 
        else {
            if (battleLogText != null) 
            {
                battleLogText.text = "Battle start! Enemy acts first.";
            }
            StartEnemyTurn();
        }
    }

    // turn flow

    private void StartPlayerTurn()
    {
        if (battleOver) return;

        player.TickCooldowns();
        int statusDamage;
        bool skipTurn = player.TickStatusAtTurnStart(out statusDamage);

        if (statusDamage > 0 && battleLogText != null)
        {
            battleLogText.text += $"\n{player.displayName} suffers {statusDamage} damage from {player.currentStatus}.";
        }

        UpdateUI();

        if (player.IsDead())
        {
            if (battleLogText != null)
                battleLogText.text += "\nYou were defeated by status...";
            battleOver = true;
            UpdateAbilityButtons();
            return;
        }

        if (skipTurn)
        {
            if (battleLogText != null)
                battleLogText.text += $"\n{player.displayName} is unable to act!";
            // go straight to enemy turn
            StartEnemyTurn();
            return;
        }

        playerTurn = true;
        UpdateAbilityButtons();

        if (battleLogText != null && !battleOver)
        {
            battleLogText.text += "\nYour turn.";
        }
    }

    private void StartEnemyTurn()
    {
        if (battleOver) return;

        playerTurn = false;
        UpdateAbilityButtons();

        enemy.TickCooldowns();
        int statusDamage;
        bool skipTurn = enemy.TickStatusAtTurnStart(out statusDamage);

        if (statusDamage > 0 && battleLogText != null)
        {
            battleLogText.text += $"\n{enemy.displayName} suffers {statusDamage} damage from {enemy.currentStatus}.";
        }

        UpdateUI();

        if (enemy.IsDead())
        {
            if (battleLogText != null)
                battleLogText.text += "\nEnemy defeated by status! You win.";
            battleOver = true;
            UpdateAbilityButtons();
            return;
        }

        if (skipTurn)
        {
            if (battleLogText != null)
                battleLogText.text += $"\n{enemy.displayName} is unable to act!";
            // go straight back to player turn
            StartPlayerTurn();
            return;
        }

        // Delay the actual enemy action a bit
        Invoke(nameof(EnemyAction), 0.8f);
    }

    // player abilities

    // basic attack
    public void OnBasicAttackPressed()
    {
        if (!CanPlayerAct()) return;

        bool isCrit;
        float elemMul;
        int damage = player.CalculateDamageAgainst(enemy, 1.0f, 0, out isCrit, out elemMul);
        enemy.TakeDamage(damage);

        if (battleLogText != null)
        {
            string critText = BuildCritText(isCrit);
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"You strike the enemy for {damage} damage.{critText}{elemText}";
        }

        EndPlayerTurn(afterDealingDamage: true);
    }

    // skill: extra damage + themed status based on your element
    public void OnSkillPressed()
    {
        if (!CanPlayerAct()) return;

        if (!player.CanUseSkill())
        {
            if (battleLogText != null)
                battleLogText.text = "Skill is on cooldown!";
            return;
        }

        bool isCrit;
        float elemMul;
        int damage = player.CalculateDamageAgainst(enemy, 1.2f, player.skillPower, out isCrit, out elemMul);
        enemy.TakeDamage(damage);
        player.PutSkillOnCooldown();

        // Apply themed status based on player's element
        string statusText = ApplyElementalStatusFromPlayerSkill();

        if (battleLogText != null)
        {
            string critText = BuildCritText(isCrit);
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"You use your skill for {damage} damage! {statusText}{critText}{elemText}";
        }

        EndPlayerTurn(afterDealingDamage: true);
    }

    // ultimate: huge damage + DefenseUp on yourself
    public void OnUltimatePressed()
    {
        if (!CanPlayerAct()) return;

        if (!player.CanUseUltimate())
        {
            if (battleLogText != null)
                battleLogText.text = "Ultimate is on cooldown!";
            return;
        }

        bool isCrit;
        float elemMul;
        int damage = player.CalculateDamageAgainst(enemy, 1.5f, player.ultimatePower, out isCrit, out elemMul);
        enemy.TakeDamage(damage);
        player.PutUltimateOnCooldown();

        // DefenseUp for 2 of YOUR turns
        player.ApplyStatus(StatusType.DefenseUp, 2);

        if (battleLogText != null)
        {
            string critText = BuildCritText(isCrit);
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"ULTIMATE! You deal {damage} damage and raise your DEFENSE!{critText}{elemText}";
        }

        EndPlayerTurn(afterDealingDamage: true);
    }

    // themed status: element → status
    private string ApplyElementalStatusFromPlayerSkill()
    {
        switch (player.element)
        {
            case ElementType.Bass:
            case ElementType.Noise:
                // harsh / heavy genres → bleeding ears
                enemy.ApplyStatus(StatusType.BleedEars, 3);
                return "You inflict BLEEDING EARS over time!";

            case ElementType.Harmony:
            case ElementType.Melody:
                // calm / soothing / musical → sleep
                enemy.ApplyStatus(StatusType.Sleep, 1);
                return "Your calm melody puts the enemy to SLEEP, skipping their next turn!";

            case ElementType.Percussion:
            case ElementType.Synth:
                // sharp hits / glitchy shocks → stun
                enemy.ApplyStatus(StatusType.Stun, 1);
                return "You STUN the enemy, they will miss their next turn!";

            default:
                return "";
        }
    }

    // enemy action

    private void EnemyAction()
    {
        if (battleOver) return;

        // simple enemy: basic attack only
        bool isCrit;
        float elemMul;
        int damage = enemy.CalculateDamageAgainst(player, 1.0f, 0, out isCrit, out elemMul);
        player.TakeDamage(damage);

        if (battleLogText != null)
        {
            string critText = BuildCritText(isCrit);
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"Enemy hits you for {damage} damage.{critText}{elemText}";
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
        StartPlayerTurn();
    }

    // core utils

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

        // hand over to enemy
        StartEnemyTurn();
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

    private string BuildElementText(float elemMul)
    {
        if (elemMul > 1.01f)
            return " (Element Advantage)";
        if (elemMul < 0.99f)
            return " (Element Disadvantage)";
        return "";
    }

    private string BuildCritText(bool isCrit)
    {
        return isCrit ? " (CRIT!)" : "";
    }
}