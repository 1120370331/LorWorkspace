# Anhier Blue-White Slash VFX Source

This folder contains original source assets and a Unity 2019 AssetBundle scaffold for Steria's Anhier slash effect.

- `Assets/Textures/*.png`: generated original transparent VFX textures.
- `UnityProject/Assets/Editor/AnhierBlueWhiteSlashBundleBuilder.cs`: Unity editor script that creates the prefab and builds `steria_anhier_bluewhite_slash`.

Build target once Unity licensing is active:

```powershell
& "C:\Program Files\Unity\Hub\Editor\2019.4.40f1\Editor\Unity.exe" `
  -batchmode -quit -nographics `
  -projectPath "C:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\VFXSource\AnhierBlueWhiteSlash\UnityProject" `
  -executeMethod AnhierBlueWhiteSlashBundleBuilder.BuildBundle `
  -logFile "C:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\VFXSource\AnhierBlueWhiteSlash\unity-build.log"
```

Unity batchmode is currently blocked locally by Unity licensing, so the generated source is ready but the AB has not been exported yet.
