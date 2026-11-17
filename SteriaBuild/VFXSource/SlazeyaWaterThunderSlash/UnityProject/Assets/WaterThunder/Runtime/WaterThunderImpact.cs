using UnityEngine;

namespace Steria.WaterThunder
{
    /// <summary>One radial needle burst with sparse spatial foam. Absolute-time seek is deterministic.</summary>
    [ExecuteAlways]
    public sealed class WaterThunderImpact : MonoBehaviour
    {
        public ParticleSystem[] systems=new ParticleSystem[3];
        public int burstSeed;
        static int sequence=19037;
        readonly ParticleSystem.Particle[] particles=new ParticleSystem.Particle[48];
        MaterialPropertyBlock needleBlock;
        int Seed {get{if(burstSeed==0)burstSeed=System.Threading.Interlocked.Increment(ref sequence);return burstSeed;}}
        static float R(int i,int salt){uint x=(uint)(i*747796405+salt*2891336453L);x=((x>>((int)(x>>28)+4))^x)*277803737u;return ((x>>22)^x)/4294967295f;}
        float RandomAt(int i,int salt){return R(i+Seed,salt);}
        float Gaussian(int i,int salt){return Mathf.Sqrt(-2*Mathf.Log(Mathf.Max(.00001f,RandomAt(i,salt))))*Mathf.Cos(2*Mathf.PI*RandomAt(i,salt+1));}
        public int NeedleCount {get{return 16+Mathf.Min(6,Mathf.FloorToInt(RandomAt(0,91)*7));}}
        public const int FoamCount=7;
        public static float BurstRadius {get{return WaterThunderMotion.ArcRadius*.72f;}}
        public int Alive {get{int n=0;foreach(var ps in systems)if(ps!=null)n+=ps.particleCount;return n;}}
        public static float NeedleTravel(float age,float life,float drag,float drive)
        {
            if(age<=0)return 0;if(age>=life)return 1;
            float sum=drag+drive;
            float current=(1-Mathf.Exp(-drag*age))/drag-(1-Mathf.Exp(-sum*age))/sum;
            float full=(1-Mathf.Exp(-drag*life))/drag-(1-Mathf.Exp(-sum*life))/sum;
            return Mathf.Clamp01(current/Mathf.Max(.00001f,full));
        }
        public void ApplyAt(float time,bool visible)
        {
            float age=time-WaterThunderMotion.HitTime;
            for(int role=0;role<systems.Length;role++)
            {
                var ps=systems[role];if(ps==null)continue;
                int count=0,total=role==0?FoamCount:role==1?NeedleCount:1;
                if(visible&&age>=0&&time<WaterThunderMotion.Duration)
                for(int i=0;i<total;i++)
                {
                    float born=role==0?RandomAt(i,12)*.085f:0;
                    float life=role==0?.32f+.15f*RandomAt(i,3):role==1?.22f+.12f*RandomAt(i,8):.065f;
                    float localAge=age-born;if(localAge<0||localAge>=life)continue;
                    float q=localAge/life;
                    float angle=(role==1?(i+RandomAt(i,1)*.85f)/total+RandomAt(0,51):RandomAt(i,1))*Mathf.PI*2;
                    var direction=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
                    var p=new ParticleSystem.Particle();p.randomSeed=(uint)(i+1);p.startLifetime=life;p.remainingLifetime=life-localAge;
                    float alpha=Mathf.Pow(1-q,role==0?1.1f:.90f);
                    if(role==1)
                    {
                        float length=Mathf.Clamp(.22f+.05f*Gaussian(i,20),.12f,.34f);
                        float radius=BurstRadius*(.45f+.55f*RandomAt(i,4));
                        float drag=5.5f+3*RandomAt(i,66),drive=22+12*RandomAt(i,67);
                        float distance=length*.5f+(radius-length)*NeedleTravel(localAge,life,drag,drive);
                        p.position=direction*distance+new Vector3(0,0,(RandomAt(i,7)-.5f)*.12f);
                        float width=Mathf.Clamp(.055f+length*.09f,.065f,.085f);
                        p.startSize3D=new Vector3(length*(1-.18f*q),width,width);
                        p.rotation=-angle*Mathf.Rad2Deg;p.startColor=new Color(1,1,1,alpha);
                    }
                    else if(role==0)
                    {
                        float radius=.24f+.40f*RandomAt(i,4);
                        p.position=direction*(radius+.10f*q)+new Vector3(0,.10f*q,(RandomAt(i,7)-.5f)*.52f);
                        float size=Mathf.Clamp(.055f+.016f*Gaussian(i,30),.025f,.09f);
                        p.startSize=size*(1+.20f*q);p.rotation=angle*Mathf.Rad2Deg+q*25;
                        p.startColor=new Color(.55f,.85f,1,alpha*.60f);
                    }
                    else
                    {
                        p.position=Vector3.zero;p.startSize=.35f*(1+.6f*q);p.startColor=new Color(.55f,.85f,1,alpha*.65f);
                    }
                    particles[count++]=p;
                }
                ps.Simulate(0,false,false,false);ps.SetParticles(particles,count);ps.Pause(false);
                if(role==1)
                {
                    if(needleBlock==null)needleBlock=new MaterialPropertyBlock();needleBlock.Clear();
                    needleBlock.SetFloat("_NeedleTime",Mathf.Max(0,age));ps.GetComponent<ParticleSystemRenderer>().SetPropertyBlock(needleBlock);
                }
            }
        }
        void OnDisable(){foreach(var ps in systems)if(ps!=null)ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);}
    }
}
