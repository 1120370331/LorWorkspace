using System;
using UnityEngine;

namespace Steria.Vfx
{
    public enum SlazeyaWindKind { Slash, Hit, Pierce, Guard, Projectile }

    /// <summary>Deterministic native Shuriken playback. The caller owns the root and its attachment.</summary>
    public sealed class SlazeyaWindVisual
    {
        public const string BundleName = "steria_slazeya_wind_combat";
        private readonly GameObject root;
        private readonly ParticleSystem[] systems;
        private readonly ParticleSystemRenderer[] renderers;
        private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        private readonly SlazeyaWindKind kind;
        private readonly uint seed;
        private readonly Transform primary;
        private readonly Transform secondary;
        private readonly bool sweptStyle;
        private readonly ParticleSystem impactRing,impactFlash,impactMotes;
        private bool pierceConfirmed,pierceImpacted;
        private float pierceImpactAt,pierceRadius=1f;
        private Vector3 pierceContact,pierceExit,pierceContactWorld,pierceExitWorld,impactWorld;
        public const float PierceContactTime=.12f;
        public bool PierceImpactOccurred { get { return pierceImpacted&&!IsComplete; } }
        public Vector3 PierceTipWorld { get { return primary==null?root.transform.position:primary.TransformPoint(Vector3.left); } }
        public Vector3 PierceImpactPosition { get { return impactWorld; } }
        private float accumulator;
        private int tick;
        private bool arrived;
        private float arrivalAt;
        public float Elapsed { get; private set; }
        public float Duration { get; private set; }
        public bool IsComplete { get; private set; }
        public int LiveParticleCount { get { int count=0; foreach(var p in systems)count+=p.particleCount; return count; } }
        public SlazeyaWindVisual(GameObject instance, SlazeyaWindKind kind, uint seed, float duration=0)
        {
            if(instance==null)throw new ArgumentNullException("instance");
            root=instance;this.kind=kind;this.seed=seed;
            Duration=duration>0?duration:(kind==SlazeyaWindKind.Pierce?.72f:kind==SlazeyaWindKind.Slash?.57f:kind==SlazeyaWindKind.Projectile?.53f:kind==SlazeyaWindKind.Guard?.50f:.48f);
            systems=root.GetComponentsInChildren<ParticleSystem>(true);
            renderers=root.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach(var system in systems)if(system.name=="Main")primary=system.transform;
            foreach(var system in systems)if(system.name=="Wake")secondary=system.transform;
            foreach(var system in systems)
            {
                if(system.name=="ImpactRing")impactRing=system;
                if(system.name=="ImpactFlash")impactFlash=system;
                if(system.name=="ImpactMotes")impactMotes=system;
            }
            if(kind==SlazeyaWindKind.Pierce&&(impactRing==null||impactFlash==null||impactMotes==null))
                throw new InvalidOperationException("Pierce impact systems missing; update the matching wind AssetBundle");
            if(primary!=null){var r=primary.GetComponent<ParticleSystemRenderer>();sweptStyle=r!=null&&r.sharedMaterial!=null&&r.sharedMaterial.HasProperty("_Sweep");}
            pierceContact=new Vector3(-2.8f,0,0);pierceExit=new Vector3(-4f,0,0);
            pierceContactWorld=root.transform.TransformPoint(pierceContact);pierceExitWorld=root.transform.TransformPoint(pierceExit);
            Reset();
        }
        private void Reset()
        {
            Elapsed=0;tick=0;accumulator=0;IsComplete=false;arrived=false;arrivalAt=0;
            pierceConfirmed=false;pierceImpacted=false;pierceImpactAt=0;
            for(int i=0;i<systems.Length;i++)
            {
                var ps=systems[i];ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.useAutoRandomSeed=false;ps.randomSeed=seed+(uint)(i*977);
                ps.Pause(false);
            }
            EmitAt(0);ApplyMaterials();
        }
        public void ApplyAt(float seconds)
        {
            Reset();Advance(Mathf.Max(0,seconds));
        }
        public void Advance(float deltaTime)
        {
            if(IsComplete)return;
            // A render frame may be shorter than the 120 Hz simulation step.
            // Restore world anchoring even when no particle tick is due.
            if(kind==SlazeyaWindKind.Pierce&&pierceImpacted)ApplyPierce();
            if(deltaTime<=0)return;
            accumulator+=deltaTime;
            const float step=1f/120f;
            while(accumulator+0.000001f>=step && !IsComplete)
            {
                accumulator-=step;tick++;Elapsed=tick*step;
                foreach(var ps in systems){ps.Simulate(step,false,false,false);ps.Pause(false);}
                EmitAt(tick);ApplyMaterials();
                if(Elapsed+0.00001f>=Duration)StopAndClear();
            }
        }
        private void EmitAt(int at)
        {
            foreach(var ps in systems)
            {
                string n=ps.name;
                bool main=n=="Main";
                if(n=="Arrival" || n=="Contact" || n.StartsWith("Impact",StringComparison.Ordinal) || arrived)continue;
                int count;
                if(main){if(at!=0)continue;count=1;}
                else if(n=="Wake"){if(at!=6)continue;count=1;}
                else if(n=="Silk"||n=="Motes")
                {
                    if(at!=3 && at!=9 && at!=15)continue;
                    count=kind==SlazeyaWindKind.Guard?2:n=="Motes"?3:kind==SlazeyaWindKind.Slash?4:3;
                }
                else{if(at!=8)continue;count=6;}
                for(int i=0;i<count;i++)
                {
                    var emit=new ParticleSystem.EmitParams();
                    float a=Hash(i+at*13),b=Hash(i+91+at);
                    if(!main && n!="Wake" && n!="Arrival")
                    {
                        float angle=(kind==SlazeyaWindKind.Guard?(i%2==0?75:-75):155+b*50)*Mathf.Deg2Rad;
                        emit.position=new Vector3(-.65f-a*.45f,(b-.5f)*1.15f,-.015f);
                        emit.velocity=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*(n=="Silk"?1.8f+a*1.5f:1+a);
                        if(kind==SlazeyaWindKind.Slash)
                        {
                            float arc=(52+163*(.18f+.64f*a))*Mathf.Deg2Rad;
                            Quaternion sweep=Quaternion.Euler(0,0,sweptStyle?0:SlashAngle(Elapsed));
                            Vector3 p=new Vector3(-.43f+1.42f*Mathf.Cos(arc),1.74f*Mathf.Sin(arc),0)*.74f;
                            emit.position=sweep*p+new Vector3(.12f,.12f,-.015f);
                            emit.velocity=sweep*new Vector3(-Mathf.Sin(arc),Mathf.Cos(arc),0)*(2.8f+a*2.6f);
                        }
                        else if(kind==SlazeyaWindKind.Projectile)
                        {
                            emit.position=new Vector3(.2f+a*.65f,(b-.5f)*.25f,0);
                            emit.velocity=new Vector3(.8f+a*.5f,(b-.5f)*.35f,0);
                        }
                        else if(kind==SlazeyaWindKind.Pierce)
                        {
                            emit.position=new Vector3(-.1f-a*.65f,(b-.5f)*.25f,0);
                            emit.velocity=new Vector3(-2.2f-a*1.3f,(b-.5f)*.8f,0);
                        }
                        emit.startSize=n=="Silk"?.28f+a*.20f:.12f+a*.10f;
                        if(n=="Motes")emit.startSize=.06f+a*.10f;
                        emit.rotation3D=new Vector3(0,0,Mathf.Atan2(emit.velocity.y,emit.velocity.x)*Mathf.Rad2Deg-180f);
                        emit.startLifetime=.17f+a*.22f;
                    }
                    emit.randomSeed=seed+(uint)(i*311+at*41);ps.Emit(emit,1);
                }
                // Unity 2019 needs zero-time synchronization after manual Emit.
                ps.Simulate(0,false,false,false);ps.Pause(false);
            }
        }
        private float Hash(int index)
        { uint x=seed+(uint)index*747796405u+2891336453u;x=(x^(x>>16))*2246822519u;return (x&65535)/65535f; }

        public void AimPierce(Vector3 worldContact,Vector3 worldExit,float worldRadius)
        {
            if(kind!=SlazeyaWindKind.Pierce||pierceImpacted||IsComplete)return;
            pierceContactWorld=worldContact;pierceExitWorld=worldExit;
            pierceContact=root.transform.InverseTransformPoint(worldContact);
            pierceExit=root.transform.InverseTransformPoint(worldExit);
            pierceRadius=Mathf.Max(.2f,worldRadius);
            ApplyMaterials();
        }

        public bool ConfirmPierceImpact()
        {
            if(kind!=SlazeyaWindKind.Pierce||IsComplete||pierceConfirmed)return false;
            pierceConfirmed=true;return true;
        }

        private void ApplyPierce()
        {
            if(kind!=SlazeyaWindKind.Pierce||primary==null)return;
            if(pierceImpacted)
            {
                pierceContact=root.transform.InverseTransformPoint(pierceContactWorld);
                pierceExit=root.transform.InverseTransformPoint(pierceExitWorld);
            }
            Vector3 direction=new Vector3(pierceContact.x,pierceContact.y,0);
            float contactDistance=Mathf.Max(.001f,direction.magnitude);
            float fullDistance=Mathf.Max(contactDistance,new Vector3(pierceExit.x,pierceExit.y,0).magnitude);
            float length=Elapsed<PierceContactTime?contactDistance*Mathf.Pow(Mathf.Clamp01(Elapsed/PierceContactTime),1.3f):
                Mathf.Lerp(contactDistance,pierceConfirmed?fullDistance:contactDistance,Mathf.SmoothStep(0,1,Mathf.Clamp01((Elapsed-PierceContactTime)/.075f)));
            length*=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((Elapsed-.20f)/.14f));
            Quaternion axis=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-180f);
            float width=Mathf.Clamp(contactDistance*.11f,.22f,.55f);
            primary.localPosition=Vector3.zero;primary.localRotation=axis;
            primary.localScale=new Vector3(Mathf.Max(.001f,length),width,1);
            if(secondary!=null)
            {
                secondary.localPosition=axis*new Vector3(0,-width*.18f,.01f);secondary.localRotation=axis;
                secondary.localScale=new Vector3(Mathf.Max(.001f,length*.78f),width*.42f,1);
            }
            if(pierceConfirmed&&!pierceImpacted&&Elapsed+0.00001f>=PierceContactTime)
            {
                pierceImpacted=true;pierceImpactAt=Elapsed;
                impactWorld=new Vector3(pierceContactWorld.x,pierceContactWorld.y,Mathf.Min(pierceContactWorld.z,0)-.2f);
                PlaceImpact(impactRing,pierceRadius*.18f,.78f);
                PlaceImpact(impactFlash,pierceRadius*.16f,1);
                PlaceImpact(impactMotes,1,1);
                EmitImpact(impactRing,1,.44f);EmitImpact(impactFlash,1,.14f);EmitImpact(impactMotes,8,.34f);
            }
            if(pierceImpacted)
            {
                float age=Elapsed-pierceImpactAt;
                float expansion=1-Mathf.Pow(1-Mathf.Clamp01(age/.22f),4);
                PlaceImpact(impactRing,pierceRadius*Mathf.Lerp(.18f,1,expansion),.78f);
                PlaceImpact(impactFlash,pierceRadius*Mathf.Lerp(.16f,.035f,Mathf.Clamp01(age/.14f)),1);
                PlaceImpact(impactMotes,1,1);
            }
        }

        private void PlaceImpact(ParticleSystem ps,float radius,float aspect)
        {
            if(ps==null)return;
            var t=ps.transform;t.position=impactWorld;t.rotation=Quaternion.identity;
            Vector3 scale=t.parent.lossyScale;
            t.localScale=new Vector3(radius*aspect/Mathf.Max(.001f,Mathf.Abs(scale.x)),radius/Mathf.Max(.001f,Mathf.Abs(scale.y)),1/Mathf.Max(.001f,Mathf.Abs(scale.z)));
        }

        private void EmitImpact(ParticleSystem ps,int count,float lifetime)
        {
            if(ps==null)return;
            for(int i=0;i<count;i++)
            {
                var particle=new ParticleSystem.EmitParams();particle.startLifetime=lifetime;
                particle.startColor=new Color(.92f,1f,.98f,1);
                particle.startSize3D=new Vector3(2,2,1);
                if(count>1)
                {
                    float angle=(i*45+(Hash(i+400)-.5f)*22)*Mathf.Deg2Rad;
                    particle.startSize=.10f+Hash(i+410)*.16f;
                    particle.velocity=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*(2+Hash(i+420)*4);
                    particle.startLifetime=lifetime*(.65f+.35f*Hash(i+430));
                }
                particle.randomSeed=seed+(uint)(i*317+700);ps.Emit(particle,1);
            }
            ps.Simulate(0,false,false,false);ps.Pause(false);
        }
        public void Arrive()
        {
            if(kind!=SlazeyaWindKind.Projectile || arrived || IsComplete)return;
            arrived=true;arrivalAt=Elapsed;
            foreach(var ps in systems)
            {
                ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                if(ps.name=="Arrival")
                {
                    bool softMotes=ps.GetComponent<ParticleSystemRenderer>().renderMode==ParticleSystemRenderMode.Billboard;
                    for(int i=0;i<(softMotes?7:9);i++)
                    {
                        float angle=(90+i*24+(Hash(i+120)-.5f)*19)*Mathf.Deg2Rad;
                        var particle=new ParticleSystem.EmitParams();
                        particle.startSize=softMotes?.12f+Hash(i+100)*.15f:.22f+Hash(i+100)*.51f;
                        particle.startLifetime=.13f+Hash(i+80)*.12f;
                        particle.position=new Vector3((Hash(i+140)-.5f)*.09f,(Hash(i+160)-.5f)*.12f,0);
                        particle.velocity=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*(2.8f+Hash(i+70)*1.4f);
                        particle.rotation3D=new Vector3(0,0,angle*Mathf.Rad2Deg-180);
                        ps.Emit(particle,1);
                    }
                }
                else if(ps.name=="Contact")ps.Emit(1);
                ps.Simulate(0,false,false,false);ps.Pause(false);
            }
            ApplyMaterials();
        }
        private void ApplyMaterials()
        {
            float t=Elapsed;
            ApplyPierce();
            foreach(var renderer in renderers)
            {
                if(renderer.sharedMaterial==null)continue;
                if(renderer.sharedMaterial.HasProperty("_Sweep"))
                {
                    bool returned=renderer.name=="Wake",contact=renderer.name=="Contact";
                    float age=t-(returned?.05f:contact?arrivalAt:0);
                    float p=Mathf.Clamp01(age/(contact?.04f:kind==SlazeyaWindKind.Slash?.15f:.065f));
                    float sweep=Mathf.Lerp(.12f,1,1-(1-p)*(1-p));
                    float span=renderer.sharedMaterial.GetFloat("_WedgeSpan");
                    float strength=Mathf.Clamp01(age/.022f)*Mathf.Clamp01((.39f-age)/.19f);
                    if(kind==SlazeyaWindKind.Projectile&&!contact){strength=1;sweep=1;}
                    else if(contact){strength=Mathf.Clamp01(age/.016f)*Mathf.Clamp01((.18f-age)/.12f);}
                    else if(kind==SlazeyaWindKind.Guard){sweep=1;span=1;strength=Mathf.Clamp01(age/.04f)*Mathf.Clamp01((.40f-age)/.24f);}
                    else{span=Mathf.Lerp(span,.12f,Mathf.Clamp01((age-.18f)/.20f));}
                    renderer.GetPropertyBlock(block);
                    block.SetFloat("_Sweep",sweep);block.SetFloat("_WedgeSpan",span);
                    block.SetFloat("_Phase",t);block.SetFloat("_Alpha",strength*(returned?.48f:1)*renderer.sharedMaterial.GetFloat("_Alpha"));
                    renderer.SetPropertyBlock(block);continue;
                }
                if(!renderer.sharedMaterial.HasProperty("_Reveal"))continue;
                bool wake=renderer.name=="Wake",arrival=renderer.name=="Contact";
                float local=t-(wake?.05f:arrival?arrivalAt:0);
                float reveal=Mathf.Clamp01(local/(arrival?.025f:kind==SlazeyaWindKind.Guard?.065f:.045f));
                reveal=1-(1-reveal)*(1-reveal);
                float retreat=Mathf.Clamp01((local-(kind==SlazeyaWindKind.Guard?.14f:.133f))/.21f);
                float alpha=local<0?0:Mathf.Clamp01((.44f-local)/.18f);
                if(kind==SlazeyaWindKind.Pierce)
                {
                    reveal=1;retreat=0;
                    alpha=Mathf.Clamp01(local/.025f)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.22f,.35f,t)));
                    if(pierceImpacted)alpha*=Mathf.Lerp(.45f,1f,Mathf.Clamp01((t-pierceImpactAt)/.10f));
                }
                if(arrival){retreat=Mathf.Clamp01((local-.035f)/.12f);alpha=Mathf.Clamp01((.17f-local)/.10f);}
                if(kind==SlazeyaWindKind.Guard)
                {
                    if(t<=.14f)
                    {
                        float open=Mathf.Clamp01(t/.065f);open=1-(1-open)*(1-open);
                        reveal=.5f+.5f*open;retreat=.5f-.5f*open;
                    }
                    else
                    {
                        float close=Mathf.Clamp01((t-.14f)/.22f);
                        reveal=1-close*.5f;retreat=close*.5f;
                    }
                }
                if(kind==SlazeyaWindKind.Projectile && !arrival)
                {retreat=Mathf.Clamp01((t-.26f)/.08f);alpha=t<.26f?1:Mathf.Clamp01((.34f-t)/.08f);}
                renderer.GetPropertyBlock(block);block.SetFloat("_Reveal",reveal);block.SetFloat("_Retreat",retreat);
                block.SetFloat("_RevealSoftness",.16f);
                block.SetFloat("_Phase",t);block.SetFloat("_Alpha",alpha*(wake?.65f:1f));renderer.SetPropertyBlock(block);
            }
            if(kind==SlazeyaWindKind.Slash)
            {
                // SweptWedge already moves the blade through pathT; rotating it
                // again would double the gesture and curl its point back at the caster.
                if(primary!=null)primary.localRotation=Quaternion.Euler(0,0,sweptStyle?0:SlashAngle(t));
                if(secondary!=null)secondary.localRotation=Quaternion.Euler(0,0,sweptStyle?-8:-18+Mathf.Clamp01((t-.05f)/.16f)*32);
            }
        }
        private static float SlashAngle(float time){return -5+Mathf.Clamp01(time/.10f)*65;}
        public void StopAndClear()
        {
            foreach(var ps in systems){if(ps==null)continue;ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);ps.Pause(false);}
            foreach(var renderer in renderers){if(renderer==null)continue;renderer.GetPropertyBlock(block);block.SetFloat("_Alpha",0);renderer.SetPropertyBlock(block);}
            IsComplete=true;
        }
    }
}
