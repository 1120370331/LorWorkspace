// Focused managed contract checks. Compiles the actual audio companion against observable Unity
// API substitutes; it does NOT certify native DSP, device playback or subjective sound quality.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public static void Destroy(Object value)
        {
            if (value == null) return;
            value.Destroyed = true;
            var root = value as GameObject;
            if (root != null) foreach (var source in root.Sources) { source.Stop(); source.Destroyed = true; }
        }
    }
    public struct Vector3 { public float x,y,z; public Vector3(float a,float b,float c) {x=a;y=b;z=c;} }
    public class Transform { public Vector3 position; }
    public class GameObject : Object
    {
        public readonly Transform transform = new Transform();
        public readonly List<AudioSource> Sources = new List<AudioSource>();
        public bool Active = true;
        public GameObject(string name) {}
        public T AddComponent<T>() where T : new()
        {
            var value = new T(); var source = value as AudioSource;
            if (source != null) Sources.Add(source);
            return value;
        }
        public void SetActive(bool value) { Active = value; }
    }
    public class AudioClip : Object
    {
        public int samples;
        public float[] Data;
        public static AudioClip Create(string name,int frames,int channels,int rate,bool stream)
        {
            if (channels != 2 || rate != 44100 || stream) throw new Exception("Unexpected clip construction");
            return new AudioClip { samples = frames };
        }
        public bool SetData(float[] data,int offset) {Data=data;return data.Length==samples*2&&offset==0;}
    }
    public class AudioSource : Object
    {
        public bool playOnAwake,loop,ignoreListenerPause,isPlaying;
        public float spatialBlend,dopplerLevel,volume,pitch;
        public int timeSamples,PlayCount,PauseCount,UnPauseCount,StopCount;
        public AudioClip clip;
        public void Play() {PlayCount++;isPlaying=true;timeSamples=0;}
        public void Pause() {PauseCount++;isPlaying=false;}
        public void UnPause() {UnPauseCount++;isPlaying=true;}
        public void Stop() {StopCount++;isPlaying=false;timeSamples=0;}
    }
    public static class Mathf
    {
        public static float Clamp01(float x) {return Clamp(x,0,1);}
        public static float Clamp(float x,float a,float b) {return Math.Max(a,Math.Min(b,x));}
    }
    public static class Time {public static float timeScale=1;}
    public static class AudioListener {public static bool pause;}
    public static class Debug {public static void LogWarning(string message) {}}
}

// These are the only dependencies on the immutable accepted visual class; source verification
// pins its full SHA256, including these constants. The visual implementation is not replaced.
public static class SlazeyaStormVisualController
{
    public const float GatherDuration=1.35f;
    public const float TailDuration=1.20f;
}

public static class SlazeyaStormAudioManagedChecks
{
    private static int _passed;
    private static string _waves, _scratch;
    private static void Require(bool condition,string message) {if(!condition)throw new Exception(message);}
    private static void Near(float value,float expected,string name) {Require(Math.Abs(value-expected)<0.00001f,name+": "+value+" != "+expected);}
    private static void Run(string name,Action action)
    {
        UnityEngine.Time.timeScale=1;UnityEngine.AudioListener.pause=false;
        action();_passed++;Console.WriteLine("PASS "+name);
    }
    private static SlazeyaStormAudioController Create(Func<float> volume=null,string directory=null)
    {
        return new SlazeyaStormAudioController(new UnityEngine.Vector3(10,0,1),directory??_waves,volume);
    }
    public static int Main(string[] args)
    {
        try
        {
            _waves=args[0];_scratch=args[1];Directory.CreateDirectory(_scratch);
            Run("crafted gather establishes early sound then grows to .55",()=>
            {
                Near(SlazeyaStormAudioController.GatherEnvelope(0),0,"initial gather");
                Near(SlazeyaStormAudioController.GatherEnvelope(.03f),.32033525f,"30ms cast onset");
                Near(SlazeyaStormAudioController.GatherEnvelope(1.35f),.55f,"full gather gain");
                Near(SlazeyaStormAudioController.GatherEnvelope(4f),.55f,"waiting gain cap");
            });
            Run("actual PCM files: exact duration/interleaving and cached AudioClip samples",()=>
            {
                var g=SlazeyaStormAudioController.ReadPcm16Wave(Path.Combine(_waves,"gather_loop.wav"),44100);
                var b=SlazeyaStormAudioController.ReadPcm16Wave(Path.Combine(_waves,"burst_tail.wav"),52920);
                Require(g.Length==88200&&b.Length==105840,"wrong PCM sample count");
                using(var a=Create())using(var c=Create())
                {
                    Require(a.Root.Sources.Count==2,"must own exactly two sources");
                    Require(a.GatherSource.clip.Data.SequenceEqual(g)&&a.BurstSource.clip.Data.SequenceEqual(b),"PCM conversion changed");
                    Require(object.ReferenceEquals(a.GatherSource.clip,c.GatherSource.clip),"clip cache not reused");
                    var shared=c.GatherSource.clip;a.Dispose();Require(!shared.Destroyed,"instance disposal destroyed cached clip");
                }
            });
            Run("gather gain, waiting gate, master-times-effects once, mute and restoration",()=>
            {
                float option=0.25f;
                using(var c=Create(()=>option))
                {
                    c.Advance(1.35f);Near(c.GatherVolume,0.1375f,"option squared");
                    c.Advance(0.4f);Require(!c.BurstTriggered&&c.BurstStartCount==0,"waiting invented burst");
                    var source=c.GatherSource;
                    Require(source.loop&&!source.playOnAwake&&!source.ignoreListenerPause&&source.spatialBlend==0&&source.dopplerLevel==0,"source flags");
                    option=0;c.Advance(0);Near(c.GatherVolume,0,"mute");
                    option=0.5f;c.Advance(0);Near(c.GatherVolume,0.275f,"restore");
                    Require(source.PlayCount==1,"volume change restarted loop");
                }
            });
            Run("one burst, duplicate callback ignored, gather stops after 40ms",()=>
            {
                using(var c=Create())
                {
                    c.Advance(1.55f);Require(c.TriggerBurst()&&!c.TriggerBurst(),"burst latch");
                    Near(c.BurstVolume,0.78f,"burst gain");Require(c.BurstStartCount==1&&c.BurstSource.PlayCount==1,"duplicate playback");
                    c.Advance(0.02f);Near(c.GatherVolume,0.275f,"gather release midpoint");
                    var gather=c.GatherSource;c.Advance(0.021f);
                    Require(!gather.isPlaying&&gather.volume==0,"loop leaked after release");
                    c.Advance(1.16f);Require(c.IsComplete&&c.Root==null,"tail source cleanup");
                }
            });
            Run("timeScale pause/deferred burst/resume seek and speed changes",()=>
            {
                using(var c=Create())
                {
                    c.Advance(0.5f);var gather=c.GatherSource;
                    UnityEngine.Time.timeScale=0;c.Advance(0.5f);Near(c.Elapsed,0.5f,"paused clock advanced");
                    Require(!gather.isPlaying&&gather.PauseCount==1,"time pause not applied");
                    c.TriggerBurst();Require(c.BurstStartCount==0,"burst started while paused");
                    c.Advance(0.2f);Near(c.BurstAge,0,"paused burst advanced");
                    UnityEngine.Time.timeScale=0.5f;c.Advance(0.1f);
                    Require(c.BurstStartCount==1&&Math.Abs(c.BurstSource.timeSamples-4410)<=1,"resume missed visual-clock seek");
                    Near(c.BurstSource.pitch,0.5f,"slow-motion pitch");
                    UnityEngine.Time.timeScale=2;c.Advance(0.1f);Near(c.BurstSource.pitch,2,"fast pitch");
                    Require(Math.Abs(c.BurstSource.timeSamples-8820)<=1,"speed-change synchronization");
                    UnityEngine.Time.timeScale=0.005f;c.Advance(0);Near(c.BurstSource.pitch,0.005f,"positive slow motion must not be sped up by a pitch floor");
                }
            });
            Run("listener pause respected without changing global state or replaying delayed hit",()=>
            {
                using(var c=Create())
                {
                    UnityEngine.AudioListener.pause=true;c.TriggerBurst();c.Advance(0.2f);
                    Require(UnityEngine.AudioListener.pause&&c.BurstStartCount==0,"listener pause bypassed");
                    UnityEngine.AudioListener.pause=false;c.Advance(0);
                    Require(c.BurstStartCount==1&&Math.Abs(c.BurstSource.timeSamples-8820)<=1,"late hit restarted at zero");
                }
            });
            Run("muted burst advances; unmute does not restart it",()=>
            {
                float volume=0;
                using(var c=Create(()=>volume))
                {
                    c.TriggerBurst();c.Advance(0.3f);Near(c.BurstVolume,0,"muted burst audible");
                    volume=0.4f;c.Advance(0);Near(c.BurstVolume,0.312f,"dynamic burst restore");
                    Require(c.BurstStartCount==1&&c.BurstSource.PlayCount==1,"unmute restarted hit");
                }
            });
            Run("cancel/dispose is immediate, idempotent, and keeps cache alive",()=>
            {
                var c=Create();var root=c.Root;var gather=c.GatherSource;var burst=c.BurstSource;
                c.Advance(0.5f);c.Dispose();c.Dispose();c.Advance(1);
                Require(c.IsDisposed&&c.IsComplete&&!root.Active&&root.Destroyed&&!gather.isPlaying&&!burst.isPlaying,"cancel leaked audio");
                Require(!c.TriggerBurst(),"disposed companion accepted callback");
            });
            Run("missing resources and missing burst clip are optional",()=>
            {
                string absent=Path.Combine(_scratch,"absent");
                using(var c=Create(null,absent)){Require(c.Root==null,"missing WAV created source");c.Advance(1.4f);c.TriggerBurst();c.Advance(1.2f);Require(c.IsComplete,"missing audio blocked completion");}
                string partial=Path.Combine(_scratch,"partial");Directory.CreateDirectory(partial);File.Copy(Path.Combine(_waves,"gather_loop.wav"),Path.Combine(partial,"gather_loop.wav"),true);
                using(var c=Create(null,partial)){Require(c.GatherSource.clip!=null&&c.BurstSource.clip==null,"partial resource handling");c.TriggerBurst();c.Advance(1.2f);Require(c.IsComplete,"missing burst blocked tail");}
            });
            Run("strict parser rejects malformed, wrong-format and oversized input",CheckRejectedWaves);
            Run("parser accepts legal odd ancillary chunks and preserves sample bytes",()=>
            {
                byte[] original=File.ReadAllBytes(Path.Combine(_waves,"gather_loop.wav"));
                byte[] changed=new byte[original.Length+12];Array.Copy(original,0,changed,0,12);
                Array.Copy(new byte[]{74,85,78,75,3,0,0,0,7,8,9,0},0,changed,12,12);
                Array.Copy(original,12,changed,24,original.Length-12);Put(changed,4,(uint)changed.Length-8);
                string path=Path.Combine(_scratch,"legal-junk.wav");File.WriteAllBytes(path,changed);
                Require(SlazeyaStormAudioController.ReadPcm16Wave(path,44100).SequenceEqual(SlazeyaStormAudioController.ReadPcm16Wave(Path.Combine(_waves,"gather_loop.wav"),44100)),"ancillary chunk changed data");
            });
            Console.WriteLine("MANAGED_AUDIO_CONTRACT_PASS cases="+_passed+"; Unity API substitutes only, NOT native playback/listening.");
            return 0;
        }
        catch(Exception ex){Console.WriteLine("FAIL "+ex);return 1;}
    }
    private static void Put(byte[] bytes,int offset,uint value){Array.Copy(BitConverter.GetBytes(value),0,bytes,offset,4);}
    private static void Reject(byte[] bytes,string name)
    {
        string path=Path.Combine(_scratch,name+".wav");File.WriteAllBytes(path,bytes);
        try{SlazeyaStormAudioController.ReadPcm16Wave(path,44100);throw new Exception("Accepted "+name);}
        catch(InvalidDataException){}
    }
    private static void CheckRejectedWaves()
    {
        byte[] source=File.ReadAllBytes(Path.Combine(_waves,"gather_loop.wav"));
        byte[] bad=(byte[])source.Clone();bad[0]=0;Reject(bad,"bad-riff");
        bad=(byte[])source.Clone();Put(bad,4,1);Reject(bad,"riff-length");
        bad=(byte[])source.Clone();bad[22]=1;Reject(bad,"mono");
        bad=(byte[])source.Clone();Put(bad,24,48000);Reject(bad,"wrong-rate");
        bad=(byte[])source.Clone();bad[34]=24;Reject(bad,"24bit");
        bad=(byte[])source.Clone();bad[20]=3;Reject(bad,"float-format");
        bad=(byte[])source.Clone();Put(bad,28,44100);Reject(bad,"wrong-byte-rate");
        bad=(byte[])source.Clone();bad[32]=2;Reject(bad,"wrong-align");
        bad=(byte[])source.Clone();Put(bad,16,uint.MaxValue);Reject(bad,"chunk-overflow");
        Reject(source.Take(25).ToArray(),"truncated");
        Reject(new byte[SlazeyaStormAudioController.MaxWaveBytes+1],"oversized");
        Reject(File.ReadAllBytes(Path.Combine(_waves,"burst_tail.wav")),"wrong-duration");
        bad=new byte[source.Length*2-36];Array.Copy(source,bad,source.Length);Array.Copy(source,36,bad,source.Length,source.Length-36);Put(bad,4,(uint)bad.Length-8);Reject(bad,"duplicate-data");
    }
}
