<!-- .github/copilot-instructions.md - guidance for AI coding agents working on this repo -->
# Repository quick guide for AI coding agents

This file contains targeted, actionable guidance to help an AI code assistant be productive in this Library of Ruina mod workspace.

## Quick purpose
- This repo holds one or more mods (notably `Steria`) for the game *Library of Ruina* using BaseMod + Harmony.
- Typical tasks: add/modify Passive/Dice/Buf code, edit XML card definitions, build the mod DLL, and deploy to the `SteriaModFolder` for testing.

## Quick start (build & deploy)
Use PowerShell on Windows. From the `SteriaBuild` folder:

```powershell
# build
cd "c:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild"
dotnet build Steria.csproj -c Release

# deploy (copy the built DLL into the mod folder)
Copy-Item -Path "bin\Release\net472\Steria.dll" -Destination "SteriaModFolder\Assemblies\Steria.dll" -Force
```

Notes: the project targets `net472`. If tooling problems occur, inspect `Steria.csproj` and ensure your environment supports building .NET Framework targets.

## Key files and places (use these as primary references)
- `SteriaBuild/Steria.csproj` — project file and build target
- `SteriaBuild/ModInitializer.cs` — mod entry point (BaseMod init)
- `SteriaBuild/HarmonyPatches.cs` — central Harmony patching and small global lists (example: `_flowBonusOnlyCardIds`)
- `SteriaBuild/Assembly-CSharp/` — decompiled game DLL used as authoritative API reference (check inheritance/method signatures here)
- `SteriaBuild/SteriaModFolder/` — final mod layout (Assemblies, Localize, Resource, StaticInfo)
- `SteriaBuild/SteriaModFolder/Localize/` — localization files (cn/*)
- `SteriaBuild/SteriaModFolder/StaticInfo/` — XML definitions (CardInfo, PassiveList, StageInfo)
- `SteriaBuild/SteriaLogger.cs` — where mod logs are written (log file: `SteriaModFolder/steria_log.txt`)
- `ModGuideDocs/` — authoritative documentation for mod structure and common C# base classes (useful lookup for triggers and XML mapping)

## Naming & structural conventions to always follow
- Game-exposed types are expected in the global (top-level) namespace when named with these patterns:
  - `PassiveAbility_XXXXXX` — passive ability classes
  - `DiceCardAbility_*` and `DiceCardSelfAbility_*` — dice/card abilities
  - `BattleUnitBuf_*` — custom buffs
- Helper and internal classes may live in a project-specific namespace (e.g. `Steria`), but do not change names for classes that the game discovers by name.
- XML data files map to in-game entities. Common locations and names follow `ModGuideDocs/` mapping (e.g. `Data/CardInfo.xml`, `Data/PassiveList.xml`).

## Common code patterns & examples (copy/paste friendly)
- Add a flow-only card id (example): open `SteriaBuild/HarmonyPatches.cs` and append the card id to `_flowBonusOnlyCardIds`.
  Example (C#):
  ```csharp
  private static readonly HashSet<int> _flowBonusOnlyCardIds = new HashSet<int> { 9001008 /* 清司风流 */, /* more ids */ };
  ```
- New passive: subclass `PassiveAbilityBase` and name it `PassiveAbility_<YourIdOrName>` so the game can reference it from XML.
- New buff: subclass `BattleUnitBuf` and name `BattleUnitBuf_<Name>`; wire effects in C# and expose localized text keys under `Localize`.

## Debugging & inspection
- Inspect the real game's types and behavior in `SteriaBuild/Assembly-CSharp/` (decompiled). Use `dnSpy/` to step through the game's code or verify signatures.
- Asset/texture extraction helpers are available in `AssetStudio.net472.v0.16.47/` if you need to inspect assets.
- Logs: `SteriaModFolder/steria_log.txt` contains mod runtime messages from `SteriaLogger.cs`.

## Integration points & dependencies
- This mod uses BaseMod and Harmony. Expect to find: BaseMod init in `ModInitializer.cs` and patch definitions in `HarmonyPatches.cs`.
- The game runtime will reflect changes when the `Steria.dll` in `SteriaModFolder/Assemblies` is replaced and the game is launched.

## Project-specific gotchas
- Naming matters: some classes must be discoverable by exact name from the global namespace. Don't wrap them in a project namespace if game discovery requires top-level names.
- XML keys and localization must match exactly. If a card or passive doesn't appear or shows missing text, check `Localize/cn/` for the corresponding keys.
- `ReferenceMod` and `寒昼事务所V4.0/` are example/reference mods — use them for patterns but do not edit them in-place.

## Where to look for examples
- `ModGuideDocs/` — mapping of C# base classes to naming conventions and which triggers to implement (e.g., `OnRoundStart`, `BeforeRollDice`, `OnSucceedAttack`)
- `SteriaBuild/` source files such as `AnhierAbilities.cs`, `AnhierCards.cs`, and `BattleUnitBuf_*.cs` — concrete implementations to mirror.

## When uncertain — checklist for the agent
1. If unsure about an API or signature, inspect `SteriaBuild/Assembly-CSharp/` (decompiled game DLL).
2. If behavior is data-driven, check `SteriaModFolder/StaticInfo` and `Localize`.
3. If cross-cutting change (e.g., flow consumption logic), search `HarmonyPatches.cs` for related sets and hooks.

---
If any of the above sections are unclear or you want more specific examples (e.g. full passive template, XML example), tell me which area to expand and I will iterate.
