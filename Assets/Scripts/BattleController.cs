using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

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
    public TextMeshProUGUI rewardsText;
    public TextMeshProUGUI floorText;
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
    public TextMeshProUGUI turnOrderText;   // shows current + next actors

    public Slider playerHpSlider;
    public Slider enemyHpSlider;

    [Header("HP Bar Damage Overlay")]
    public Slider playerHpDamageSlider;
    public Slider enemyHpDamageSlider;

    [Header("Ability Buttons")]
    public Button basicAttackButton;
    public Button skillButton;
    public Button ultimateButton;

    private bool battleOver = false;

    private CharacterStats[] partyMembers = new CharacterStats[4];
    
    private List<CharacterStats> turnOrder = new List<CharacterStats>();
    private int currentTurnIndex = 0;
    private CharacterStats currentActor = null;

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
            if (floorText != null)
            {
                floorText.text = $"Floor {gm.playerData.towerCurrentFloor + 1}";
            }

            var enemyData = gm.GetCurrentEnemyData();
            if (enemyPortraitImage != null &&
                enemyData != null &&
                enemyData.silhouetteSprite != null)
            {
                enemyPortraitImage.sprite = enemyData.silhouetteSprite;
            }
        }

        EnsurePartySlotsActive();
        
        UpdateUI();
        UpdateTurnOrderUI();

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

    public void ResetBattleState()
    {
        Debug.Log("[BattleController] ResetBattleState() called");
        hasStarted = false;
        battleOver = false;
        currentTurnIndex = 0;
        currentActor = null;
        turnOrder.Clear();
        
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
        
        var enemyData = gm.GetCurrentEnemyData();
        if (enemyData != null)
        {
            enemy.InitFrom(enemyData);

            int floorNumber = gm.playerData != null ? gm.playerData.towerCurrentFloor + 1 : 1;
            var floorCfg = gm.GetCurrentTowerFloor();
            bool isBoss = floorCfg != null && floorCfg.isBossFloor;
            ScaleEnemyForFloor(floorNumber, isBoss);

            Debug.Log($"[BattleController] StartBattleNow: Enemy initialized: {enemyData.displayName} for Floor {floorNumber} (Boss: {isBoss})");
        }

        if (enemyPortraitImage != null && enemyData != null && enemyData.silhouetteSprite != null)
        {
            enemyPortraitImage.sprite = enemyData.silhouetteSprite;
        }

        EnsurePartySlotsActive();
        FillPartyUI();
        InitializePartyMemberDisplays();

        BuildTurnOrder();
        
        if (turnOrder.Count == 0)
        {
            Debug.LogError("[BattleController] StartBattleNow: No characters in turn order! Cannot start battle!");
            return;
        }

        currentTurnIndex = 0;
        currentActor = turnOrder[0];
        
        if (AudioManager.Instance != null)
        {
            if (enemyData != null && enemyData.isBoss && enemyData.bossIntroClip != null)
            {
                AudioManager.Instance.PlayClip(enemyData.bossIntroClip);
            }
            else
            {
                AudioManager.Instance.Play("battle_start");
            }
        }

        UpdateUI();

        if (battleLogText != null)
        {
            battleLogText.text = $"Battle start! Turn order: {string.Join(", ", turnOrder.ConvertAll(c => c.displayName))}";
        }

        ProcessNextTurn();
    }

    private void ProcessNextTurn()
    {
        if (battleOver) return;
        
        RemoveDeadCharactersFromTurnOrder();
        
        if (CheckBattleEndConditions())
            return;
        
        if (turnOrder.Count == 0)
        {
            Debug.LogError("[BattleController] ProcessNextTurn: Turn order is empty!");
            return;
        }
        
        if (currentTurnIndex >= turnOrder.Count)
        {
            currentTurnIndex = 0;
        }
        
        currentActor = turnOrder[currentTurnIndex];

        UpdateTurnOrderUI();
        
        if (currentActor == null || currentActor.IsDead())
        {
            AdvanceTurn();
            return;
        }
        
        Debug.Log($"[BattleController] ProcessNextTurn: {currentActor.displayName}'s turn (speed: {currentActor.speed})");
        
        if (IsPlayerControlled(currentActor))
        {
            StartPlayerControlledTurn(currentActor);
        }
        else
        {
            StartEnemyTurn(currentActor);
        }
    }

    private void AdvanceTurn()
    {
        if (battleOver) return;
        
        RemoveDeadCharactersFromTurnOrder();
        
        if (CheckBattleEndConditions())
            return;
        
        if (turnOrder.Count == 0)
        {
            Debug.LogError("[BattleController] AdvanceTurn: Turn order is empty!");
            return;
        }
        
        currentTurnIndex++;
        
        if (currentTurnIndex >= turnOrder.Count)
        {
            currentTurnIndex = 0;
            Debug.Log("[BattleController] AdvanceTurn: New round starting");
        }
        
        ProcessNextTurn();
    }

    private void UpdateTurnOrderUI()
    {
        if (turnOrderText == null || turnOrder == null || turnOrder.Count == 0)
            return;

        // Show the next few actors in order, starting from currentTurnIndex.
        const int maxShown = 4;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < maxShown && i < turnOrder.Count; i++)
        {
            int idx = (currentTurnIndex + i) % turnOrder.Count;
            var actor = turnOrder[idx];
            if (actor == null) continue;

            if (i == 0)
                sb.Append($"Now: {actor.displayName}");
            else if (i == 1)
                sb.Append($"\nNext: {actor.displayName}");
            else
                sb.Append($"\nLater: {actor.displayName}");
        }

        turnOrderText.text = sb.ToString();
    }

    private void StartPlayerControlledTurn(CharacterStats actor)
    {
        if (battleOver || actor == null) return;

        currentActor = actor;
        
        actor.TickCooldowns();
        int statusDamage;
        bool skipTurn = actor.TickStatusAtTurnStart(out statusDamage);

        if (statusDamage > 0 && battleLogText != null)
        {
            battleLogText.text += $"\n{actor.displayName} suffers {statusDamage} damage from {actor.currentStatus}.";
        }

        UpdateUI();

        if (actor.IsDead())
        {
            if (battleLogText != null)
                battleLogText.text += $"\n{actor.displayName} was defeated by status...";
            RemoveDeadCharactersFromTurnOrder();
            if (CheckBattleEndConditions())
                return;
            AdvanceTurn();
            return;
        }

        if (skipTurn)
        {
            if (battleLogText != null)
                battleLogText.text += $"\n{actor.displayName} is unable to act!";
            AdvanceTurn();
            return;
        }

        UpdateAbilityButtons();

        if (battleLogText != null && !battleOver)
        {
            battleLogText.text += $"\n{actor.displayName}'s turn.";
        }
    }

    private void StartEnemyTurn(CharacterStats enemyActor)
    {
        if (battleOver || enemyActor == null) return;
        StartCoroutine(EnemyTurnSequence(enemyActor));
    }

    private IEnumerator EnemyTurnSequence(CharacterStats enemyActor)
    {
        if (battleOver || enemyActor == null) yield break;

        UpdateAbilityButtons();

        enemyActor.TickCooldowns();
        int statusDamage;
        bool skipTurn = enemyActor.TickStatusAtTurnStart(out statusDamage);

        bool statusDidDamage = statusDamage > 0;

        if (statusDidDamage)
        {
            int newHp = enemyActor.currentHP;
            int oldHp = Mathf.Clamp(newHp + statusDamage, 0, enemyActor.maxHP);

            if (battleLogText != null)
            {
                battleLogText.text += $"\n{enemyActor.displayName} suffers {statusDamage} damage from {enemyActor.currentStatus}.";
            }

            if (enemyHpDamageSlider != null)
            {
                enemyHpDamageSlider.maxValue = enemyActor.maxHP;
                enemyHpDamageSlider.value = oldHp;
            }

            if (enemyHpText != null)
            {
                enemyHpText.text = $"{enemyActor.displayName} {oldHp}/{enemyActor.maxHP}";
            }
            if (enemyHpSlider != null && !isEnemyHpAnimating)
            {
                enemyHpSlider.maxValue = enemyActor.maxHP;
                enemyHpSlider.value = oldHp;
            }

            float preHitDelay = 0.5f;
            yield return new WaitForSeconds(preHitDelay);
            StartCoroutine(Shake(enemyPortraitRect));

            SpawnImpact(onEnemy: true, color: GetStatusColor(enemyActor.currentStatus));
            SpawnDamagePopup(onEnemy: true, amount: statusDamage, isCrit: false);

            if (enemyHpText != null)
            {
                enemyHpText.text = $"{enemyActor.displayName} {newHp}/{enemyActor.maxHP}";
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

        if (enemyActor.IsDead())
        {
            RemoveDeadCharactersFromTurnOrder();
            if (CheckBattleEndConditions())
                yield break;
            AdvanceTurn();
            yield break;
        }

        if (skipTurn)
        {
            if (battleLogText != null)
                battleLogText.text += $"\n{enemyActor.displayName} is unable to act!";
            AdvanceTurn();
            yield break;
        }

        float preActionDelay = 0.8f;
        yield return new WaitForSeconds(preActionDelay);

        EnemyAction(enemyActor);
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
        if (currentActor == null || enemy == null) yield break;

        if (AudioManager.Instance != null) AudioManager.Instance.Play("basic");

        bool isCrit;
        float elemMul;
        int damage = currentActor.CalculateDamageAgainst(enemy, 1.0f, 0, out isCrit, out elemMul);

        StartCoroutine(LungeForward(GetCurrentActorPortraitRect(), towardsCenter: true));

        int oldHp = enemy.currentHP;
        enemy.TakeDamage(damage);
        int newHp = enemy.currentHP;

        if (isCrit)
            StartCoroutine(CameraShake());
        SpawnImpact(onEnemy: true, color: GetElementColor(currentActor.element));
        SpawnDamagePopup(onEnemy: true, amount: damage, isCrit: isCrit);
        StartCoroutine(Shake(enemyPortraitRect));

        if (battleLogText != null)
        {
            string critText = BuildCritText(isCrit);
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"{currentActor.displayName} strikes the enemy for {damage} damage.{critText}{elemText}";
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

        if (enemy.IsDead())
        {
            RemoveDeadCharactersFromTurnOrder();
            if (CheckBattleEndConditions())
                yield break;
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

        if (currentActor == null || !currentActor.CanUseSkill())
        {
            if (battleLogText != null)
                battleLogText.text = "Skill is on cooldown!";
            return;
        }

        StartCoroutine(PlayerSkillRoutine());
    }

    private IEnumerator PlayerSkillRoutine()
    {
        if (currentActor == null || enemy == null) yield break;

        if (AudioManager.Instance != null) AudioManager.Instance.Play("skill");

        bool isCrit;
        float elemMul;
        int damage = currentActor.CalculateDamageAgainst(enemy, 1.2f, currentActor.skillPower, out isCrit, out elemMul);

        StartCoroutine(LungeForward(GetCurrentActorPortraitRect(), towardsCenter: true));

        int oldHp = enemy.currentHP;
        enemy.TakeDamage(damage);

        if (isCrit)
            StartCoroutine(CameraShake());
        SpawnImpact(onEnemy: true, color: GetElementColor(currentActor.element));
        SpawnDamagePopup(onEnemy: true, amount: damage, isCrit: isCrit);
        StartCoroutine(Shake(enemyPortraitRect));
        currentActor.PutSkillOnCooldown();

        string statusText = ApplyElementalStatusFromPlayerSkill();

        if (battleLogText != null)
        {
            string critText = BuildCritText(isCrit);
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"{currentActor.displayName} uses skill for {damage} damage! {statusText}{critText}{elemText}";
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

        if (enemy.IsDead())
        {
            RemoveDeadCharactersFromTurnOrder();
            if (CheckBattleEndConditions())
                yield break;
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

        if (currentActor == null || !currentActor.CanUseUltimate())
        {
            if (battleLogText != null)
                battleLogText.text = "Ultimate is on cooldown!";
            return;
        }

        StartCoroutine(PlayerUltimateRoutine());
    }

    private IEnumerator PlayerUltimateRoutine()
    {
        if (currentActor == null || enemy == null) yield break;

        if (AudioManager.Instance != null) AudioManager.Instance.Play("ultimate");

        bool isCrit;
        float elemMul;
        int damage = currentActor.CalculateDamageAgainst(enemy, 1.5f, currentActor.ultimatePower, out isCrit, out elemMul);

        StartCoroutine(LungeForward(GetCurrentActorPortraitRect(), towardsCenter: true));

        int oldHp = enemy.currentHP;
        enemy.TakeDamage(damage);

        if (isCrit)
            StartCoroutine(CameraShake());
        SpawnImpact(onEnemy: true, color: GetElementColor(currentActor.element));
        SpawnDamagePopup(onEnemy: true, amount: damage, isCrit: isCrit);
        StartCoroutine(Shake(enemyPortraitRect));
        currentActor.PutUltimateOnCooldown();

        currentActor.ApplyStatus(StatusType.DefenseUp, 2);

        if (battleLogText != null)
        {
            string critText = BuildCritText(isCrit);
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"ULTIMATE! {currentActor.displayName} deals {damage} damage and raises DEFENSE!{critText}{elemText}";
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

        if (enemy.IsDead())
        {
            RemoveDeadCharactersFromTurnOrder();
            if (CheckBattleEndConditions())
                yield break;
        }

        EndPlayerTurn(afterDealingDamage: true);
    }

    private string ApplyElementalStatusFromPlayerSkill()
    {
        if (currentActor == null || enemy == null) return "";
        
        switch (currentActor.element)
        {
            case ElementType.Bass:
            case ElementType.Noise:
                enemy.ApplyStatus(StatusType.BleedEars, 3);
                return $"{currentActor.displayName} inflicts BLEEDING EARS over time!";

            case ElementType.Harmony:
            case ElementType.Melody:
                enemy.ApplyStatus(StatusType.Sleep, 1);
                return $"{currentActor.displayName}'s calm melody puts the enemy to SLEEP, skipping their next turn!";

            case ElementType.Percussion:
            case ElementType.Synth:
                enemy.ApplyStatus(StatusType.Stun, 1);
                return $"{currentActor.displayName} STUNS the enemy, they will miss their next turn!";

            default:
                return "";
        }
    }

    private void EnemyAction(CharacterStats enemyActor)
    {
        if (battleOver || enemyActor == null) return;
        StartCoroutine(EnemyActionRoutine(enemyActor));
    }

    private CharacterStats GetFirstAlivePartyMember()
    {
        for (int i = 0; i < 4; i++)
        {
            if (partyMembers[i] != null && !partyMembers[i].IsDead())
                return partyMembers[i];
        }
        return null;
    }

    private int GetPartyMemberIndex(CharacterStats member)
    {
        for (int i = 0; i < 4; i++)
        {
            if (partyMembers[i] == member)
                return i;
        }
        return -1;
    }

    private IEnumerator EnemyActionRoutine(CharacterStats enemyActor)
    {
        if (battleOver || enemyActor == null) yield break;

        CharacterStats target = GetFirstAlivePartyMember();
        if (target == null)
        {
            Debug.LogError("[BattleController] EnemyActionRoutine: No alive party members to target!");
            AdvanceTurn();
            yield break;
        }

        bool isCrit;
        float elemMul;
        int damage = enemyActor.CalculateDamageAgainst(target, 1.0f, 0, out isCrit, out elemMul);
        StartCoroutine(LungeForward(enemyPortraitRect, towardsCenter: false));

        int oldHp = target.currentHP;
        target.TakeDamage(damage);
        if (isCrit)
            StartCoroutine(CameraShake());
        
        int targetIndex = GetPartyMemberIndex(target);
        RectTransform targetPortraitRect = GetPartyMemberPortraitRect(targetIndex);
        SpawnImpact(onEnemy: false, color: GetElementColor(enemyActor.element), targetIndex: targetIndex);
        SpawnDamagePopup(onEnemy: false, amount: damage, isCrit: isCrit, targetIndex: targetIndex);
        StartCoroutine(Shake(targetPortraitRect != null ? targetPortraitRect : playerPortraitRect));

        if (battleLogText != null)
        {
            string critText = BuildCritText(isCrit);
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"{enemyActor.displayName} hits {target.displayName} for {damage} damage.{critText}{elemText}";
        }
        UpdateUI();

        Slider targetDamageSlider = GetPartyMemberDamageSlider(targetIndex);
        if (targetDamageSlider != null)
        {
            targetDamageSlider.maxValue = target.maxHP;
            targetDamageSlider.value = oldHp;
        }

        float postHitDelay = 0.35f;
        yield return new WaitForSeconds(postHitDelay);

        if (targetDamageSlider != null)
        {
            yield return StartCoroutine(
                AnimatePartyMemberHpBar(targetIndex, oldHp, target.currentHP)
            );
        }

        if (target.IsDead())
        {
            if (battleLogText != null)
                battleLogText.text += $"\n{target.displayName} was defeated!";
            
            RemoveDeadCharactersFromTurnOrder();
            if (CheckBattleEndConditions())
                yield break;
        }

        AdvanceTurn();
    }

    private bool CanPlayerAct()
    {
        if (battleOver) return false;
        if (currentActor == null) return false;
        if (!IsPlayerControlled(currentActor)) return false;
        return true;
    }

    private void EndPlayerTurn(bool afterDealingDamage)
    {
        UpdateUI();

        if (afterDealingDamage && (enemy == null || enemy.IsDead() || !turnOrder.Contains(enemy)))
        {
            RemoveDeadCharactersFromTurnOrder();
            if (CheckBattleEndConditions())
                return;
        }

        AdvanceTurn();
    }

    private void UpdateUI()
    {
        if (currentActor != null && IsPlayerControlled(currentActor))
        {
            int actorIndex = GetPartyMemberIndex(currentActor);
            if (actorIndex >= 0 && playerPortraitImage != null)
            {
                var gm = GameManager.Instance;
                if (gm != null)
                {
                    var pd = gm.playerData;
                    if (pd.activeLineupIndices != null && actorIndex < pd.activeLineupIndices.Length)
                    {
                        int idx = pd.activeLineupIndices[actorIndex];
                        if (idx >= 0 && idx < pd.ownedCharacters.Count)
                        {
                            var inst = pd.ownedCharacters[idx];
                            if (inst.data != null && inst.data.silhouetteSprite != null)
                                playerPortraitImage.sprite = inst.data.silhouetteSprite;
                        }
                    }
                }
            }
            
            if (playerHpText != null) {
                playerHpText.text = $"{currentActor.displayName} {currentActor.currentHP}/{currentActor.maxHP}";
            }
            if (playerHpSlider != null) {
                playerHpSlider.maxValue = currentActor.maxHP;
                if (!isPlayerHpAnimating) {
                    playerHpSlider.value = currentActor.currentHP;  
                }
            }
            if (playerHpDamageSlider != null) {
                playerHpDamageSlider.maxValue = currentActor.maxHP;
                if (!isPlayerHpAnimating) {
                    playerHpDamageSlider.value = currentActor.currentHP;
                }
            }
        }
        else if (currentActor == null || !IsPlayerControlled(currentActor))
        {
            if (playerHpText != null) {
                playerHpText.text = "";
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
        bool canAct = CanPlayerAct();

        if (basicAttackButton != null) {
            basicAttackButton.interactable = canAct;
            SetAbilityButtonLabel(basicAttackButton, "Strike", 0);
        }

        if (skillButton != null && currentActor != null && IsPlayerControlled(currentActor)) {
            skillButton.interactable = canAct && currentActor.CanUseSkill();
            SetAbilityButtonLabel(skillButton, "Skill", currentActor.skillCooldownRemaining);
        }
        else if (skillButton != null) {
            skillButton.interactable = false;
            SetAbilityButtonLabel(skillButton, "Skill", 0);
        }

        if (ultimateButton != null && currentActor != null && IsPlayerControlled(currentActor)) {
            ultimateButton.interactable = canAct && currentActor.CanUseUltimate();
            SetAbilityButtonLabel(ultimateButton, "Ultimate", currentActor.ultimateCooldownRemaining);
        }
        else if (ultimateButton != null) {
            ultimateButton.interactable = false;
            SetAbilityButtonLabel(ultimateButton, "Ultimate", 0);
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
        if (playerStatusIcon != null && currentActor != null && IsPlayerControlled(currentActor)) {
            playerStatusIcon.color = GetStatusColor(currentActor.currentStatus);
        }
        else if (playerStatusIcon != null) {
            playerStatusIcon.color = noStatusColor;
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

    private void SpawnDamagePopup(bool onEnemy, int amount, bool isCrit, int targetIndex = -1) {
        if (damagePopupPrefab == null) return;

        RectTransform anchor;
        if (onEnemy)
        {
            anchor = enemyPopupAnchor;
        }
        else
        {
            if (targetIndex >= 0)
                anchor = GetPartyMemberPopupAnchor(targetIndex);
            else
                anchor = GetCurrentActorPopupAnchor();
        }
        
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

    /// <summary>
    /// Scales enemy stats based on tower floor.
    /// Uses a moderate exponential curve so that early floors are forgiving,
    /// mid floors become challenging, and late floors are tough but still
    /// realistically beatable with leveled characters.
    /// Boss floors get an additional, smaller multiplier.
    /// </summary>
    private void ScaleEnemyForFloor(int floorNumber, bool isBoss)
    {
        if (enemy == null) return;

        int t = Mathf.Max(0, floorNumber - 1);

        // ~3.5% HP growth, 2.5% ATK, 2% DEF per floor (compounded).
        float hpMul = Mathf.Pow(1.035f, t);
        float atkMul = Mathf.Pow(1.025f, t);
        float defMul = Mathf.Pow(1.02f,  t);

        if (isBoss)
        {
            hpMul *= 1.6f;
            atkMul *= 1.3f;
            defMul *= 1.2f;
        }

        enemy.maxHP = Mathf.RoundToInt(enemy.maxHP * hpMul);
        enemy.currentHP = enemy.maxHP;
        enemy.attack = Mathf.RoundToInt(enemy.attack * atkMul);
        enemy.defense = Mathf.RoundToInt(enemy.defense * defMul);

        Debug.Log($"[BattleController] ScaleEnemyForFloor: floor={floorNumber}, boss={isBoss}, hpMul={hpMul:F2}, atkMul={atkMul:F2}, defMul={defMul:F2}, " +
                  $"result HP={enemy.maxHP}, ATK={enemy.attack}, DEF={enemy.defense}");
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
            if (backHomeButton != null)
            {
                backHomeButton.gameObject.SetActive(true);
                backHomeButton.interactable = true;
            }
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
        if (backHomeButton != null)
        {
            backHomeButton.gameObject.SetActive(true);
            backHomeButton.interactable = true;
        }
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

    private void SpawnImpact(bool onEnemy, Color color, int targetIndex = -1) {
        if (impactEffectPrefab == null) return;

        RectTransform anchor;
        if (onEnemy)
        {
            anchor = enemyImpactAnchor;
        }
        else
        {
            if (targetIndex >= 0)
                anchor = GetPartyMemberImpactAnchor(targetIndex);
            else
                anchor = GetCurrentActorImpactAnchor();
        }
        
        if (anchor == null) return;

        var fx = Instantiate(impactEffectPrefab, anchor);
        var rect = fx.GetComponent<RectTransform>();
        if (rect != null)
            rect.anchoredPosition = Vector2.zero;
        
        fx.gameObject.SetActive(true);
        fx.Init(color);
    }

    private RectTransform GetPartyMemberImpactAnchor(int index)
    {
        if (index >= 0 && index < partyImpactAnchors.Length && partyImpactAnchors[index] != null)
            return partyImpactAnchors[index];
        return playerImpactAnchor;
    }

    private RectTransform GetPartyMemberPopupAnchor(int index)
    {
        if (index >= 0 && index < partyPopupAnchors.Length && partyPopupAnchors[index] != null)
            return partyPopupAnchors[index];
        return playerPopupAnchor;
    }

    private RectTransform GetPartyMemberPortraitRect(int index)
    {
        if (index >= 0 && index < partyPortraitRects.Length && partyPortraitRects[index] != null)
            return partyPortraitRects[index];
        return playerPortraitRect;
    }

    private Slider GetPartyMemberDamageSlider(int index)
    {
        if (index >= 0 && index < partyHpDamageSliders.Length && partyHpDamageSliders[index] != null)
            return partyHpDamageSliders[index];
        return playerHpDamageSlider;
    }

    private RectTransform GetCurrentActorImpactAnchor()
    {
        if (currentActor == null) return playerImpactAnchor;
        int index = GetPartyMemberIndex(currentActor);
        if (index >= 0)
            return GetPartyMemberImpactAnchor(index);
        return playerImpactAnchor;
    }

    private RectTransform GetCurrentActorPopupAnchor()
    {
        if (currentActor == null) return playerPopupAnchor;
        int index = GetPartyMemberIndex(currentActor);
        if (index >= 0)
            return GetPartyMemberPopupAnchor(index);
        return playerPopupAnchor;
    }

    private RectTransform GetCurrentActorPortraitRect()
    {
        if (currentActor == null) return playerPortraitRect;
        int index = GetPartyMemberIndex(currentActor);
        if (index >= 0)
            return GetPartyMemberPortraitRect(index);
        return playerPortraitRect;
    }

    private IEnumerator AnimatePartyMemberHpBar(int memberIndex, int startValue, int targetValue)
    {
        if (memberIndex < 0 || memberIndex >= 4 || partyMembers[memberIndex] == null)
            yield break;

        Slider slider = GetPartyMemberDamageSlider(memberIndex);
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
        if (abilityCardPanel == null || currentActor == null || !IsPlayerControlled(currentActor)) return;

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
                dmg = currentActor.attack;
                cd = 0;
                break;

            case PendingAbility.Skill:
                name = "Signature Skill";
                desc = DescribeSkillByElement(currentActor.element);
                dmg = currentActor.attack + currentActor.skillPower;
                cd = currentActor.skillCooldownTurns;
                break;

            case PendingAbility.Ultimate:
                name = "Ultimate";
                desc = "Massive attack that also grants Harmonic Shield (Defense Up) for 2 of your turns.";
                dmg = currentActor.attack + currentActor.ultimatePower;
                cd = currentActor.ultimateCooldownTurns;
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

    private bool IsPlayerControlled(CharacterStats character)
    {
        if (character == null) return false;
        for (int i = 0; i < 4; i++)
        {
            if (partyMembers[i] == character)
                return true;
        }
        return false;
    }

    private void BuildTurnOrder()
    {
        turnOrder.Clear();
        
        for (int i = 0; i < 4; i++)
        {
            if (partyMembers[i] != null && !partyMembers[i].IsDead())
            {
                turnOrder.Add(partyMembers[i]);
                Debug.Log($"[BattleController] BuildTurnOrder: Added party member {partyMembers[i].displayName} (speed: {partyMembers[i].speed})");
            }
        }
        
        if (enemy != null && !enemy.IsDead())
        {
            turnOrder.Add(enemy);
            Debug.Log($"[BattleController] BuildTurnOrder: Added enemy {enemy.displayName} (speed: {enemy.speed})");
        }
        
        turnOrder.Sort((a, b) => b.speed.CompareTo(a.speed));
        
        Debug.Log($"[BattleController] BuildTurnOrder: Turn order established with {turnOrder.Count} characters");
        for (int i = 0; i < turnOrder.Count; i++)
        {
            Debug.Log($"[BattleController] BuildTurnOrder: Position {i}: {turnOrder[i].displayName} (speed: {turnOrder[i].speed})");
        }
    }

    private void RemoveDeadCharactersFromTurnOrder()
    {
        for (int i = turnOrder.Count - 1; i >= 0; i--)
        {
            if (turnOrder[i] == null || turnOrder[i].IsDead())
            {
                Debug.Log($"[BattleController] RemoveDeadCharactersFromTurnOrder: Removing dead character at index {i}");
                turnOrder.RemoveAt(i);
            }
        }
        
        if (currentTurnIndex >= turnOrder.Count && turnOrder.Count > 0)
        {
            currentTurnIndex = 0;
        }
    }

    private bool CheckBattleEndConditions()
    {
        if (enemy == null || enemy.IsDead() || !turnOrder.Contains(enemy))
        {
            if (battleLogText != null)
                battleLogText.text += "\nEnemy defeated! You win.";
            battleOver = true;
            UpdateAbilityButtons();
            var gm = GameManager.Instance;
            if (gm != null)
            {
                // Cache the floor index we just cleared before advancing.
                int clearedFloorIndex = gm.playerData != null ? gm.playerData.towerCurrentFloor : 0;

                var floor = gm.GetCurrentTowerFloor();
                if (floor != null && gm.rewardManager != null)
                {
                    gm.rewardManager.GrantFloorRewards(gm.playerData, floor);
                    if (rewardsText != null)
                    {
                        string itemPart = floor.rewardItem != null ? $", Item: {floor.rewardItem.displayName}" : "";
                        rewardsText.text = $"+{floor.rewardSoftCurrency} Credits, +{floor.rewardPremiumCurrency} Gems{itemPart}";
                    }
                }
                if (gm.towerProgression != null)
                {
                    gm.towerProgression.TryAdvanceFloor(gm.playerData);
                }
                gm.SetEnemyFromTowerFloor();
                gm.NotifyPlayerDataChanged();
                gm.SavePlayerData();

                // Quest hook: report the floor that was just cleared (before advancing).
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.OnBattleWon(clearedFloorIndex);
                }
            }
            PlayWinSequence();
            return true;
        }
        
        bool anyAlivePartyMember = false;
        for (int i = 0; i < 4; i++)
        {
            if (partyMembers[i] != null && !partyMembers[i].IsDead())
            {
                anyAlivePartyMember = true;
                break;
            }
        }
        
        if (!anyAlivePartyMember)
        {
            if (battleLogText != null)
                battleLogText.text += "\nYour entire party was defeated...";
            battleOver = true;
            UpdateAbilityButtons();
            PlayLoseSequence();
            return true;
        }
        
        return false;
    }
}
