using UnityEngine;

namespace Steria.WaterThunder
{
    /// <summary>Authoring-only deterministic four-layer preview. No game/skin dependency.</summary>
    [ExecuteAlways]
    public sealed class WaterThunderSlash : MonoBehaviour
    {
        public const float Duration = WaterThunderMotion.Duration;
        [Range(0, Duration)] public float sampleTime = .24f;
        public bool autoPlay = true;
        [Range(.05f, 2)] public float playbackSpeed = 1;
        public bool mirror;
        public bool water = true, splash = true, lightning = true, core = true;
        public MeshRenderer[] layers = new MeshRenderer[4];
        public Mesh splashSource;
        public bool evaporate=true, impactBurst=true;
        public MeshRenderer evaporationMist;
        public WaterThunderImpact impact;
        Mesh splashMesh;
        Vector3[] rest, positions;
        Vector2[] births, velocities;
        Color32[] colors;
        MaterialPropertyBlock block;
        float clock;
        bool previewDirty=true;
        public bool NeedsRefresh {get{return autoPlay||previewDirty;}}
        static readonly int Reveal = Shader.PropertyToID("_Reveal"), Retreat = Shader.PropertyToID("_Retreat"),
            Phase = Shader.PropertyToID("_Phase"), Alpha = Shader.PropertyToID("_Alpha"), Dissolve = Shader.PropertyToID("_Dissolve");

        void OnEnable() { clock = Time.realtimeSinceStartup;previewDirty=true; }
        void Update()
        {
            if(Application.isPlaying) TickPreview();
        }
        public void TickPreview()
        {
            float now = Time.realtimeSinceStartup;
            if (autoPlay) sampleTime = Mathf.Repeat(sampleTime + Mathf.Min(now-clock, .1f)*playbackSpeed, Duration+.25f);
            else if(!previewDirty){clock=now;return;}
            clock=now; ApplyAt(sampleTime);
        }
        void OnValidate() { previewDirty=true; }
        static float Span(float a,float b,float t) { return Mathf.Clamp01((t-a)/(b-a)); }
        void Prepare()
        {
            if (block == null) block = new MaterialPropertyBlock();
            if (layers == null || layers.Length != 4 || layers[1] == null) return;
            var filter = layers[1].GetComponent<MeshFilter>();
            if(splashMesh!=null){filter.sharedMesh=splashMesh;return;}
            Mesh source=splashSource!=null?splashSource:filter.sharedMesh;
            if (source == null) return;
            splashMesh = Instantiate(source); splashMesh.name="Splash (instance simulation)";
            splashMesh.hideFlags=HideFlags.DontSave;filter.sharedMesh=splashMesh;
            rest=splashMesh.vertices; positions=new Vector3[rest.Length];births=splashMesh.uv2;
            velocities=splashMesh.uv3; colors=splashMesh.colors32;
        }
        public void ApplyAt(float time)
        {
            if(!gameObject.scene.IsValid())return;
            if (layers == null || layers.Length != 4) return;
            Prepare();sampleTime=time;previewDirty=false;
            var scale=transform.localScale;scale.x=Mathf.Abs(scale.x)*(mirror?-1:1);transform.localScale=scale;
            float reveal=WaterThunderMotion.Progress(time);
            bool[] toggles={water,splash,lightning,core};
            for(int i=0;i<4;i++)
            {
                if(layers[i]==null) continue;
                float a=0,retreat=0,fracture=0;
                if(i==0) { a=1-Span(.72f,.94f,time); }
                if(i==1) a=1;
                if(i==2) { a=1-Span(.52f,.72f,time); }
                if(i==3) { a=1-Span(.32f,.435f,time); }
                if(time<0 || time>=Duration) a=0;
                layers[i].enabled=toggles[i] && a>.002f && (i!=1 || time>.035f && time<Duration);
                block.Clear();block.SetFloat(Reveal,1);block.SetFloat("_SweepProgress",reveal);block.SetFloat(Retreat,retreat);
                block.SetFloat(Phase,time);block.SetFloat(Alpha,a);block.SetFloat(Dissolve,fracture);
                block.SetFloat("_TailGrowth",i==0?Mathf.SmoothStep(0,1,Span(.12f,.37f,reveal)):1);
                block.SetFloat("_Evaporation",i==0&&evaporate?1:0);
                block.SetVector("_EvapFrame",WaterThunderMotion.EvaporationFrame);
                block.SetFloat("_EvapProgress",WaterThunderMotion.EvaporationProgress(time));
                block.SetFloat("_EvapSpread",WaterThunderMotion.EvaporationSpread(time));
                block.SetFloat("_EvapLift",WaterThunderMotion.EvaporationLift(time));
                layers[i].SetPropertyBlock(block);
            }
            if(evaporationMist!=null)
            {
                float q=WaterThunderMotion.EvaporationProgress(time),spread=WaterThunderMotion.EvaporationSpread(time);
                var frame=WaterThunderMotion.EvaporationFrame;
                evaporationMist.enabled=evaporate&&q>0&&q<1&&time<Duration&&time>=0;
                evaporationMist.transform.localPosition=new Vector3(frame.x,frame.y+WaterThunderMotion.EvaporationLift(time),-.055f);
                evaporationMist.transform.localScale=new Vector3(frame.z*spread,frame.w*spread,1);
                block.Clear();block.SetFloat(Reveal,1);block.SetFloat(Retreat,0);block.SetFloat(Phase,time);
                block.SetFloat(Alpha,Mathf.Sin(q*Mathf.PI)*Mathf.Pow(1-q,.65f)*.90f);block.SetFloat(Dissolve,0);block.SetFloat("_Evaporation",0);
                evaporationMist.SetPropertyBlock(block);
            }
            if(impact!=null)impact.ApplyAt(time,impactBurst);
            if(splashMesh==null) return;
            for(int start=0;start<rest.Length;start+=4)
            {
                int index=start/4;float age=time-births[start].x,life=births[start].y,seed=WaterDropletMotion.Seed(index);
                bool ligament=index%8==0;
                Vector3 center=(rest[start]+rest[start+1]+rest[start+2]+rest[start+3])*.25f;
                float radius=Vector3.Distance(rest[start],rest[start+1])*.5f;
                Vector3 initial=new Vector3(velocities[start].x,velocities[start].y,0),offset,velocity;
                WaterDropletMotion.Flight(initial,age,seed,out offset,out velocity);
                var axis=new Vector3(velocity.x,velocity.y,0).normalized;if(axis.sqrMagnitude<.1f)axis=Vector3.right;
                var across=new Vector3(-axis.y,axis.x,0);
                float stretch=WaterDropletMotion.Stretch(initial.magnitude,age,seed,ligament);
                float fade=age<0?0:Span(0,.018f,age)*(1-Span(life*.88f,life,age));
                if(transform.TransformPoint(center+offset).y<.025f)fade=0;
                for(int j=0;j<4;j++)
                {
                    float u=j%2-.5f,v=j/2-.5f;
                    positions[start+j]=center+offset+axis*(u*radius*2*stretch)+across*(v*radius*2/Mathf.Sqrt(stretch));
                    colors[start+j]=WaterDropletMotion.DataColor(seed,age,life,ligament,fade);
                }
            }
            splashMesh.vertices=positions;splashMesh.colors32=colors;splashMesh.RecalculateBounds();
        }
        public void RestoreSourceForSave()
        {
            previewDirty=true;
            if(splashSource!=null && layers!=null && layers.Length==4 && layers[1]!=null)
                layers[1].GetComponent<MeshFilter>().sharedMesh=splashSource;
        }
        void OnDestroy()
        {
            if(splashMesh==null) return;
            if(Application.isPlaying) Destroy(splashMesh); else DestroyImmediate(splashMesh);
        }
    }
}
