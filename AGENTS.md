# Soundborn Agent Guide

## Project Purpose
Soundborn is a Unity 6.2 (6000.2.15f1) iOS turn-based RPG where musical genres are personified as characters battling “The Silence.” Core loops include team building, gacha collection, tower progression, and tactical, speed-based combat.

## Intended Final Product
- A polished, iOS-first RPG featuring an expanded roster (8+ characters) with genre-themed audio integration.
- Full equipment and item systems that deepen team strategy.
- Boss encounters with unique mechanics and presentation.
- Progression features such as quests, rewards, and cosmetic/skin unlocks.

## Current State (as implemented)
- **Combat:** Speed-based turn order, 4-enemy battles, status effects, element advantage, target selection, and battle UI (`Assets/Scripts/BattleController.cs`).
- **Player Data:** Player profile, currencies, tower floor tracking, and onboarding flags (`Assets/Scripts/PlayerData.cs`).
- **Persistence:** Save/load via PlayerPrefs JSON (`Assets/Scripts/SaveSystem.cs`).
- **Collection:** Gacha pulls, character catalog, and upgrades (`Assets/Scripts/GachaController.cs`, `Assets/Scripts/CharacterCatalogPanel.cs`, `Assets/Scripts/UpgradePanel.cs`).
- **Tower:** Config-based or procedural floor generation with rewards (`Assets/Scripts/TowerProgression.cs`, `Assets/Scripts/TowerConfig.cs`).
- **Quests:** Basic quest system with UI and claim flow (`Assets/Scripts/QuestManager.cs`, `Assets/Scripts/QuestPanel.cs`).
- **Settings & Audio:** PlayerPrefs-backed settings and audio routing (`Assets/Scripts/SettingsManager.cs`, `Assets/Scripts/AudioManager.cs`).
- **Items/Inventory:** Item data + inventory UI exists; equipment flow is still minimal (`Assets/Scripts/ItemData.cs`, `Assets/Scripts/InventoryPanel.cs`).

## Key Systems and Data
- **Singletons:** `GameManager`, `QuestManager`, `SettingsManager`, `AudioManager`.
- **ScriptableObject data:** `CharacterData`, `ItemData`, `QuestData`, `TowerConfig`.
- **Scenes:** `HomeScene`, `GachaScene`, `BattleScene` (see `Assets/Scenes`).

## Unity Workflow After Adding Things
Use these steps every time you add or change a system.

### Adding or Modifying Scripts
- **Unity follow-up:** Add the component to the correct prefab or scene object, then wire all serialized fields in the Inspector (especially arrays in `BattleController` or UI panels).

### Adding New Characters (CharacterData)
- **Unity follow-up:** Create a new `CharacterData` asset in `Assets/Configs`, assign sprites/icons, then register it in `CharacterDatabase.asset` and, if needed, add it to `GachaController.gachaPool` or tower configs.

### Adding New Items (ItemData)
- **Unity follow-up:** Create `ItemData` assets in `Assets/Configs` or `Assets/Items`, add to `ItemDatabase.asset`, then verify inventory UI renders the icon and lock status.

### Adding or Tuning Quests (QuestData)
- **Unity follow-up:** Create/edit `QuestData` assets and assign them to `QuestManager.initialQuests` in the scene; confirm the quest list updates in play mode.

### Adding Tower Floors or Enemies
- **Unity follow-up:** Edit `Tower.asset` / `TowerConfig` floors or set `TowerProgression` defaults in the scene, then run a battle to verify enemy lineups and rewards.

### Adding Battle Abilities or Status Effects
- **Unity follow-up:** Update any UI tooltips/ability cards in the battle scene and ensure new effects display on status icons; test in `BattleScene` with a controlled lineup.

### Adding UI Panels or Navigation
- **Unity follow-up:** Ensure panel roots are hooked into `HomeUIController` or scene buttons, then validate open/close flows in play mode.

### Adding Audio Clips or Settings
- **Unity follow-up:** Drop new clips onto `AudioManager` in the scene, then adjust sliders/toggles in the Settings panel to confirm routing and volume behavior.

## Conventions
- Follow C# conventions (PascalCase classes, camelCase fields).
- Prefer ScriptableObjects for tunable data, and keep scene dependencies explicit in the Inspector.
- Use `GameManager.NotifyPlayerDataChanged()` after mutating player state, and save via `SaveSystem.Save()` or `GameManager.SavePlayerData()`.
