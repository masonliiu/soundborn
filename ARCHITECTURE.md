# Soundborn Architecture

Soundborn is a mobile turn-based RPG built around two progression loops:

- Main Tower: push floors, beat harder enemies, unlock systems, earn first-clear rewards.
- Trials: repeatable farming stages for level-cap materials, equipment, and steady resources.

The goal is simple: fight, earn resources, upgrade characters/equipment, push farther.

## Design Pillars

Every system should support at least one pillar:

```text
Clear progress: the player always knows what to do next.
Meaningful upgrades: every reward moves a character, item, or unlock forward.
Team expression: characters and equipment should create different team choices.
Controlled randomness: drops and cases can be exciting, but rates and costs must be clear.
Fast sessions: one battle should be useful even when the player only has a few minutes.
```

If a feature does not support these pillars, do not add it yet.

## Core Loop

```text
Enter Tower
Fight floor
Earn Notes, Character EXP, items, and milestone rewards
Upgrade characters/equipment
Push higher floors
Unlock harder Trials and better rewards
```

## Farming Loop

```text
Enter Trial
Farm Notes, Character EXP, Resonance Materials, or equipment
Use farmed rewards to break level caps or improve builds
Return to Tower stronger
```

Trials should feel separate from the Tower. Tower is progression. Trials are repeatable farming.

## Gacha Loop

```text
Earn or buy Premium Currency
Open cases
Unlock characters or receive duplicate shards
Use roster/shards to build stronger teams
Push Tower and Trials
```

Cases guarantee a character result, so they should not be easy to spam.

```text
1 case: 400 Premium Currency
10 cases: 3600 Premium Currency
```

Internal rarity:

```text
Standard: blue
Rare: purple
Legendary: gold
```

The UI does not need to show the words everywhere. Color, glow, sound, and reveal animation should communicate rarity.

Starting rates:

```text
Standard: 75%
Rare: 22%
Legendary: 3%
```

Duplicates become character shards.

## Currencies

```text
Notes
```

Default soft currency. Used for equipment upgrades, shop purchases, conversion costs, and some upgrade costs.

```text
Premium Currency
```

Rare/paid currency. Used for cases, 10-pulls, level packs, and premium shop offers.

```text
Character EXP
```

Universal EXP bank earned from battles. The player spends it on whichever character they want.

```text
Resonance Materials
```

Milestone materials used to break character level caps every 10 levels.

```text
Character Shards
```

Character-specific resource from duplicates. Used for unlock/rank-up systems.

## Character Progression

Each owned character needs:

```text
CharacterData reference
level
current level cap
equipped items
character-specific shards/rank later
```

Leveling uses:

```text
Character EXP
Notes if needed
Resonance Materials at level-cap milestones
```

Level caps:

```text
Base cap: 10
Tier I material x2: unlock cap 20
Tier I material x5: unlock cap 30
Tier II material x3: unlock cap 40
Tier II material x8: unlock cap 50
```

The same material tier can be reused for two cap breaks before switching to the next tier. The second use should cost more, so the player feels increasing pressure to unlock higher-tier Trials without making old materials useless too quickly.

Resonance conversion:

```text
10 Tier I -> 1 Tier II
10 Tier II -> 1 Tier III
10 Tier III -> 1 Tier IV
```

This keeps old materials useful without replacing higher-tier farming.

## Tower

Tower is the main campaign and progress meter.

Track:

```text
current stage
current floor in stage
highest stage cleared
highest floor cleared in current stage
first-clear rewards claimed
```

Tower progress should be displayed as:

```text
Stage-Floor
```

Examples:

```text
1-10 = Stage 1, Floor 10
2-10 = Stage 2, Floor 10
15-50 = Stage 15, Floor 50
16-50 = Stage 16, Floor 50
```

Stages control the world/progression tier. Floors are the battles inside that stage.

Each stage should have a theme so progress feels memorable, not just numeric.

Examples:

```text
Stage 1: Broken Backstage
Stage 2: Static Hall
Stage 3: Bass Catacombs
Stage 4: Silent Conservatory
Stage 5: Amp Spire
```

Stage themes can control enemy visuals, background art, music, and reward flavor.

Early stages should be short:

```text
Stage 1: 10 floors
Stage 2: 10 floors
Stage 3: 15 floors
Stage 4: 15 floors
Stage 5: 20 floors
```

Later stages can gradually grow:

```text
Stages 6-10: 25-35 floors
Stages 11-14: 40 floors
Stage 15+: 50 floors
```

This makes early progression fast while giving later stages more weight.

Floor types:

```text
Normal floor: standard battle and standard rewards
Elite floor: harder battle, better repeat rewards
Boss floor: stage checkpoint, first-clear premium reward, system unlocks
Reward floor later: no battle, gives a chest/shop/event choice
```

Early implementation can start with normal and boss floors only.

Tower rewards:

```text
Notes
Character EXP
items/equipment
first-clear Premium Currency on milestones
Trial unlocks
```

Tower should separate first-clear rewards from repeat rewards.

```text
First-clear rewards: premium currency, unlocks, larger Notes/EXP, special items
Repeat rewards: smaller Notes/EXP, normal item chances
```

This makes pushing new floors exciting while still allowing replay farming if needed.

System unlock examples:

```text
Stage 1-3: character upgrade tutorial
Stage 1-5: Trials
Stage 1-8: equipment
Stage 1-10: first boss and gacha
```

These are starting values, not final balance.

Hard bottlenecks should use stage-floor checkpoints, not account level.

Examples:

```text
Trials Tier I unlocks after Stage 1-5
Gacha unlocks after Stage 1-10
Equipment unlocks after Stage 1-8
Resonance Tier II unlocks after Stage 3-15
Resonance Tier III unlocks after Stage 8-30
Resonance Tier IV unlocks after Stage 15-50
```

This keeps progression tied to actually beating content.

Tower attempts are unlimited, so losses should not grant rewards.

```text
Win: rewards and floor progress
Loss: no rewards and no floor progress
```

Losses should teach the player they need upgrades, team changes, or better targeting. The player can immediately retry without spending stamina or tickets.

## Trials

Trials are repeatable farming stages.

Trial categories:

```text
Resonance Trials: farm level-cap materials
Equipment Trials: farm equipment drops
```

Example Resonance Trial structure:

```text
Resonance Trial Tier I: Resonance Material I
Resonance Trial Tier II: Resonance Material II
Resonance Trial Tier III: Resonance Material III
Resonance Trial Tier IV: Resonance Material IV
```

Example Equipment Trial structure:

```text
Equipment Trial Tier I: early equipment
Equipment Trial Tier II: stronger equipment
Equipment Trial Tier III: late equipment
```

Unlocks should be tied to Tower progress:

```text
Tier I Trial: Stage 1-5
Tier II Trial: Stage 3-15
Tier III Trial: Stage 8-30
Tier IV Trial: Stage 15-50
```

Trials use RNG drops with no pity.

```text
Resonance Trial: Notes, Character EXP, chance for Resonance Material
Equipment Trial: Notes, Character EXP, chance for equipment
```

Since Tower attempts and Trials are not stamina-limited, losses or bad drops do not need compensation rewards.

## Equipment

Equipment is the main stat customization system.

Each item should have:

```text
rarity
slot/type
stat bonuses
level later
```

Early version:

```text
equip/unequip
flat stat bonuses
Notes used for upgrades later
```

Do not add complex equipment enhancement until character leveling and tower rewards are solid.

## Shop

Shop is a resource sink.

Notes shop:

```text
basic items
small EXP packs
low-tier materials
equipment
```

Premium shop:

```text
cases
10-pull bundles
level packs
resource bundles
```

Premium packs should accelerate progress, not replace gameplay entirely.

## Quests And Goals

Quests should guide players toward the core loop, not become a separate chore list.

Use three simple categories later:

```text
Daily: short tasks that reward Notes, Character EXP, and small premium amounts
Weekly: larger goals that reward cases, shards, or Resonance Materials
Milestone: permanent achievements tied to Tower stages, character levels, and collection
```

Do not build quests before Tower rewards and upgrades are stable.

## Reward Pacing

The player should always be near one of these goals:

```text
next character level
next level-cap break
next Tower boss
next Trial unlock
next case pull
next equipment upgrade
```

Avoid long stretches where the player gains resources but cannot spend anything.

Premium currency should be rare because cases guarantee character results.

```text
Small milestone: 25-50 Premium Currency
Boss first-clear: 100-200 Premium Currency
Major stage clear: 200-400 Premium Currency
```

These values are starting points and should be tuned after playtesting.

## Gating Rules

Use hard gates sparingly.

Good gates:

```text
clear Stage 1-10 to unlock gacha
clear Stage 3-15 to unlock Tier II Resonance farming
reach character level 10 before using Tier I Resonance material
```

Bad gates:

```text
wait 8 hours
reach arbitrary account level
collect 7 unrelated materials
```

Progress should be blocked by understandable goals, not confusion.

## Save Data

`PlayerData` should own persistent player state:

```text
Notes
Premium Currency
Character EXP
Resonance Material counts
owned characters
character shards
inventory/items
active lineup
tower progress
trial unlock progress
```

`GameManager` should own the live `PlayerData` instance and save/load it through `SaveSystem`.

## Data Architecture

Use ScriptableObjects for authored/tunable data:

```text
CharacterData
ItemData
TowerConfig
QuestData
Reward tables later
TrialConfig
GachaConfig later
```

Use serializable classes for player-owned runtime state:

```text
CharacterInstance
ItemInstance
PlayerData
```

Rule:

```text
ScriptableObject = base design data
Instance class = player-owned mutable state
```

## Implementation Order

1. Stabilize `PlayerData` resource fields.
2. Add universal Character EXP.
3. Add Resonance Material counts and level-cap checks.
4. Make battle rewards grant Notes and Character EXP.
5. Make upgrade panel consume Character EXP/Notes/materials.
6. Add Trial mode data and reward flow.
7. Add gacha case pricing, rarity, duplicates, and shard rewards.
8. Add shop/resource packs after the economy loop works.

## Current Rule

Do not add stamina, account level, battle pass, or many upgrade materials yet.

The foundation is:

```text
Tower + Trials + Notes + Premium Currency + Character EXP + Resonance Materials + Shards + Equipment
```
