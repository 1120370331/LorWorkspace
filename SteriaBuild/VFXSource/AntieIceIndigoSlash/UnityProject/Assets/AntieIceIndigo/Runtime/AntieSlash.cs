using UnityEngine;

namespace Steria.AntieVfx
{
    public enum AntieAttackKind { HitVertical, SlashHorizontal, PierceDual }
    [ExecuteAlways]
    public sealed class AntieSlash : MonoBehaviour
    {
        public const float Duration=.94f;
        public AntieAttackKind kind;
        public MeshRenderer[] blades=new MeshRenderer[2],glows=new MeshRenderer[2];
        public bool autoPlay=true,animationDriven=true,mirror,showBlade=true,showGlow=true;
        [Range(0,Duration)] public float sampleTime=.20f;
        [Range(.1f,2)] public float speed=1;
        MaterialPropertyBlock block;float last;
        public static float Span(float a,float b,float x){return Mathf.Clamp01((x-a)/(b-a));}
        static Vector3 Cubic(float t,Vector3 a,Vector3 b,Vector3 c,Vector3 d){float q=1-t;return q*q*q*a+3*q*q*t*b+3*q*t*t*c+t*t*t*d;}
        // Horizontal: convex cutting edge toward the camera (-Z); concave side
        // rises away from the camera. Keep a 16-degree tilt from the ground.
        // Dual: reverse the two faces around their pivot; retain their camera cant.
        public static Quaternion BladePlane(AntieAttackKind kind,int blade)
        {
            if(kind==AntieAttackKind.HitVertical)return Quaternion.identity;
            if(kind==AntieAttackKind.SlashHorizontal)return Quaternion.Euler(-106,0,0);
            return Quaternion.Euler(0,blade==0?246:250,0);
        }
        static Vector3 PlanePivot(AntieAttackKind kind)
        {return kind==AntieAttackKind.SlashHorizontal?new Vector3(.20f,1.65f,0):new Vector3(.06f,1.70f,0);}
        static Vector3 FlatCurve(AntieAttackKind kind,int blade,float t)
        {
            t=Mathf.Clamp01(t);
            if(kind==AntieAttackKind.HitVertical)
            {
                float a=Mathf.Lerp(90,-90,t)*Mathf.Deg2Rad;
                return new Vector3(.3f+1.85f*Mathf.Cos(a),1.9f+1.85f*Mathf.Sin(a),0);
            }
            if(kind==AntieAttackKind.SlashHorizontal)return Cubic(t,new Vector3(-1.9f,1.45f,0),new Vector3(-.55f,2.28f,0),new Vector3(2.15f,1.93f,0),new Vector3(2.25f,.96f,0));
            var v=Cubic(t,new Vector3(-1.45f,3.05f,0),new Vector3(-1.10f,2.60f,0),new Vector3(1.40f,1.30f,0),new Vector3(1.55f,.32f,0));
            if(blade==1){v.x=-v.x+.12f;v.y+=.09f;v.z=.018f;}return v;
        }
        public static Vector3 Curve(AntieAttackKind kind,int blade,float t)
        {
            var point=FlatCurve(kind,blade,t);
            if(kind==AntieAttackKind.HitVertical)return point;
            var pivot=PlanePivot(kind);return pivot+BladePlane(kind,blade)*(point-pivot);
        }
        public static Vector3 Normal(AntieAttackKind kind,int blade,float t)
        {
            var d=FlatCurve(kind,blade,Mathf.Min(1,t+.001f))-FlatCurve(kind,blade,Mathf.Max(0,t-.001f));
            return BladePlane(kind,blade)*new Vector3(d.y,-d.x,0).normalized;
        }
        public static float Delay(AntieAttackKind kind,int blade){return kind==AntieAttackKind.PierceDual&&blade==1?.065f:0;}
        public static float SweepDuration(AntieAttackKind kind){return kind==AntieAttackKind.SlashHorizontal?.125f:kind==AntieAttackKind.HitVertical?.160f:.145f;}
        public static float Progress(AntieAttackKind kind,int blade,float time)
        {
            return Mathf.SmoothStep(0,1,Span(0,SweepDuration(kind),time-Delay(kind,blade)));
        }
        public static float RibbonWidth(AntieAttackKind kind,float t)
        {
            float w=kind==AntieAttackKind.HitVertical?.84f:kind==AntieAttackKind.SlashHorizontal?.64f:.54f;
            return w*Mathf.Lerp(.55f,1,Span(.04f,.70f,t))*(1-Mathf.SmoothStep(0,1,Span(.975f,1,t)));
        }
        public void BindNative()
        {
            for(int i=0;i<2;i++)
            {
                var blade=transform.Find("Blade_"+i);var glow=transform.Find("Glow_"+i);
                blades[i]=blade==null?null:blade.GetComponent<MeshRenderer>();glows[i]=glow==null?null:glow.GetComponent<MeshRenderer>();
            }
        }
        void OnEnable(){last=Time.realtimeSinceStartup;}
        void LateUpdate(){if(Application.isPlaying){if(animationDriven)ApplyAt(sampleTime);else Tick();}}
        public void Tick()
        {
            float now=Time.realtimeSinceStartup;
            if(autoPlay){sampleTime=Mathf.Repeat(sampleTime+Mathf.Min(.1f,now-last)*speed,Duration+.24f);ApplyAt(sampleTime);}
            last=now;
        }
        public void ApplyAt(float time)
        {
            if(block==null)block=new MaterialPropertyBlock();sampleTime=time;
            var scale=transform.localScale;scale.x=Mathf.Abs(scale.x)*(mirror?-1:1);transform.localScale=scale;
            for(int i=0;i<2;i++)
            {
                float local=time-Delay(kind,i),sweep=SweepDuration(kind),progress=Progress(kind,i,time);
                float alpha=Span(0,.012f,local)*(1-Span(.66f,.85f,local));
                if(local<0||time>=Duration)alpha=0;
                if(blades[i]!=null)
                {
                    blades[i].enabled=showBlade&&alpha>.001f;
                    block.Clear();block.SetFloat("_Reveal",1);block.SetFloat("_SweepProgress",progress);
                    block.SetFloat("_SweepWidth",kind==AntieAttackKind.HitVertical?.34f:kind==AntieAttackKind.SlashHorizontal?.29f:.31f);
                    block.SetFloat("_Retreat",.70f*Mathf.SmoothStep(0,1,Span(.29f,.70f,local)));
                    block.SetFloat("_Phase",Mathf.Max(0,local));block.SetFloat("_Alpha",alpha*(kind==AntieAttackKind.PierceDual?.91f:1));
                    block.SetFloat("_Dissolve",Span(.44f,.85f,local));block.SetFloat("_FlowSpeed",kind==AntieAttackKind.HitVertical?1.05f:kind==AntieAttackKind.SlashHorizontal?1.40f:1.20f);
                    block.SetVector("_FlowOffset",kind==AntieAttackKind.SlashHorizontal?new Vector4(.15f,.008f,0,0):new Vector4(.12f,i==0?.012f:-.008f,0,0));
                    block.SetColor("_Tint",kind==AntieAttackKind.PierceDual&&i==1?new Color(.90f,.96f,1,1):Color.white);
                    blades[i].SetPropertyBlock(block);
                }
                if(glows[i]!=null)
                {
                    float a=alpha*(1-Span(sweep,.31f,local))*.14f;glows[i].enabled=showGlow&&a>.001f;
                    var plane=BladePlane(kind,i);
                    glows[i].transform.localPosition=Curve(kind,i,progress)+plane*Vector3.forward*.035f;
                    glows[i].transform.localRotation=plane;
                    glows[i].transform.localScale=Vector3.one*Mathf.Lerp(.38f,.82f,Span(0,sweep,local));
                    block.Clear();block.SetFloat("_Alpha",a);glows[i].SetPropertyBlock(block);
                }
            }
        }
    }
}
