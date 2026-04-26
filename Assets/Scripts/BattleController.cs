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
    public CharacterStats[] enemyActors = new CharacterStats[4];

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

    [Header("Enemy Member Displays (Arrays for 4 slots)")]
    public Image[] enemyPortraitImages = new Image[4];
    public TextMeshProUGUI[] enemyHpTexts = new TextMeshProUGUI[4];
    public Slider[] enemyHpSliders = new Slider[4];
    public Slider[] enemyHpDamageSliders = new Slider[4];
    public RectTransform[] enemyPortraitRects = new RectTransform[4];
    public RectTransform[] enemyImpactAnchors = new RectTransform[4];
    public RectTransform[] enemyPopupAnchors = new RectTransform[4];
    public Image[] enemyStatusIcons = new Image[4];

    [Header("UI References")]
    public TextMeshProUGUI playerHpText;
    public TextMeshProUGUI enemyHpText;
    public TextMeshProUGUI battleLogText;
    public TextMeshProUGUI turnOrderText;

    public Slider playerHpSlider;
    public Slider enemyHpSlider;

    [Header("HP Bar Damage Overlay")]
    public Slider playerHpDamageSlider;
    public Slider enemyHpDamageSlider;

    [Header("Ability Buttons")]
    public Button basicAttackButton;
    public Button skillButton;
    public Button ultimateButton;
    public RectTransform abilityButtonPanel;

    private bool battleOver = false;

    private CharacterStats[] partyMembers = new CharacterStats[4];
    private CharacterStats[] enemyMembers = new CharacterStats[4];
    private readonly bool[] partySlotsHidden = new bool[4];
    private readonly bool[] enemySlotsHidden = new bool[4];
    private int currentEnemyTargetIndex = 0;

    [Header("Target Indicators")]
    public GameObject[] enemyTargetIndicators = new GameObject[4];
    public string outerTargetIndicatorName = "OuterDashed";
    public string innerTargetIndicatorName = "InnerTargetIndicator";
    public float outerTargetIndicatorRotateSpeed = 240f;
    public float innerTargetIndicatorRotateSpeed = 240f;
    public float selectedTargetIndicatorScale = 1f;
    public float aoeTargetIndicatorScale = 0.7f;
    private bool isSelectingTarget = false;
    private PendingAbility selectingAbility = PendingAbility.None;

    private List<CharacterStats> turnOrder = new List<CharacterStats>();
    private int currentTurnIndex = 0;
    private CharacterStats currentActor = null;

    private Vector2 abilityPanelShownPos;
    private Vector2 abilityPanelHiddenPos;
    private bool abilityPanelPositionsInitialized = false;

    private void Start()
    {

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera != null)
        {
            baseCamPos = mainCamera.transform.position;
            baseCamSize = mainCamera.orthographicSize;
        }

        var gm = GameManager.Instance;

        if (gm != null)
        {
            if (floorText != null)
            {
                floorText.text = $"Floor {gm.GetFloorLabel(gm.playerData.towerCurrentFloor)}";
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

        // Cache ability panel positions for slide in/out (capture shown pos once)
        EnsureAbilityPanelPositionsInitialized();

        if (autoStartBattle)
            StartBattleNow();
    }

    private void Update()
    {
        if (!isSelectingTarget) return;

        RotateActiveTargetIndicators();
    }

    public void ResetBattleState()
    {
        hasStarted = false;
        battleOver = false;
        currentTurnIndex = 0;
        currentActor = null;
        turnOrder.Clear();

        for (int i = 0; i < 4; i++)
        {
            partySlotsHidden[i] = false;
            enemySlotsHidden[i] = false;

            if (partyMembers[i] != null)
            {
                Destroy(partyMembers[i].gameObject);
                partyMembers[i] = null;
            }
        }

        for (int i = 0; i < enemyMembers.Length; i++)
        {
            enemyMembers[i] = null;
        }
    }

    public void StartBattleNow()
    {

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

        if (gm == null)
        {
            Debug.LogError("[BattleController] StartBattleNow: GameManager.Instance is NULL! Cannot start battle!");
            return;
        }

        var pd = gm.playerData;

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

        InitializePartyMembers(gm);
        InitializeEnemies(gm);

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
            var enemyData = GetCurrentEnemyData();
            if (enemyData != null && enemyData.isBoss && enemyData.bossIntroClip != null)
                AudioManager.Instance.PlayClip(enemyData.bossIntroClip);
            else
                AudioManager.Instance.Play("battle_start");
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
        EnsureEnemyTargetValid();

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
            int deadIdx = GetPartyMemberIndex(actor);
            if (deadIdx >= 0)
                StartCoroutine(PartyMemberDeathPixelateRoutine(deadIdx));
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

        // Auto-setup: show ability panel, select lowest HP enemy, and select best available ability.
        StartCoroutine(SlideAbilityPanelIn());
        BeginTargetSelection(GetBestAvailableAbilityForActor(actor));
    }

    private PendingAbility GetBestAvailableAbilityForActor(CharacterStats actor)
    {
        if (actor == null) return PendingAbility.Basic;
        if (actor.CanUseUltimate()) return PendingAbility.Ultimate;
        if (actor.CanUseSkill()) return PendingAbility.Skill;
        return PendingAbility.Basic;
    }

    private void BeginTargetSelection(PendingAbility ability)
    {
        selectingAbility = ability;
        isSelectingTarget = true;
        AutoSelectLowestHpEnemy();
        ShowAbilityCard(ability);
        UpdateTargetIndicators();
    }

    private void AutoSelectLowestHpEnemy()
    {
        int bestIdx = -1;
        int bestHp = int.MaxValue;

        for (int i = 0; i < enemyMembers.Length; i++)
        {
            var e = enemyMembers[i];
            if (e == null || e.IsDead()) continue;

            if (e.currentHP < bestHp)
            {
                bestHp = e.currentHP;
                bestIdx = i;
            }
        }

        if (bestIdx >= 0)
            SetEnemyTarget(bestIdx);
        else
            EnsureEnemyTargetValid();
    }

    private void CancelTargetSelection()
    {
        isSelectingTarget = false;
        selectingAbility = PendingAbility.None;
        HideAbilityCard();
        UpdateTargetIndicators();
    }

    private void UpdateTargetIndicators()
    {
        if (enemyTargetIndicators == null) return;

        bool isAoe = IsAoeAbility(selectingAbility);

        for (int i = 0; i < enemyTargetIndicators.Length; i++)
        {
            var go = enemyTargetIndicators[i];
            if (go == null) continue;

            bool isAliveEnemy = enemyMembers != null
                                && i < enemyMembers.Length
                                && enemyMembers[i] != null
                                && !enemyMembers[i].IsDead();
            bool isMainTarget = i == currentEnemyTargetIndex;
            bool on = isSelectingTarget && isAliveEnemy && (isMainTarget || isAoe);

            go.SetActive(on);

            if (on)
            {
                float scale = isMainTarget ? selectedTargetIndicatorScale : aoeTargetIndicatorScale;
                go.transform.localScale = Vector3.one * scale;
            }
        }
    }

    private bool IsAoeAbility(PendingAbility ability)
    {
        return false;
    }

    private void RotateActiveTargetIndicators()
    {
        if (enemyTargetIndicators == null) return;

        for (int i = 0; i < enemyTargetIndicators.Length; i++)
        {
            var indicator = enemyTargetIndicators[i];
            if (indicator == null || !indicator.activeSelf) continue;

            RotateTargetIndicatorChild(indicator.transform, outerTargetIndicatorName, outerTargetIndicatorRotateSpeed);
            RotateTargetIndicatorChild(indicator.transform, innerTargetIndicatorName, -innerTargetIndicatorRotateSpeed);
        }
    }

    private void RotateTargetIndicatorChild(Transform parent, string childName, float speed)
    {
        Transform child = parent.Find(childName);
        if (child == null) return;

        child.Rotate(0f, 0f, speed * Time.deltaTime);
    }

    public void OnEnemySlotPressed(int index)
    {
        if (!isSelectingTarget) return;
        if (index < 0 || index >= enemyMembers.Length) return;
        if (enemyMembers[index] == null || enemyMembers[index].IsDead()) return;

        if (index == currentEnemyTargetIndex)
        {
            ConfirmTargetSelectionAndExecute();
            return;
        }

        SetEnemyTarget(index);
    }

    private void ConfirmTargetSelectionAndExecute()
    {
        if (!isSelectingTarget) return;
        if (selectingAbility == PendingAbility.None) return;

        // Ensure we have a valid target (prefer current selection, fallback to lowest HP).
        int idx = currentEnemyTargetIndex;
        if (idx < 0 || idx >= enemyMembers.Length || enemyMembers[idx] == null || enemyMembers[idx].IsDead())
        {
            AutoSelectLowestHpEnemy();
            idx = currentEnemyTargetIndex;
        }

        if (idx < 0 || idx >= enemyMembers.Length || enemyMembers[idx] == null || enemyMembers[idx].IsDead())
            return;

        var ability = selectingAbility;
        isSelectingTarget = false;
        selectingAbility = PendingAbility.None;
        HideAbilityCard();
        UpdateTargetIndicators();

        if (ability == PendingAbility.Basic)
            StartCoroutine(PlayerAttackWithPanelRoutine(PlayerBasicAttackRoutine(idx)));
        else if (ability == PendingAbility.Skill)
            StartCoroutine(PlayerAttackWithPanelRoutine(PlayerSkillRoutine(idx)));
        else if (ability == PendingAbility.Ultimate)
            StartCoroutine(PlayerAttackWithPanelRoutine(PlayerUltimateRoutine(idx)));
    }

    private void StartEnemyTurn(CharacterStats enemyActor)
    {
        if (battleOver || enemyActor == null) return;
        int idx = GetEnemyIndex(enemyActor);
        if (idx >= 0)
            currentEnemyTargetIndex = idx;
        EnsureEnemyTargetValid();
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
            int eIdx = GetEnemyIndex(enemyActor);

            if (battleLogText != null)
            {
                battleLogText.text += $"\n{enemyActor.displayName} suffers {statusDamage} damage from {enemyActor.currentStatus}.";
            }

            var dmgSlider = GetEnemyHpDamageSlider(eIdx);
            if (dmgSlider != null)
            {
                dmgSlider.maxValue = enemyActor.maxHP;
                dmgSlider.value = oldHp;
            }

            UpdateUI();

            float preHitDelay = 0.5f;
            yield return new WaitForSeconds(preHitDelay);
            StartCoroutine(Shake(GetEnemyPortraitRect(eIdx)));
            SpawnImpact(onEnemy: true, color: GetStatusColor(enemyActor.currentStatus), enemyIndex: eIdx);
            SpawnDamagePopup(onEnemy: true, amount: statusDamage, isCrit: false, enemyIndex: eIdx);

            UpdateUI();

            float postHitDelay = 0.25f;
            yield return new WaitForSeconds(postHitDelay);

            if (dmgSlider != null)
            {
                yield return StartCoroutine(
                    AnimateHpBar(dmgSlider, oldHp, newHp, isEnemy: true)
                );
            }
        }
        else
        {
            UpdateUI();
        }

        if (enemyActor.IsDead())
        {
            int deadIdx = GetEnemyIndex(enemyActor);
            if (deadIdx >= 0)
                yield return StartCoroutine(EnemyDeathPixelateRoutine(deadIdx));
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

        if (currentActor == null) return;

        if (isSelectingTarget)
        {
            if (selectingAbility == PendingAbility.Basic)
                ConfirmTargetSelectionAndExecute();
            else
                BeginTargetSelection(PendingAbility.Basic);
            return;
        }

        BeginTargetSelection(PendingAbility.Basic);
    }

    private IEnumerator PlayerBasicAttackRoutine(int enemyIndex)
    {
        if (currentActor == null) yield break;
        if (enemyIndex < 0 || enemyIndex >= enemyMembers.Length) yield break;
        var target = enemyMembers[enemyIndex];
        if (target == null || target.IsDead()) yield break;

        if (AudioManager.Instance != null) AudioManager.Instance.Play("basic");

        bool isCrit;
        float elemMul;
        int damage = currentActor.CalculateDamageAgainst(target, 1.0f, 0, out isCrit, out elemMul);

        StartCoroutine(LungeForward(GetCurrentActorPortraitRect(), towardsCenter: true));

        int oldHp = target.currentHP;
        target.TakeDamage(damage);
        int newHp = target.currentHP;

        if (isCrit)
            StartCoroutine(CameraShake());
        SpawnImpact(onEnemy: true, color: GetElementColor(currentActor.element), enemyIndex: enemyIndex);
        SpawnDamagePopup(onEnemy: true, amount: damage, isCrit: isCrit, enemyIndex: enemyIndex);
        StartCoroutine(Shake(GetEnemyPortraitRect(enemyIndex)));

        if (battleLogText != null)
        {
            string critText = BuildCritText(isCrit);
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"{currentActor.displayName} strikes the enemy for {damage} damage.{critText}{elemText}";
        }
        UpdateUI();

        var dmgSlider = GetEnemyHpDamageSlider(enemyIndex);
        if (dmgSlider != null)
        {
            dmgSlider.maxValue = target.maxHP;
            dmgSlider.value = oldHp;
        }

        float postHitDelay = 0.35f;
        yield return new WaitForSeconds(postHitDelay);

        if (dmgSlider != null)
        {
            yield return StartCoroutine(
                AnimateHpBar(dmgSlider, oldHp, target.currentHP, isEnemy: true)
            );
        }

        if (target.IsDead())
        {
            yield return StartCoroutine(EnemyDeathPixelateRoutine(enemyIndex));
            RemoveDeadCharactersFromTurnOrder();
            if (CheckBattleEndConditions())
                yield break;
            EnsureEnemyTargetValid();
        }

        EndPlayerTurn(afterDealingDamage: true);
    }

    public void OnSkillPressed()
    {
        if (!CanPlayerAct()) return;

        if (currentActor == null || !currentActor.CanUseSkill())
        {
            if (battleLogText != null)
                battleLogText.text = "Skill is on cooldown!";
            return;
        }

        if (isSelectingTarget)
        {
            if (selectingAbility == PendingAbility.Skill)
                ConfirmTargetSelectionAndExecute();
            else
                BeginTargetSelection(PendingAbility.Skill);
            return;
        }

        BeginTargetSelection(PendingAbility.Skill);
    }

    private IEnumerator PlayerSkillRoutine(int enemyIndex)
    {
        if (currentActor == null) yield break;
        if (enemyIndex < 0 || enemyIndex >= enemyMembers.Length) yield break;
        var target = enemyMembers[enemyIndex];
        if (target == null || target.IsDead()) yield break;

        if (AudioManager.Instance != null) AudioManager.Instance.Play("skill");

        bool isCrit;
        float elemMul;
        int damage = currentActor.CalculateDamageAgainst(target, 1.2f, currentActor.skillPower, out isCrit, out elemMul);

        StartCoroutine(LungeForward(GetCurrentActorPortraitRect(), towardsCenter: true));

        int oldHp = target.currentHP;
        target.TakeDamage(damage);

        if (isCrit)
            StartCoroutine(CameraShake());
        SpawnImpact(onEnemy: true, color: GetElementColor(currentActor.element), enemyIndex: enemyIndex);
        SpawnDamagePopup(onEnemy: true, amount: damage, isCrit: isCrit, enemyIndex: enemyIndex);
        StartCoroutine(Shake(GetEnemyPortraitRect(enemyIndex)));
        currentActor.PutSkillOnCooldown();

        string statusText = ApplyElementalStatusFromPlayerSkill(target);

        if (battleLogText != null)
        {
            string critText = BuildCritText(isCrit);
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"{currentActor.displayName} uses skill for {damage} damage! {statusText}{critText}{elemText}";
        }
        UpdateUI();

        var dmgSlider = GetEnemyHpDamageSlider(enemyIndex);
        if (dmgSlider != null)
        {
            dmgSlider.maxValue = target.maxHP;
            dmgSlider.value = oldHp;
        }

        float postHitDelay = 0.35f;
        yield return new WaitForSeconds(postHitDelay);

        if (dmgSlider != null)
        {
            yield return StartCoroutine(
                AnimateHpBar(dmgSlider, oldHp, target.currentHP, isEnemy: true)
            );
        }

        if (target.IsDead())
        {
            yield return StartCoroutine(EnemyDeathPixelateRoutine(enemyIndex));
            RemoveDeadCharactersFromTurnOrder();
            if (CheckBattleEndConditions())
                yield break;
            EnsureEnemyTargetValid();
        }

        EndPlayerTurn(afterDealingDamage: true);
    }

    public void OnUltimatePressed()
    {
        if (!CanPlayerAct()) return;

        if (currentActor == null || !currentActor.CanUseUltimate())
        {
            if (battleLogText != null)
                battleLogText.text = "Ultimate is on cooldown!";
            return;
        }

        if (isSelectingTarget)
        {
            if (selectingAbility == PendingAbility.Ultimate)
                ConfirmTargetSelectionAndExecute();
            else
                BeginTargetSelection(PendingAbility.Ultimate);
            return;
        }

        BeginTargetSelection(PendingAbility.Ultimate);
    }

    private IEnumerator PlayerUltimateRoutine(int enemyIndex)
    {
        if (currentActor == null) yield break;
        if (enemyIndex < 0 || enemyIndex >= enemyMembers.Length) yield break;
        var target = enemyMembers[enemyIndex];
        if (target == null || target.IsDead()) yield break;

        if (AudioManager.Instance != null) AudioManager.Instance.Play("ultimate");

        bool isCrit;
        float elemMul;
        int damage = currentActor.CalculateDamageAgainst(target, 1.5f, currentActor.ultimatePower, out isCrit, out elemMul);

        StartCoroutine(LungeForward(GetCurrentActorPortraitRect(), towardsCenter: true));

        int oldHp = target.currentHP;
        target.TakeDamage(damage);

        if (isCrit)
            StartCoroutine(CameraShake());
        SpawnImpact(onEnemy: true, color: GetElementColor(currentActor.element), enemyIndex: enemyIndex);
        SpawnDamagePopup(onEnemy: true, amount: damage, isCrit: isCrit, enemyIndex: enemyIndex);
        StartCoroutine(Shake(GetEnemyPortraitRect(enemyIndex)));
        currentActor.PutUltimateOnCooldown();

        currentActor.ApplyStatus(StatusType.DefenseUp, 2);

        if (battleLogText != null)
        {
            string critText = BuildCritText(isCrit);
            string elemText = BuildElementText(elemMul);
            battleLogText.text = $"ULTIMATE! {currentActor.displayName} deals {damage} damage and raises DEFENSE!{critText}{elemText}";
        }
        UpdateUI();

        var dmgSlider = GetEnemyHpDamageSlider(enemyIndex);
        if (dmgSlider != null)
        {
            dmgSlider.maxValue = target.maxHP;
            dmgSlider.value = oldHp;
        }

        float postHitDelay = 0.35f;
        yield return new WaitForSeconds(postHitDelay);

        if (dmgSlider != null)
        {
            yield return StartCoroutine(
                AnimateHpBar(dmgSlider, oldHp, target.currentHP, isEnemy: true)
            );
        }

        if (target.IsDead())
        {
            yield return StartCoroutine(EnemyDeathPixelateRoutine(enemyIndex));
            RemoveDeadCharactersFromTurnOrder();
            if (CheckBattleEndConditions())
                yield break;
            EnsureEnemyTargetValid();
        }

        EndPlayerTurn(afterDealingDamage: true);
    }

    private string ApplyElementalStatusFromPlayerSkill(CharacterStats target)
    {
        if (currentActor == null || target == null) return "";

        switch (currentActor.element)
        {
            case ElementType.Bass:
            case ElementType.Noise:
                target.ApplyStatus(StatusType.BleedEars, 3);
                return $"{currentActor.displayName} inflicts BLEEDING EARS over time!";

            case ElementType.Harmony:
            case ElementType.Melody:
                target.ApplyStatus(StatusType.Sleep, 1);
                return $"{currentActor.displayName}'s calm melody puts the enemy to SLEEP, skipping their next turn!";

            case ElementType.Percussion:
            case ElementType.Synth:
                target.ApplyStatus(StatusType.Stun, 1);
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

            yield return StartCoroutine(PartyMemberDeathPixelateRoutine(targetIndex));
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

        if (afterDealingDamage)
        {
            RemoveDeadCharactersFromTurnOrder();
            if (CheckBattleEndConditions())
                return;
        }

        AdvanceTurn();
    }

    private void UpdateUI()
    {
        if (currentActor != null && IsPlayerControlled(currentActor) && !currentActor.IsDead())
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

        if (enemy != null && !enemy.IsDead()) {
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
        else
        {
            if (enemyHpText != null)
                enemyHpText.text = "";
            if (enemyHpSlider != null)
                enemyHpSlider.value = 0;
            if (enemyHpDamageSlider != null)
                enemyHpDamageSlider.value = 0;
            if (enemyPortraitImage != null && !AnyAliveEnemy())
                enemyPortraitImage.enabled = false;
        }

        UpdatePartyMemberUI();
        UpdateEnemyMemberUI();
        UpdateStatusIcons();
    }

    private void UpdateEnemyMemberUI()
    {
        for (int i = 0; i < 4; i++)
        {
            var e = (enemyMembers != null && i < enemyMembers.Length) ? enemyMembers[i] : null;
            bool visible = e != null && !enemySlotsHidden[i];
            SetEnemySlotVisible(i, visible);

            if (!visible) continue;

            if (enemyHpTexts != null && i < enemyHpTexts.Length && enemyHpTexts[i] != null)
                enemyHpTexts[i].text = $"{e.currentHP}/{e.maxHP}";

            if (enemyHpSliders != null && i < enemyHpSliders.Length && enemyHpSliders[i] != null)
            {
                enemyHpSliders[i].maxValue = e.maxHP;
                enemyHpSliders[i].value = e.currentHP;
            }

            if (enemyHpDamageSliders != null && i < enemyHpDamageSliders.Length && enemyHpDamageSliders[i] != null)
            {
                enemyHpDamageSliders[i].maxValue = e.maxHP;
                enemyHpDamageSliders[i].value = e.currentHP;
            }

            if (enemyStatusIcons != null && i < enemyStatusIcons.Length && enemyStatusIcons[i] != null)
                enemyStatusIcons[i].color = GetStatusColor(e.currentStatus);
        }
    }

    private void UpdatePartyMemberUI()
    {
        for (int i = 0; i < 4; i++)
        {
            bool visible = partyMembers[i] != null && !partySlotsHidden[i];
            SetPartyMemberSlotVisible(i, visible);

            if (!visible) continue;

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

    private void HideEnemySlot(int index)
    {
        if (index < 0 || index >= enemySlotsHidden.Length) return;
        enemySlotsHidden[index] = true;
        SetEnemySlotVisible(index, false);
        if (index == currentEnemyTargetIndex)
            EnsureEnemyTargetValid();
        UpdateTargetIndicators();
        UpdateUI();
    }

    private void HidePartyMemberSlot(int index)
    {
        if (index < 0 || index >= partySlotsHidden.Length) return;
        partySlotsHidden[index] = true;
        SetPartyMemberSlotVisible(index, false);
    }

    private void SetEnemySlotVisible(int index, bool visible)
    {
        SetComponentVisible(enemyPortraitImages, index, visible, true);
        SetComponentVisible(enemyHpTexts, index, visible);
        SetComponentVisible(enemyHpSliders, index, visible);
        SetComponentVisible(enemyHpDamageSliders, index, visible);
        SetComponentVisible(enemyStatusIcons, index, visible, true);

        if (enemyTargetIndicators != null && index >= 0 && index < enemyTargetIndicators.Length && enemyTargetIndicators[index] != null && !visible)
            enemyTargetIndicators[index].SetActive(false);
    }

    private void SetPartyMemberSlotVisible(int index, bool visible)
    {
        SetComponentVisible(partyPortraitImages, index, visible, true);
        SetComponentVisible(partyHpTexts, index, visible);
        SetComponentVisible(partyHpSliders, index, visible);
        SetComponentVisible(partyHpDamageSliders, index, visible);
        SetComponentVisible(partyStatusIcons, index, visible, true);
    }

    private void SetComponentVisible<T>(T[] components, int index, bool visible, bool toggleGraphic = false) where T : Component
    {
        if (components == null || index < 0 || index >= components.Length || components[index] == null)
            return;

        components[index].gameObject.SetActive(visible);

        if (toggleGraphic && components[index] is Graphic graphic)
            graphic.enabled = visible;
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

    private IEnumerator PlayerAttackWithPanelRoutine(IEnumerator attackRoutine)
    {
        yield return StartCoroutine(SlideAbilityPanelOut());

        yield return new WaitForSeconds(0.05f);

        yield return StartCoroutine(attackRoutine);

        yield return new WaitForSeconds(0.08f);

        yield return StartCoroutine(SlideAbilityPanelIn());

        EnsureEnemyTargetValid();
        UpdateTargetIndicators();
    }

    private IEnumerator SlideAbilityPanelOut()
    {
        if (abilityButtonPanel == null) yield break;
        EnsureAbilityPanelPositionsInitialized();

        Vector2 start = abilityButtonPanel.anchoredPosition;
        Vector2 end = abilityPanelHiddenPos; // fully hidden position
        float duration = 0.25f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            n = n * n * (3f - 2f * n);
            abilityButtonPanel.anchoredPosition = Vector2.Lerp(start, end, n);
            yield return null;
        }

        abilityButtonPanel.anchoredPosition = end;
    }

    private IEnumerator SlideAbilityPanelIn()
    {
        if (abilityButtonPanel == null) yield break;
        EnsureAbilityPanelPositionsInitialized();

        Vector2 start = abilityButtonPanel.anchoredPosition;
        Vector2 end = abilityPanelShownPos;
        float duration = 0.25f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            n = n * n * (3f - 2f * n);
            abilityButtonPanel.anchoredPosition = Vector2.Lerp(start, end, n);
            yield return null;
        }

        abilityButtonPanel.anchoredPosition = end;
    }

    private void EnsureAbilityPanelPositionsInitialized()
    {
        if (abilityButtonPanel == null) return;

        Canvas.ForceUpdateCanvases();

        if (!abilityPanelPositionsInitialized)
        {
            abilityPanelShownPos = abilityButtonPanel.anchoredPosition;
            abilityPanelPositionsInitialized = true;
        }

        float height = abilityButtonPanel.rect.height;
        float extraMargin = 40f;
        float offsetY = -(height + extraMargin);
        abilityPanelHiddenPos = abilityPanelShownPos + new Vector2(0f, offsetY);
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

    private void SpawnDamagePopup(bool onEnemy, int amount, bool isCrit, int targetIndex = -1, int enemyIndex = -1) {
        if (damagePopupPrefab == null) return;

        RectTransform anchor;
        if (onEnemy)
        {
            anchor = GetEnemyPopupAnchor(enemyIndex >= 0 ? enemyIndex : currentEnemyTargetIndex);
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

    private void PlayWinSequence()
    {
        StartCoroutine(WinSequenceRoutine());
    }

    private IEnumerator WinSequenceRoutine()
    {
        float postHitDelay = 0.35f;
        yield return new WaitForSeconds(postHitDelay);

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

    private IEnumerator EnemyDeathPixelateRoutine(int enemyIndex)
    {
        Image img = null;
        if (enemyPortraitImages != null && enemyIndex >= 0 && enemyIndex < enemyPortraitImages.Length)
            img = enemyPortraitImages[enemyIndex];

        if (enemyPixelateMaterialTemplate == null || img == null)
        {
            HideEnemySlot(enemyIndex);
            yield break;
        }

        Material runtimeMat = new Material(enemyPixelateMaterialTemplate);
        var originalMat = img.material;

        img.material = runtimeMat;
        runtimeMat.SetFloat("_PixelAmount", 0f);

        float t = 0f;
        while (t < enemyDeathPixelDuration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / enemyDeathPixelDuration);
            runtimeMat.SetFloat("_PixelAmount", n);
            yield return null;
        }

        runtimeMat.SetFloat("_PixelAmount", 1f);
        img.material = originalMat;
        img.enabled = false;
        Destroy(runtimeMat);
        HideEnemySlot(enemyIndex);
    }

    private IEnumerator PartyMemberDeathPixelateRoutine(int partyIndex)
    {
        Image img = null;
        if (partyPortraitImages != null && partyIndex >= 0 && partyIndex < partyPortraitImages.Length)
            img = partyPortraitImages[partyIndex];

        if (enemyPixelateMaterialTemplate == null || img == null)
        {
            HidePartyMemberSlot(partyIndex);
            yield break;
        }

        Material runtimeMat = new Material(enemyPixelateMaterialTemplate);
        var originalMat = img.material;

        img.material = runtimeMat;
        runtimeMat.SetFloat("_PixelAmount", 0f);

        float t = 0f;
        while (t < enemyDeathPixelDuration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / enemyDeathPixelDuration);
            runtimeMat.SetFloat("_PixelAmount", n);
            yield return null;
        }

        runtimeMat.SetFloat("_PixelAmount", 1f);
        img.material = originalMat;
        img.enabled = false;
        Destroy(runtimeMat);
        HidePartyMemberSlot(partyIndex);
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

    private void SpawnImpact(bool onEnemy, Color color, int targetIndex = -1, int enemyIndex = -1) {
        if (impactEffectPrefab == null) return;

        RectTransform anchor;
        if (onEnemy)
        {
            anchor = GetEnemyImpactAnchor(enemyIndex >= 0 ? enemyIndex : currentEnemyTargetIndex);
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

        for (int i = 0; i < 4; i++)
        {
            partySlotsHidden[i] = false;

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

        for (int i = 0; i < 4; i++)
        {
            int idx = pd.activeLineupIndices[i];

            if (idx >= 0 && idx < pd.ownedCharacters.Count)
            {
                var inst = pd.ownedCharacters[idx];

                if (inst != null)
                {
                    GameObject statObj = new GameObject($"PartyMember_{i}_Stats");
                    statObj.transform.SetParent(this.transform);
                    var stats = statObj.AddComponent<CharacterStats>();
                    stats.InitFrom(inst);
                    partyMembers[i] = stats;
                }
                else
                {
                    Debug.LogError($"[BattleController] InitializePartyMembers: Slot {i}: Character instance is NULL!");
                    partyMembers[i] = null;
                }
            }
            else
            {
                partyMembers[i] = null;
            }
        }
    }

    private void InitializePartyMemberDisplays()
    {

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

            if (i < partyPortraitImages.Length && partyPortraitImages[i] != null)
            {
                if (idx >= 0 && idx < pd.ownedCharacters.Count && partyMembers[i] != null)
                {
                    var inst = pd.ownedCharacters[idx];
                    if (inst.data != null && inst.data.silhouetteSprite != null)
                    {
                        partyPortraitImages[i].sprite = inst.data.silhouetteSprite;
                        partyPortraitImages[i].enabled = true;
                        partyPortraitImages[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        Debug.LogWarning($"[BattleController] InitializePartyMemberDisplays: Slot {i}: Character data or sprite is NULL!");
                        partyPortraitImages[i].gameObject.SetActive(false);
                    }
                }
                else
                {
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
            }
            else
            {
                Debug.LogError($"[BattleController] EnsurePartySlotsActive: Slot {i} Image or GameObject is NULL!");
            }
        }
    }

    private void FillPartyUI()
    {

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

        EnsurePartySlotsActive();

        for (int i = 0; i < 4; i++)
        {
            int idx = pd.activeLineupIndices[i];
            var img = partySlotImages[i];

            if (img == null)
            {
                Debug.LogError($"[BattleController] FillPartyUI: Slot {i}: partySlotImages[{i}] is NULL! Assign it in Inspector!");
                continue;
            }

            if (img.gameObject != null && !img.gameObject.activeSelf)
            {
                img.gameObject.SetActive(true);
            }

            img.enabled = true;

            if (idx >= 0 && idx < pd.ownedCharacters.Count && pd.ownedCharacters != null)
            {
                var inst = pd.ownedCharacters[idx];

                if (inst != null && inst.data != null)
                {
                    if (inst.data.silhouetteSprite != null)
                    {
                        img.sprite = inst.data.silhouetteSprite;
                        img.color = partyFilledColor;
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
                img.sprite = null;
                img.color = partyEmptyColor;
            }
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
            }
        }

        for (int i = 0; i < enemyMembers.Length; i++)
        {
            if (enemyMembers[i] != null && !enemyMembers[i].IsDead())
                turnOrder.Add(enemyMembers[i]);
        }

        turnOrder.Sort((a, b) => b.speed.CompareTo(a.speed));
        for (int i = 0; i < turnOrder.Count; i++)
        {
        }
    }

    private void RemoveDeadCharactersFromTurnOrder()
    {
        for (int i = turnOrder.Count - 1; i >= 0; i--)
        {
            if (turnOrder[i] == null || turnOrder[i].IsDead())
            {
                turnOrder.RemoveAt(i);
            }
        }

        if (currentTurnIndex >= turnOrder.Count && turnOrder.Count > 0)
        {
            currentTurnIndex = 0;
        }
    }

    private bool AnyAliveEnemy()
    {
        for (int i = 0; i < enemyMembers.Length; i++)
        {
            if (enemyMembers[i] != null && !enemyMembers[i].IsDead())
                return true;
        }
        return false;
    }

    private int GetEnemyIndex(CharacterStats stats)
    {
        if (stats == null) return -1;
        for (int i = 0; i < enemyMembers.Length; i++)
        {
            if (enemyMembers[i] == stats)
                return i;
        }
        return -1;
    }

    private RectTransform GetEnemyPortraitRect(int index)
    {
        if (enemyPortraitRects != null && index >= 0 && index < enemyPortraitRects.Length && enemyPortraitRects[index] != null)
            return enemyPortraitRects[index];
        return enemyPortraitRect;
    }

    private RectTransform GetEnemyImpactAnchor(int index)
    {
        if (enemyImpactAnchors != null && index >= 0 && index < enemyImpactAnchors.Length && enemyImpactAnchors[index] != null)
            return enemyImpactAnchors[index];
        return enemyImpactAnchor;
    }

    private RectTransform GetEnemyPopupAnchor(int index)
    {
        if (enemyPopupAnchors != null && index >= 0 && index < enemyPopupAnchors.Length && enemyPopupAnchors[index] != null)
            return enemyPopupAnchors[index];
        return enemyPopupAnchor;
    }

    private Slider GetEnemyHpDamageSlider(int index)
    {
        if (enemyHpDamageSliders != null && index >= 0 && index < enemyHpDamageSliders.Length && enemyHpDamageSliders[index] != null)
            return enemyHpDamageSliders[index];
        return enemyHpDamageSlider;
    }

    private void EnsureEnemyTargetValid()
    {
        if (currentEnemyTargetIndex < 0 || currentEnemyTargetIndex >= enemyMembers.Length)
            currentEnemyTargetIndex = 0;

        if (enemyMembers[currentEnemyTargetIndex] != null && !enemyMembers[currentEnemyTargetIndex].IsDead())
        {
            enemy = enemyMembers[currentEnemyTargetIndex];
            return;
        }

        for (int i = 0; i < enemyMembers.Length; i++)
        {
            if (enemyMembers[i] != null && !enemyMembers[i].IsDead())
            {
                currentEnemyTargetIndex = i;
                enemy = enemyMembers[i];
                return;
            }
        }

        enemy = null;
    }

    public void SetEnemyTarget(int index)
    {
        if (index < 0 || index >= enemyMembers.Length) return;
        if (enemyMembers[index] == null || enemyMembers[index].IsDead()) return;
        currentEnemyTargetIndex = index;
        enemy = enemyMembers[index];
        RefreshFocusedEnemyUI();
        UpdateUI();
        UpdateTargetIndicators();
    }

    private void RefreshFocusedEnemyUI()
    {
        EnsureEnemyTargetValid();
        var data = GetCurrentEnemyData();
        if (enemyPortraitImage != null && data != null && data.silhouetteSprite != null)
        {
            enemyPortraitImage.sprite = data.silhouetteSprite;
            enemyPortraitImage.enabled = true;
            enemyPortraitImage.gameObject.SetActive(true);
        }
    }

    private CharacterData GetCurrentEnemyData()
    {
        var gm = GameManager.Instance;
        var floorCfg = gm != null ? gm.GetCurrentTowerFloor() : null;
        if (floorCfg != null && floorCfg.enemies != null && currentEnemyTargetIndex >= 0 && currentEnemyTargetIndex < floorCfg.enemies.Length)
        {
            var d = floorCfg.enemies[currentEnemyTargetIndex];
            if (d != null) return d;
        }
        if (floorCfg != null && floorCfg.enemyData != null) return floorCfg.enemyData;
        return gm != null ? gm.GetCurrentEnemyData() : null;
    }

    private void InitializeEnemies(GameManager gm)
    {
        var floorCfg = gm != null ? gm.GetCurrentTowerFloor() : null;
        int floorNumber = gm != null && gm.playerData != null ? gm.playerData.towerCurrentFloor + 1 : 1;
        bool isBoss = floorCfg != null && floorCfg.isBossFloor;

        // If the scene only has a single enemy actor wired (legacy setup),
        // auto-create additional "logic-only" enemy actors so multi-enemy battles work.
        // These extra actors don't need renderers; UI is driven by arrays (portrait images, hp bars, etc).
        CharacterStats CreateAutoEnemyActor(int index)
        {
            var go = new GameObject($"EnemyActor_{index}");
            // Keep hierarchy tidy near the existing enemy object if possible
            if (enemy != null)
                go.transform.SetParent(enemy.transform.parent, worldPositionStays: false);
            else
                go.transform.SetParent(transform, worldPositionStays: false);
            return go.AddComponent<CharacterStats>();
        }

        for (int i = 0; i < enemyMembers.Length; i++)
        {
            CharacterStats stats = null;
            if (enemyActors != null && i < enemyActors.Length && enemyActors[i] != null)
                stats = enemyActors[i];
            else if (i == 0)
                stats = enemy;
            else if (enemy != null)
            {
                // No actor assigned for this slot; create one so it participates in battle logic.
                stats = CreateAutoEnemyActor(i);
                if (enemyActors != null && i < enemyActors.Length)
                    enemyActors[i] = stats;
                Debug.LogWarning($"[BattleController] InitializeEnemies: enemyActors[{i}] was not assigned; auto-created {stats.name}. Consider wiring Enemy Actors[0..3] in the inspector.");
            }

            enemyMembers[i] = stats;
        }

        var datas = ResolveEnemyDatasForFloor(floorCfg, gm);
        for (int i = 0; i < enemyMembers.Length; i++)
        {
            var stats = enemyMembers[i];
            var data = datas[i];
            if (stats == null)
            {
                enemySlotsHidden[i] = true;
                SetEnemySlotVisible(i, false);
                continue;
            }

            bool active = data != null;
            enemySlotsHidden[i] = !active;
            stats.gameObject.SetActive(active);
            SetEnemySlotVisible(i, active);
            if (!active)
                continue;

            stats.InitFrom(data);
            ScaleEnemyForFloorInstance(stats, floorNumber, isBoss && i == 0);

            if (enemyPortraitImages != null && i < enemyPortraitImages.Length && enemyPortraitImages[i] != null && data.silhouetteSprite != null)
            {
                enemyPortraitImages[i].sprite = data.silhouetteSprite;
                enemyPortraitImages[i].enabled = true;
                enemyPortraitImages[i].gameObject.SetActive(true);
            }
        }

        EnsureEnemyTargetValid();
        RefreshFocusedEnemyUI();
        UpdateUI();
    }

    private CharacterData[] ResolveEnemyDatasForFloor(TowerFloor floorCfg, GameManager gm)
    {
        var result = new CharacterData[4];

        if (floorCfg != null && floorCfg.enemies != null && floorCfg.enemies.Length > 0)
        {
            for (int i = 0; i < 4; i++)
            {
                if (i < floorCfg.enemies.Length)
                    result[i] = floorCfg.enemies[i];
            }
        }

        if (result[0] == null)
        {
            if (floorCfg != null && floorCfg.enemyData != null)
                result[0] = floorCfg.enemyData;
            else if (gm != null)
                result[0] = gm.GetCurrentEnemyData();
        }

        for (int i = 1; i < 4; i++)
        {
            if (result[i] == null)
                result[i] = result[0];
        }

        return result;
    }

    private void ScaleEnemyForFloorInstance(CharacterStats stats, int floorNumber, bool isBoss)
    {
        if (stats == null) return;

        var gm = GameManager.Instance;
        if (gm != null && gm.towerProgression != null)
        {
            gm.towerProgression.ApplyEnemyScaling(stats, floorNumber, isBoss);
            return;
        }

        int t = Mathf.Max(0, floorNumber - 1);
        float hpMul = Mathf.Pow(1.035f, t);
        float atkMul = Mathf.Pow(1.025f, t);
        float defMul = Mathf.Pow(1.02f, t);
        if (isBoss)
        {
            hpMul *= 1.85f;
            atkMul *= 1.45f;
            defMul *= 1.25f;
        }
        stats.maxHP = Mathf.RoundToInt(stats.maxHP * hpMul);
        stats.currentHP = stats.maxHP;
        stats.attack = Mathf.RoundToInt(stats.attack * atkMul);
        stats.defense = Mathf.RoundToInt(stats.defense * defMul);
    }

    private bool CheckBattleEndConditions()
    {
        if (!AnyAliveEnemy())
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
                        string itemPart = "";
                        if (floor.rewardItems != null && floor.rewardItems.Count > 0)
                        {
                            if (floor.rewardItems.Count == 1)
                            {
                                itemPart = $", Item: {floor.rewardItems[0].displayName}";
                            }
                            else
                            {
                                string first = floor.rewardItems[0] != null ? floor.rewardItems[0].displayName : "Item";
                                string second = floor.rewardItems.Count > 1 && floor.rewardItems[1] != null
                                    ? floor.rewardItems[1].displayName
                                    : "Item";
                                string extra = floor.rewardItems.Count > 2 ? $" (+{floor.rewardItems.Count - 2} more)" : "";
                                itemPart = $", Items: {first}, {second}{extra}";
                            }
                        }
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
