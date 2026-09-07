# Music dice original artwork

`MusicDiceSpriteGenerator.cs` preserves the complete original clean-frame / glyph vector paths and 4x rasterization that produced the visually accepted D92B candidate. It is offline authoring code. The existing `VFXSource/**` project exclusion keeps it out of Steria.dll.

The game loads only the three baked PNGs under `SteriaBuild/VisualAssets/MusicDice/` from explicit assembly manifest resource names. There is no expensive runtime generation fallback.

To regenerate in an isolated Unity 2019 project using Gamma color space:

1. Copy these two `.cs` files into that project's `Assets/Editor` directory.
2. Run `Unity.exe -batchmode -projectPath <project> -executeMethod Steria.VisualAuthoring.MusicDiceRegeneration.Run -musicDiceOutput <output-directory> -logFile <log-path>`. On affected CPUs set the temporary process environment `OPENSSL_ia32cap=:~0x20000000` for the Unity editor launch.
3. Compare output pixels and 24/32 px previews before replacing the three source assets. Generating them takes roughly 23 seconds; this is intentionally outside the runtime path.

Current baked files are byte-for-byte copies of the main thread's visually accepted PNGs in `output/music-dice-ui/accepted-clean-frame/`:

- Card.png: 773421dc9439f15ce43f49d9da18b0aa5cf000056f9f290d6cc4c3c384283b4e
- Glyph.png: 98def165d1acbf8403b2627f37abc49983bfb345923eb830d204e834cc6eed0f
- BlankFrame.png: 32158ccbdf6228c20ef41f6052a48ceb734cb1e6b44a37b7ac90466faaac2d84
