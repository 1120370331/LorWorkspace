"""Fire acoustic audition: persistent combustion + falling burning mass.

Concept reference: Andy Farnell, Designing Sound, Practical 11 (fire).
Original implementation and sound; no textbook patch or recording is copied.
Dimensionless heat/fuel/oxygen controls are an acoustic approximation, not CFD.
"""
from pathlib import Path
import hashlib, importlib.util, json, sys, wave, io
import numpy as np
import scipy
from scipy import signal, ndimage

HERE=Path(__file__).resolve().parent
REPO=HERE.parents[2]
SHARED=REPO/'SteriaBuild/VFXSource/SlazeyaStormMass/generate_slazeya_storm_audio.py'
sys.dont_write_bytecode=True
spec=importlib.util.spec_from_file_location('fire_math',SHARED)
s=importlib.util.module_from_spec(spec);spec.loader.exec_module(s)
SR=s.SR;SEED=2026091206
OUT=HERE/'intermediate'

def heat_controls(duration):
    """Fuel consumption, ignition, oxygen fluctuation and coupled heat envelopes."""
    fs=400;dt=1/fs;warmup=2.;n=round((duration+warmup)*fs)
    rng=np.random.default_rng(SEED)
    fuel=rng.uniform(2.,4.,24);initial=fuel.copy()
    heat=rng.uniform(.79,1.03,24);oxygen=np.ones(24)
    surface=rng.uniform(.55,1.4,24);tau=rng.uniform(.015,.12,24)
    q=np.zeros(n);temp=np.zeros(n)
    for i in range(n):
        oxygen+=(1-oxygen)*dt/tau+rng.normal(size=24)*np.sqrt(2*dt/tau)*.20
        oxygen=np.clip(oxygen,.16,1.9)
        ignite=1/(1+np.exp(-(heat-.67)*13))
        rate=.085*surface*oxygen*ignite*np.minimum(fuel,1)
        fuel=np.maximum(0,fuel-rate*dt)
        neighbors=(np.roll(heat,1)+np.roll(heat,-1))/2
        heat+=dt*(rate*9.2-.83*(heat-.20)+.28*(neighbors-heat))
        heat=np.clip(heat,.15,2.)
        q[i]=rate.sum();temp[i]=heat.mean()
    cut=round(warmup*fs);q=q[cut:];temp=temp[cut:]
    q=q/max(float(q.mean()),1e-12)
    slow=ndimage.gaussian_filter1d(q,fs*.040)
    # Broad heat-release changes drive lapping; faster oxygen changes drive hiss.
    lapping=np.clip((slow-1)*4+1,.30,1.8)
    uneven=np.clip(1+8*(q-slow),.12,2.7)
    t=np.arange(round(duration*SR))/SR;tc=np.arange(len(q))/fs
    interp=lambda a:np.interp(t,tc,a)
    return interp(lapping),interp(uneven),{'rateHz':fs,'cells':24,'warmupSeconds':warmup,
        'initialFuel':initial.tolist(),'finalFuel':fuel.tolist(),'heatMeanRange':[float(temp.min()),float(temp.max())],
        'relativeReleaseRange':[float(q.min()),float(q.max())],
        'model':'dimensionless combustion envelope approximation, not thermodynamic/CFD validation'}

def burning(duration=7.2):
    n=round(duration*SR);t=np.arange(n)/SR
    lap,oxygen,control=heat_controls(duration)
    # Slow fire lapping is a saturated irregular low pressure signal, rather than
    # a continuously loud sub-bass oscillator. Gentle nonlinearity is internal.
    slow=s.noise(n,76101,18,85,-.2)
    lapping=np.tanh(3.2*slow)
    lapping=signal.sosfilt(signal.butter(2,45,'highpass',fs=SR,output='sos'),lapping)
    lapping=np.column_stack([lapping,lapping])*(.22*lap)[:,None]
    body=s.stereo_noise(n,76111,110,1150,-.38,.09)
    # Independently evolving sub-Hz and several-Hz envelopes avoid a flat noise bed.
    breath=s.modulation(n,76121,.7,4)
    hiss_control=(.12+.88*s.modulation(n,76131,.8,6))**3
    body*=((.20+.36*breath)*lap)[:,None]
    hiss=s.stereo_noise(n,76141,1500,10000,-.45,.22)
    hiss*=((.045+.17*hiss_control)*oxygen)[:,None]
    flame=body+.22*lapping+hiss
    # Clusters of different-sized ruptures, with a few larger 25-60ms pops and
    # many 3-16ms fine crackles. Bandwidth/decay changes together, no gun samples.
    rng=np.random.default_rng(SEED+1);events=[];crackle=np.zeros((n,2))
    cluster_centers=np.sort(rng.uniform(0,duration,round(duration*3.5)))
    for center in cluster_centers:
        for j in range(int(rng.integers(2,7))):
            at=float(np.clip(center+rng.normal(0,.065),.01,duration-.10))
            big=bool(rng.random()<.20);length=float(rng.uniform(.025,.065) if big else rng.uniform(.004,.018))
            size=round(length*SR);tt=np.arange(size)/SR
            grain=rng.normal(size=size)
            lo=float(rng.uniform(300,800) if big else rng.uniform(1300,3000))
            hi=float(rng.uniform(2900,6200) if big else rng.uniform(5800,12000))
            grain=signal.sosfilt(signal.butter(2,[lo,hi],btype='bandpass',fs=SR,output='sos'),grain)
            env=s.smooth(tt/(.0008 if big else .00025))*(1-np.clip(tt/length,0,1))**2.5
            amp=float(rng.uniform(.70,1.25) if big else rng.uniform(.23,.65))
            amp*=float(np.interp(at,t,lap))
            # Local surface resonance is deliberately low Q / broadband: no bell.
            pan=float(rng.uniform(-.40,.40));start=round(at*SR)
            grain=grain*env*amp
            crackle[start:start+size]+=grain[:,None]*np.array([np.sqrt(1-pan),np.sqrt(1+pan)])
            events.append({'time':at,'duration':length,'large':big,'lowHz':lo,'highHz':hi,'gain':amp})
    return s.finish_transient(flame+crackle,highpass=38),control,events

def distance_motion(x,landing=.95):
    """Falling source approaches a listener; distance/pitch cues, not vertical HRTF."""
    t=np.arange(len(x))/SR
    height=.5*9.81*landing**2
    z=np.maximum(height-.5*9.81*t*t,0)
    d=np.sqrt(1.25**2+(z-.75)**2)
    radial=np.gradient(d)*SR
    ratio=343/(343+np.clip(radial,-30,30))
    phase=np.cumsum(ratio)/SR
    shifted=np.column_stack([np.interp(phase,t,x[:,c],left=0,right=0) for c in range(2)])
    far=signal.sosfilt(signal.butter(2,1700,'lowpass',fs=SR,output='sos'),shifted,axis=0)
    nearness=np.clip(1.25/d,0,1)
    atten=.12+.88*nearness**1.4
    # High frequencies emerge as the source approaches; continuous path avoids cuts.
    return (far*(1-nearness[:,None])+shifted*nearness[:,None])*atten[:,None]

def falling(base,landing=.95,duration=3.9,seed=0,weight=1.):
    n=round(duration*SR);t=np.arange(n)/SR
    bed=base[:n].copy()
    # The sustained fire remains audible after arrival instead of expiring at .6s.
    sustain=np.interp(t,[0,.14,landing,landing+.25,2.5,duration-.2,duration],[.16,.24,.72,1.,.8,.55,0])
    bed=distance_motion(bed,landing)*sustain[:,None]
    rush=s.stereo_noise(n,76201+seed,170,6500,-.34,.10)
    rush_env=s.smooth(t/.15)*np.exp(-((t-(landing-.08))/.21)**2)
    rush*=.42*rush_env[:,None]
    plume=s.stereo_noise(n,76211+seed,95,3200,-.30,.13)
    eruption=s.event_envelope(n,landing,.009,.14,landing+.7)
    # A dispersed flare, not a click/gunshot/solid object impact.
    plume*=.75*weight*eruption[:,None]
    raw=bed+rush+plume
    return s.finish_transient(raw,highpass=38)

def master(x,target=-19,cap=-4.8):
    gain=min(10**(target/20)/s.rms(x),10**(cap/20)/np.max(np.abs(signal.resample_poly(x,4,1,axis=0))))
    return x*gain,float(gain)

def encode(x):
    assert np.isfinite(x).all() and abs(x).max()<.99
    return s.encoded(s.to_pcm(x))

def metrics(x):
    windows=[s.db(s.rms(x[i:i+round(.25*SR)])) for i in range(0,len(x)-round(.25*SR)+1,round(.25*SR))]
    return {'seconds':len(x)/SR,'rmsDbfs':s.db(s.rms(x)),
      'truePeak4xDbfs':s.db(np.max(np.abs(signal.resample_poly(x,4,1,axis=0)))),
      'monoFoldLossDb':s.db(s.rms(x.mean(axis=1))/s.rms(x)),
      'maxDC':float(abs(x.mean(axis=0)).max()),'quarterSecondRmsDbfs':windows}

def build(write=True):
    raw,control,events=burning()
    steady=s.finish_transient(raw[round(.7*SR):round(6.7*SR)],highpass=38)
    fall=falling(raw)
    outputs={};records={}
    for name,x in [('持续燃烧_6秒.wav',steady),('烈焰坠落_完整.wav',fall)]:
        y,gain=master(x);outputs[name]=encode(y);records[name]={'gain':gain,'metrics':metrics(y)}
    # Three application sketches share the developed fire material. These remain
    # secondary to auditioning the full combustion/fall quality first.
    variants=[('回忆斩击_火流.wav',0,1.35,.34,1.),('回忆打击_爆燃.wav',-3,1.65,.27,1.28),('回忆突刺_喷焰.wav',4,1.10,.24,.75)]
    for name,semi,duration,arrival,weight in variants:
        rate=2**(semi/12);t=np.arange(round(3*SR))/SR
        shifted=np.column_stack([np.interp(t*rate,np.arange(len(raw))/SR,raw[:,c]) for c in range(2)])
        x=falling(shifted,landing=arrival,duration=duration,seed=100,weight=weight)
        x*= (1-s.smooth((np.arange(len(x))/SR-(duration-.25))/.25))[:,None]
        y,gain=master(s.finish_transient(x,38));outputs[name]=encode(y)
        records[name]={'semitones':semi,'arrival':arrival,'gain':gain,'metrics':metrics(y)}
    for name,y in outputs.items():
        if write:(OUT/name).write_bytes(y)
        else:assert (OUT/name).read_bytes()==y,'Non-deterministic output: '+name
    for name,r in records.items():
        assert r['metrics']['truePeak4xDbfs']<=-4.7 and r['metrics']['maxDC']<1e-5
        assert r['metrics']['monoFoldLossDb']>-.8
    report={'version':4,'scope':'audition only; no runtime application',
      'reference':'https://aspress.co.uk/sd/practical11.html',
      'referenceUse':'Concept of lapping, hissing and irregular crackles only; no reference recording or Pd code copied',
      'simulationBoundary':'Procedural fire acoustic approximation driven by dimensionless heat/fuel/oxygen controls, pressure and clustered ruptures. Not full fluid/combustion CFD and not a real recording.',
      'directionBoundary':'Source distance/approach and Doppler cues; no claim of listener-independent vertical localization without HRTF.',
      'trajectory':{'gravity':9.81,'landingSeconds':.95,'horizontalOffset':1.25},
      'control':control,'events':events,'records':records,'seed':SEED,
      'generatorSha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
      'sharedMathSha256':hashlib.sha256(SHARED.read_bytes()).hexdigest(),
      'numpy':np.__version__,'scipy':scipy.__version__,
      'hashes':{name:hashlib.sha256(b).hexdigest() for name,b in outputs.items()},
      'subjectiveListening':'Awaiting user; previous versions were rejected by user'}
    if write:(HERE/'manifest.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
    print(json.dumps({'result':'SYNTH_PASS' if write else 'REPRO_PASS','crackleEvents':len(events),'records':records},ensure_ascii=False))

if __name__=='__main__':build('--verify-repro' not in sys.argv)
