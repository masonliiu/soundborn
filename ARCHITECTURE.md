# Architecture

WIP notes only.

- `GameManager` owns runtime player state and save/load.
- `BattleController` runs battle setup, turns, targeting, rewards, and battle UI.
- `PlayerData` stores currencies, roster, lineup, inventory, quests, and tower progress.
- `CharacterData`, `ItemData`, `QuestData`, and `TowerConfig` are ScriptableObject data.
- `HomeScene`, `BattleScene`, and `GachaScene` are the current playable scenes.

Keep new systems small, inspector-wired, and backed by ScriptableObjects when the values need tuning.
