using UnityEngine;

namespace Steria.WaterThunder
{
    /// <summary>Real Shuriken particles, analytically seekable; no damage/game-time side effects.</summary>
    [ExecuteAlways]
    public sealed class WaterThunderImpact : MonoBehaviour
    {
        public ParticleSystem[] systems=new ParticleSystem[3];
        readonly ParticleSystem.Particle[] particles=new ParticleSystem.Particle[48];
        MaterialPropertyBlock electrical;
        static float R(int i,int salt){uint x=(uint)(i*747796405+salt*2891336453L);x=((x>>((int)(x>>28)+4))^x)*277803737u;return ((x>>22)^x)/4294967295f;}
        public int Alive {get{int n=0;foreach(var ps in systems)if(ps!=null)n+=ps.particleCount;return n;}}
        public void ApplyAt(float time,bool visible)
        {
            float age=time-WaterThunderMotion.HitTime;
            for(int role=0;role<systems.Length;role++)
            {
                var ps=systems[role];if(ps==null)continue;
                int count=0,total=role==0?38:role==1?14:2;
                if(visible&&age>=0&&time<WaterThunderMotion.Duration)
                for(int i=0;i<total;i++)
                {
                    float life=role==0?.29f+.28f*R(i,3):role==1?.10f+.18f*R(i,8):.12f-i*.025f;
                    if(age>=life)continue;
                    float angle=R(i,1)*Mathf.PI*2;
                    float speed=role==0?1.45f+3.7f*R(i,4):2+4*R(i,4);
                    var velocity=new Vector3(Mathf.Cos(angle)*speed,Mathf.Sin(angle)*speed*.8f+.65f,(R(i,7)-.5f)*1.3f);
                    float q=age/life;
                    var p=new ParticleSystem.Particle();p.randomSeed=(uint)(i+1);p.startLifetime=life;p.remainingLifetime=life-age;
                    p.position=role==2?Vector3.zero:velocity*age+new Vector3(0,-2.8f*age*age,0);
                    float size=role==0?.05f+.09f*R(i,9):role==1?.32f+.40f*R(i,9):i==0?1.25f:1.65f;
                    if(role==0)p.startSize3D=new Vector3(size*.55f,size*1.6f,size);
                    else p.startSize=size*(role==2?1+q*.8f:1-q*.6f);
                    p.rotation=angle*Mathf.Rad2Deg;
                    float alpha=Mathf.Pow(1-q,role==2?1.8f:.8f);
                    p.startColor=role==0?new Color(.34f,.84f,1,alpha):role==1?new Color(.69f,.39f,1,alpha):new Color(.65f,.9f,1,alpha*(i==0?1:.35f));
                    if(role==0)
                    {
                        float seed=WaterDropletMotion.Seed(i+100);bool ligament=i%8==0;
                        Vector3 offset,current;WaterDropletMotion.Flight(velocity,age,seed,out offset,out current);p.position=offset;
                        float stretch=WaterDropletMotion.Stretch(velocity.magnitude,age,seed,ligament);
                        p.startSize3D=new Vector3(size*stretch,size/Mathf.Sqrt(stretch),size/Mathf.Sqrt(stretch));
                        p.rotation=-Mathf.Atan2(current.y,current.x)*Mathf.Rad2Deg;
                        alpha=1-Mathf.SmoothStep(0,1,WaterThunderMotion.Span(.88f,1,q));
                        if(transform.TransformPoint(p.position).y<.025f)alpha=0;
                        p.startColor=WaterDropletMotion.DataColor(seed,age,life,ligament,alpha);
                    }
                    particles[count++]=p;
                }
                // Initialize native Shuriken before assigning particles: its first Simulate clears cold data.
                ps.Simulate(0,false,false,false);ps.SetParticles(particles,count);ps.Pause(false);
                if(role==1)
                {
                    if(electrical==null)electrical=new MaterialPropertyBlock();
                    electrical.Clear();electrical.SetFloat("_ElectricalTime",age);ps.GetComponent<ParticleSystemRenderer>().SetPropertyBlock(electrical);
                }
            }
        }
        void OnDisable(){foreach(var ps in systems)if(ps!=null)ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);}
    }
}
