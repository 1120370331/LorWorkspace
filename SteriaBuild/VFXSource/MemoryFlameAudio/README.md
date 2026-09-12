# Memory flame audio

User-approved v5 fire sounds: Slash 0.46s, Hit 0.55s, Pierce 0.36s. Production copies
are byte-identical to the audition WAVs; no external audio recordings are included.

`combustion.py` retains the original procedural fire generator; `build_audio.py`
reproduces the accepted edits, including the original intermediate PCM16 rounding.
`accepted-audio.json` locks the three outputs. Run `build_audio.py --verify` for a
read-only reproduction and source/shipping check. Do not regenerate the approval
hashes merely to make a changed sound pass.

The original conceptual reference is Andy Farnell's *Designing Sound*, Practical
11: https://aspress.co.uk/sd/practical11.html. No Pd patch or reference recording
was copied. The heat/fuel/oxygen envelope is an acoustic approximation, not CFD.

The runtime entry is `SteriaBuild/MemoryFlameAudio.cs`. It replaces only successful
ordinary near-attack motion sounds on six explicit Steria cards, via the actual
PrintSound event. Existing native audio prefab/FX volume/lifecycle and failure
fallback are retained. Memory/Tide visuals and other skins/cards are unchanged.

See [guide 16](../../../ModGuideDocs/16_音效选材合成与书页应用.md) for sourcing,
selection, synthesis, the card matrix, routing, verification and deployment.
