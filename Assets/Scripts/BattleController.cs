using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BattleController : MonoBehaviour
{

    [Header("HP Bar Animation")]
    public float hpBarAnimDuration = 0.35f;

    private bool isPlayerHpAnimating = false;
    private bool isEnemyHpAnimating = false;

    [Header("Death FX")]
    public Material enemyPixelateMaterialTemplate;
    public float enemyDeathPixelDuration = 0.8f;
    public float enemyDeathHoldDelay = 0.4f;

    private Material enemyPixelateMaterialRuntime;

    [Header("Result UI")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public float resultFadeDuration = 0.7f;

    [Header("Camera Effects")]
    public Camera mainCamera;
    public float baseCamSize = 5f;
    public float camShakeStrength = 0.15f;
    public float camShakeDuration = 0.12f;

    [Header("Screen Shake Root")]
    public RectTransform battleRoot;

    private Vector2 baseRootPos;

    private Vector3 baseCamPos;

    [Header("Ability Card")]
    public GameObject abilityCardPanel;
    public TextMeshProUGUI abilityCardName;
    public TextMeshProUGUI abilityCardStats;
    public TextMeshProUGUI abilityCardDescription;

    private enum PendingAbility { None, Basic, Skill, Ultimate }
    private PendingAbility pendingAbility = PendingAbility.None;

    [Header("Impact Effects")]
    public ImpactEffect impactEffectPrefab;
    public RectTransform playerImpactAnchor;
    public RectTransform enemyImpactAnchor;

    [Header("Status Icons")]
    public Image playerStatusIcon;
    public Image enemyStatusIcon;

    [Header("Damage Popup")]
    public DamagePopup damagePopupPrefab;
    public RectTransform playerPopupAnchor;
    public RectTransform enemyPopupAnchor;

    [Header("Attack Animation")]
    public RectTransform playerPortraitRect;
    public RectTransform enemyPortraitRect;
    public float attackMoveDistance = 80f;
    public float attackMoveDuration = 0.15f;
    public float hitShakeDistance = 20f;
    public float hitShakeDuration = 0.1f;

    // placeholder for sprites because im not an artist..
    public Color bleedColor = Color.red;
    public Color stunColor = new Color(1f, 0.8f, 0f);
    public Color sleepColor = new Color(0.5f, 0.7f, 1f);
    public Color defenseUpColor = new Color(0.3f, 1f, 0.3f);
    public Color noStatusColor = new Color(1f, 1f, 1f, 0f); //transp

    [Header("Characters")]
    public CharacterStats player;
    public CharacterStats enemy;

    [Header("Portraits")]
    public Image playerPortraitImage;
    public Image enemyPortraitImage;

    [Header("UI References")]
    public TextMeshProUGUI playerHpText;
    public TextMeshProUGUI enemyHpText;
    public TextMeshProUGUI battleLogText;

    public Slider playerHpSlider;
    public Slider enemyHpSlider;

    [Header("HP Bar Damage Overlay")]
    public Slider playerHpDamageSlider;   
    public Slider enemyHpDamageSlider;  

    [Header("Ability Buttons")]
    public Button basicAttackButton;
    public Button skillButton;
    public Button ultimateButton;

    private bool playerTurn = true;
    private bool battleOver = false;

    private void Start()
    {
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainCamera != null)
            {
                baseCamPos = mainCamera.transform.position;
                baseCamSize = mainCamera.orthographicSize;
            }

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

        if (gm != null) {
            if (playerPortraitImage != null) {
                var activeInstance = gm.GetActiveCharacterInstance();
                if (activeInstance != null && activeInstance.data.silhouetteSprite != null) {
                    playerPortraitImage.sprite = activeInstance.data.silhouetteSprite;
                }
            }

            if (enemyPortraitImage != null) {
                var enemyData = gm.GetCurrentEnemyData();
                if (enemyData != null && enemyData.silhouetteSprite != null) {
                    enemyPortraitImage.sprite = enemyData.silhouetteSprite;
                }
            }
        }

        UpdateUI();
        if (battleRoot != null)
        {
            baseRootPos = battleRoot.anchoredPosition;
        }

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

        StartCoroutine(EnemyTurnSequence());
    }

    private IEnumerator EnemyTurnSequence()
    {
        if (battleOver) yield break;

        playerTurn = false;
        UpdateAbilityButtons();

        enemy.TickCooldowns();
        int statusDamage;
        bool skipTurn = enemy.TickStatusAtTurnStart(out statusDamage);

        bool statusDidDamage = statusDamage > 0;

        if (statusDidDamage)
        {
            // TickStatusAtTurnStart has already reduced enemy.currentHP by statusDamage.
            int newHp = enemy.currentHP;
            int oldHp = Mathf.Clamp(newHp + statusDamage, 0, enemy.maxHP);

            // Log the status damage line
            if (battleLogText != null)
            {
                battleLogText.text += $"\n{enemy.displayName} suffers {statusDamage} damage from {enemy.currentStatus}.";
            }

            // Set up the overlay to start from the pre-status HP value.
            if (enemyHpDamageSlider != null)
            {
                enemyHpDamageSlider.maxValue = enemy.maxHP;
                enemyHpDamageSlider.value = oldHp;
            }

            if (enemyHpText != null)
            {
                enemyHpText.text = $"{enemy.displayName} {oldHp}/{enemy.maxHP}";
            }
            if (enemyHpSlider != null && !isEnemyHpAnimating)
            {
                enemyHpSlider.maxValue = enemy.maxHP;
                enemyHpSlider.value = oldHp;
            }

            float preHitDelay = 0.5f;
            yield return new WaitForSeconds(preHitDelay);
            StartCoroutine(Shake(enemyPortraitRect));

            SpawnImpact(onEnemy: true, color: GetStatusColor(enemy.currentStatus));
            SpawnDamagePopup(onEnemy: true, amount: statusDamage, isCrit: false);

            if (enemyHpText != null)
            {
                enemyHpText.text = $"{enemy.displayName} {newHp}/{enemy.maxHP}";
            }
            if (enemyHpSlider != null && !isEnemyHpAnimating)
            {
                enemyHpSlider.value = newHp;
            }

            float postHitDelay = 0.25f;
            yield return new WaitForSeconds(postHitDelay);

            if (enemyHpDamageSlider != null)
            {
                yield return StartCoroutine(
                    AnimateHpBar(enemyHpDamageSlider, oldHp, newHp, isEnemy: true)
                );
            }
        }
        else
        {
            UpdateUI();
        }

        if (enemy.IsDead())
        {
            if (battleLogText != null)
                battleLogText.text += "\nEnemy defeated by status! You win.";
            battleOver = true;
            UpdateAbilityButtons();
            PlayWinSequence();
            yield break;
        }

        if (skipTurn)
        {
            if (battleLogText != null)
                battleLogText.text += $"\n{enemy.displayName} is unable to act!";
            StartPlayerTurn();
            yield break;
        }

        float preActionDelay = 0.8f;
        yield return new WaitForSeconds(preActionDelay);

        EnemyAction();
    }

    // player abilities

    // basic attack
    public void OnBasicAttackPressed()
    {
        if (!CanPlayerAct()) return;

        if (pendingAbility != PendingAbility.Basic) {
            ShowAbilityCard(PendingAbility.Basic);
            return;
        }

        HideAbilityCard();
        StartCoroutine(PlayerBasicAttackRoutine());
    }
    private IEnumerator PlayerBasicAttackRoutine()
    {
        bool isCrit;
        float elemMul;
        int damage = player.CalculateDamageAgainst(enemy, 1.0f, 0, out isCrit, out elemMul);

        StartCoroutine(LungeForward(playerPortraitRect, towardsCenter: true));

        int oldHp = enemy.currentHP;
        enemy.TakeDamage(damage);
        int newHp = enemy.currentHP;

        if (isCrit)
            StartCoroutine(CameraShake());
        SpawnImpact(onEnemy: true, color: GetElementColor(player.element));
        SpawnDamagePopup(onEnemy: true, amount: damage, isCrit: isCrit);
        StartCoroutine(Shake(enemyPortraitRect));

        if (battleLogText != null)
        {
            string critText = BuildCritText(isCrit);
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"You strike the enemy for {damage} damage.{critText}{elemText}";
        }
        UpdateUI();

        if (enemyHpDamageSlider != null)
        {
            enemyHpDamageSlider.maxValue = enemy.maxHP;
            enemyHpDamageSlider.value = oldHp;
        }

        float postHitDelay = 0.35f;
        yield return new WaitForSeconds(postHitDelay);

        if (enemyHpDamageSlider != null)
        {
            yield return StartCoroutine(
                AnimateHpBar(enemyHpDamageSlider, oldHp, enemy.currentHP, isEnemy: true)
            );
        }

        EndPlayerTurn(afterDealingDamage: true);
    }

    public void OnSkillPressed()
    {
        if (!CanPlayerAct()) return;

        if (pendingAbility != PendingAbility.Skill) {
            ShowAbilityCard(PendingAbility.Skill);
            return;
        }

        HideAbilityCard();

        if (!player.CanUseSkill())
        {
            if (battleLogText != null)
                battleLogText.text = "Skill is on cooldown!";
            return;
        }

        StartCoroutine(PlayerSkillRoutine());
    }

    private IEnumerator PlayerSkillRoutine()
    {
        bool isCrit;
        float elemMul;
        int damage = player.CalculateDamageAgainst(enemy, 1.2f, player.skillPower, out isCrit, out elemMul);

        StartCoroutine(LungeForward(playerPortraitRect, towardsCenter: true));

        int oldHp = enemy.currentHP;
        enemy.TakeDamage(damage);

        if (isCrit)
            StartCoroutine(CameraShake());
        SpawnImpact(onEnemy: true, color: GetElementColor(player.element));
        SpawnDamagePopup(onEnemy: true, amount: damage, isCrit: isCrit);
        StartCoroutine(Shake(enemyPortraitRect));
        player.PutSkillOnCooldown();

        // Apply themed status based on player's element
        string statusText = ApplyElementalStatusFromPlayerSkill();

        if (battleLogText != null)
        {
            string critText = BuildCritText(isCrit);
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"You use your skill for {damage} damage! {statusText}{critText}{elemText}";
        }
        UpdateUI();

        if (enemyHpDamageSlider != null)
        {
            enemyHpDamageSlider.maxValue = enemy.maxHP;
            enemyHpDamageSlider.value = oldHp;
        }

        float postHitDelay = 0.35f;
        yield return new WaitForSeconds(postHitDelay);

        if (enemyHpDamageSlider != null)
        {
            yield return StartCoroutine(
                AnimateHpBar(enemyHpDamageSlider, oldHp, enemy.currentHP, isEnemy: true)
            );
        }

        EndPlayerTurn(afterDealingDamage: true);
    }

    // ultimate: huge damage + DefenseUp on yourself
    public void OnUltimatePressed()
    {
        if (!CanPlayerAct()) return;

        if (pendingAbility != PendingAbility.Ultimate) {
            ShowAbilityCard(PendingAbility.Ultimate);
            return;
        }

        HideAbilityCard();

        if (!player.CanUseUltimate())
        {
            if (battleLogText != null)
                battleLogText.text = "Ultimate is on cooldown!";
            return;
        }

        StartCoroutine(PlayerUltimateRoutine());
    }

    private IEnumerator PlayerUltimateRoutine()
    {
        bool isCrit;
        float elemMul;
        int damage = player.CalculateDamageAgainst(enemy, 1.5f, player.ultimatePower, out isCrit, out elemMul);

        StartCoroutine(LungeForward(playerPortraitRect, towardsCenter: true));

        int oldHp = enemy.currentHP;
        enemy.TakeDamage(damage);

        if (isCrit)
            StartCoroutine(CameraShake());
        SpawnImpact(onEnemy: true, color: GetElementColor(player.element));
        SpawnDamagePopup(onEnemy: true, amount: damage, isCrit: isCrit);
        StartCoroutine(Shake(enemyPortraitRect));
        player.PutUltimateOnCooldown();

        // DefenseUp for 2 of YOUR turns
        player.ApplyStatus(StatusType.DefenseUp, 2);

        if (battleLogText != null)
        {
            string critText = BuildCritText(isCrit);
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"ULTIMATE! You deal {damage} damage and raise your DEFENSE!{critText}{elemText}";
        }
        UpdateUI();

        if (enemyHpDamageSlider != null)
        {
            enemyHpDamageSlider.maxValue = enemy.maxHP;
            enemyHpDamageSlider.value = oldHp;
        }

        float postHitDelay = 0.35f;
        yield return new WaitForSeconds(postHitDelay);

        if (enemyHpDamageSlider != null)
        {
            yield return StartCoroutine(
                AnimateHpBar(enemyHpDamageSlider, oldHp, enemy.currentHP, isEnemy: true)
            );
        }

        EndPlayerTurn(afterDealingDamage: true);
    }

    private string ApplyElementalStatusFromPlayerSkill()
    {
        switch (player.element)
        {
            case ElementType.Bass:
            case ElementType.Noise:
                enemy.ApplyStatus(StatusType.BleedEars, 3);
                return "You inflict BLEEDING EARS over time!";

            case ElementType.Harmony:
            case ElementType.Melody:
                enemy.ApplyStatus(StatusType.Sleep, 1);
                return "Your calm melody puts the enemy to SLEEP, skipping their next turn!";

            case ElementType.Percussion:
            case ElementType.Synth:
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

        StartCoroutine(EnemyActionRoutine());
    }

    private IEnumerator EnemyActionRoutine()
    {
        if (battleOver) yield break;

        // simple enemy: basic attack only
        bool isCrit;
        float elemMul;
        int damage = enemy.CalculateDamageAgainst(player, 1.0f, 0, out isCrit, out elemMul);
        StartCoroutine(LungeForward(enemyPortraitRect, towardsCenter: false));

        int oldHp = player.currentHP;
        player.TakeDamage(damage);
        if (isCrit)
            StartCoroutine(CameraShake());
        SpawnImpact(onEnemy: false, color: GetElementColor(enemy.element));
        SpawnDamagePopup(onEnemy: false, amount: damage, isCrit: isCrit);
        StartCoroutine(Shake(playerPortraitRect));

        if (battleLogText != null)
        {
            string critText = BuildCritText(isCrit);
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"Enemy hits you for {damage} damage.{critText}{elemText}";
        }
        UpdateUI();

        if (playerHpDamageSlider != null)
        {
            playerHpDamageSlider.maxValue = player.maxHP;
            playerHpDamageSlider.value = oldHp;
        }

        float postHitDelay = 0.35f;
        yield return new WaitForSeconds(postHitDelay);

        if (playerHpDamageSlider != null)
        {
            yield return StartCoroutine(
                AnimateHpBar(playerHpDamageSlider, oldHp, player.currentHP, isEnemy: false)
            );
        }

        if (player.IsDead())
        {
            if (battleLogText != null)
                battleLogText.text += "\nYou were defeated...";
            battleOver = true;
            UpdateAbilityButtons();
            PlayLoseSequence();
            yield break;
        }

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
            PlayWinSequence();
            return;
        }

        // hand over to enemy
        StartEnemyTurn();
    }

    private void UpdateUI()
    {
        if (player != null) {
            if (playerHpText != null) {
                playerHpText.text = $"{player.displayName} {player.currentHP}/{player.maxHP}";
            }
            if (playerHpSlider != null) {
                playerHpSlider.maxValue = player.maxHP;
                if (!isPlayerHpAnimating) {
                    playerHpSlider.value = player.currentHP;  
                }
            }
            if (playerHpDamageSlider != null) {
                playerHpDamageSlider.maxValue = player.maxHP;
                if (!isPlayerHpAnimating) {
                    playerHpDamageSlider.value = player.currentHP;
                }
            }
        }

        if (enemy != null) {
            if (enemyHpText != null) {
                enemyHpText.text = $"{enemy.displayName} {enemy.currentHP}/{enemy.maxHP}";
            }
            if (enemyHpSlider != null) {
                enemyHpSlider.maxValue = enemy.maxHP;
                if (!isEnemyHpAnimating) {
                    enemyHpSlider.value = enemy.currentHP;
                }
            }
            if (enemyHpDamageSlider != null) {
                enemyHpDamageSlider.maxValue = enemy.maxHP;
                if (!isEnemyHpAnimating) {
                    enemyHpDamageSlider.value = enemy.currentHP;
                }
            }
        }

        UpdateStatusIcons();
    }

    private void UpdateAbilityButtons()
    {
        bool canAct = playerTurn && !battleOver;

        if (basicAttackButton != null) {

            basicAttackButton.interactable = canAct;
            SetAbilityButtonLabel(basicAttackButton, "Strike", 0);
        }

        if (skillButton != null && player != null) {
            skillButton.interactable = canAct && player.CanUseSkill();
            SetAbilityButtonLabel(skillButton, "Skill", player.skillCooldownRemaining);
        }

        if (ultimateButton != null && player != null) {
            ultimateButton.interactable = canAct && player.CanUseUltimate();
            SetAbilityButtonLabel(ultimateButton, "Ultimate", player.ultimateCooldownRemaining);
        }            
    }

    private void SetAbilityButtonLabel(Button button, string baseName, int cooldownRemaining) {
        if (button == null) return;

        var label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label == null) return;

        if (cooldownRemaining > 0) {
            label.text = $"{baseName}\n({cooldownRemaining})";
        } else {
            label.text = baseName;
        }
    }

    private void UpdateStatusIcons() {
        if (playerStatusIcon != null && player != null) {
            playerStatusIcon.color = GetStatusColor(player.currentStatus);
        }

        if (enemyStatusIcon != null && enemy != null) {
            enemyStatusIcon.color = GetStatusColor(enemy.currentStatus);
        }
    }

    private Color GetStatusColor(StatusType status) {
        switch (status) {
            case StatusType.BleedEars:
                return bleedColor;
            case StatusType.Stun:
                return stunColor;
            case StatusType.Sleep:
                return sleepColor;
            case StatusType.DefenseUp:
                return defenseUpColor;
            case StatusType.None:
            default:
                return noStatusColor;
        }
    }

    private void SpawnDamagePopup(bool onEnemy, int amount, bool isCrit) {
        if (damagePopupPrefab == null) return;

        RectTransform anchor = onEnemy ? enemyPopupAnchor : playerPopupAnchor;
        if (anchor == null) return;

        var popup = Instantiate(damagePopupPrefab, anchor);
        var rect = popup.GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;

        popup.Init(amount, isCrit);
    }

    private IEnumerator LungeForward(RectTransform rect, bool towardsCenter) {
        if (rect == null) yield break;

        Vector2 start = rect.anchoredPosition;
        Vector2 dir = towardsCenter ? new Vector2(1f, 0f) : new Vector2(-1f, 0f);
        Vector2 end = start + dir * attackMoveDistance;
        
        float t = 0f;

        while (t < attackMoveDuration) {
            t += Time.deltaTime;
            float n = t / attackMoveDuration;
            rect.anchoredPosition = Vector2.Lerp(start, end, n);
            yield return null;
        }

        t = 0f;
        while (t < attackMoveDuration) {
            t += Time.deltaTime;
            float n = t / attackMoveDuration;
            rect.anchoredPosition = Vector2.Lerp(end, start, n);
            yield return null;
        }
    }

    private IEnumerator Shake(RectTransform rect) {
        if (rect == null) yield break;

        Vector2 start = rect.anchoredPosition;
        float t= 0f;

        while (t < hitShakeDuration) {
            t += Time.deltaTime;
            float n = t / hitShakeDuration;
            float strength = (1f - n) * hitShakeDistance;
            float offsetX = Random.Range(-strength, strength);
            float offsetY = Random.Range(-strength, strength);
            rect.anchoredPosition = start + new Vector2(offsetX, offsetY);
            yield return null;
        }

        rect.anchoredPosition = start;
    }


    private IEnumerator CameraShake()
    {
        if (battleRoot == null) yield break;

        float t = 0f;
        while (t < camShakeDuration)
        {
            t += Time.deltaTime;
            float n = t / camShakeDuration;
            float strength = (1f - n) * camShakeStrength * 80f; // *80 to convert to UI pixels

            float offsetX = Random.Range(-strength, strength);
            float offsetY = Random.Range(-strength, strength);
            battleRoot.anchoredPosition = baseRootPos + new Vector2(offsetX, offsetY);

            yield return null;
        }

        battleRoot.anchoredPosition = baseRootPos;
    }

    private void PlayWinSequence()
    {
        StartCoroutine(WinSequenceRoutine());
    }

    private IEnumerator WinSequenceRoutine()
    {
        float postHitDelay = 0.35f;
        yield return new WaitForSeconds(postHitDelay);
        
        yield return StartCoroutine(EnemyDeathPixelateRoutine());

        yield return new WaitForSeconds(enemyDeathHoldDelay);

        if (resultPanel != null && resultText != null)
        {
            resultPanel.SetActive(true);
            resultText.text = "Victory!";
            yield return StartCoroutine(FadeResultPanel(true));
        }
    }
    
    private IEnumerator EnemyDeathPixelateRoutine()
    {
        if (enemyPixelateMaterialTemplate == null || enemyPortraitImage == null)
            yield break;

        if (enemyPixelateMaterialRuntime == null)
            enemyPixelateMaterialRuntime = new Material(enemyPixelateMaterialTemplate);

        var img = enemyPortraitImage;
        var originalMat = img.material;

        img.material = enemyPixelateMaterialRuntime;
        enemyPixelateMaterialRuntime.SetFloat("_PixelAmount", 0f);

        float t = 0f;
        while (t < enemyDeathPixelDuration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / enemyDeathPixelDuration);
            enemyPixelateMaterialRuntime.SetFloat("_PixelAmount", n);
            yield return null;
        }

        enemyPixelateMaterialRuntime.SetFloat("_PixelAmount", 1f);
        img.enabled = false;
    }

    private void PlayLoseSequence()
    {
        if (resultPanel == null || resultText == null) return;

        resultPanel.SetActive(true);
        resultText.text = "Defeat...";
        StartCoroutine(FadeResultPanel(false));
    }


    private IEnumerator FadeResultPanel(bool isWin)
    {
        Image bg = resultPanel.GetComponent<Image>();
        Color bgColor = bg != null ? bg.color : new Color(0f, 0f, 0f, 0f);
        Color textColor = resultText.color;

        if (bg != null)
        {
            bgColor.a = 0f;
            bg.color = bgColor;
        }
        textColor.a = 0f;
        resultText.color = textColor;

        float t = 0f;
        while (t < resultFadeDuration)
        {
            t += Time.deltaTime;
            float n = t / resultFadeDuration;

            if (bg != null)
            {
                bgColor.a = Mathf.Lerp(0f, 0.85f, n);
                bg.color = bgColor;
            }

            textColor.a = Mathf.Lerp(0f, 1f, n);
            resultText.color = textColor;

            yield return null;
        }
    }

    private IEnumerator AnimateHpBar(Slider slider, int startValue, int targetValue, bool isEnemy)
    {
        if (slider == null || startValue == targetValue)
            yield break;

        if (isEnemy) isEnemyHpAnimating = true;
        else        isPlayerHpAnimating = true;

        float t = 0f;

        while (t < hpBarAnimDuration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / hpBarAnimDuration);

            float barValue = Mathf.Lerp(startValue, targetValue, n);
            slider.value = barValue;

            yield return null;
        }

        slider.value = targetValue;

        if (isEnemy) isEnemyHpAnimating = false;
        else        isPlayerHpAnimating = false;

        UpdateUI();
    }


    private Color GetElementColor(ElementType element)
    {
        switch (element)
        {
            case ElementType.Bass:
                return new Color(0.6f, 0.1f, 0.2f);   // deep red/purple
            case ElementType.Percussion:
                return new Color(0.9f, 0.6f, 0.1f);   // punchy orange
            case ElementType.Harmony:
                return new Color(0.2f, 0.8f, 0.5f);   // teal/green
            case ElementType.Noise:
                return new Color(0.8f, 0.2f, 0.8f);   // magenta
            case ElementType.Melody:
                return new Color(0.4f, 0.7f, 1f);     // light blue
            case ElementType.Synth:
                return new Color(0.2f, 1f, 1f);       // neon cyan
            case ElementType.None:
            default:
                return Color.clear;
        }
    }

    private void SpawnImpact(bool onEnemy, Color color) {
        if (impactEffectPrefab == null) return;

        RectTransform anchor = onEnemy ? enemyImpactAnchor : playerImpactAnchor;
        if (anchor == null) return;

        var fx = Instantiate(impactEffectPrefab, anchor);
        var rect = fx.GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;

        fx.Init(color);
    }

    private void ShowAbilityCard(PendingAbility ability)
    {
        if (abilityCardPanel == null || player == null) return;

        pendingAbility = ability;
        abilityCardPanel.SetActive(true);

        string name = "";
        string desc = "";
        int dmg = 0;
        int cd = 0;

        switch (ability)
        {
            case PendingAbility.Basic:
                name = "Strike";
                desc = "A basic attack that scales with your Attack stat.";
                dmg = player.attack;
                cd = 0;
                break;

            case PendingAbility.Skill:
                name = "Signature Skill";
                desc = DescribeSkillByElement(player.element);
                dmg = player.attack + player.skillPower;
                cd = player.skillCooldownTurns;
                break;

            case PendingAbility.Ultimate:
                name = "Ultimate";
                desc = "Massive attack that also grants Harmonic Shield (Defense Up) for 2 of your turns.";
                dmg = player.attack + player.ultimatePower;
                cd = player.ultimateCooldownTurns;
                break;
        }

        if (abilityCardName != null) abilityCardName.text = name;
        if (abilityCardStats != null) abilityCardStats.text = $"Dmg: {dmg}   CD: {cd}";
        if (abilityCardDescription != null) abilityCardDescription.text = desc;
    }

    public void HideAbilityCard()
    {
        if (abilityCardPanel != null)
            abilityCardPanel.SetActive(false);

        pendingAbility = PendingAbility.None;
    }

    private string DescribeSkillByElement(ElementType element)
    {
        switch (element)
        {
            case ElementType.Bass:
            case ElementType.Noise:
                return "Feedback Overload: a harsh attack that inflicts Feedback Overload (damage over time) for 3 turns.";
            case ElementType.Harmony:
            case ElementType.Melody:
                return "Lullaby: a soothing pattern that puts the enemy to sleep, skipping their next turn.";
            case ElementType.Percussion:
            case ElementType.Synth:
                return "Tempo Break: sharp strikes that stun the enemy and make them miss their next turn.";
            default:
                return "A special attack tied to your genre.";
        }
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