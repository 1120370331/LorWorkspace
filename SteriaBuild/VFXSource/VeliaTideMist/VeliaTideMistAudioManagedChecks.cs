// Actual companion/PCM code against the existing observable Unity API substitutes.
// This verifies state, counts, seek requests and ownership, not native DSP or listening.
using System;
using System.IO;
using System.Linq;

public static class VeliaTideMistAudioManagedChecks
{
    private static int _passed;
    private static string _waves, _scratch, _storm;
    private static void Require(bool value,string message){if(!value)throw new Exception(message);}
    private static void Near(float value,float expected,string message){Require(Math.Abs(value-expected)<.00001f,message+": "+value+" != "+expected);}
    private static void Run(string name,Action action)
    {
        UnityEngine.Time.timeScale=1;UnityEngine.AudioListener.pause=false;
        action();_passed++;Console.WriteLine("PASS "+name);
    }
    private static VeliaTideMistAudioController Create(Func<float> volume=null,string directory=null)
    {return new VeliaTideMistAudioController(new UnityEngine.Vector3(0,0,0),directory??_waves,volume);}
    private static void Reject<T>(Action action,string label) where T:Exception
    {try{action();}catch(T){return;}throw new Exception("Accepted invalid "+label);}

    public static int Main(string[] args)
    {
        try
        {
            _waves=args[0];_scratch=args[1];_storm=args[2];Directory.CreateDirectory(_scratch);
            Run("exact PCM, two owned 2D sources and shared successful clip cache",()=>
            {
                var cast=VeliaTideMistAudioController.ReadPcm16Wave(Path.Combine(_waves,"cast.wav"),14112);
                var hit=VeliaTideMistAudioController.ReadPcm16Wave(Path.Combine(_waves,"hit.wav"),31752);
                Require(cast.Length==28224&&hit.Length==63504,"frozen cue frames");
                using(var a=Create())using(var b=Create())
                {
                    Require(a.Root.Sources.Count==2&&a.CastStartCount==1&&a.HitStartCount==0,"initial cast only, bounded sources");
                    Require(a.CastSource.clip.Data.SequenceEqual(cast)&&a.HitSource.clip.Data.SequenceEqual(hit),"PCM interleaving/conversion");
                    Require(object.ReferenceEquals(a.CastSource.clip,b.CastSource.clip)&&object.ReferenceEquals(a.HitSource.clip,b.HitSource.clip),"cache sharing");
                    foreach(var s in a.Root.Sources)Require(!s.loop&&!s.playOnAwake&&!s.ignoreListenerPause&&s.spatialBlend==0&&s.dopplerLevel==0,"local 2D source policy");
                    var cached=b.CastSource.clip;a.Dispose();Require(!cached.Destroyed,"instance disposal touched shared clip");
                }
            });
            Run("one cast across two ordinals, duplicate/stale callbacks rejected, hit ends naturally",()=>
            {
                using(var c=Create())
                {
                    Require(!c.TriggerHit(0)&&!c.BeginDice(-1)&&c.BeginDice(0)&&!c.BeginDice(0),"arming rules");
                    c.Advance(.32f);Require(c.CastFinished&&!c.CastSource.isPlaying,"cast natural endpoint");
                    Require(c.TriggerHit(0)&&!c.TriggerHit(0)&&!c.TriggerHit(1),"ordinal callback latch");
                    c.Advance(.72f);Require(c.HitFinished&&!c.HitSource.isPlaying&&!c.IsComplete,"hit ends without ending card");
                    float elapsed=c.Elapsed;Require(c.BeginDice(1)&&!c.HitTriggered,"second ordinal arms");
                    Require(c.Elapsed==elapsed&&c.CastStartCount==1&&!c.TriggerHit(0)&&c.TriggerHit(1),"no replay/reset/old hit");
                    Require(c.HitStartCount==2&&c.HitTriggerCount==2&&c.HitSource.PlayCount==2,"two real callbacks, two starts");
                    c.Advance(.77f);Require(c.HitFinished&&c.HitVolume==0&&c.LastHitOrdinal==1,"second final .05s is silent");
                }
            });
            Run("early hit fades current cast for40ms or earlier natural end",()=>
            {
                using(var c=Create())
                {
                    c.BeginDice(0);c.Advance(.10f);c.TriggerHit(0);Near(c.CastVolume,.55f,"release starts at current level");
                    Near(c.HitVolume,.75f,"hit starts immediately");c.Advance(.02f);Near(c.CastVolume,.275f,"release midpoint");
                    c.Advance(.021f);Require(c.CastFinished&&!c.CastSource.isPlaying&&c.CastVolume==0,"cast stopped by40ms");
                }
                using(var c=Create())
                {
                    c.BeginDice(0);c.Advance(.31f);c.TriggerHit(0);c.Advance(.011f);
                    Require(c.CastFinished&&c.HitSource.isPlaying,"natural .32 wins over release endpoint");
                }
            });
            Run("option product once, Advance0 sliders, mute consumes time without replay",()=>
            {
                float volume=.25f;int reads=0;
                using(var c=Create(()=>{reads++;return volume;}))
                {
                    Near(c.CastVolume,.1375f,"option product multiplied once");int before=reads;c.Advance(0);Require(reads==before+1,"one option read per playback update");
                    c.BeginDice(0);volume=0;c.TriggerHit(0);c.Advance(.20f);Near(c.HitVolume,0,"mute");
                    volume=.4f;c.Advance(0);Near(c.HitVolume,.30f,"live hit gain restoration");
                    Require(c.HitStartCount==1&&c.HitSource.PlayCount==1&&Math.Abs(c.HitSource.timeSamples-8820)<=1,"unmute restarts neither age nor sound");
                    c.Advance(.53f);volume=1;c.Advance(0);Require(c.HitFinished&&c.HitVolume==0&&c.HitStartCount==1,"expired cue never backfilled");
                }
            });
            Run("pause preserves ages, .005 pitch and resumed sample seek, upper pitch cap",()=>
            {
                using(var c=Create())
                {
                    c.BeginDice(0);c.Advance(.10f);var cast=c.CastSource;
                    UnityEngine.Time.timeScale=0;c.Advance(.5f);Near(c.Elapsed,.10f,"paused age");
                    Require(cast.PauseCount==1&&!cast.isPlaying,"source paused");c.TriggerHit(0);c.Advance(.5f);
                    Require(c.HitStartCount==0&&c.HitAge==0,"paused cue waits without growing age");
                    UnityEngine.Time.timeScale=.005f;c.Advance(.005f);Near(c.HitSource.pitch,.005f,"no positive pitch floor");
                    Require(c.HitStartCount==1&&Math.Abs(c.HitSource.timeSamples-220)<=1,"resume seeks consumed samples");
                    UnityEngine.Time.timeScale=2;c.Advance(.10f);Near(c.HitSource.pitch,2,"speed change");
                    Require(Math.Abs(c.HitSource.timeSamples-4630)<=1,"resync after speed change");
                    UnityEngine.Time.timeScale=4;c.Advance(0);Near(c.HitSource.pitch,3,"native pitch upper bound");Near(UnityEngine.Time.timeScale,4,"no global time mutation");
                }
            });
            Run("listener pause respected; delayed or fully expired cues do not restart at zero",()=>
            {
                UnityEngine.AudioListener.pause=true;
                using(var c=Create())
                {
                    c.BeginDice(0);c.Advance(.10f);c.TriggerHit(0);c.Advance(.10f);
                    Require(c.CastStartCount==0&&c.HitStartCount==0&&UnityEngine.AudioListener.pause,"listener pause authority");
                    UnityEngine.AudioListener.pause=false;c.Advance(0);
                    Require(c.CastStartCount==0&&c.HitStartCount==1&&Math.Abs(c.HitSource.timeSamples-4410)<=1,"resume current hit, skip released cast");
                }
                UnityEngine.AudioListener.pause=true;
                using(var c=Create())
                {
                    c.BeginDice(0);c.TriggerHit(0);c.Advance(.8f);UnityEngine.AudioListener.pause=false;c.Advance(0);
                    Require(c.CastStartCount==0&&c.HitStartCount==0&&c.CastFinished&&c.HitFinished,"expired listener-paused events stay consumed");
                }
            });
            Run("one hit source reused even if next valid ordinal arrives early",()=>
            {
                using(var c=Create())
                {
                    c.BeginDice(0);c.TriggerHit(0);var hit=c.HitSource;c.Advance(.02f);c.BeginDice(1);c.TriggerHit(1);
                    Require(object.ReferenceEquals(hit,c.HitSource)&&c.Root.Sources.Count==2&&hit.PlayCount==2&&hit.isPlaying,"no simultaneous hit copies");
                    c.Advance(.021f);Require(c.CastFinished,"new ordinal must not prolong first cast release");
                }
            });
            Run("finish/cancel/dispose release only owned sources with no additional wait",()=>
            {
                var c=Create();c.BeginDice(0);c.TriggerHit(0);var root=c.Root;var hit=c.HitSource;var clip=hit.clip;
                c.Finish();Require(c.IsComplete&&!c.IsDisposed&&c.Root==null&&!root.Active&&!hit.isPlaying&&!clip.Destroyed,"immediate finish ownership");
                Require(!c.BeginDice(1)&&!c.TriggerHit(1),"finished rejects new events");c.Cancel();c.Dispose();Require(c.IsDisposed,"idempotent final disposal");
                c=Create();root=c.Root;c.Cancel();Require(c.IsComplete&&c.IsDisposed&&root.Destroyed&&!root.Active,"cancel cast immediately");
            });
            Run("missing or malformed cue skips only itself",()=>
            {
                foreach(string absentCue in new[]{"cast","hit"})foreach(bool corrupt in new[]{false,true})
                {
                    string dir=Path.Combine(_scratch,absentCue+(corrupt?"-bad":"-absent"));Directory.CreateDirectory(dir);
                    string valid=absentCue=="cast"?"hit":"cast";File.Copy(Path.Combine(_waves,valid+".wav"),Path.Combine(dir,valid+".wav"),true);
                    if(corrupt)File.WriteAllBytes(Path.Combine(dir,absentCue+".wav"),new byte[]{1,2,3});
                    using(var c=Create(null,dir))
                    {
                        c.BeginDice(0);c.TriggerHit(0);Require(c.HitTriggerCount==1,"event still consumed");
                        Require(c.CastStartCount==(absentCue=="cast"?0:1)&&c.HitStartCount==(absentCue=="hit"?0:1),"valid other cue blocked");
                        c.Advance(.8f);c.Finish();Require(c.IsComplete&&c.Root==null,"optional cue added wait");
                    }
                }
                using(var c=Create(null,Path.Combine(_scratch,"both-absent"))){Require(c.Root==null,"missing assets created sources");c.BeginDice(0);c.TriggerHit(0);c.Advance(2);c.Finish();Require(c.IsComplete,"no-resource lifetime");}
            });
            Run("cue wrappers retain disjoint exact-duration guards and bounded shared reader",()=>
            {
                Reject<ArgumentOutOfRangeException>(()=>VeliaTideMistAudioController.ReadPcm16Wave(Path.Combine(_storm,"gather_loop.wav"),44100),"storm through Velia");
                Reject<ArgumentOutOfRangeException>(()=>SlazeyaStormAudioController.ReadPcm16Wave(Path.Combine(_waves,"cast.wav"),14112),"Velia through storm");
                Reject<InvalidDataException>(()=>VeliaTideMistAudioController.ReadPcm16Wave(Path.Combine(_waves,"hit.wav"),14112),"wrong precise Velia cue length");
                Reject<ArgumentOutOfRangeException>(()=>SkillPcmWave.ReadStereo44100(Path.Combine(_waves,"cast.wav"),int.MaxValue),"unbounded allocation");
            });
            Console.WriteLine("VELIA_MANAGED_AUDIO_PASS cases="+_passed+"; API substitutes only, native DSP/subjective listening NOT certified");return 0;
        }
        catch(Exception ex){Console.WriteLine("FAIL "+ex);return 1;}
    }
}
