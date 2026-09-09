# Flow integration checks

`./run_checks.ps1` builds the current mod and exercises its compiled DLL against the actual game Assembly-CSharp.dll. The fixture constructs minimal battle models without launching the game. Enemy-faction models avoid unrelated player-history/UI bookkeeping; the Unity fixture separately exercises a controllable player model.

`./run_checks.ps1 -Preview` additionally starts an isolated Unity 2019.3 preview in a hidden process and prints its PID and unique log path. Wait for `FLOW_UI_FIXTURE_PASS` in that log; inspect `output/flow-rework/ui-checks.txt` and the PNGs. It uses actual game hand/card components, the candidate DLL, and actual Harmony patches on card submission. Canvas layout, card faces and fixture font are simplified, so this is not full in-game viewport acceptance.

Requires the repository's existing game references, .NET Framework 4.7.2 targeting support, Python, Unity at `C:/Program Files/Unity/Editor/Unity.exe` (override with `-Unity`), and the locally installed SimHei font for fixture Chinese text. The font is only copied into ignored preview output and is not shipped with the mod.

Core scenarios: strict greater-than balance gate; one click per level; Slazeya extra limit; high-cap page limits; card identity and foreign-mod IDs; no automatic/unpaid strengthening; once-only passive/refund commit; immunity changes; next-round settlement without extra delay/double multiplication; preserved clash-loss penalty; mass special investment; cross-round cleanup; automatic enemy planning.
