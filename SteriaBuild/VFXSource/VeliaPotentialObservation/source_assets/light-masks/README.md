# Scalar light masks

These are unchanged original PNG bytes from Kenney **Light Masks 1.0** and **Particle Pack 1.1**, both licensed CC0. Source pages, actual download URLs, archive/file hashes and original package paths are recorded in `source-provenance.json`; original licenses are under `licenses/`.

| Local file | Original file | Runtime purpose |
|---|---|---|
| `surge-mask.png` | Light Masks `Default/cone_d_blur.png` | One coherent bottom-to-top widening light field |
| `soft-source.png` | Particle Pack `PNG (Black background)/star_05.png` | One soft body hotspot without rings |
| `small-streak.png` | Particle Pack `PNG (Black background)/trace_01.png` | Sparse short straight rising lights |

Import all three as linear data (`sRGB=false`), RGBA32, no compression/mips, Clamp and Bilinear. Read **R**, ignoring PNG alpha: black pixels represent zero light. Current shader uses `Blend One One`, placing coverage into emitted RGB once. These files are masks rather than coloured objects, and they replace the rejected coloured wisp atlas in runtime dependencies.

`import_light_masks.py` copies the approved research sources without image editing, rescaling or generation and verifies the original SHA256 hashes. Normal runtime does not depend on the research/output directory.
