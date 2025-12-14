using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BattleController : MonoBehaviour
{
    [Header("Flow")]
    public bool autoStartBattle = false;
    private bool hasStarted = false;

    [Header("Party UI")]
    public Image[] partySlotImages;
    public Color partyEmptyColor = new Color(1f, 1f, 1f, 0.15f);
    public Color partyFilledColor = Color.white;

    [Header("Result / Return")]
    public UnityEngine.UI.Button backHomeButton;
    public float backHomeDelay = 1.25f;
    public string homeSceneName = "HomeScene";

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

    public Color bleedColor = Color.red;
    public Color stunColor = new Color(1f, 0.8f, 0f);
    public Color sleepColor = new Color(0.5f, 0.7f, 1f);
    public Color defenseUpColor = new Color(0.3f, 1f, 0.3f);
    public Color noStatusColor = new Color(1f, 1f, 1f, 0f);

    [Header("Characters")]
    public CharacterStats player;
    public CharacterStats enemy;

    [Header("Portraits")]
    public Image playerPortraitImage;
    public Image enemyPortraitImage;

    [Header("Party Member Displays (Arrays for 4 slots)")]
    public Image[] partyPortraitImages = new Image[4];
    public TextMeshProUGUI[] partyHpTexts = new TextMeshProUGUI[4];
    public Slider[] partyHpSliders = new Slider[4];
    public Slider[] partyHpDamageSliders = new Slider[4];
    public RectTransform[] partyPortraitRects = new RectTransform[4];
    public RectTransform[] partyImpactAnchors = new RectTransform[4];
    public RectTransform[] partyPopupAnchors = new RectTransform[4];
    public Image[] partyStatusIcons = new Image[4];

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

    private CharacterStats[] partyMembers = new CharacterStats[4];
    private int activePartyIndex = 0;

    private void Start()
    {
        Debug.Log("[BattleController] Start() called");
        
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera != null)
        {
            baseCamPos = mainCamera.transform.position;
            baseCamSize = mainCamera.orthographicSize;
        }

        var gm = GameManager.Instance;
        Debug.Log($"[BattleController] GameManager.Instance: {(gm != null ? "EXISTS" : "NULL")}");
        
        if (gm != null)
        {
            var enemyData = gm.GetCurrentEnemyData();
            if (enemyData != null)
            {
                enemy.InitFrom(enemyData);
                Debug.Log($"[BattleController] Enemy initialized: {enemyData.displayName}");
            }

            if (enemyPortraitImage != null)
            {
                if (enemyData != null && enemyData.silhouetteSprite != null)
                {
                    enemyPortraitImage.sprite = enemyData.silhouetteSprite;
                }
            }
        }

        EnsurePartySlotsActive();
        
        UpdateUI();

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (backHomeButton != null)
        {
            backHomeButton.gameObject.SetActive(false);
            backHomeButton.interactable = false;
            backHomeButton.onClick.RemoveAllListeners();
            backHomeButton.onClick.AddListener(() =>
                UnityEngine.SceneManagement.SceneManager.LoadScene(homeSceneName)
            );
        }

        if (battleRoot != null)
            baseRootPos = battleRoot.anchoredPosition;

        if (autoStartBattle)
            StartBattleNow();
    }

    public void OnClick_Battle()
    {
        Debug.Log("[BattleController] OnClick_Battle() called - This should not be called when using lineup system!");
        ResetBattleState();
        StartBattleNow();
    }

    public void ResetBattleState()
    {
        Debug.Log("[BattleController] ResetBattleState() called");
        hasStarted = false;
        battleOver = false;
        playerTurn = true;
        activePartyIndex = 0;
        
        for (int i = 0; i < 4; i++)
        {
            if (partyMembers[i] != null)
            {
                Destroy(partyMembers[i].gameObject);
                partyMembers[i] = null;
            }
        }
    }

    public void StartBattleNow()
    {
        Debug.Log("[BattleController] StartBattleNow() called");
        
        if (battleOver)
        {
            Debug.LogWarning("[BattleController] StartBattleNow: Battle already over!");
            return;
        }
        
        if (hasStarted)
        {
            Debug.LogWarning("[BattleController] StartBattleNow: Battle already started! Call ResetBattleState() first if you want to restart.");
            return;
        }
        
        hasStarted = true;

        var gm = GameManager.Instance;
        Debug.Log($"[BattleController] StartBattleNow: GameManager.Instance: {(gm != null ? "EXISTS" : "NULL")}");
        
        if (gm == null)
        {
            Debug.LogError("[BattleController] StartBattleNow: GameManager.Instance is NULL! Cannot start battle!");
            return;
        }

        var pd = gm.playerData;
        Debug.Log($"[BattleController] StartBattleNow: playerData: {(pd != null ? "EXISTS" : "NULL")}");
        
        if (pd == null)
        {
            Debug.LogError("[BattleController] StartBattleNow: playerData is NULL!");
            return;
        }

        if (pd.activeLineupIndices == null || pd.activeLineupIndices.Length != 4)
        {
            Debug.LogError($"[BattleController] StartBattleNow: activeLineupIndices is invalid! Length: {(pd.activeLineupIndices != null ? pd.activeLineupIndices.Length.ToString() : "null")}");
            return;
        }

        Debug.Log($"[BattleController] StartBattleNow: activeLineupIndices = [{pd.activeLineupIndices[0]}, {pd.activeLineupIndices[1]}, {pd.activeLineupIndices[2]}, {pd.activeLineupIndices[3]}]");
        Debug.Log($"[BattleController] StartBattleNow: ownedCharacters.Count = {pd.ownedCharacters.Count}");

        InitializePartyMembers(gm);

        activePartyIndex = 0;
        if (partyMembers[activePartyIndex] != null)
        {
            player = partyMembers[activePartyIndex];
            Debug.Log($"[BattleController] StartBattleNow: Active party member set to index {activePartyIndex}: {player.displayName}");
        }
        else
        {
            Debug.LogError($"[BattleController] StartBattleNow: Party member at index {activePartyIndex} is NULL!");
        }

        var enemyData = gm.GetCurrentEnemyData();
        if (enemyData != null)
        {
            enemy.InitFrom(enemyData);
            Debug.Log($"[BattleController] StartBattleNow: Enemy initialized: {enemyData.displayName}");
        }

        if (playerPortraitImage != null && player != null)
        {
            var activeInstance = gm.GetActiveCharacterInstance();
            if (activeInstance != null && activeInstance.data != null && activeInstance.data.silhouetteSprite != null)
            {
                playerPortraitImage.sprite = activeInstance.data.silhouetteSprite;
                Debug.Log($"[BattleController] StartBattleNow: Legacy player portrait sprite set");
            }
        }

        if (enemyPortraitImage != null && enemyData != null && enemyData.silhouetteSprite != null)
        {
            enemyPortraitImage.sprite = enemyData.silhouetteSprite;
        }

        EnsurePartySlotsActive();
        FillPartyUI();
        InitializePartyMemberDisplays();

        UpdateUI();

        if (battleLogText != null)
            battleLogText.text = "Battle start...";

        if (player != null && enemy != null)
        {
            if (player.speed >= enemy.speed)
            {
                if (battleLogText != null)
                    battleLogText.text = "Battle start! You act first.";
                StartPlayerTurn();
            }
            else
            {
                if (battleLogText != null)
                    battleLogText.text = "Battle start! Enemy acts first.";
                StartEnemyTurn();
            }
        }
        else
        {
            Debug.LogError("[BattleController] StartBattleNow: player or enemy is null, cannot start battle!");
        }
    }

    private void StartPlayerTurn()
    {
        if (battleOver) return;

        if (player == null)
        {
            Debug.LogError("[BattleController] StartPlayerTurn: player is NULL!");
            return;
        }

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
            int newHp = enemy.currentHP;
            int oldHp = Mathf.Clamp(newHp + statusDamage, 0, enemy.maxHP);

            if (battleLogText != null)
            {
                battleLogText.text += $"\n{enemy.displayName} suffers {statusDamage} damage from {enemy.currentStatus}.";
            }

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

        StartCoroutine(LungeForward(GetActivePartyPortraitRect(), towardsCenter: true));

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

        StartCoroutine(LungeForward(GetActivePartyPortraitRect(), towardsCenter: true));

        int oldHp = enemy.currentHP;
        enemy.TakeDamage(damage);

        if (isCrit)
            StartCoroutine(CameraShake());
        SpawnImpact(onEnemy: true, color: GetElementColor(player.element));
        SpawnDamagePopup(onEnemy: true, amount: damage, isCrit: isCrit);
        StartCoroutine(Shake(enemyPortraitRect));
        player.PutSkillOnCooldown();

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

        StartCoroutine(LungeForward(GetActivePartyPortraitRect(), towardsCenter: true));

        int oldHp = enemy.currentHP;
        enemy.TakeDamage(damage);

        if (isCrit)
            StartCoroutine(CameraShake());
        SpawnImpact(onEnemy: true, color: GetElementColor(player.element));
        SpawnDamagePopup(onEnemy: true, amount: damage, isCrit: isCrit);
        StartCoroutine(Shake(enemyPortraitRect));
        player.PutUltimateOnCooldown();

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

    private void EnemyAction()
    {
        if (battleOver) return;
        StartCoroutine(EnemyActionRoutine());
    }

    private IEnumerator EnemyActionRoutine()
    {
        if (battleOver) yield break;

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
        StartCoroutine(Shake(GetActivePartyPortraitRect()));

        if (battleLogText != null)
        {
            string critText = BuildCritText(isCrit);
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"Enemy hits {player.displayName} for {damage} damage.{critText}{elemText}";
        }
        UpdateUI();

        Slider activeMemberDamageSlider = GetActivePartyDamageSlider();
        if (activeMemberDamageSlider != null)
        {
            activeMemberDamageSlider.maxValue = player.maxHP;
            activeMemberDamageSlider.value = oldHp;
        }

        float postHitDelay = 0.35f;
        yield return new WaitForSeconds(postHitDelay);

        if (activeMemberDamageSlider != null)
        {
            yield return StartCoroutine(
                AnimatePartyMemberHpBar(activePartyIndex, oldHp, player.currentHP)
            );
        }

        if (player.IsDead())
        {
            if (battleLogText != null)
                battleLogText.text += $"\n{player.displayName} was defeated!";
            
            if (!SwitchToNextPartyMember())
            {
                if (battleLogText != null)
                    battleLogText.text += "\nYour entire party was defeated...";
                battleOver = true;
                UpdateAbilityButtons();
                PlayLoseSequence();
                yield break;
            }
        }

        StartPlayerTurn();
    }

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

        UpdatePartyMemberUI();
        UpdateStatusIcons();
    }

    private void UpdatePartyMemberUI()
    {
        for (int i = 0; i < 4; i++)
        {
            if (partyMembers[i] == null) continue;

            if (i < partyHpTexts.Length && partyHpTexts[i] != null)
            {
                partyHpTexts[i].text = $"{partyMembers[i].displayName} {partyMembers[i].currentHP}/{partyMembers[i].maxHP}";
            }

            if (i < partyHpSliders.Length && partyHpSliders[i] != null)
            {
                partyHpSliders[i].maxValue = partyMembers[i].maxHP;
                partyHpSliders[i].value = partyMembers[i].currentHP;
            }

            if (i < partyHpDamageSliders.Length && partyHpDamageSliders[i] != null)
            {
                partyHpDamageSliders[i].maxValue = partyMembers[i].maxHP;
                partyHpDamageSliders[i].value = partyMembers[i].currentHP;
            }

            if (i < partyStatusIcons.Length && partyStatusIcons[i] != null)
            {
                partyStatusIcons[i].color = GetStatusColor(partyMembers[i].currentStatus);
            }
        }
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

        RectTransform anchor = onEnemy ? enemyPopupAnchor : GetActivePartyPopupAnchor();
        if (anchor == null) return;

        var popup = Instantiate(damagePopupPrefab, anchor);
        var rect = popup.GetComponent<RectTransform>();
        if (rect != null)
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
            float strength = (1f - n) * camShakeStrength * 80f;

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
            StartCoroutine(EnableBackHomeAfterDelay());
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
        StartCoroutine(EnableBackHomeAfterDelay());
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
                return new Color(0.6f, 0.1f, 0.2f);
            case ElementType.Percussion:
                return new Color(0.9f, 0.6f, 0.1f);
            case ElementType.Harmony:
                return new Color(0.2f, 0.8f, 0.5f);
            case ElementType.Noise:
                return new Color(0.8f, 0.2f, 0.8f);
            case ElementType.Melody:
                return new Color(0.4f, 0.7f, 1f);
            case ElementType.Synth:
                return new Color(0.2f, 1f, 1f);
            case ElementType.None:
            default:
                return Color.clear;
        }
    }

    private void SpawnImpact(bool onEnemy, Color color) {
        if (impactEffectPrefab == null) return;

        RectTransform anchor = onEnemy ? enemyImpactAnchor : GetActivePartyImpactAnchor();
        if (anchor == null) return;

        var fx = Instantiate(impactEffectPrefab, anchor);
        var rect = fx.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = Vector2.zero;
        
        fx.gameObject.SetActive(true);
        fx.Init(color);
    }

    private RectTransform GetActivePartyImpactAnchor()
    {
        if (activePartyIndex >= 0 && activePartyIndex < partyImpactAnchors.Length && partyImpactAnchors[activePartyIndex] != null)
            return partyImpactAnchors[activePartyIndex];
        return playerImpactAnchor;
    }

    private RectTransform GetActivePartyPopupAnchor()
    {
        if (activePartyIndex >= 0 && activePartyIndex < partyPopupAnchors.Length && partyPopupAnchors[activePartyIndex] != null)
            return partyPopupAnchors[activePartyIndex];
        return playerPopupAnchor;
    }

    private RectTransform GetActivePartyPortraitRect()
    {
        if (activePartyIndex >= 0 && activePartyIndex < partyPortraitRects.Length && partyPortraitRects[activePartyIndex] != null)
            return partyPortraitRects[activePartyIndex];
        return playerPortraitRect;
    }

    private Slider GetActivePartyDamageSlider()
    {
        if (activePartyIndex >= 0 && activePartyIndex < partyHpDamageSliders.Length && partyHpDamageSliders[activePartyIndex] != null)
            return partyHpDamageSliders[activePartyIndex];
        return playerHpDamageSlider;
    }

    private bool SwitchToNextPartyMember()
    {
        for (int i = activePartyIndex + 1; i < 4; i++)
        {
            if (partyMembers[i] != null && !partyMembers[i].IsDead())
            {
                activePartyIndex = i;
                player = partyMembers[i];
                
                if (playerPortraitImage != null)
                {
                    var gm = GameManager.Instance;
                    if (gm != null)
                    {
                        var pd = gm.playerData;
                        if (pd.activeLineupIndices != null && i < pd.activeLineupIndices.Length)
                        {
                            int idx = pd.activeLineupIndices[i];
                            if (idx >= 0 && idx < pd.ownedCharacters.Count)
                            {
                                var inst = pd.ownedCharacters[idx];
                                if (inst.data != null && inst.data.silhouetteSprite != null)
                                    playerPortraitImage.sprite = inst.data.silhouetteSprite;
                            }
                        }
                    }
                }
                
                UpdateUI();
                return true;
            }
        }

        for (int i = 0; i < activePartyIndex; i++)
        {
            if (partyMembers[i] != null && !partyMembers[i].IsDead())
            {
                activePartyIndex = i;
                player = partyMembers[i];
                
                if (playerPortraitImage != null)
                {
                    var gm = GameManager.Instance;
                    if (gm != null)
                    {
                        var pd = gm.playerData;
                        if (pd.activeLineupIndices != null && i < pd.activeLineupIndices.Length)
                        {
                            int idx = pd.activeLineupIndices[i];
                            if (idx >= 0 && idx < pd.ownedCharacters.Count)
                            {
                                var inst = pd.ownedCharacters[idx];
                                if (inst.data != null && inst.data.silhouetteSprite != null)
                                    playerPortraitImage.sprite = inst.data.silhouetteSprite;
                            }
                        }
                    }
                }
                
                UpdateUI();
                return true;
            }
        }

        return false;
    }

    private IEnumerator AnimatePartyMemberHpBar(int memberIndex, int startValue, int targetValue)
    {
        if (memberIndex < 0 || memberIndex >= 4 || partyMembers[memberIndex] == null)
            yield break;

        Slider slider = GetActivePartyDamageSlider();
        if (slider == null || startValue == targetValue)
            yield break;

        isPlayerHpAnimating = true;

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
        isPlayerHpAnimating = false;

        UpdateUI();
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

    private void InitializePartyMembers(GameManager gm)
    {
        Debug.Log("[BattleController] InitializePartyMembers() called");
        
        for (int i = 0; i < 4; i++)
        {
            if (partyMembers[i] != null)
            {
                Destroy(partyMembers[i].gameObject);
                partyMembers[i] = null;
            }
        }

        var pd = gm.playerData;
        if (pd.activeLineupIndices == null || pd.activeLineupIndices.Length != 4)
        {
            Debug.LogError("[BattleController] InitializePartyMembers: activeLineupIndices is invalid!");
            return;
        }

        Debug.Log($"[BattleController] InitializePartyMembers: Processing lineup indices: [{pd.activeLineupIndices[0]}, {pd.activeLineupIndices[1]}, {pd.activeLineupIndices[2]}, {pd.activeLineupIndices[3]}]");
        Debug.Log($"[BattleController] InitializePartyMembers: ownedCharacters.Count = {pd.ownedCharacters.Count}");

        for (int i = 0; i < 4; i++)
        {
            int idx = pd.activeLineupIndices[i];
            Debug.Log($"[BattleController] InitializePartyMembers: Slot {i}: character index = {idx}");
            
            if (idx >= 0 && idx < pd.ownedCharacters.Count)
            {
                var inst = pd.ownedCharacters[idx];
                Debug.Log($"[BattleController] InitializePartyMembers: Slot {i}: Found character instance: {(inst != null ? inst.data.displayName : "NULL")}");
                
                if (inst != null)
                {
                    GameObject statObj = new GameObject($"PartyMember_{i}_Stats");
                    statObj.transform.SetParent(this.transform);
                    var stats = statObj.AddComponent<CharacterStats>();
                    stats.InitFrom(inst);
                    partyMembers[i] = stats;
                    Debug.Log($"[BattleController] InitializePartyMembers: Slot {i}: Created CharacterStats for {stats.displayName}");
                }
                else
                {
                    Debug.LogError($"[BattleController] InitializePartyMembers: Slot {i}: Character instance is NULL!");
                    partyMembers[i] = null;
                }
            }
            else
            {
                Debug.Log($"[BattleController] InitializePartyMembers: Slot {i}: Empty slot (idx={idx})");
                partyMembers[i] = null;
            }
        }

        Debug.Log($"[BattleController] InitializePartyMembers: Complete. Party members: [{GetPartyMemberSummary(0)}, {GetPartyMemberSummary(1)}, {GetPartyMemberSummary(2)}, {GetPartyMemberSummary(3)}]");
    }

    private string GetPartyMemberSummary(int index)
    {
        if (partyMembers[index] == null) return "NULL";
        return partyMembers[index].displayName;
    }

    private void InitializePartyMemberDisplays()
    {
        Debug.Log("[BattleController] InitializePartyMemberDisplays() called");
        
        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[BattleController] InitializePartyMemberDisplays: GameManager.Instance is NULL!");
            return;
        }

        var pd = gm.playerData;
        if (pd.activeLineupIndices == null || pd.activeLineupIndices.Length != 4)
        {
            Debug.LogError("[BattleController] InitializePartyMemberDisplays: activeLineupIndices is invalid!");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            int idx = pd.activeLineupIndices[i];
            
            Debug.Log($"[BattleController] InitializePartyMemberDisplays: Slot {i}: idx={idx}, partyMember={(partyMembers[i] != null ? partyMembers[i].displayName : "NULL")}");
            
            if (i < partyPortraitImages.Length && partyPortraitImages[i] != null)
            {
                if (idx >= 0 && idx < pd.ownedCharacters.Count && partyMembers[i] != null)
                {
                    var inst = pd.ownedCharacters[idx];
                    if (inst.data != null && inst.data.silhouetteSprite != null)
                    {
                        partyPortraitImages[i].sprite = inst.data.silhouetteSprite;
                        partyPortraitImages[i].gameObject.SetActive(true);
                        Debug.Log($"[BattleController] InitializePartyMemberDisplays: Slot {i}: Portrait sprite set for {inst.data.displayName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[BattleController] InitializePartyMemberDisplays: Slot {i}: Character data or sprite is NULL!");
                        partyPortraitImages[i].gameObject.SetActive(false);
                    }
                }
                else
                {
                    Debug.Log($"[BattleController] InitializePartyMemberDisplays: Slot {i}: Empty slot, hiding portrait");
                    partyPortraitImages[i].gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.LogWarning($"[BattleController] InitializePartyMemberDisplays: Slot {i}: partyPortraitImages[{i}] is NULL!");
            }

            if (i < partyHpTexts.Length && partyHpTexts[i] != null)
            {
                if (partyMembers[i] != null)
                {
                    partyHpTexts[i].text = $"{partyMembers[i].displayName} {partyMembers[i].currentHP}/{partyMembers[i].maxHP}";
                    partyHpTexts[i].gameObject.SetActive(true);
                }
                else
                {
                    partyHpTexts[i].gameObject.SetActive(false);
                }
            }

            if (i < partyHpSliders.Length && partyHpSliders[i] != null)
            {
                if (partyMembers[i] != null)
                {
                    partyHpSliders[i].maxValue = partyMembers[i].maxHP;
                    partyHpSliders[i].value = partyMembers[i].currentHP;
                    partyHpSliders[i].gameObject.SetActive(true);
                }
                else
                {
                    partyHpSliders[i].gameObject.SetActive(false);
                }
            }

            if (i < partyHpDamageSliders.Length && partyHpDamageSliders[i] != null)
            {
                if (partyMembers[i] != null)
                {
                    partyHpDamageSliders[i].maxValue = partyMembers[i].maxHP;
                    partyHpDamageSliders[i].value = partyMembers[i].currentHP;
                    partyHpDamageSliders[i].gameObject.SetActive(true);
                }
                else
                {
                    partyHpDamageSliders[i].gameObject.SetActive(false);
                }
            }

            if (i < partyStatusIcons.Length && partyStatusIcons[i] != null)
            {
                if (partyMembers[i] != null)
                {
                    partyStatusIcons[i].color = GetStatusColor(partyMembers[i].currentStatus);
                    partyStatusIcons[i].gameObject.SetActive(true);
                }
                else
                {
                    partyStatusIcons[i].gameObject.SetActive(false);
                }
            }
        }

        UpdatePartyMemberUI();
    }

    private void EnsurePartySlotsActive()
    {
        Debug.Log("[BattleController] EnsurePartySlotsActive() called");
        
        if (partySlotImages == null || partySlotImages.Length < 4)
        {
            Debug.LogError($"[BattleController] EnsurePartySlotsActive: partySlotImages array is invalid! Null: {partySlotImages == null}, Length: {(partySlotImages != null ? partySlotImages.Length.ToString() : "N/A")}");
            return;
        }

        for (int i = 0; i < 4 && i < partySlotImages.Length; i++)
        {
            if (partySlotImages[i] != null && partySlotImages[i].gameObject != null)
            {
                partySlotImages[i].gameObject.SetActive(true);
                partySlotImages[i].enabled = true;
                Debug.Log($"[BattleController] EnsurePartySlotsActive: Slot {i} activated and enabled");
            }
            else
            {
                Debug.LogError($"[BattleController] EnsurePartySlotsActive: Slot {i} Image or GameObject is NULL!");
            }
        }
    }

    private void FillPartyUI()
    {
        Debug.Log("[BattleController] FillPartyUI() called");
        
        if (partySlotImages == null || partySlotImages.Length < 4)
        {
            Debug.LogError($"[BattleController] FillPartyUI: partySlotImages array is invalid! Null: {partySlotImages == null}, Length: {(partySlotImages != null ? partySlotImages.Length.ToString() : "N/A")}");
            return;
        }

        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[BattleController] FillPartyUI: GameManager.Instance is NULL!");
            return;
        }

        var pd = gm.playerData;
        if (pd == null)
        {
            Debug.LogError("[BattleController] FillPartyUI: playerData is NULL!");
            return;
        }

        if (pd.activeLineupIndices == null || pd.activeLineupIndices.Length != 4)
        {
            Debug.LogError($"[BattleController] FillPartyUI: activeLineupIndices is invalid! Null: {pd.activeLineupIndices == null}, Length: {(pd.activeLineupIndices != null ? pd.activeLineupIndices.Length.ToString() : "N/A")}");
            return;
        }

        Debug.Log($"[BattleController] FillPartyUI: activeLineupIndices = [{pd.activeLineupIndices[0]}, {pd.activeLineupIndices[1]}, {pd.activeLineupIndices[2]}, {pd.activeLineupIndices[3]}]");
        Debug.Log($"[BattleController] FillPartyUI: ownedCharacters.Count = {pd.ownedCharacters.Count}");

        EnsurePartySlotsActive();

        for (int i = 0; i < 4; i++)
        {
            int idx = pd.activeLineupIndices[i];
            var img = partySlotImages[i];

            Debug.Log($"[BattleController] FillPartyUI: Processing slot {i}: idx={idx}, img={(img != null ? "EXISTS" : "NULL")}");

            if (img == null)
            {
                Debug.LogError($"[BattleController] FillPartyUI: Slot {i}: partySlotImages[{i}] is NULL! Assign it in Inspector!");
                continue;
            }

            if (img.gameObject != null && !img.gameObject.activeSelf)
            {
                img.gameObject.SetActive(true);
                Debug.Log($"[BattleController] FillPartyUI: Slot {i}: Activated GameObject");
            }

            img.enabled = true;

            if (idx >= 0 && idx < pd.ownedCharacters.Count && pd.ownedCharacters != null)
            {
                var inst = pd.ownedCharacters[idx];
                Debug.Log($"[BattleController] FillPartyUI: Slot {i}: Found character instance at index {idx}: {(inst != null ? inst.data.displayName : "NULL")}");
                
                if (inst != null && inst.data != null)
                {
                    if (inst.data.silhouetteSprite != null)
                    {
                        img.sprite = inst.data.silhouetteSprite;
                        img.color = partyFilledColor;
                        Debug.Log($"[BattleController] FillPartyUI: Slot {i}: Sprite set for {inst.data.displayName}, color = {partyFilledColor}");
                    }
                    else
                    {
                        Debug.LogWarning($"[BattleController] FillPartyUI: Slot {i}: Character {inst.data.displayName} has no silhouetteSprite!");
                        img.sprite = null;
                        img.color = partyEmptyColor;
                    }
                }
                else
                {
                    Debug.LogError($"[BattleController] FillPartyUI: Slot {i}: Character instance or data is NULL at index {idx}!");
                    img.sprite = null;
                    img.color = partyEmptyColor;
                }
            }
            else
            {
                Debug.Log($"[BattleController] FillPartyUI: Slot {i}: Empty slot (idx={idx}), setting empty color");
                img.sprite = null;
                img.color = partyEmptyColor;
            }

            Debug.Log($"[BattleController] FillPartyUI: Slot {i}: Final sprite={(img.sprite != null ? img.sprite.name : "NULL")}, color={img.color}, enabled={img.enabled}, gameObject.activeSelf={img.gameObject.activeSelf}");
        }
        
        Debug.Log("[BattleController] FillPartyUI() complete");
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

    private IEnumerator EnableBackHomeAfterDelay()
    {
        yield return new WaitForSeconds(backHomeDelay);

        if (backHomeButton != null)
        {
            backHomeButton.gameObject.SetActive(true);
            backHomeButton.interactable = true;
        }
    }
}
