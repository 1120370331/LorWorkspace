# Anhier texture combat source

Candidate pipeline: approved card/native-reference-guided ImageGen masters → original
color-dominance key/despill → spatially disjoint primary/fleck layers → authored local-flow
phase drawings and directional retreat → the same Unity renderer in game and fixture.

Run from this directory:

```powershell
python -X utf8 import_masters.py
python -X utf8 verify_source.py
powershell -ExecutionPolicy Bypass -File run_preview.ps1
```

`imagegen_masters/inputs.json` selects the six approved RGBA masters and separate SeaFarHit
splash. Its paths are relative to that directory. Original responses, prompts, keyed masters,
source-artist cleanup scripts, metadata and provenance are stored alongside them. The
source artists' absolute tool-response locations are historical provenance, not rebuild
dependencies. Only `source_assets/*.png` copied into the dedicated shipping directory plus
the six JSON profiles are runtime inputs. Native game art is reference-only and never
appears in shipping inputs.

`author_assets.py` is the superseded direct-painting prototype. It writes only the isolated
`experiments/procedural_v0/` directory and cannot overwrite the final manifest or shipping
assets. The final workflow above rebuilds only the selected ImageGen-derived phases.

`AnhierTextureTimeline.cs` is copied byte-for-byte from the production root for the fixture.
The thin game adapter compiles against the actual game DLLs. Unity preview uses native
Bada default art and the serialized native character effect root (-2.2,2.34), but supplies a
representative stable target/torso root. It does not run the game battle controller or assert
that live camera, success/failure sound, damage processing or other skins have been tested.

Review authoritative `previews/all-six-60fps.mp4`, browser-safe `all-six-30fps.gif`,
`phase-contact.png`, `facing-and-body-contact.png`, individual 60fps renders and
`candidate-hashes.json`. The MP4 encodes every source frame at 60fps. The GIF uses every
other frame at 30fps with 30/40/30ms delays (none shorter than 30ms, avoiding browser
clamping); both keep gameplay speed and briefly pause on empty before replay. All six profiles together require
about 43MiB decoded RGBA, loaded once per profile; runtime releases CPU pixels and uses
one shared alpha material. No AssetBundle, custom shader, new sound or shake is required.

Source art and runtime integration are candidates until independent visual review and main
acceptance complete. Deployment and final game smoke belong to the main task.
