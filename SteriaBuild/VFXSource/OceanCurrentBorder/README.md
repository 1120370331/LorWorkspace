# Flow current border preview

The production effect is `FlowCardCurrentBorder.cs`, called by the existing hand-card stamp. It renders one reusable, noninteractive mesh around the transformed union of native `img_linearDodges` rectangles. Two short currents and six small particles move clockwise in 2.4 seconds. Levels only slightly increase brightness; the native Flow icon/count remains authoritative. The border reads per-model `FlowPlanning` state and clears when consumed, rebound, reset, disabled or destroyed.

## Reproduce

After the final production Release build, stage that exact DLL and run:

```powershell
powershell -ExecutionPolicy Bypass -File SteriaBuild/VFXSource/OceanCurrentBorder/run_managed.ps1 -StageCurrentDll
```

`-SkipRender` reruns the mechanics and mesh/lifecycle checks without spending time recreating unchanged media. The script does not build production, deploy files or alter licensing.

Outputs are isolated to `output/ocean-current-vfx/ui/managed/`: normal-speed 30fps MP4, a 4.8-second GIF with 30/30/40ms frame durations, full three-card frames at 0/.3/.6/.9/1.2/1.8 seconds, stage contact sheet, same-ID unenhanced state, scaled native frame, split-frame union, pooled reuse and consumed states. `source-hashes.json`, `production-meshes.json`, `mesh-checks.txt` and `real-mechanics-checks.txt` identify the inputs and results.

## Evidence boundary

The mechanics executable invokes the frozen production DLL against the actual game Assembly-CSharp.dll. Its actual enhancement levels, same-ID independence, cross-round persistence and consumption states feed the managed UI fixture. The UI adapter compiles the unchanged production border file directly and copies the production stamp, Apply, Restore, Flash and spacing methods verbatim; it supplies bounded stand-ins for Unity transforms, UGUI and model presentation. The resulting production-generated triangles are CPU-composited on clearly labeled illustrative card faces. Text and artwork rectangles in these media are schematic and do not establish real game text layout or mouse event acceptance.

An actual Unity/game-component fixture is also provided: `run_preview.ps1`. The September 20 attempt was blocked before scene execution by Unity 2019.3 licensing (`Unity has not been activated with a valid License`). No Unity-render or full in-game acceptance is claimed. The owned blocked process was stopped; no license files were changed. With an already working installation, this independent fixture uses the frozen DLL, original card UI components and actual Harmony OnPdSubmit entry, and writes its own screenshots/checks to the UI output root.

Full live-game masking, overlap with neighboring cards, click routing and camera layout remain external acceptance. No asset bundle is required for this UGUI geometry effect.
