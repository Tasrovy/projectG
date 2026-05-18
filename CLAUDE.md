# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 2022.3 2D card-battle / visual novel dating-sim game using URP. Chinese-language codebase (comments, strings, commit messages). Uses **Yarn Spinner** for branching dialogue and **ExcelDataReader** (vendored in `Assets/Packages/`) for importing game data from `.xlsx` files at the project root.

## Build & Development

- **Unity version**: 2022.3.49f1c1
- Open `Assets/Scenes/Tasrovy.unity` as the main scene
- Game data is authored in `.xlsx` files at project root. In Editor, `ExcelLoader.SyncExcelToSO()` converts them to `ScriptableObject` assets in `Assets/Resources/`. At runtime, `Resources.Load<ScriptableObject>()` is used.
- No CI/CD, no tests.

## Architecture

### Singleton Pattern

`Assets/C#/Base/Singleton.cs` provides a generic `Singleton<T> : MonoBehaviour` base class supporting lazy GameObject creation, `DontDestroyOnLoad` (via `IsPersistent`), and duplicate detection. Most managers extend this. Key singletons:
- `CardManager` — central hub: deck, hand, draw, card repository
- `DayManager` — day progression, events, rarity scaling
- `DataManager` — player stats (natures, money, charm)
- `ExcelLoader` — Excel-to-ScriptableObject conversion (Editor-only sync + runtime load)
- `DialogueHandler` (manual singleton, `Scripts/Dialogue/`) — wraps Yarn Spinner, drives daily story flow

### Card System

```
CardData (ScriptableObject from Excel)
  → Card (runtime instance with Components: CardEffectPlan, CardNatureState, CardRuntime)
    → CardEffect.ExecuteEffectList() → CardEffectExecutor (async chain) → CardEffectInvoker → CardEffectLibrary
```

- `CardManager` delegates to services: `CardRepositoryService`, `DeckService`, `HandService`, `DrawService` (weighted rarity + pity)
- `CardEffectExecutor` uses a paused-chain pattern: when effects need player input (e.g., selecting a card), it pauses and resumes via `CardEffect.OnSelectCardEnd()`
- `CardEffectLibrary` contains ~30 effect implementations (natures, card gen/destroy, growth, branching, pruning, money, dialogue, visual FX, conditionals)
- Card IDs are 5-digit bit-fields: 10,000s=type (1=gift,2=event,3=func), 1,000s=rarity (1-3), rest=instance ID

### UI / DUEL System

`DUELUIObjectManager` lazy-loads the DUEL prefab. Key UI singletons:
- `CardSelector` — selection/submit/cancel state
- `CardActionResolver` — mode controller (NormalPlay vs EffectSelect)
- `CardSubmitHelper` — concrete flow for each card action type
- `BattleDialogController` — portrait + dialogue bubble during combat
- `CardUIObject` — interactive card (click, hover, drag-to-reorder, long-press for detail)
- `CardDetailUI` (new) — popup card detail viewer on long-press
- `PromptItem` / `PromptItemSO` (new) — prompt/tooltip system

### Dialogue / Story Flow

`DialogueHandler` drives the day loop: Begin → Talk → Select → AfterClass → DayMenu. Daily events, deal dialogues, special dialogues (every 3 days), and failure dialogues are triggered based on day progression from `DayDataSO`.

### Shop System

`ShopController` handles buy/sell modes. Buy generates daily inventory from `ShopInventoryGenerator`. Sell is paginated view of hand cards. Closing shop triggers `DialogueHandler.TriggerEndDayWithDeal()`.

### Legacy Code

`Assets/C#/yjtc/` contains an older state-machine-based card system from a previous developer. Not actively used but referenced. Also contains `AudioManager` (object-pooled SFX singleton).

### Data Pipeline

```
.xlsx files (root) → [#if UNITY_EDITOR] ExcelLoader.SyncExcelToSO() → .asset files in Resources/
                    → [#else] Resources.Load<ScriptableObject>()
```

`ExcelLoader` uses `#if UNITY_EDITOR` to switch between Editor sync and runtime loading.

### Planned: Save System

Architecture decision (not yet implemented). Save system should use this structure:

```
[System.Serializable] SaveData (pure C# class, fields only)
    ↑ DataManager holds instance internally, keeps existing Add()/Get() interface
    ↑ Save() = JsonUtility.ToJson → File.WriteAllText to persistentDataPath
    ↑ Load() = File.ReadAllText → JsonUtility.FromJson
    ↑ Optional DefaultDataSO (ScriptableObject) for new-game initial values
```

**Key rules:**
- `SaveData` is a plain `[System.Serializable]` class — no MonoBehaviour, no SO inheritance
- `DataManager` stays as the intermediary; existing `DataManager.Instance.Add(id, num)` callers don't change
- Save files go to `Application.persistentDataPath`, not project assets
- `DefaultDataSO` (if created) only provides initial values on first-time / new game
- Do NOT write runtime state directly to .asset files — they are read-only at runtime

## Current Branch: `tasrovy`

Feature branch merging changes from `main` and `alpleateauh`. Current in-progress work adds `CardDetailUI` (long-press card detail) and `PromptItem`/`PromptItemSO` (tooltip system).
