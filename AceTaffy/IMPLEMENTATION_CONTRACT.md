# AceTaffy playable first delivery

Source of truth: `永雏塔菲介绍.md`. This document resolves incomplete draft wording for a playable first version; original design remains intact.

## Current reception: autonomous invention units, 2026-10-26

The latest user revision replaces permanent whole-page invention upgrades with actual summons. Five progress produces a qualifier/type unit; up to three active inventions occupy the native five-slot enemy formation alongside Taffy and the workshop, with excess completions queued. Unit stats use the requested 150 HP / 80 stagger baseline with type/qualifier variation; all 25 six-resistance profiles are distinct. Each unit has exactly two speed dice at all emotion levels, two type-specific enemy-only pages, a type system and debuffs. Unit/Book IDs 1883400..1883424, cards 1883500..1883509, unit passive 1883205, ward passive 1883206.

Taffy starts with eight permanent protection-equivalent stacks. Each owned invention's real death is counted once: kills 1-8 remove one protection; kill 8 leaves zero; kill 9 onward adds one permanent vulnerability per kill. Workshop death and final cleanup do not count. Qualifier auras apply only while their source units live: Odd speed +1; Weird one Weak on the first hit of each page; Fragile HP/stagger damage +15%; Mysterious defensive dice +1; Super attacking dice +1. Counts stack per live unit. Already rolled speed/results are not retroactively changed. The old page-upgrade Harmony hooks and persistent device buffs on Taffy are removed.

The 25 independent imagegen unit skins keep low highlights and use native rigid-sprite motion. Full numeric and lifecycle design: `../docs/superpowers/specs/2026-10-26-taffy-invention-units.md`; reproducible data: `generate_invention_units.py`; art import: `import_invention_units.py`. The sections below preserve earlier delivery history and do not override this current contract.

## Superseded reception revision, 2026-10-26
The user's subsequent reception design supersedes the first-delivery guest stats and deck rules below: five librarians; guest 600 HP / 350 stagger, six opening speed dice; guest-only passives 1883201..1883204 and 1883211..1883213. Workshop enemy 1884 / book 1883003 has 500 HP, all six Resist values, no stagger/action, and can be destroyed. Each hit restores 10 stagger gauge to Taffy and advances invention progress; each scene starts with +1 while the workshop survives. Five progress resets to zero and creates one of 25 persistent qualifier/device combinations.

The final user-directed pattern is two zero-cost Improvise pages (1883010), followed by random ordinary pages that fit the actual light budget; there is no fixed three-scene sequence or zero-cost override for ordinary pages. Improvise is not in the player inventory/drop pool. Losing/tied attack dice and explicitly destroyed dice do not resume against the workshop. Native card replay is bounded; invention procs and energy payouts occur once per original page, without recursive chain generation. Qualifier status amounts default to one layer next scene; the mysterious pool is Strength/Endurance/Quickness/Protection/Weak/Disarm/Binding/Vulnerable. Device snapshots freeze when a page starts, so UI refresh cannot alter an executing page. Librarian stats, rewards, ordinary card data, and prior portrait centering remain unchanged.

## Scope and identity
- Independent BaseMod invitation, no changes to Steria production files. Root `AceTaffy/`; source `src/`; package `AceTaffyMod/`; artwork sources `ArtSource/`; verification `Verification/`.
- Package/workshop ID `AceTaffy`; C# assembly `AceTaffy`; namespace `AceTaffy` for helpers; script class names use `AceTaffy_` prefix, except numeric passive classes.
- Card IDs 1883001 through 1883009 in design order: Ignition, BrassRivet, ExhaustRecycle, PistonChain, FlueReversal, SafetyValve, BoilerArmor, Supercritical, SteamDetonation.
- Librarian book 1883001; enemy book and enemy deck 1883002 (native BookXmlInfo.DeckId uses the mod book's own ID); enemy/stage/dropbook/card-drop-table ID 1883.
- Custom passives 1883101 SteamHeart, 1883102 CompoundCylinder, 1883103 CrystalGuard. Reuse native Speed III after checking its actual ID. Custom passives cannot transfer in first release.
- Assets: cards `Resource/CombatPageArtwork/AceTaffy_<EnglishName>.png`; skin `Resource/CharacterSkin/AceTaffy/`; audio `Resource/CustomAudio/`; icons `Resource/ArtWork/AceTaffy_Pressure.png` and `AceTaffy_Guard.png`.

## Rules resolved
- Use the three named passive bodies in the latest draft; obsolete names in its introductory list are aliases, not additional passives. Overpressure is a pressure state, not a fourth passive.
- Pressure integer 0..10, loses one at round end. At 6..9, third ordinary attack die on a page gains +1 power and incoming attack HP damage increases 10%; at 10 every attack die (including attack counter dice) gains +1, incoming attack HP damage increases 20%. Tiers replace each other. Self HP payments are fixed, not amplified by pressure.
- Third attack means attack ordinal, ignoring defence/evasion and counter dice. Reroll is the same die, once per owner per round, costs 3 pressure, after either a clash win or a successful one-sided hit with at least 6 pressure. Both callbacks share one latch; a clash-hit callback cannot cause a second trigger. Recheck pressure before later rolls.
- Ordinary smoke caps at 15 for SteamHeart, otherwise native cap. Pressure participates in all smoke counts, including native thresholds, damage scaling and ordinary smoke spending; combined amount is ordinary smoke + pressure, each counted exactly once. General spending uses ordinary smoke first, pressure second; insufficient total means no spend/no gated reward. ExhaustRecycle is the explicit ordinary-smoke-only payment exception below. UI still displays the two physical resources separately.
- Smoke gain awards one pressure per positive gain event, not per smoke layer. A pressure gain never triggers another smoke-gain event. SteamHeart and native Puffy Brume suppress the smoke incoming-damage penalty, including pressure's contribution to that count; the separate pressure-tier penalty remains. Native Puffy Brume outgoing scaling uses the full smoke count only when its actual passive is present.
- Own card HP payments award one pressure per actual payment event with SteamHeart. CrystalGuard activates on the first own-card HP loss each round, grants one 4..8 standby counter Block die once that round, all defensive dice +2 while active; defensive clash wins while active give +1 pressure and inflict 1 ordinary smoke. Track any actual HP loss separately for BoilerArmor recovery.
- ExhaustRecycle pays 3 ordinary smoke atomically for draw 2 and gain 3 pressure; pressure cannot fund this payment. Fewer than 3 ordinary smoke means no payment, no use-time draw and no use-time pressure reward, even if aggregate smoke is sufficient. The page otherwise remains usable; its dice effects remain independent.
- FlueReversal transfers one available ordinary smoke, or one pressure when ordinary smoke is zero, into one ordinary smoke on target.
- SteamDetonation has THREE dice, design's dice5 means final third die. All page dice receive floor((ordinary smoke + pressure)/3) power, snapshotted on page use. Final attack extra break damage uses floor(current combined smoke/3). Clash win of first die destroys target's remaining current-page dice.
- All nine cards are ordinary equippable combat pages, no undocumented exclusive restriction. Core-only strengths come from passives.
- Player-facing smoke conditions simply say smoke; do not repeat aggregate accounting or fallback-spending explanations. ExhaustRecycle may name ordinary smoke to express its exception. Preserve the original Smoke and Smoke_buf localization and description getters; do not replace native smoke text with custom compatibility prose. Chinese prose uses fullwidth punctuation while machine keyword tokens retain their required syntax.
- First playable invitation uses Chapter 6 (Star of City), one floor, up to three librarians, one Taffy. Invitation uses one appropriate BookValue-based book, chosen against actual schema and documented. No free enemy resource cheats or hidden stat inflation. Enemy uses provided deck with meaningful priorities if native priorities suffice.
- HP/break: librarian 100/60, guest 250/121; speed 3..6; initial light 4; blunt HP/break Endure, slash/pierce Normal.

## Shared API (owner: mechanics worker)
`public static class AceTaffy.TaffyMechanics`:
- `int Pressure(BattleUnitModel unit)`
- `int Smoke(BattleUnitModel unit)` returns ordinary smoke
- `int EffectiveSmoke(BattleUnitModel unit)` returns combined once
- `void AddPressure(BattleUnitModel unit, int amount)`
- `bool SpendPressure(BattleUnitModel unit, int amount)`
- `void AddSmoke(BattleUnitModel unit, int amount, BattleUnitModel source)`
- `bool SpendSmoke(BattleUnitModel unit, int amount)` atomic, ordinary first
- `bool SpendOrdinarySmoke(BattleUnitModel unit, int amount)` atomic, ordinary only; ExhaustRecycle payment
- `void TransferSmoke(BattleUnitModel from, BattleUnitModel to, int amount)`
- `void PayHp(BattleUnitModel unit, int amount)` marks and triggers self-card HP loss
- `bool LostHpThisRound(BattleUnitModel unit)`
- `bool IsThirdAttack(BattleDiceBehavior behavior)`
- `void Install(HarmonyLib.Harmony harmony)` only if explicit patches needed; initializer calls it.
Helper `AceTaffy.AceTaffyMod.RootPath` is absolute package root and `AceTaffyMod.Log(string)` logs tagged messages.

## Ownership
- Main: this contract, art direction, generated images, art source/prompts, narrative prose, acceptance and delivery records.
- mechanics worker: `src/TaffyMechanics.cs`, `src/TaffyPassives.cs`, mechanics-specific verification only.
- cards worker: `src/TaffyCards.cs`, `AceTaffyMod/Data/CardInfo.xml`, card/ability localization only.
- content worker: all other package XML and non-card localization, story wiring, `src/TaffyEnemyAI.cs` if necessary; excludes skin/audio/art assets and initializer.
- bootstrap worker: project/build/deploy scripts, initializer and scoped resource/skin integration; may write skin ModInfo XML but not PNG art or gameplay files.
- VFX worker assigned after approved design: VFX/actions/audio playback source and focused visual preview tooling; excludes core gameplay.

## Gates
- Real referenced-assembly Release compilation; XML and script resolution against actual game loaders, no placeholder scripts.
- Focused mechanical boundary checks at pressure 0/5/6/9/10, smoke gain/spend, self-harm defence, reroll once per round, dice destruction and ordinary native units unaffected.
- Nine correct card images; coherent character sprites with real alpha and correct facing; current art previews reviewed.
- Real game smoke where available: load package, invitation, fight, drop/burn/equip/reuse. Evidence must distinguish automated contract checks from unexercised UI paths.
- Final package, local reproducible build/install instructions, exact candidate hashes, remaining risks. Do not claim unobserved gameplay or visual acceptance.
