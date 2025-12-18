# Soundborn Architecture Documentation

This document provides an in-depth look at the technical architecture of Soundborn, intended for developers working on the codebase.

## Table of Contents

- [System Overview](#system-overview)
- [Core Systems](#core-systems)
- [Battle System Architecture](#battle-system-architecture)
- [Data Flow](#data-flow)
- [Design Patterns](#design-patterns)
- [Key Components](#key-components)

## System Overview

Soundborn follows a **component-based architecture** using Unity's MonoBehaviour system, with clear separation of concerns:

- **GameManager:** Singleton managing global game state
- **BattleController:** Central battle orchestrator
- **Data Layer:** ScriptableObjects and serialized data structures
- **UI Layer:** Separate controllers for each scene/panel

## Core Systems

### Game State Management

**GameManager (Singleton Pattern)**
- Lives across scenes via `DontDestroyOnLoad`
- Manages `PlayerData` instance
- Handles save/load via `SaveSystem`
- Provides access to configuration assets (CharacterDatabase, TowerConfig)

**PlayerData**
- Serialized JSON structure
- Contains: owned characters, inventory, currency, progression data
- Persisted to `Application.persistentDataPath`

### Character System

**CharacterData (ScriptableObject)**
- Base character definition (stats, abilities, sprites)
- Created in Unity Inspector
- Stored in `CharacterDatabase.asset`

**CharacterInstance**
- Runtime character with level and ownership
- Contains reference to `CharacterData`
- Tracks level and experience

**CharacterStats (MonoBehaviour)**
- Runtime combat representation
- Instantiated per battle
- Handles stat calculations, status effects, cooldowns

## Battle System Architecture

### Turn Order System

The battle system uses a **unified turn order queue** that contains all active characters sorted by speed:

```csharp
private List<CharacterStats> turnOrder = new List<CharacterStats>();
private int currentTurnIndex = 0;
```

**Flow:**
1. `BuildTurnOrder()` - Collects all alive characters, sorts by speed
2. `ProcessNextTurn()` - Gets current actor from queue
3. Routes to `StartPlayerControlledTurn()` or `StartEnemyTurn()`
4. `AdvanceTurn()` - Increments index, wraps around

### Multi-Enemy Tracking

The system supports 4 enemies simultaneously using arrays:

```csharp
private CharacterStats[] enemyMembers = new CharacterStats[4];  // runtime tracking
public CharacterStats[] enemyActors = new CharacterStats[4];    // inspector assignment
```

**Initialization:**
- `InitializeEnemies()` attempts to use `enemyActors` from inspector
- Falls back to auto-creating GameObjects if not assigned
- Each enemy slot is independent with its own:
  - CharacterStats instance
  - UI elements (portrait, HP bar, status icon)
  - Target indicator

**UI Arrays:**
All enemy UI elements are arrays indexed by enemy slot:
- `enemyPortraitImages[4]`
- `enemyHpTexts[4]`
- `enemyHpSliders[4]`
- `enemyTargetIndicators[4]`
- `enemyPortraitRects[4]`

### Combat Calculation

**Damage Formula:**
```csharp
int raw = attack - target.defense;
float scaled = (raw * multiplier) + flatBonus;
scaled *= elementMultiplier;  // 1.25x advantage, 0.75x disadvantage
if (crit) scaled *= critDamageMultiplier;
int finalDamage = Mathf.RoundToInt(scaled);
```

**Element Multiplier:**
- Calculated in `CalculateElementMultiplier()`
- Checks circular advantage chain
- Returns 1.25f (advantage), 0.75f (disadvantage), or 1.0f (neutral)

### Target Selection System

**Input Handling:**
- Uses Unity Input System (new) with legacy fallback
- `TryGetPointerDown()` detects taps/clicks
- `GetEnemyIndexAtScreenPoint()` uses `RectTransformUtility.RectangleContainsScreenPoint()` to detect which enemy UI was tapped

**Selection Flow:**
1. `BeginTargetSelection(ability)` - Enters target selection mode
2. `AutoSelectLowestHpEnemy()` - Finds and selects lowest HP enemy
3. `UpdateTargetIndicators()` - Shows rotating indicator on selected enemy
4. Player can:
   - Tap different enemy → `SetEnemyTarget(index)` switches selection
   - Tap selected enemy → `ConfirmTargetSelectionAndExecute()` fires attack
   - Press ability button again → `ConfirmTargetSelectionAndExecute()` fires attack

**Visual Feedback:**
- Only `currentEnemyTargetIndex`'s indicator rotates
- Other indicators remain visible but static
- Indicator rotation handled in `Update()` during `isSelectingTarget`

### Status Effect System

**Application:**
- Status applied via `CharacterStats.ApplyStatus(statusType, duration)`
- Status stored in `currentStatus` and `statusDurationTurns`
- Defense Up modifies defense stat immediately

**Tick Processing:**
- `TickStatusAtTurnStart()` called at start of each character's turn
- Returns whether turn should be skipped (stun/sleep)
- Outputs damage amount (for DoT effects)
- Decrements duration, clears when expired

**Status Types:**
- **BleedEars:** DoT (damage at turn start)
- **Stun/Sleep:** Skip turn
- **DefenseUp:** Temporary stat buff

### Death Effects

**Enemy Death:**
- `EnemyDeathPixelateRoutine(enemyIndex)` - Pixelates specific enemy portrait
- Creates material instance per enemy
- Animates `_PixelAmount` shader parameter from 0 to 1
- Disables image when complete

**Party Death:**
- `PartyMemberDeathPixelateRoutine(partyIndex)` - Same process for party members
- Uses `partyPortraitImages[partyIndex]`

### UI Update System

**UpdateUI()** - Called frequently to refresh UI:
- Loops through all party members, updates HP text/bars
- Loops through all enemies, updates HP text/bars
- Updates status icons
- Updates ability button states (cooldowns)

**Per-Character UI Helpers:**
- `GetEnemyHpDamageSlider(index)` - Returns slider for specific enemy
- `GetEnemyPortraitRect(index)` - Returns RectTransform for click detection
- `GetPartyMemberHpDamageSlider(index)` - Party member equivalent

## Data Flow

### Battle Initialization

```
GameManager.Awake()
  → SaveSystem.TryLoad()
  → InitializeNewPlayerData() (if needed)

BattleLineupController.ConfirmAndStartBattle()
  → BattleController.StartBattleNow()
    → InitializePartyMembers()
    → InitializeEnemies()
    → BuildTurnOrder()
    → ProcessNextTurn()
```

### Turn Execution

```
ProcessNextTurn()
  → StartPlayerControlledTurn()
    → TickCooldowns()
    → TickStatusAtTurnStart()
    → SlideAbilityPanelIn()
    → BeginTargetSelection(bestAbility)
      → AutoSelectLowestHpEnemy()
      → ShowAbilityCard()
      → UpdateTargetIndicators()

Player Input
  → ConfirmTargetSelectionAndExecute()
    → PlayerBasicAttackRoutine(index) / PlayerSkillRoutine(index) / etc.
      → CalculateDamageAgainst()
      → TakeDamage()
      → SpawnImpact() / SpawnDamagePopup()
      → AnimateHpBar()
      → EnemyDeathPixelateRoutine() (if dead)
      → RemoveDeadCharactersFromTurnOrder()
      → CheckBattleEndConditions()
      → EndPlayerTurn()
        → AdvanceTurn()
          → ProcessNextTurn()
```

### Save/Load Flow

```
GameManager.SavePlayerData()
  → SaveSystem.Save(playerData, characterDatabase, itemDatabase)
    → Serialize to JSON
    → Write to Application.persistentDataPath

GameManager.Awake()
  → SaveSystem.TryLoad()
    → Read JSON from disk
    → Deserialize
    → Restore references to CharacterData/ItemData from databases
```

## Design Patterns

### Singleton Pattern

**GameManager:**
```csharp
public static GameManager Instance { get; private set; }
private void Awake() {
    if (Instance != null && Instance != this) Destroy(gameObject);
    Instance = this;
    DontDestroyOnLoad(gameObject);
}
```

Used for:
- Global game state access
- Cross-scene data persistence

### Component Pattern

**CharacterStats as MonoBehaviour:**
- Each character is a GameObject with CharacterStats component
- Allows per-character coroutines (status ticks, cooldowns)
- Easy to attach to UI elements or world objects

### Observer Pattern

**GameManager Events:**
```csharp
public event Action OnPlayerDataChanged;
```

Used to notify UI when data changes without tight coupling.

### Strategy Pattern

**Ability Execution:**
- Different ability types (Basic/Skill/Ultimate) handled by separate coroutines
- Polymorphic damage calculations via `CalculateDamageAgainst()`
- Status application varies by element type

## Key Components

### BattleController.cs

**Responsibilities:**
- Turn order management
- Combat calculations
- UI updates
- Animation/effect coordination
- Battle state management

**Key Methods:**
- `StartBattleNow()` - Initialize battle
- `ProcessNextTurn()` - Turn processing entry point
- `BuildTurnOrder()` - Sort characters by speed
- `BeginTargetSelection()` - Enter targeting mode
- `ConfirmTargetSelectionAndExecute()` - Execute selected ability
- `UpdateUI()` - Refresh all UI elements

**Size:** ~2600 lines (complex but well-organized)

### CharacterStats.cs

**Responsibilities:**
- Stat storage and calculation
- Damage calculation formulas
- Status effect management
- Cooldown tracking
- Element calculations

**Key Methods:**
- `CalculateDamageAgainst()` - Damage formula with element/crit
- `ApplyStatus()` - Apply status effect
- `TickStatusAtTurnStart()` - Process status at turn start
- `InitFrom(CharacterData)` - Initialize from ScriptableObject

### GameManager.cs

**Responsibilities:**
- Singleton instance management
- Player data persistence
- Configuration asset references
- Cross-system coordination

**Key Methods:**
- `SavePlayerData()` - Persist player data
- `GetCurrentTowerFloor()` - Get current floor config
- `GetCurrentEnemyData()` - Get enemy for current floor

### SaveSystem.cs

**Responsibilities:**
- JSON serialization/deserialization
- File I/O operations
- Data validation

**Format:**
- JSON files in `Application.persistentDataPath`
- Stores character/item IDs, restores references from databases

## Extension Points

### Adding New Status Effects

1. Add enum value to `StatusType` in `CharacterStats.cs`
2. Handle in `ApplyStatus()` (apply immediate effects)
3. Handle in `TickStatusAtTurnStart()` (turn processing)
4. Add color to `GetStatusColor()` in BattleController
5. Update UI status icon display

### Adding New Abilities

1. Create new coroutine: `PlayerNewAbilityRoutine(int enemyIndex)`
2. Add button to UI
3. Create `OnNewAbilityPressed()` method
4. Wire button to method
5. Add ability card description in `ShowAbilityCard()`

### Adding New Elements

1. Add enum value to `ElementType`
2. Update `CalculateElementMultiplier()` with new relationships
3. Add element colors if needed
4. Update ability descriptions

### Adding New Character Stats

1. Add field to `CharacterData` (ScriptableObject)
2. Add field to `CharacterStats`
3. Copy in `InitFrom()`
4. Use in damage calculations or other systems

## Performance Considerations

### Current Optimizations

- UI arrays avoid `FindObjectOfType` calls
- Cached references to frequently accessed components
- Coroutines for async operations (no blocking)
- Material pooling for death effects (could be improved)

### Potential Improvements

- Object pooling for damage popups and impact effects
- Batch UI updates (update less frequently)
- Material instance pooling for death effects
- Consider using Unity's Job System for turn order sorting (if battle gets large)

## Testing Strategy

### Manual Testing Checklist

1. **Battle Flow:**
   - [ ] Turn order correct for all speeds
   - [ ] All 4 enemies tracked correctly
   - [ ] HP bars update for all characters
   - [ ] Status effects apply and tick
   - [ ] Death effects play correctly

2. **Target Selection:**
   - [ ] Auto-selects lowest HP enemy
   - [ ] Tap to switch targets works
   - [ ] Tap selected enemy executes attack
   - [ ] Indicators rotate only for selected enemy

3. **Edge Cases:**
   - [ ] Battle with 1 enemy
   - [ ] Battle with all party members dead
   - [ ] Status effect expires mid-turn
   - [ ] Multiple enemies die in same turn

This architecture supports the current feature set while remaining extensible for future additions.

