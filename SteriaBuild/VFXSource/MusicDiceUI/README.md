# Music dice original artwork

`MusicDiceSpriteGenerator.cs` preserves the complete original clean-frame / glyph vector paths and 4x rasterization that produced the visually accepted D92B candidate. It is offline authoring code. The existing `VFXSource/**` project exclusion keeps it out of Steria.dll.

The game loads only the three baked PNGs under `SteriaBuild/VisualAssets/MusicDice/` from explicit assembly manifest resource names. There is no expensive runtime generation fallback.

To regenerate in an isolated Unity 2019 project using Gamma color space:

1. Copy these two `.cs` files into that project's `Assets/Editor` directory.
2. Run `Unity.exe -batchmode -projectPath <project> -executeMethod Steria.VisualAuthoring.MusicDiceRegeneration.Run -musicDiceOutput <output-directory> -logFile <log-path>`. On affected CPUs set the temporary process environment `OPENSSL_ia32cap=:~0x20000000` for the Unity editor launch.
3. Compare output pixels and 24/32 px previews before replacing the three source assets. Generating them takes roughly 23 seconds; this is intentionally outside the runtime path.

Before the 2026-09-08 80% size adjustment, baked files were byte-for-byte copies of the main thread's visually accepted PNGs in `output/music-dice-ui/accepted-clean-frame/`:

- Card.png: 773421dc9439f15ce43f49d9da18b0aa5cf000056f9f290d6cc4c3c384283b4e
- Glyph.png: 98def165d1acbf8403b2627f37abc49983bfb345923eb830d204e834cc6eed0f
- BlankFrame.png: 32158ccbdf6228c20ef41f6052a48ceb734cb1e6b44a37b7ac90466faaac2d84

## 2026-09-08 size adjustment

`Rasterize` now maps every sample to `50 + (coordinate - 50) / .80` before the existing frame/glyph layout. All three visible shapes, stroke widths and their global color fields shrink together; canvas dimensions, pivot, PPU and runtime loader do not change. The original geometry and colors are unchanged.

In this workspace run `output/music-dice-ui/implementation/regenerate_size80_and_preview.ps1` with the temporary Unity launch environment described above. It copies this authoring source into the existing isolated editor project, regenerates to a staging folder, copies the three successful baked outputs to source assets, refreshes real manifest resources and runs the existing preview contract. `before-size-reduction/` supplies the immutable old-size bounding boxes and visual comparison. The script writes `size80-assets-hashes.json` after generation; the main thread runs this flow and records validation before deployment.

### Current 80% baked assets (main-thread validated)

The main thread ran `regenerate_size80_and_preview.ps1` successfully on 2026-09-08. The actual manifest-loader fixture passed 52 checks. Card/BlankFrame visible bounds changed from 198x218 to 160x174; Glyph changed from 98x102 to 79x82. The shrink stays centered on the original canvas and the color field follows author coordinates. UI canvas/pivot/PPU and the runtime source remain unchanged.

- Card.png: e66e73713c2564149bb34a9b5a6f3f67106d94c3e5a9849f51aa1f4930d546e4
- Glyph.png: c6b2f26212ee622c0eb292563cbb0ea3e3d508416a0bf78404d6a6a457d68f98
- BlankFrame.png: de07c78dbf77b3edcefc66a52d8b93498151d354256cf60137674847e799f095
- MusicDiceSpriteGenerator.cs: 91db9689dbb92e9025df0d8c6807e452800568126dd8a5c321d474e8cc4504f6
- Unchanged MusicDiceSystem.cs: 92036e71316bd7c19012999dc911c14bbae4155291f1ba13e2a9fff02a4e3176

The fixture extraction appends the baked-artwork hash to its generated source so new PNG bytes invalidate the Unity compile input; the runtime loader itself is unchanged.
