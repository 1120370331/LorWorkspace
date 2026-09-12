"""Reproduce the three user-approved v5 WAVs from original synthesis, offline."""
from pathlib import Path
import hashlib,importlib.util,json,sys,io,wave
import numpy as np

HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[2]
sys.dont_write_bytecode=True
spec=importlib.util.spec_from_file_location('memory_combustion',HERE/'combustion.py')
c=importlib.util.module_from_spec(spec);spec.loader.exec_module(c)
manifest=json.loads((HERE/'accepted-audio.json').read_text(encoding='utf8'))
raw,_,_=c.burning()
recipes={'slash':(0,1.35,.34,1.),'hit':(-3,1.65,.27,1.28),'pierce':(4,1.10,.24,.75)}
for cue,(semi,duration,arrival,weight) in recipes.items():
 rate=2**(semi/12);t=np.arange(round(3*c.SR))/c.SR
 shifted=np.column_stack([np.interp(t*rate,np.arange(len(raw))/c.SR,raw[:,channel]) for channel in range(2)])
 x=c.falling(shifted,landing=arrival,duration=duration,seed=100,weight=weight)
 x*=(1-c.s.smooth((np.arange(len(x))/c.SR-(duration-.25))/.25))[:,None]
 original,_=c.master(c.s.finish_transient(x,38))
 # v5 edits were made from the quantized v4 PCM, preserve that round trip exactly.
 with wave.open(io.BytesIO(c.encode(original)),'rb') as f:
  original=np.frombuffer(f.readframes(f.getnframes()),dtype='<i2').reshape(-1,2).astype(float)/32768
 r=manifest['files'][cue];start=round(r['sourceStartSeconds']*c.SR);n=r['frames']
 x=original[start:start+n].copy();t=np.arange(n)/c.SR
 gate=c.s.smooth(t/.004)*(1-c.s.smooth((t-r['releaseStartSeconds'])/(r['durationSeconds']-r['releaseStartSeconds']-.005)))**.72
 x*=gate[:,None];x-=gate[:,None]*(x.sum(axis=0)/gate.sum())[None,:];x[[0,-1]]=0
 encoded=c.encode(x);assert hashlib.sha256(encoded).hexdigest()==r['sha256'],cue+' reproduction mismatch'
 dest=HERE/'source_audio'/r['file']
 if '--verify' not in sys.argv:dest.parent.mkdir(exist_ok=True);dest.write_bytes(encoded)
 assert dest.read_bytes()==encoded
 ship=ROOT/'SteriaBuild/SteriaModFolder/Resource/CustomAudio/MemoryFlame'/r['file']
 assert ship.read_bytes()==encoded,cue+' shipping drift'
print('APPROVED_AUDIO_REPRO_PASS: 3 original v5 PCM files, source/shipping byte-exact; no external recordings')
