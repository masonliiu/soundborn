# Soundborn

**Genre:** Turn-Based RPG  
**Platform:** iOS  
**Language:** C#  
**Engine:** Unity 6.2 (6000.2.15f1)  
**Created by:** [masonliiu](https://github.com/masonliiu)

Soundborn is a turn-based RPG set in a universe where musical genres manifest as living characters to fight against their oppressors: The Silence.

---

## Table of Contents

- [Current Status](#current-status)
- [Features](#features)
- [Core Gameplay](#core-gameplay)
- [Getting Started](#getting-started)
- [Technical Documentation](#technical-documentation)
- [Project Structure](#project-structure)
- [Development Roadmap](#development-roadmap)
- [Contributing](#contributing)
- [License](#license)

---

## Current Status

The game is in **active development** with core systems completed:

**Completed Systems:**
- 4 original characters with unique ability sets
- Character collection and gacha system
- Party lineup management (4-member teams)
- Turn-based battle system with speed-based turn order
- Multi-enemy combat (4 enemies simultaneously)
- Intelligent target selection system
- Character progression and leveling
- Element-based combat mechanics with advantage/disadvantage
- Status effects system (DoT, Stun, Sleep, Defense Up)
- Ability cooldowns and resource management
- Tower progression system
- Save/load system with persistent player data

**Currently In Development:**
- Equipment and item systems
- Additional character abilities and synergies
- Boss encounters with unique mechanics
- Audio integration with genre-specific sound design
- Expanded character roster

---

## Features

### Battle System Highlights

- **Multi-Enemy Combat:** Battle up to 4 enemies simultaneously with per-enemy HP tracking
- **Smart Targeting:** Auto-selects lowest HP enemy; tap to switch targets or confirm attack
- **Visual Feedback:** Rotating target indicators, pixelated death effects, damage popups, and impact animations
- **Auto-Setup:** Ability panel auto-opens with best available ability selected on player turn
- **Turn-Based Strategy:** Speed-based turn order creates dynamic combat scenarios

### Character Progression

- Level up characters using soft currency
- Character stats scale with floor progression
- Tower-based difficulty scaling

---

## Core Gameplay

### Character Collection

Players collect Soundborn characters through a gacha system. Each character has:
- Unique stats (HP, Attack, Defense, Speed, Crit Chance)
- One of six musical element types
- Three abilities: Basic Attack, Skill (with cooldown), and Ultimate (with longer cooldown)
- Level progression system

### Team Building

Assemble a lineup of exactly 4 characters before battle. The lineup system allows strategic team composition based on:
- Element types and synergies
- Role distribution
- Stat balance

### Battle System

Combat uses a **speed-based turn order** where all characters (party members and enemies) act in order of their speed stat. The fastest character acts first, followed by the second fastest, and so on.

#### Combat Actions

- **Basic Attack:** Standard damage-dealing ability (no cooldown)
- **Skill:** Elemental ability with cooldown that may apply status effects
  - Bass/Noise: Apply "Bleed Ears" (damage over time)
  - Harmony/Melody: Apply "Sleep" (skip next turn)
  - Percussion/Synth: Apply "Stun" (skip next turn)
- **Ultimate:** Powerful ability with longer cooldown that provides significant damage and grants "Defense Up" buff

#### Status Effects

- **Bleed Ears:** Damage over time (deals damage at start of each turn)
- **Stun:** Skip next turn (applied by sharp strikes)
- **Sleep:** Skip next turn (applied by calming melodies)
- **Defense Up:** Temporary defense buff (lasts multiple turns)

#### Targeting System

When you press an ability button:
1. Ability panel slides in showing ability details
2. Lowest HP enemy is automatically selected as target
3. Rotating indicator appears around the selected enemy
4. Tap a different enemy to switch targets
5. Tap the selected enemy (or press the ability button again) to execute the attack

#### Multi-Enemy Mechanics

- All 4 enemy slots are tracked independently in the battle engine
- Each enemy has individual HP bars, portraits, and status indicators
- Victory only triggers when **all** enemies are defeated
- Turn order includes all alive enemies (not just the first one)

---

## Character System

### Element Types

Characters belong to one of six musical element types:

- **Bass:** Power and heaviness
- **Percussion:** Rhythm and speed
- **Harmony:** Support and progressions
- **Noise:** Chaos and distortion
- **Melody:** Hooks and leads
- **Synth:** Electronic and modulation

### Elemental Affinities

Elements have a circular advantage/disadvantage relationship:

```
Bass > Synth > Harmony > Noise > Melody > Percussion > Bass
```

- **Advantage:** +25% damage dealt
- **Disadvantage:** -25% damage dealt

### Character Stats

Each character has the following stats:

- **HP:** Health points (determines survivability)
- **Attack:** Base damage output (affects all abilities)
- **Defense:** Damage reduction (subtracted from incoming damage)
- **Speed:** Determines turn order (higher = acts sooner)
- **Crit Chance:** Probability of critical hits (default: 10%)
- **Crit Damage Multiplier:** Damage multiplier on crits (default: 1.5x)
- **Skill Power:** Additional flat damage for skills
- **Ultimate Power:** Additional flat damage for ultimates

---

## Progression Systems

### Character Leveling

Characters gain experience and can be leveled up using soft currency. Each level increases:
- HP: +25 per level
- Attack: +5 per level
- Defense: +3 per level

### Tower Progression

The tower system provides:
- Floor-based difficulty scaling (HP, Attack, Defense scale per floor)
- Boss floors with increased multipliers
- Progressive rewards

### Currency

- **Soft Currency:** Earned through gameplay, used for character leveling
- **Premium Currency:** Obtained through quests, events, and purchases. Used for gacha pulls

---

## Getting Started

### Requirements

- **Unity Version:** 6.2 (6000.2.15f1) or later
- **Platform:** iOS (Android support planned)
- **Input System:** Unity Input System Package (new input system)
- **Dependencies:**
  - TextMesh Pro
  - Universal Render Pipeline (URP)

### Setup Instructions

1. **Clone the repository**
   ```bash
   git clone https://github.com/masonliiu/Soundborn.git
   cd Soundborn
   ```

2. **Open in Unity**
   - Launch Unity Hub
   - Add the project folder
   - Open the project (Unity will import assets)

3. **Verify Project Settings**
   - Ensure Input System Package is installed (Window → Package Manager)
   - Verify URP is configured (Graphics settings should use URP asset)
   - Check that TextMesh Pro assets are imported

4. **Run the Game**
   - Open `HomeScene` from Assets/Scenes/
   - Press Play in the Unity Editor
   - Or build for iOS: File → Build Settings → iOS → Build

### First Steps

1. **HomeScene** is the main hub where you can:
   - View your character collection
   - Manage your party lineup
   - Access the gacha system
   - Start battles

2. **Starting a Battle:**
   - Select your 4-character lineup
   - Click "Climb Tower" to start a battle
   - The battle system will auto-setup your turn with the best ability selected

3. **Testing the Battle System:**
   - The battle scene supports 4 enemies simultaneously
   - Try different abilities and observe status effects
   - Test target selection by tapping different enemies

---

## Technical Documentation

### Battle System Architecture

The battle system uses a unified turn order queue that contains all active characters sorted by speed.

#### Turn Processing Flow

```
1. ProcessNextTurn()
   ├── RemoveDeadCharactersFromTurnOrder()
   ├── CheckBattleEndConditions()
   ├── Get current actor from turnOrder[currentTurnIndex]
   └── Route to:
       ├── StartPlayerControlledTurn() (if player character)
       └── StartEnemyTurn() (if enemy)
```

#### Player Turn Flow

```
StartPlayerControlledTurn()
├── Tick cooldowns and status effects
├── Check for skip turn (stun/sleep)
├── SlideAbilityPanelIn()
├── BeginTargetSelection(GetBestAvailableAbilityForActor())
│   ├── AutoSelectLowestHpEnemy()
│   ├── ShowAbilityCard()
│   └── UpdateTargetIndicators()
└── Wait for player input:
    ├── Tap enemy → SetEnemyTarget() or ConfirmTargetSelectionAndExecute()
    └── Tap ability button → ConfirmTargetSelectionAndExecute()
```

#### Key Classes

- **BattleController.cs:** Main battle logic, turn order management, combat calculations
- **CharacterStats.cs:** Character data structure, stat calculations, status effects
- **GameManager.cs:** Singleton managing game state, player data, save/load
- **BattleLineupController.cs:** Team selection UI and lineup management

### Multi-Enemy Implementation

The battle system tracks up to 4 enemies using arrays:

```csharp
private CharacterStats[] enemyMembers = new CharacterStats[4];
public CharacterStats[] enemyActors = new CharacterStats[4];  // inspector-assigned
```

**UI Arrays (per-enemy):**
- `enemyPortraitImages[4]` - Character portraits
- `enemyHpTexts[4]` - HP text displays
- `enemyHpSliders[4]` - HP bar sliders
- `enemyTargetIndicators[4]` - Rotating target selection indicators
- `enemyPortraitRects[4]` - RectTransforms for click detection

**Auto-Initialization:**
If `enemyActors` aren't assigned in the inspector, the system auto-creates "logic-only" enemy actors at runtime to ensure all 4 slots work properly.

### Target Selection System

The targeting system uses Unity's Input System (new) with fallback to legacy input:

1. **Detection:** `GetEnemyIndexAtScreenPoint()` uses `RectTransformUtility.RectangleContainsScreenPoint()` to detect taps on enemy UI elements
2. **Selection:** `SetEnemyTarget(index)` updates `currentEnemyTargetIndex`
3. **Visual Feedback:** Only the selected enemy's indicator rotates; others remain static
4. **Execution:** Tapping the selected enemy or pressing the ability button again calls `ConfirmTargetSelectionAndExecute()`

### Save System

Player data is persisted using JSON serialization:
- Location: Application.persistentDataPath
- Auto-saves after significant events (battle completion, leveling, etc.)
- Loads on GameManager.Awake()

---

## Project Structure

```
Soundborn/
├── Assets/
│   ├── Scripts/
│   │   ├── BattleController.cs          # Main battle logic, turn order, combat
│   │   ├── BattleLineupController.cs    # Team selection UI
│   │   ├── CharacterStats.cs            # Character combat data and calculations
│   │   ├── GameManager.cs               # Singleton game state manager
│   │   ├── PlayerData.cs                # Player progression data structure
│   │   ├── SaveSystem.cs                # Save/load persistence
│   │   ├── TowerProgression.cs          # Tower floor management
│   │   └── [Additional systems]
│   ├── Scenes/
│   │   ├── HomeScene.unity              # Main hub (character management)
│   │   ├── GachaScene.unity             # Character summoning
│   │   └── BattleScene.unity            # Combat encounters
│   ├── Configs/
│   │   ├── CharacterDatabase.asset      # Character data configuration
│   │   ├── ItemDatabase.asset           # Item data configuration
│   │   └── Tower.asset                  # Tower floor configuration
│   ├── Prefabs/                         # Reusable GameObject prefabs
│   ├── Sprites/                         # Character and UI sprites
│   └── Settings/                        # Unity project settings
├── ProjectSettings/                     # Unity project configuration
└── README.md                            # This file
```

### Key Scripts Overview

**BattleController.cs** (game engine made from scratch; 2600+ lines)
- Manages entire battle flow
- Turn order calculation and processing
- Combat damage calculations with element multipliers
- Status effect tick processing
- UI updates for all characters
- Death effects and animations

**CharacterStats.cs**
- Character stat structure
- Damage calculation formulas
- Element advantage calculations
- Status effect application and ticking
- Cooldown management

**GameManager.cs**
- Singleton pattern implementation
- Player data initialization and loading
- Tower floor progression tracking
- Event system for data changes

---

## Development Roadmap

### Planned Features (By January 15th, 2026)

- [ ] Equipment and item systems
- [ ] Additional character abilities and synergies
- [ ] Boss encounters with unique mechanics
- [ ] Audio integration with genre-specific sound design
- [ ] Expanded character roster (8+ characters)
- [ ] Progression rewards and achievements
- [ ] Daily quests and events
- [ ] Character skins/costumes

### Known Issues / Future Improvements

- Consider adding visual feedback for elemental advantages in combat
- Optimize battle UI for different screen sizes
- Add battle replay/auto-battle features
- Implement skill previews before execution

---

## Contributing

Contributions are welcome! Here's how you can help:

### Reporting Issues

1. Check if the issue already exists in the issues tab
2. Create a new issue with:
   - Clear description of the problem
   - Steps to reproduce
   - Expected vs. actual behavior
   - Unity version and platform

### Contributing Code

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Make your changes following the existing code style
4. Test thoroughly in Unity Editor
5. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
6. Push to the branch (`git push origin feature/AmazingFeature`)
7. Open a Pull Request

### Code Style Guidelines

- Follow C# naming conventions (PascalCase for classes, camelCase for variables)
- Add XML comments for public methods
- Use meaningful variable names
- Keep methods focused and under 100 lines when possible
- Add debug logs for important state changes (use `[Conditional("UNITY_EDITOR")]` if performance-sensitive)

### Areas for Contribution

- **Bug Fixes:** Help squash bugs in the issue tracker
- **UI/UX Improvements:** Enhance the battle interface or menus
- **New Features:** Implement features from the roadmap
- **Documentation:** Improve code comments or add tutorials
- **Testing:** Add unit tests or integration tests
- **Optimization:** Performance improvements or code refactoring

---

## Usage Examples

### Creating a New Character

1. Open `CharacterDatabase.asset` in the Unity Inspector
2. Add a new entry with:
   - Display name
   - Element type
   - Base stats (HP, Attack, Defense, Speed)
   - Ability power values
   - Portrait sprite

### Modifying Battle Difficulty

Edit `Tower.asset` to adjust:
- Floor scaling multipliers
- Enemy configurations per floor
- Boss floor flags
- Floor rewards

### Adding a New Status Effect

1. Add enum value to `StatusType` in `CharacterStats.cs`
2. Add handling in `ApplyStatus()` and `TickStatusAtTurnStart()`
3. Update UI color mapping in `GetStatusColor()`
4. Add visual effect if needed

---

## License

Copyright © 2025 masonliiu. All rights reserved.

This project is not open source. All rights reserved.

---

## Credits

- **Developer:** masonliiu
- **Engine:** Unity 6.2
- **Art Assets:** Original work
- **Inspiration:** Musical genre theming and turn-based RPG mechanics

---

## Contact & Feedback

- **GitHub:** [@masonliiu](https://github.com/masonliiu)
- **Issues:** Use GitHub Issues for bug reports and feature requests

Feedback is welcome! If you find bugs, have suggestions, or want to contribute, please open an issue or pull request.
