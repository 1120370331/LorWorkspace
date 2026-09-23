using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Steria.Vfx
{
    public interface IChristashaStrikePlayback
    {
        void Aim(Vector3 localTarget, Vector3 localExit);
        void ConfirmImpact();
        bool ImpactOccurred { get; }
    }

    /// <summary>Authored platinum thrust / flying sword; tip is the contact origin.</summary>
    public sealed class ChristashaNativePierceRecipes : IDisposable
    {
        // Research identifier only; the authored strikes do not load this resource.
        public const string DaggerResource = "prefabs/battle/diceattackeffects/new/fx/pc/rolrang/fx_pc_rolrang_dagger";
        const string Marker = "__ChristashaNativePierce_";
        readonly Texture2D brush;
        readonly Shader strikeShader;
        readonly List<Object> owned = new List<Object>();
        readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
        GameObject cache;
        Material lightMaterial, brushMaterial, glowMaterial, swordMaterial, ringMaterial, nearRingMaterial;

        public ChristashaNativePierceRecipes(Func<string, GameObject> resolver, Texture2D originalBrush, Shader strikeShader = null)
        {
            if (resolver == null) throw new ArgumentNullException("resolver");
            brush = originalBrush;
            this.strikeShader = strikeShader;
        }

        public GameObject CreatePrototype(string recipe, Transform parent = null)
        {
            if (recipe != "Pierce" && recipe != "Projectile") throw new ArgumentException("Unknown strike: " + recipe);
            GameObject template;
            if (!templates.TryGetValue(recipe, out template))
            {
                EnsureMaterials();
                if (cache == null)
                {
                    cache = new GameObject("ChristashaStrike_InactiveCache"); cache.SetActive(false);
                    if (Application.isPlaying) Object.DontDestroyOnLoad(cache);
                }
                template = new GameObject("ChristashaNative_" + recipe + "Template");
                template.SetActive(false); template.transform.SetParent(cache.transform, false);
                new GameObject(Marker + recipe).transform.SetParent(template.transform, false);
                var aim = new GameObject("StrikeTarget"); aim.transform.SetParent(template.transform, false);
                aim.transform.localPosition = new Vector3(-4.2f, 0, 0);
                var exit=new GameObject("StrikeExit");exit.transform.SetParent(template.transform,false);
                exit.transform.localPosition=new Vector3(-9f,0,0);
                MeshPart(template.transform, "StrikeBlade", BladeMesh(recipe == "Projectile"), recipe == "Projectile" ? swordMaterial : lightMaterial);
                if(recipe=="Projectile")MeshPart(template.transform.Find("StrikeBlade"),"SwordHilt",HiltMesh(),Surface("SwordHilt",5));
                MeshPart(template.transform, "ImpactFlash", FlashMesh(), Surface("ContactFlash",4));
                var trail = Line(template.transform, "SwordWake", false);
                trail.useWorldSpace = true; trail.numCapVertices = 0; trail.numCornerVertices = 3;
                trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
                var ring = Line(template.transform, "ImpactRing", true);
                ring.sharedMaterial=recipe=="Pierce"?nearRingMaterial:ringMaterial;
                ring.useWorldSpace = true; ring.positionCount = 65;
                ParticlePart(template.transform, "StrikeMotes");
                ParticlePart(template.transform, "ImpactSparks");
                if(recipe=="Pierce")
                {
                    // The thrust's long carrier crosses the target. Its small
                    // confirmed contact must render in front of that carrier.
                    template.transform.Find("ImpactFlash").GetComponent<MeshRenderer>().sortingOrder=7;
                    ring.sortingOrder=6;
                    template.transform.Find("ImpactSparks").GetComponent<ParticleSystemRenderer>().sortingOrder=8;
                }
                templates.Add(recipe, template);
            }
            var instance = Object.Instantiate(template, parent, false);
            instance.name = "ChristashaNative_" + recipe + "Prototype";
            return instance;
        }

        void EnsureMaterials()
        {
            if (lightMaterial != null) return;
            var shader = Shader.Find("Sprites/Default");
            if (shader == null || brush == null) throw new InvalidOperationException("Strike requires Sprites/Default and the local tide brush");
            if(strikeShader==null || !strikeShader.isSupported)throw new InvalidOperationException("Platinum strike shader bundle is missing or unsupported");
            lightMaterial=Surface("ThrustEnergy",0);swordMaterial=Surface("LightSword",1);
            glowMaterial=Surface("BrokenWake",2);ringMaterial=Surface("ErodingShock",3);
            nearRingMaterial=new Material(ringMaterial){name="Christasha_NearContactRing"};
            // Role 3 applies a second narrow, noisy coverage band inside the
            // LineRenderer width. Lift alpha only for the near follow-through.
            nearRingMaterial.SetColor("_Color",new Color(1f,.97f,.86f,2.5f));owned.Add(nearRingMaterial);
            var pixels = brush.GetPixels();
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(1, 1, 1, Mathf.Clamp01(pixels[i].r * 1.55f));
            var texture = new Texture2D(brush.width, brush.height, TextureFormat.RGBA32, false, true)
            { name = "Christasha_Strike_BrushAlpha", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            texture.SetPixels(pixels); texture.Apply(false, true); owned.Add(texture);
            brushMaterial = new Material(shader) { name = "Christasha_Strike_GoldSplinters" };
            brushMaterial.SetTexture("_MainTex", texture); owned.Add(brushMaterial);
        }

        Material Surface(string role,float index)
        {
            var material=new Material(strikeShader){name="Christasha_"+role};
            material.SetTexture("_MainTex",brush);material.SetFloat("_Role",index);
            if(index==0 || index==1 || index==5)material.SetInt("_Cull",2);
            owned.Add(material);return material;
        }

        void MeshPart(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.sortingLayerName = "Particle"; renderer.sortingOrder = 5;
            renderer.enabled = false;
        }

        LineRenderer Line(Transform parent, string name, bool loop)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = glowMaterial;
            line.loop = loop; line.enabled = false;
            line.sortingLayerName = "Particle"; line.sortingOrder = 4;
            return line;
        }

        void ParticlePart(Transform parent, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var p = go.AddComponent<ParticleSystem>(); p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = p.main; main.playOnAwake = false; main.loop = false; main.maxParticles = 64;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startSpeed = 0; main.startSize3D = true; main.startSize = .10f;
            var emission = p.emission; emission.enabled = false;
            var shape = p.shape; shape.enabled = false;
            var colors = p.colorOverLifetime; colors.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(1,.86f,.53f), 1) },
                new[] { new GradientAlphaKey(1,0), new GradientAlphaKey(.8f,.3f), new GradientAlphaKey(0,1) });
            colors.color = gradient;
            var size = p.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0,1,1,0));
            var renderer = p.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = brushMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingLayerName = "Particle"; renderer.sortingOrder = 6;
        }

        // A genuine diamond section preserves the narrow ridge from oblique
        // views. Sword proportions follow the character's existing weapon.
        Mesh BladeMesh(bool sword)
        {
            var vertices = new List<Vector3>(); var colors = new List<Color>(); var indices = new List<int>();
            float length = sword ? 2.55f : 3.6f;
            for(int step=0;step<48;step++)
            {
                float u=step/48f,w=(step+1)/48f;
                float a=BladeWidth(u,sword),b=BladeWidth(w,sword);
                float da=a*(sword?.42f:.72f),db=b*(sword?.42f:.72f);
                var left=new[]{new Vector3(length*u,a,0),new Vector3(length*u,0,-da),new Vector3(length*u,-a,0),new Vector3(length*u,0,da)};
                var right=new[]{new Vector3(length*w,b,0),new Vector3(length*w,0,-db),new Vector3(length*w,-b,0),new Vector3(length*w,0,db)};
                for(int face=0;face<4;face++)
                {
                    int next=(face+1)%4;
                    Vector3 outward=(left[face]+left[next]+right[face]+right[next])*.25f;outward.x=0;
                    Color tint=sword && face==1?new Color(.80f,.87f,.89f):Color.white;
                    OutQuad(vertices,colors,indices,left[face],left[next],right[next],right[face],outward,tint);
                }
            }
            return MeshFrom(vertices,colors,indices,sword?"Christasha_FlyingSword":"Christasha_ThrustPyramid");
        }

        static float BladeWidth(float u,bool sword)
        {
            u=Mathf.Clamp01(u);
            if(!sword)return .30f*u;
            if(u<.15f)return Mathf.Lerp(0,.072f,u/.15f);
            if(u<.92f)return Mathf.Lerp(.072f,.100f,(u-.15f)/.77f);
            return Mathf.Lerp(.100f,.088f,(u-.92f)/.08f);
        }

        Mesh HiltMesh()
        {
            var vertices=new List<Vector3>();var colors=new List<Color>();var indices=new List<int>();
            // Compact straight quillons, gently beveled in 3D, not a bow shape.
            for(int segment=0;segment<16;segment++)
            {
                float y0=Mathf.Lerp(-.29f,.29f,segment/16f),y1=Mathf.Lerp(-.29f,.29f,(segment+1)/16f);
                float r0=Mathf.Lerp(.034f,.053f,1-Mathf.Abs(y0)/.29f),r1=Mathf.Lerp(.034f,.053f,1-Mathf.Abs(y1)/.29f);
                for(int face=0;face<8;face++)
                {
                    float a=face*Mathf.PI/4,b=(face+1)*Mathf.PI/4;
                    Vector3 p=new Vector3(2.59f+Mathf.Cos(a)*r0,y0,Mathf.Sin(a)*r0);
                    Vector3 q=new Vector3(2.59f+Mathf.Cos(b)*r0,y0,Mathf.Sin(b)*r0);
                    Vector3 r=new Vector3(2.59f+Mathf.Cos(b)*r1,y1,Mathf.Sin(b)*r1);
                    Vector3 s=new Vector3(2.59f+Mathf.Cos(a)*r1,y1,Mathf.Sin(a)*r1);
                    OutQuad(vertices,colors,indices,p,q,r,s,new Vector3(Mathf.Cos((a+b)*.5f),0,Mathf.Sin((a+b)*.5f)),new Color(1,.94f,.73f));
                }
            }
            for(int side=-1;side<=1;side+=2)for(int face=0;face<8;face++)
            {
                float a=face*Mathf.PI/4,b=(face+1)*Mathf.PI/4;
                var center=new Vector3(2.59f,side*.29f,0);
                var p=center+new Vector3(Mathf.Cos(a)*.034f,0,Mathf.Sin(a)*.034f);
                var q=center+new Vector3(Mathf.Cos(b)*.034f,0,Mathf.Sin(b)*.034f);
                if(Vector3.Dot(Vector3.Cross(p-center,q-center),Vector3.up*side)<0)Face(vertices,colors,indices,center,q,p,new Color(1,.94f,.73f));
                else Face(vertices,colors,indices,center,p,q,new Color(1,.94f,.73f));
            }
            Lathe(vertices,colors,indices,new[]{new Vector2(2.61f,.067f),new Vector2(2.69f,.047f),new Vector2(3.13f,.039f),new Vector2(3.17f,.055f)},new Color(.76f,.80f,.72f));
            Lathe(vertices,colors,indices,new[]{new Vector2(3.15f,0),new Vector2(3.18f,.049f),new Vector2(3.24f,.071f),new Vector2(3.31f,.045f),new Vector2(3.34f,0)},new Color(1,.95f,.79f));
            return MeshFrom(vertices,colors,indices,"Christasha_SwordHilt");
        }

        static void Lathe(List<Vector3> v,List<Color> c,List<int> t,Vector2[] profile,Color color)
        {
            for(int segment=0;segment<profile.Length-1;segment++)for(int face=0;face<12;face++)
            {
                float a=face*Mathf.PI/6,b=(face+1)*Mathf.PI/6;
                var p=profile[segment];var q=profile[segment+1];
                OutQuad(v,c,t,new Vector3(p.x,Mathf.Cos(a)*p.y,Mathf.Sin(a)*p.y),new Vector3(p.x,Mathf.Cos(b)*p.y,Mathf.Sin(b)*p.y),
                    new Vector3(q.x,Mathf.Cos(b)*q.y,Mathf.Sin(b)*q.y),new Vector3(q.x,Mathf.Cos(a)*q.y,Mathf.Sin(a)*q.y),new Vector3(0,Mathf.Cos((a+b)*.5f),Mathf.Sin((a+b)*.5f)),color);
            }
        }
        static void OutQuad(List<Vector3> v,List<Color> c,List<int> t,Vector3 a,Vector3 b,Vector3 d,Vector3 e,Vector3 outward,Color color)
        {
            Vector3 normal=Vector3.Cross(b-a,d-a);
            if(normal.sqrMagnitude<.00000001f)normal=Vector3.Cross(d-a,e-a);
            if(Vector3.Dot(normal,outward)<0)Quad(v,c,t,a,e,d,b,color);
            else Quad(v,c,t,a,b,d,e,color);
        }

        Mesh FlashMesh()
        {
            var v=new List<Vector3>(); var c=new List<Color>(); var t=new List<int>();
            Quad(v,c,t,new Vector3(-1,-1,0),new Vector3(-1,1,0),new Vector3(1,1,0),new Vector3(1,-1,0),Color.white);
            return MeshFrom(v,c,t,"Christasha_GoldContact");
        }
        static void Face(List<Vector3> v,List<Color> c,List<int> t,Vector3 a,Vector3 b,Vector3 d,Color color)
        { int n=v.Count; v.Add(a);v.Add(b);v.Add(d);c.Add(color);c.Add(color);c.Add(color);t.Add(n);t.Add(n+1);t.Add(n+2); }
        static void Quad(List<Vector3> v,List<Color> c,List<int> t,Vector3 a,Vector3 b,Vector3 d,Vector3 e,Color color)
        { Face(v,c,t,a,b,d,color);Face(v,c,t,a,d,e,color); }
        Mesh MeshFrom(List<Vector3> v,List<Color> c,List<int> t,string name)
        {
            var mesh=new Mesh { name=name }; mesh.SetVertices(v);mesh.SetColors(c);mesh.SetTriangles(t,0);
            var uv=new Vector2[v.Count];
            for(int i=0;i<uv.Length;i++)
            {
                if(name=="Christasha_GoldContact")uv[i]=new Vector2(v[i].x*.5f+.5f,v[i].y*.5f+.5f);
                else
                {
                    bool sword=name=="Christasha_FlyingSword";
                    float u=v[i].x/(sword?2.55f:3.6f);
                    float width=BladeWidth(u,sword);
                    uv[i]=new Vector2(u,.5f+v[i].y/Mathf.Max(width*2,.01f));
                }
            }
            mesh.uv=uv;mesh.RecalculateNormals();mesh.RecalculateBounds();owned.Add(mesh);return mesh;
        }

        public static IChristashaNativePlaybackHooks TryCreatePlayback(GameObject instance, uint seed)
        {
            foreach(var t in instance.GetComponentsInChildren<Transform>(true))
                if(t.name.StartsWith(Marker,StringComparison.Ordinal))return new Playback(instance,t.name.Substring(Marker.Length),seed);
            return null;
        }

        sealed class Playback : IChristashaNativePlaybackHooks, IChristashaStrikePlayback
        {
            readonly GameObject root;
            readonly bool projectile;
            readonly Transform aim, exit, blade;
            readonly MeshRenderer body, flash, hilt;
            readonly LineRenderer wake, ring;
            readonly ParticleSystem motes, sparks;
            readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
            readonly Vector3[] history=new Vector3[32]; readonly float[] times=new float[32];
            readonly System.Random random;
            int samples, emitted;
            bool confirmed, impacted, impactEmitted;
            float impactAge;
            Vector3 contact, renderContact;
            Quaternion impactRotation;
            public float Duration { get { return projectile ? ChristashaProjectileArc.VisualDuration : .72f; } }
            float ContactTime { get { return projectile ? ChristashaProjectileArc.FlightTime : .16f; } }
            public bool ImpactOccurred { get { return impacted; } }
            public Playback(GameObject root,string recipe,uint seed)
            {
                this.root=root;projectile=recipe=="Projectile";random=new System.Random(unchecked((int)seed));
                aim=root.transform.Find("StrikeTarget");exit=root.transform.Find("StrikeExit");blade=root.transform.Find("StrikeBlade");
                body=blade.GetComponent<MeshRenderer>();flash=root.transform.Find("ImpactFlash").GetComponent<MeshRenderer>();
                hilt=projectile?blade.Find("SwordHilt").GetComponent<MeshRenderer>():null;
                wake=root.transform.Find("SwordWake").GetComponent<LineRenderer>();ring=root.transform.Find("ImpactRing").GetComponent<LineRenderer>();
                motes=root.transform.Find("StrikeMotes").GetComponent<ParticleSystem>();sparks=root.transform.Find("ImpactSparks").GetComponent<ParticleSystem>();
                body.SetPropertyBlock(null);flash.SetPropertyBlock(null);body.enabled=false;flash.enabled=false;wake.enabled=false;ring.enabled=false;
            }
            public void Aim(Vector3 localTarget,Vector3 localExit) { if(!impacted && !projectile){aim.localPosition=localTarget;exit.localPosition=localExit;} }
            public void ConfirmImpact() { confirmed=true; }
            public void BeforeAnimator(float dt,float age) { }
            public void AfterAnimator(float dt,float age)
            {
                float reveal=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.045f));
                float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(projectile?ContactTime:.28f,projectile?ContactTime+.12f:.54f,age));
                float thrustLength=0,thrustHalfWidth=0;
                if(projectile) { blade.localPosition=Vector3.zero;blade.localRotation=Quaternion.identity; }
                else
                {
                    Vector3 d=new Vector3(aim.localPosition.x,aim.localPosition.y,0);
                    float contactDistance=d.magnitude;
                    float fullDistance=Mathf.Max(contactDistance,new Vector3(exit.localPosition.x,exit.localPosition.y,0).magnitude);
                    thrustLength=age<ContactTime?contactDistance*Mathf.Pow(Mathf.Clamp01(age/ContactTime),1.15f):Mathf.Lerp(contactDistance,fullDistance,Mathf.SmoothStep(0,1,Mathf.Clamp01((age-ContactTime)/.075f)));
                    blade.localPosition=contactDistance>.001f?d*(thrustLength/contactDistance):Vector3.zero;
                    blade.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg-180f);
                    thrustHalfWidth=Mathf.Clamp(fullDistance*.22f,2.1f,6f)*Mathf.SmoothStep(0,1,Mathf.Clamp01(thrustLength/Mathf.Max(fullDistance,.001f)));
                }
                float artScale=projectile?1.65f:1.35f;
                // Form the thrust forward from the hand before it becomes a
                // travelling packet; its wide rear cannot extend behind the grip.
                float axisScale=projectile?1.80f:thrustLength/3.6f;
                blade.localScale=projectile?new Vector3(reveal*axisScale,Mathf.Lerp(.35f,1,reveal)*artScale,artScale):new Vector3(axisScale,thrustHalfWidth/.30f,Mathf.Max(.1f,thrustHalfWidth/.60f));
                Vector3 tail=root.transform.TransformPoint(blade.localPosition + blade.localRotation * new Vector3((projectile?3.34f:3.52f)*axisScale,0,.03f));
                if(age<ContactTime && (samples==0 || Vector3.Distance(tail,history[0])>.02f))
                {
                    samples=Mathf.Min(samples+1,history.Length);
                    for(int i=samples-1;i>0;i--){history[i]=history[i-1];times[i]=times[i-1];}
                    history[0]=tail;times[0]=age;
                }
                while(samples>0 && age-times[samples-1]>.18f)samples--;
                wake.enabled=samples>1;wake.positionCount=samples;
                wake.widthMultiplier=(projectile?.80f:.65f)*Mathf.Max(.1f,Mathf.Abs(root.transform.lossyScale.y));
                wake.startColor=new Color(1,1,1,.85f*fade);wake.endColor=new Color(1,1,1,0);
                SurfaceBlock(wake,1,age,0);
                for(int i=0;i<samples;i++)wake.SetPosition(i,history[i]);
                if(confirmed && !impacted && age>=ContactTime)
                {
                    impacted=true;impactAge=age;contact=projectile?root.transform.position:root.transform.TransformPoint(aim.localPosition);
                    impactRotation=projectile?root.transform.rotation:blade.rotation;
                    // Near contact pivots can sit behind the victim sprite (the
                    // mirrored skin's Z becomes positive). Keep the same XY hit
                    // point but draw the confirmed follow-through in front.
                    renderContact=projectile?contact:new Vector3(contact.x,contact.y,Mathf.Min(contact.z,0f)-.65f);
                    flash.transform.position=renderContact+new Vector3(0,0,projectile?-.15f:0f);
                    flash.transform.rotation=impactRotation;
                }
                // Only a confirmed near contact yields a brief gap in the broad
                // thrust body. The native spark, compact flash, ring and splinters
                // can then read as one hit before the sword's follow-through.
                float contactFocus=!projectile && impacted ?
                    Mathf.Lerp(.35f,1f,Mathf.SmoothStep(0f,1f,Mathf.Clamp01((age-impactAge)/.28f))) : 1f;
                SetSurface(body,fade*reveal*contactFocus,age,1-fade);
                if(hilt!=null)SetSurface(hilt,fade*reveal,age,1-fade);
                if(impacted)
                {
                    float t=age-impactAge;
                    float expansion=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/(projectile?.16f:.12f)));
                    float opacity=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.40f));
                    ring.enabled=opacity>.001f;
                    float radius=Mathf.Lerp(projectile?.48f:.70f,projectile?3.6f:5.2f,expansion);
                    ring.widthMultiplier=projectile?Mathf.Lerp(.60f,.14f,expansion):Mathf.Lerp(1.05f,.55f,expansion);
                    ring.startColor=ring.endColor=Color.white;
                    SurfaceBlock(ring,opacity,t,Mathf.Clamp01(t/.34f));
                    for(int i=0;i<65;i++){float a=i/64f*Mathf.PI*2;ring.SetPosition(i,renderContact+impactRotation*new Vector3(Mathf.Cos(a)*radius*(projectile?.62f:.88f),Mathf.Sin(a)*radius,0)+new Vector3(0,0,projectile?-.12f:0f));}
                    flash.transform.localScale=Vector3.one*Mathf.Lerp(projectile?4.4f:3.3f,.4f,Mathf.Clamp01(t/.16f));
                    SetSurface(flash,projectile?1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.025f,.14f,t)):1-Mathf.Clamp01(t/.12f),t,0);
                }
            }
            public void AfterParticles(float dt,float age)
            {
                int count=Mathf.FloorToInt(Mathf.Min(age,ContactTime)/.024f);
                while(emitted<count)
                {
                    emitted++;
                    Vector3 p=blade.TransformPoint(new Vector3(Range(.8f,2.7f),Range(-.12f,.12f),-.1f));
                    Emit(motes,p,new Vector3(Range(-.7f,.7f),Range(-.8f,.8f),0),Range(.10f,.22f),Range(.14f,.26f));
                }
                if(impacted && !impactEmitted)
                {
                    impactEmitted=true;
                    for(int i=0;i<(projectile?24:16);i++)
                    {
                        float a=Range(0,Mathf.PI*2);
                        float velocity=i%4==0?Range(9f,14f):Range(3.2f,7f);
                        Vector3 radial=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*velocity;
                        if(projectile)radial=impactRotation*(radial+new Vector3(-2f,0,0));
                        Emit(sparks,renderContact,radial,projectile?Range(.28f,.62f):Range(.16f,.48f),Range(.16f,.34f));
                    }
                }
            }
            void SetSurface(Renderer renderer,float alpha,float age,float dissolve)
            { renderer.enabled=alpha>.001f;SurfaceBlock(renderer,alpha,age,dissolve); }
            void SurfaceBlock(Renderer renderer,float alpha,float age,float dissolve)
            { block.Clear();block.SetFloat("_Opacity",Mathf.Clamp01(alpha));block.SetFloat("_Phase",age);block.SetFloat("_Dissolve",dissolve);renderer.SetPropertyBlock(block); }
            void Emit(ParticleSystem p,Vector3 at,Vector3 velocity,float size,float life)
            { p.Emit(new ParticleSystem.EmitParams{position=at,velocity=velocity,startSize3D=new Vector3(size*.35f,size,1),startLifetime=life,startColor=new Color(1,Range(.87f,.99f),Range(.52f,.82f),1),rotation=Mathf.Atan2(velocity.y,velocity.x)*Mathf.Rad2Deg-90f,applyShapeToPosition=false},1); }
            float Range(float a,float b){return Mathf.Lerp(a,b,(float)random.NextDouble());}
            public void StopAndClear()
            {
                body.enabled=false;flash.enabled=false;ring.enabled=false;wake.enabled=false;wake.positionCount=0;
                if(hilt!=null)hilt.enabled=false;
                motes.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);sparks.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                samples=0;
            }
        }

        public void Dispose()
        {
            if(cache!=null)Object.DestroyImmediate(cache);
            foreach(var item in owned)if(item!=null)Object.DestroyImmediate(item);
            owned.Clear();templates.Clear();cache=null;lightMaterial=null;brushMaterial=null;glowMaterial=null;swordMaterial=null;ringMaterial=null;nearRingMaterial=null;
        }
    }
}
