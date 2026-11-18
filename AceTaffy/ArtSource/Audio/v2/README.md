# AceTaffy combat audio v2 candidate

This candidate answers the rejection of the original oscillator based cues. It uses short CC0 game impact recordings and an actual steam release recording. The original archives and snapshots of their source pages are in `sources/`. No music, voice, old v1 samples, sine oscillators or synthesized noise are in the v2 mixes. **No subjective listening acceptance is claimed.**

## Source and license record

Retrieved 2026-09-27. Licenses were checked on the individual source pages, not inferred from a search result.

| Pack | Author and source page | Download and original archive SHA256 | License evidence |
| --- | --- | --- | --- |
| Impact Sounds 1.0 | Kenney, https://kenney.nl/assets/impact-sounds | https://kenney.nl/media/pages/assets/impact-sounds/87b4ddecda-1677589768/kenney_impact-sounds.zip — `029d734af1582474edf3a694d1b0cebc97c1c152f2f39fa34d4c2bafc5de77f8` | Source page states Creative Commons CC0; ZIP `License.txt` states CC0 and optional credit. Page snapshot: `sources/Kenney_impact-sounds_source.html`. |
| Steam release sounds | bart, https://opengameart.org/content/steam-release-sounds | https://opengameart.org/sites/default/files/steam_hisses.zip — `7dc5ae6f43ef0e9a47b8333e1e1508463cb64877ab06baa0259c643a9ab4677f` | Artwork page explicitly lists License(s): CC0. Page snapshot: `sources/bart_steam-release_source.html`. |

Embedded source samples used, with SHA256 of each original byte sequence inside its unmodified archive:

| Sample | SHA256 |
| --- | --- |
| `Audio/impactMetal_heavy_002.ogg` | `b914c8f1eb7c0f34bb165d7c77f4be0351f6be0660c13c53e65424e262e2c093` |
| `Audio/impactPunch_heavy_002.ogg` | `7993dd4c156b9979ad69f17be5ebe31850b16039a3857f8477141be54dfee1b3` |
| `Audio/impactMetal_medium_002.ogg` | `7e89ce2ca0dbda95ea2b78d4b50791cab35c10d20e9f5ccd45dd0b00e99ff548` |
| `Audio/impactMetal_light_004.ogg` | `9b1c35820fba507081261f19ab5f461f69a5e2627a7d91acdb73ca3cfc997979` |
| `Audio/impactTin_medium_001.ogg` | `84253095543c094d83cd749f42ecfbc96e356b60385f18a192c18eafb25b3cc6` |
| `Audio/impactPlate_heavy_003.ogg` | `59775f8979a396c8a793b0a1db206d5bdc651eae808d858e9acaee8ebda5bc6d` |
| `steam hisses - Marker #1.wav` | `1dd3462e1320c9a620c35328fee71d4ca30a5284dd9948b24da0faecb476817c` |
| `steam hisses - Marker #2.wav` | `e0f94117729588440da6f56fec934fc637b299266556fd6a27df68a03277ef6a` |
| `steam hisses - Marker #3.wav` | `f064442e7f7974f7f526f5d7dafc62015ea2d0ad7b1ca252e2841df4709ebe24` |
| `steam hisses - Marker #5.wav` | `eef1eaa97198ae5abf4f9961116658e60575eb10fd77aa3dae9cad05313f14c5` |

## Mix and review list

Run `python ArtSource/Audio/v2/build_v2.py` from the AceTaffy directory. The script decodes originals with ffmpeg, mixes with NumPy/SciPy, and writes canonical 44.1 kHz stereo PCM16 WAV to `v2/candidate/`. The immutable ZIP files supply the originals. The script applies bounded low pass filters, short fades and one fixed per cue bus gain. Its final peak ceiling is 0.64, below -3 dBFS. It does not pitch shift sources. `candidate-measures.json` records duration, peak, RMS and output hash; those measurements verify files, not taste.

| Cue | Source mix | Review focus |
| --- | --- | --- |
| SteamBlunt | heavy metal hit, filtered punch body, one short valve release | thick and dry, no electronic ring |
| SteamPierce | brief medium metal hit, narrow release | lighter and quicker than blunt |
| SteamGuard | light metal plus very quiet tin contact | subdued contact, no loud sustained chime |
| SteamDetonation | heavy plate/punch low body, three uneven steam releases | pressure weight, short tail, no cinematic boom or noise wall |

Listening order: each cue alone, then Blunt → Pierce → Guard → Detonation at equal system volume, then compare in the actual battle mix. The candidate WAVs were copied byte for byte to `AceTaffyMod/Resource/CustomAudio/AceTaffy/` for runtime evaluation. `candidate/` preserves the review files. This copy is not a claim of listening approval.
