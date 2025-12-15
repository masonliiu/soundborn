# Soundborn
**Genre:** Turn-Based RPG

**Platform:** iOS

**Language (primarily):** C#

**Engine:** Unity 2D

**Created by:** [masonliiu](https://github.com/masonliiu)

Soundborn is a turn-based RPG set in a universe where musical genres manifest as living characters in order to fight against their oppresors: the Silence.


## Current Status

The game is in active development with such systems completed already:

- 4 completed and original characters with their own ability-sets
- Character collection and gacha system
- Party lineup management (4-member teams)
- Turn-based battle system with speed-based turn order
- Character progression and leveling
- Element-based combat mechanics
- Status effects and ability cooldowns


## Core Gameplay

### Character Collection
Players collect Soundborn characters through a gacha system. Each character has unique stats, element types, and abilities. Characters can be leveled up using soft currency earned through gameplay.

### Team Building
Players assemble a lineup of up to 4 characters before battle. The lineup system allows strategic team composition based on element types, roles, and synergies.

### Battle System
Combat uses a speed-based turn order where all characters (party members and enemies) act in order of their speed stat. The fastest character acts first, followed by the second fastest, and so on. This creates dynamic combat where turn order can shift based on character speeds.

**Combat Actions:**
- Basic Attack: Standard damage dealing ability
- Skill: Elemental ability with cooldown that may apply status effects
- Ultimate: Powerful ability with longer cooldown that provides significant damage or buffs

**Status Effects (More to come):**
- Bleed Ears: Damage over time
- Stun: Skip next turn
- Sleep: Skip next turn (applied by calm melodies)
- Defense Up: Temporary defense buff
  

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
Elements have advantage/disadvantage relationships:

- Bass > Synth
- Synth > Harmony
- Harmony > Noise
- Noise > Melody
- Melody > Percussion
- Percussion > Bass

Elemental advantages provide 25% damage bonus, while disadvantages reduce damage by 25%.

### Character Stats
Each character has:
- **HP:** Health points
- **Attack:** Base damage output
- **Defense:** Damage reduction
- **Speed:** Determines turn order
- **Crit Chance:** Probability of critical hits
- **Skill Power:** Additional damage for skills
- **Ultimate Power:** Additional damage for ultimates


## Progression Systems

### Character Leveling
Characters gain experience and can be leveled up using soft currency. Each level increases HP, attack, and defense stats.

### Currency
- **Soft Currency:** Earned through gameplay, used for character leveling
- **Premium Currency:** Can be obtained through quests, events, and purchases. Used for gacha pulls to obtain new characters


## Technical Implementation

### Battle System Architecture
The battle system uses a unified turn order queue that contains all active characters sorted by speed. Turn processing is handled through a state machine that:
- Processes each character's turn in speed order
- Handles status effects and cooldowns
- Manages character death and removal from turn order
- Checks battle end conditions every turn

### Scene Structure
- **HomeScene:** Character management, team selection, and navigation
- **GachaScene:** Character summoning interface
- **BattleScene:** Complex battle engine displaying combat encounters with varying lineup selection and battle execution


## Development Roadmap

### Planned Features
- Tower/dungeon progression system
- Equipment and item systems
- Additional character abilities and synergies
- Boss encounters with unique mechanics
- Audio integration with genre-specific sound design
- Expanded character roster
- Progression rewards and achievements
- **All to be completed by January 1st, 2026**


## Project Structure

```
Assets/
├── Scripts/
│   ├── BattleController.cs          # Main battle logic and turn order
│   ├── BattleLineupController.cs    # Team selection and lineup management
│   ├── GameManager.cs               # Core game state management
│   ├── PlayerData.cs                # Player progression data
│   ├── CharacterStats.cs            # Character combat statistics
│   └── [Additional systems]
├── Scenes/
│   ├── HomeScene.unity              # Main hub
│   ├── GachaScene.unity             # Character summoning
│   └── BattleScene.unity            # Combat encounters
└── [Assets, Prefabs, Settings]
```


## Getting Started

### Requirements
- Unity 2021.3 LTS or later
- iOS development environment (for iOS builds)

### Setup
1. Clone the repository
2. Open the project in Unity
3. Open HomeScene to begin testing
4. Configure character data assets in the GameManager
5. Build and run on target platform


## Development Notes

This project is built from scratch using Unity 2D and C#. All code and game design are original work. The battle system architecture is designed to be extensible for future features including equipment, additional status effects, and more complex ability interactions.




## License

Copyright© 2025 masonliiu. All rights reserved.
