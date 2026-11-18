# Enemy pattern card costs

Regression: scripted enemy pages must show their printed cost and normal cost sprites while native selection, reservation and spending still see zero. Genuine discounts, player cards, other mods and unrelated instances of the same ID must retain their normal display.

From the repository root, build the current mod once, then run:

```powershell
dotnet build SteriaBuild/Steria.csproj -c Release --no-restore
dotnet build SteriaBuild/Verification/EnemyPatternCardCost/Checks.csproj -c Release
SteriaBuild/Verification/EnemyPatternCardCost/bin/Release/net472/EnemyPatternCostChecks.exe
python SteriaBuild/Verification/EnemyPatternCardCost/prepare_ui.py
```

For the native UI smoke, start Unity 2019.3.15f1 with `-batchmode -projectPath <repo>/output/enemy-pattern-cost/UnityProject -executeMethod EnemyPatternCostEntry.Run -logFile <unique-log-path>` in a hidden process. Require `ENEMY_PATTERN_NATIVE_UI_PASS` in the log and exit code 0.

The Unity check applies the production Harmony transpiler and runs the original game `BattleDiceCardUI.SetCard` through its native `NumbersData` cost binding, reusing the same UI for scripted/ordinary/scripted cards. Only the unrelated description/artwork tail is cut off in the fixture. It checks the selected ordinary/discount digit sprites and the cost cache used by native color styling; it is not full game battle or screenshot acceptance. Fixture sprites are test inputs, never shipped.

`prepare_ui.py` records the tested source and DLL hashes in `output/enemy-pattern-cost/candidate.json`. Normal checks require .NET Framework 4.7.2 and the repository's game references; Unity additionally requires its existing UGUI and TextMeshPro packages.
