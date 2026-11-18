# AceTaffy combat audio

The original `build_audio.py` reproduces the rejected v1 procedural set for historical comparison only. The revised recording based candidate, its sources and review list are in [`v2/README.md`](v2/README.md). Do not run the v1 script over the packaged sounds during v2 review.

Four original deterministic procedural cues, made with `python ArtSource/Audio/build_audio.py` from the `AceTaffy` directory. No samples, libraries, speech or music are used. The script writes canonical stereo PCM16, 44.1 kHz WAV files into `AceTaffyMod/Resource/CustomAudio/AceTaffy/`.

| Cue | Length | Intended gesture |
| --- | ---: | --- |
| SteamBlunt | 0.28 s | dry brass piston, short vent |
| SteamPierce | 0.22 s | sharp rivet, narrow hiss |
| SteamGuard | 0.30 s | restrained resonant brass clink |
| SteamDetonation | 0.65 s | low local pressure thump, irregular vent tail |

The random noise generator is seeded per cue. Output is deliberately below the -3 dBFS ceiling, with onset and exit fades. Regenerate from this source; do not normalize the WAV files independently because the relative hierarchy is intentional.
