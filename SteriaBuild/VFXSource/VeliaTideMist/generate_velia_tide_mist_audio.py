#!/usr/bin/env python3
"""Original .32s cast / .72s hit for Velia. Source audio only, no runtime writes.

Shared deterministic noise/mastering/analysis primitives come from the sibling
storm generator; all Velia layers, seed offsets, envelopes and cues live here.
Neither generator downloads recordings or creates voice, harmony or music.
"""
from __future__ import annotations
import importlib.util
import json
from pathlib import Path
import sys

import numpy as np

ROOT = Path(__file__).resolve().parent
SHARED = ROOT.parent / 'SlazeyaStormMass/generate_slazeya_storm_audio.py'
sys.dont_write_bytecode = True
_spec = importlib.util.spec_from_file_location('storm_audio_primitives', SHARED)
s = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(s)
SR = s.SR
OUT = ROOT / 'source_audio'
CONTRACTS = {
    'cast.wav': {'frames': 14112, 'peak_dbfs': -7., 'true_peak_dbfs': -6.5, 'rms_dbfs': -17.},
    'hit.wav': {'frames': 31752, 'peak_dbfs': -5.5, 'true_peak_dbfs': -5., 'rms_dbfs': -17., 'front_dbfs': -14.},
}


def waterlight_modes(n, offset, count, start, stop, lo, hi):
    """Irregular low-Q water/glass contacts; independent non-scale frequencies.

    Q is about 5-14; no held oscillator bank, repeated chord or musical pitch
    grid. The lower modes dominate the centre, with restricted upper width.
    """
    rng = s.rng_for(offset)
    result = np.zeros((n, 2))
    events = []
    for i in range(count):
        onset = float(rng.uniform(start, stop))
        f0 = float(np.exp(rng.uniform(np.log(lo), np.log(hi))))
        q = float(rng.uniform(5, 14))
        tau = q / (np.pi * f0)
        length = min(round(7*tau*SR), round(.075*SR))
        t = np.arange(length)/SR
        ratio = float(rng.uniform(1.43, 2.28))
        drift = float(rng.uniform(-.13, .09))
        phase = s.TAU*f0*(t + drift*(t-tau*(1-np.exp(-t/tau))))
        grain = (np.sin(phase) + .14*np.sin(phase*ratio+.7)*np.exp(-t/(tau*.6)))
        grain *= np.exp(-t/tau) * s.smooth(t/.0012) * (1-s.smooth((t-(t[-1]-.003))/.003))
        w = np.sin(np.linspace(0,np.pi,length))**2
        grain -= grain.sum()/w.sum()*w
        amp = float(rng.uniform(.3, .8))
        pan = float(rng.uniform(-.24, .24))
        at = round(onset*SR)
        amount = min(length,n-at)
        result[at:at+amount] += amp * grain[:amount,None] * np.array([1+pan,1-pan])
        events.append({'start_sample':at,'f0_hz':f0,'Q':q,'decay_s':tau,
                       'inharmonic_ratio':ratio,'frequency_drift':drift,'amplitude':amp,'pan':pan})
    return result, events


def synthesize_sources():
    n = 14112
    t = np.arange(n)/SR
    open_env = s.smooth(t/.010) * np.interp(t,[0,.018,.055,.13,.18,.23,.29,.32],
                                         [1,1,.92,.83,.60,.36,.16,0])
    water = s.stereo_noise(n,1101,350,1380,-.25,.13) * open_env[:,None]
    air = s.stereo_noise(n,1111,650,2360,-.25,.19)
    air *= (open_env*(.91+.09*s.modulation(n,1113,7,24)))[:,None]
    # Brief soft contact under the main opening, with no hard electrical edge.
    touch = s.stereo_noise(n,1121,430,1900,-.6,.08)
    touch *= s.event_envelope(n,0,.009,.037,.115)[:,None]
    dust = s.stereo_noise(n,1131,2600,4850,-.75,.23)
    dust *= (s.smooth(t/.022)*(1-s.smooth((t-.10)/.19))*.36)[:,None]
    glass,cast_events = waterlight_modes(n,1141,38,0,.285,350,1480)
    glass *= (.38+.62*s.smooth(t/.13))[:,None]
    cast = s.finish_transient(.66*water + .38*air + .24*touch + .12*dust + .44*glass,highpass=110)
    cast_pcm,cm = s.master(cast,-17,-7)

    n = 31752
    t = np.arange(n)/SR
    body_env = s.smooth(t/.009) * np.interp(t,[0,.025,.055,.09,.12,.28,.45,.55,.69,.72],
                                         [1,1,.95,.77,.65,.54,.38,.17,0,0])
    pressure = s.stereo_noise(n,1201,260,1120,-.35,.075) * body_env[:,None]
    resonance = s.stereo_noise(n,1211,740,2120,-.55,.15) * body_env[:,None]
    contacts,hit_events = waterlight_modes(n,1241,54,.005,.57,460,2000)
    contacts *= (s.smooth(t/.008) * (1-s.smooth((t-.12)/.47)))[:,None]
    air = s.stereo_noise(n,1221,620,2170,-.35,.20)
    air *= (s.smooth(t/.065) * np.interp(t,[0,.12,.32,.48,.69,.72],[0,.6,.6,.40,0,0]))[:,None]
    dust = s.stereo_noise(n,1231,2350,4400,-.8,.23)
    dust *= (s.smooth(t/.027)*(1-s.smooth((t-.16)/.50))*.3)[:,None]
    hit = s.finish_transient(.79*pressure + .26*resonance + .42*contacts + .27*air + .15*dust,highpass=110)
    hit_pcm,hm = s.master(hit,-17,-5.5,front_target=-14.2)
    design = {
        'cast.wav': {
            'prospective_timbre':'Soft but immediate opening brush; outward warm air with a short irregular water/glass glint, no chord or sustained ring.',
            'layers': [
                {'name':'warm_water_opening','band_hz':[350,1380],'weight':.66,'width':.13,'seed_offsets':[1101,1102]},
                {'name':'outward_soft_air','band_hz':[650,2360],'weight':.38,'width':.19,'seed_offsets':[1111,1112,1113]},
                {'name':'gentle_opening_contact','attack_s':.009,'end_s':.115,'weight':.24,'seed_offsets':[1121,1122]},
                {'name':'sparse_light_dust','band_hz':[2600,4850],'weight':.12,'seed_offsets':[1131,1132]},
                {'name':'low_Q_water_glass_contacts','count':38,'weight':.44,'seed_offset':1141}],
            'envelope_attack_s':.010,'envelope_knots_s':[0,.018,.055,.13,.18,.23,.29,.32],
            'envelope_values':[1,1,.92,.83,.60,.36,.16,0],
            'modal_events':cast_events,'highpass_hz':110,'mastering':cm,
            'final_fade_s':[(14112-1)/SR-.03,(14112-1)/SR]},
        'hit.wav': {
            'prospective_timbre':'One warm water-light slap with a clear rounded centre; lower-level airy illumination trails into silence. No storm crack.',
            'layers': [
                {'name':'warm_water_pressure','band_hz':[260,1120],'weight':.79,'width':.075,'seed_offsets':[1201,1202]},
                {'name':'soft_bright_resonance','band_hz':[740,2120],'weight':.26,'width':.15,'seed_offsets':[1211,1212]},
                {'name':'irregular_low_Q_contacts','count':54,'weight':.42,'seed_offset':1241},
                {'name':'lingering_warm_air','band_hz':[620,2170],'weight':.27,'seed_offsets':[1221,1222]},
                {'name':'light_dust_tail','band_hz':[2350,4400],'weight':.15,'seed_offsets':[1231,1232]}],
            'envelope_attack_s':.009,'envelope_knots_s':[0,.025,.055,.09,.12,.28,.45,.55,.69,.72],
            'envelope_values':[1,1,.95,.77,.65,.54,.38,.17,0,0],
            'modal_events':hit_events,'highpass_hz':110,'mastering':hm,
            'final_fade_s':[(31752-1)/SR-.03,(31752-1)/SR]}}
    return {'cast.wav':cast_pcm,'hit.wav':hit_pcm},design


def preview_context():
    videos=[]
    for facing in ('player','enemy'):
        path=s.REPO/f'preview_exports/velia_tide_mist/reviewed-r9-{facing}/video-manifest.json'
        m=json.loads(path.read_text(encoding='utf-8-sig'))
        video=path.parent/Path(m['video']).name
        assert s.sha(video.read_bytes())==m['sha256']
        assert (m['sourceFrameCount'],m['sourceFrameRate'])==(205,60)
        timing=path.parent/'timing.csv'
        assert s.sha(timing.read_bytes())==m['timingSha256']
        videos.append({'path':str(video.relative_to(s.REPO)).replace('\\','/'),
                       'sha256':m['sha256'],'manifest_sha256':s.sha(path.read_bytes()),
                       'timing_sha256':m['timingSha256']})
    return {'videos':videos,'frames':205*SR//60,'video_frames':205,'video_fps':60,
            'cast_start_s':0,'hit_start_s':[.60,1.95],'hit_start_samples':[26460,85995],
            'second_begin_s':1.65,'cast_gain':.55,'hit_gain':.75,'cast_crossrelease_s':.04,
            'option_gain':1.,'normalization_after_mix':False,
            'timing_scope':'Accepted r9 proxy callbacks; never production trigger constants'}


def preview(pcm,context):
    mix=np.zeros((context['frames'],2))
    cast=pcm['cast.wav'].astype(float)/32768
    t=np.arange(len(cast))/SR
    first=context['hit_start_samples'][0]/SR
    gain=.55*(1-np.clip((t-first)/.04,0,1))
    mix[:min(len(mix),len(cast))] += (cast*gain[:,None])[:len(mix)]
    hit=pcm['hit.wav'].astype(float)/32768
    for start in context['hit_start_samples']:
        count=min(len(hit),len(mix)-start)
        mix[start:start+count] += hit[:count]*.75
    return s.to_pcm(mix)


def main():
    s.run_generator(OUT,Path(__file__),CONTRACTS,synthesize_sources,preview,preview_context(),
                    'VELIA / DAWN',{'cast_gain':.55,'hit_gain':.75,'cast_duration_s':.32,
                                   'hit_duration_s':.72,'hit_gain_same_for_each_dice':True,
                                   'early_hit_cast_crossrelease_s':.04,
                                   'early_hit_cast_crossrelease_curve':'linear, naturally ends at .32s if sooner',
                                   'option_gain_in_preview':1.,'cast_count':1,
                                   'hit_trigger':'once per effective callback ordinal',
                                   'runtime_path':'Resource/CustomAudio/VeliaTideMist/'},dependencies=(SHARED,))


if __name__=='__main__':
    main()
