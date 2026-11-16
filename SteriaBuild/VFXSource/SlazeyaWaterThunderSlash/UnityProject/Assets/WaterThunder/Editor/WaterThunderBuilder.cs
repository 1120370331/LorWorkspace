using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Steria.WaterThunder;
using Object=UnityEngine.Object;

public static class WaterThunderBuilder
{
    public const string Root="Assets/WaterThunder";
    public const string Prefab=Root+"/Prefabs/SlazeyaWaterThunderSlash.prefab";
    public static readonly string[] Names={"L1_WaterBlade","L2_SplashWake","L3_Lightning","L4_BladeCore"};
    static readonly string[] TextureNames={"L1_WaterBody","L2_Splash","L3_Lightning","L4_Core"};
    static Vector3 Curve(float t)
    {
        return WaterThunderMotion.Curve(t);
    }
    static Vector3 Normal(float t)
    {
        return WaterThunderMotion.Normal(t);
    }
    static float Width(float t)
    {
        return WaterThunderMotion.Width(t);
    }
    sealed class Geometry
    {
        public List<Vector3> p=new List<Vector3>();public List<Vector2> uv=new List<Vector2>(),birth=new List<Vector2>(),velocity=new List<Vector2>();
        public List<int> triangles=new List<int>();public List<Color32> color=new List<Color32>();
        public void Vertex(Vector3 pos,Vector2 tex,Color32 c,Vector2 born,Vector2 speed) { p.Add(pos);uv.Add(tex);color.Add(c);birth.Add(born);velocity.Add(speed); }
        public void Quad(int a,int b,int c,int d) {triangles.AddRange(new[]{a,c,b,c,d,b});}
        public Mesh Mesh(string name)
        {
            for(int i=0;i<triangles.Count;i+=3)if(Vector3.Cross(p[triangles[i+1]]-p[triangles[i]],p[triangles[i+2]]-p[triangles[i]]).z>0){int v=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=v;}
            var m=new Mesh{name=name};m.SetVertices(p);m.SetUVs(0,uv);m.SetUVs(1,birth);m.SetUVs(2,velocity);m.SetColors(color);m.SetTriangles(triangles,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
    }
    static Mesh Ribbon(bool core)
    {
        var g=new Geometry();int n=192,rows=core?8:14;
        for(int i=0;i<=n;i++)
        {
            float t=(float)i/n;Vector3 center=Curve(t),normal=Normal(t);
            for(int j=0;j<=rows;j++)
            {
                float v=(float)j/rows;float w=Width(t);
                float offset;
                if(core) offset=.015f*w+(v-.5f)*(.080f+.46f*Mathf.Pow(t,2.3f))*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.18f);
                else offset=(v-.09f)*w*(1+.055f*Mathf.Sin(t*57+v*9));
                var tailOffset=Vector3.zero;
                if(!core)
                {
                    float tail=Mathf.Pow(1-WaterThunderMotion.Span(0,.37f,t),1.4f);
                    var tangent=new Vector3(-normal.y,normal.x,0);
                    tailOffset=normal*((v-.5f)*.78f*tail)-tangent*(tail*(.15f+.35f*v));
                }
                var pos=center+normal*offset+tailOffset;pos.z=core?-.018f:.012f*Mathf.Sin(t*10)*Mathf.Sin(v*Mathf.PI);
                float strength=core?Mathf.Pow(Mathf.Max(0,Mathf.Sin(Mathf.PI*t)),.08f)*(.75f+.25f*WaterThunderMotion.Span(0,.2f,t)):(.75f+.25f*WaterThunderMotion.Span(0,.2f,t))*(1-WaterThunderMotion.Span(.985f,1,t));
                byte a=(byte)Mathf.RoundToInt(255*strength);
                g.Vertex(pos,new Vector2(t,v),new Color32(255,255,255,a),Vector2.zero,new Vector2(tailOffset.x,tailOffset.y));
                if(i<n && j<rows){int k=i*(rows+1)+j;g.Quad(k,k+rows+1,k+1,k+rows+2);}
            }
        }
        return g.Mesh(core?Names[3]:Names[0]);
    }
    static Mesh Splash()
    {
        var g=new Geometry();var rng=new System.Random(19037);
        for(int k=0;k<80;k++)
        {
            float t=.04f+.93f*(float)rng.NextDouble(),side=k%4==0?1:-1;
            Vector3 n=Normal(t),d=new Vector3(n.y,-n.x,0),c=Curve(t)+n*(Width(t)*(side>0?.8f:-.13f));
            c+=n*side*(.012f+.065f*(float)rng.NextDouble());c.z=-.025f;
            float born=WaterThunderMotion.TimeAtPath(t)+.015f*(float)rng.NextDouble(),life=.25f+.27f*(float)rng.NextDouble();
            float radius=.022f+.045f*Mathf.Pow((float)rng.NextDouble(),2);
            Vector3 speed=d*(-.18f-.48f*(float)rng.NextDouble())+n*side*(.10f+.28f*(float)rng.NextDouble());
            Vector3 axis=(d*.8f+n*side*.2f).normalized;Vector3 across=new Vector3(-axis.y,axis.x,0);
            int start=g.p.Count;
            for(int j=0;j<4;j++)
            {
                float u=j%2,v=j/2;g.Vertex(c+axis*(u-.5f)*radius*2+across*(v-.5f)*radius*2,new Vector2(u,v),new Color32(255,255,255,255),new Vector2(born,life),new Vector2(speed.x,speed.y));
            }
            g.Quad(start,start+1,start+2,start+3);
        }
        return g.Mesh(Names[1]);
    }
    static void ElectricalLine(Geometry g,Vector3[] points,float[] path,float[] reserve,float[] born,float width,bool particle=false)
    {
        int start=g.p.Count;
        for(int i=0;i<points.Length;i++)
        {
            var d=points[Math.Min(i+1,points.Length-1)]-points[Math.Max(i-1,0)];var n=new Vector3(-d.y,d.x,0).normalized;
            float q=(float)i/(points.Length-1),taper=Mathf.Pow(Mathf.Max(0,Mathf.Sin(q*Mathf.PI)),.16f);
            for(int j=0;j<2;j++)g.Vertex(points[i]+n*((j-.5f)*width*taper),new Vector2(path[i],j),new Color32(255,255,particle?(byte)Mathf.RoundToInt(reserve[i]*255):(byte)255,255),new Vector2(born[i],reserve[i]),Vector2.zero);
            if(i<points.Length-1){int k=start+i*2;g.Quad(k,k+2,k+1,k+3);}
        }
    }
    static Mesh Lightning()
    {
        var g=new Geometry();var rng=new System.Random(4307);const int count=46;
        var trunk=new Vector3[count];var path=new float[count];var reserve=new float[count];var born=new float[count];
        for(int i=0;i<count;i++)
        {
            float t=Mathf.Lerp(.12f,.985f,(float)i/(count-1));path[i]=t;reserve[i]=1;born[i]=WaterThunderMotion.TimeAtPath(t);
            float jag=i==0||i==count-1?0:((float)rng.NextDouble()-.5f)*.19f;
            trunk[i]=Curve(t)+Normal(t)*(Width(t)*.38f+jag)+Vector3.back*.04f;
        }
        ElectricalLine(g,trunk,path,reserve,born,.105f);
        foreach(int anchor in new[]{9,16,24,31,38})
        {
            const int nodes=7;var branch=new Vector3[nodes];var bp=new float[nodes];var br=new float[nodes];var bt=new float[nodes];
            var tangent=new Vector3(-Normal(path[anchor]).y,Normal(path[anchor]).x,0);
            float side=anchor%2==0?1:-1;
            var end=trunk[anchor]+Normal(path[anchor])*side*(.25f+.22f*(float)rng.NextDouble())+tangent*(.12f+.1f*(float)rng.NextDouble());
            for(int i=0;i<nodes;i++)
            {
                float q=(float)i/(nodes-1);branch[i]=Vector3.Lerp(trunk[anchor],end,q);
                if(i>0&&i<nodes-1)branch[i]+=tangent*(((float)rng.NextDouble()-.5f)*.13f);
                bp[i]=Mathf.Clamp01(path[anchor]+q*.035f);br[i]=Mathf.Lerp(.74f,.25f,q);bt[i]=born[anchor]+q*.012f;
            }
            ElectricalLine(g,branch,bp,br,bt,.061f);
            if(anchor==16||anchor==31)
            {
                var fork=new[]{branch[3],branch[3]+tangent*.12f+Normal(path[anchor])*side*.06f,branch[3]+tangent*.22f+Normal(path[anchor])*side*.14f};
                ElectricalLine(g,fork,new[]{bp[3],bp[3]+.01f,bp[3]+.02f},new[]{.45f,.32f,.18f},new[]{bt[3],bt[3]+.004f,bt[3]+.009f},.043f);
            }
        }
        return g.Mesh(Names[2]);
    }
    static void StoreMesh(Mesh mesh,string name)
    {
        string path=Root+"/Meshes/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old==null)AssetDatabase.CreateAsset(mesh,path);else {EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);}
    }
    static Material Material(int layer)
    {
        string path=Root+"/Materials/"+Names[layer]+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m!=null){ConfigureSurface(m,layer);return m;} // Preserve artist palette and flow settings.
        var shader=Shader.Find("Steria/WaterThunder/FormedArc");if(shader==null||!shader.isSupported)throw new Exception("WaterThunder shader unavailable");
        m=new Material(shader){name=Names[layer]};
        m.SetTexture("_ShapeTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+TextureNames[layer]+".png"));
        string mask=layer==0?"L1_WaterMask":layer==1?"L2_SplashMask":TextureNames[layer]+"Mask";
        m.SetTexture("_MaskTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+mask+".png"));
        m.SetTexture("_FlowTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Shared_FlowRepeat.png"));
        m.SetTexture("_DistortTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Shared_FlowRepeat.png"));
        m.SetTexture("_HighlightTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Shared_CausticsRepeat.png"));
        m.SetTexture("_DissolveTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Shared_ErosionRepeat.png"));
        m.SetFloat("_UseShapeRGB",1);m.SetFloat("_BodyStrength",layer==0?.88f:1);m.SetColor("_BaseColor",Color.white);
        ConfigureSurface(m,layer);
        m.SetFloat("_SrcBlend",5);m.SetFloat("_DstBlend",layer<2?10:1);m.SetFloat("_ZWrite",0);m.SetFloat("_Cull",0);
        m.SetFloat("_Emission",layer==0?.65f:0);m.SetColor("_EmissionColor",new Color(.12f,.56f,.85f));
        m.SetFloat("_HighlightStrength",layer==0?.35f:0);m.SetFloat("_HighlightBandCenter",.35f);m.SetFloat("_HighlightBandWidth",.75f);
        m.SetVector("_HighlightTiling",new Vector4(2,1,0,0));m.SetVector("_HighlightSpeed",new Vector4(-.8f,.12f,0,0));
        m.SetFloat("_DistortStrength",layer==0?.014f:0);m.SetFloat("_FlowStrength",layer==0?.018f:0);
        if(layer==2)m.SetColor("_BaseColor",new Color(1.3f,.9f,1.65f,1));
        if(layer==3)m.SetColor("_BaseColor",new Color(1.5f,1.65f,1.8f,1));
        m.renderQueue=3000+layer*10;AssetDatabase.CreateAsset(m,path);return m;
    }
    static void ConfigureSurface(Material m,int layer)
    {
        m.SetFloat("_HeadWeight",0);m.SetFloat("_SweepEnabled",layer==1?0:1);
        m.SetFloat("_SweepAcross",layer==2?0:1);m.SetFloat("_SweepCore",layer==3?1:0);
        m.SetFloat("_SweepWidth",layer==0?.42f:layer==3?.30f:.38f);m.SetFloat("_SweepWake",layer==3?0:layer==0?.60f:.24f);
        m.SetTexture("_SweepMaskTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Shared_SweepBrushMask.png"));
        m.SetTexture("_EvapScatterTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Evap_WaterScatter.png"));
        m.SetTexture("_TailMaskTex",layer==0?AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/L1_TailFanMask.png"):Texture2D.whiteTexture);
        m.SetFloat("_TailMaskStrength",layer==0?1:0);
        m.SetFloat("_WhiteFluid",layer==0?1:0);m.SetFloat("_WhiteFluidStrength",.75f);m.SetFloat("_WhiteFluidSpin",4.5f);
        if(layer==0)m.SetTexture("_WhiteFluidTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/L1_WhiteFluidQuadMask.png"));
        m.SetFloat("_Voltage",layer==2?1:0);m.SetFloat("_VoltageParticle",0);m.SetFloat("_ElectricalTime",-1);m.SetFloat("_VoltageDecay",3.2f);
        ConfigureLiquid(m,layer==1);
    }
    static void ConfigureLiquid(Material m,bool liquid)
    {
        m.SetFloat("_Liquid",liquid?1:0);m.SetFloat("_LiquidWobble",.85f);m.SetFloat("_LiquidShine",1.1f);
        if(liquid)
        {
            m.SetTexture("_WaterNormalTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/L2_WaterNormal.png"));
            m.SetTexture("_EvapScatterTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Evap_WaterScatter.png"));m.SetFloat("_LiquidDissolve",1);
        }
        EditorUtility.SetDirty(m);
    }
    static void ConfigureScatter(Material m,string name)
    {
        m.SetFloat("_Voltage",name=="ImpactElectric"?1:0);m.SetFloat("_VoltageParticle",name=="ImpactElectric"?1:0);m.SetFloat("_VoltageDecay",3.2f);
        if(name!="EvaporationWaterScatter")return;
        m.SetFloat("_UseShapeRGB",0);m.SetColor("_BaseColor",new Color(.16f,.71f,.97f,1));EditorUtility.SetDirty(m);
    }
    static Mesh ScatterSheet()
    {
        var g=new Geometry();
        for(int j=0;j<4;j++){float u=j%2,v=j/2;g.Vertex(new Vector3(u-.5f,v-.5f,0),new Vector2(u,v),new Color32(255,255,255,255),Vector2.zero,Vector2.zero);}
        g.Quad(0,1,2,3);return g.Mesh("EvaporationScatter");
    }
    static Mesh Spark()
    {
        var g=new Geometry();var p=new[]{new Vector3(-.45f,-.05f,0),new Vector3(-.17f,.12f,0),new Vector3(-.03f,-.08f,0),new Vector3(.19f,.09f,0),new Vector3(.45f,.02f,0)};
        ElectricalLine(g,p,new[]{0f,.25f,.5f,.75f,1f},new[]{.85f,.95f,1f,.95f,.8f},new float[5],.10f,true);
        ElectricalLine(g,new[]{p[2],new Vector3(-.02f,.12f,0),new Vector3(.11f,.23f,0)},new[]{.5f,.62f,.72f},new[]{.65f,.4f,.23f},new float[3],.068f,true);
        return g.Mesh("ImpactArcShard");
    }
    static Material SupportMaterial(string name,string texture,string mask,bool additive,int order)
    {
        string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null){ConfigureLiquid(m,name=="ImpactWater");ConfigureScatter(m,name);return m;}
        m=new Material(Shader.Find("Steria/WaterThunder/FormedArc")){name=name};
        m.SetTexture("_ShapeTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+texture+".png"));
        m.SetTexture("_MaskTex",mask==null?Texture2D.whiteTexture:AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+mask+".png"));
        m.SetFloat("_UseShapeRGB",1);m.SetFloat("_BodyStrength",1);m.SetFloat("_HighlightStrength",0);m.SetFloat("_Emission",0);
        m.SetFloat("_FlowStrength",0);m.SetFloat("_DistortStrength",0);m.SetFloat("_SrcBlend",5);m.SetFloat("_DstBlend",additive?1:10);m.SetFloat("_Cull",0);
        m.SetColor("_BaseColor",Color.white);ConfigureLiquid(m,name=="ImpactWater");ConfigureScatter(m,name);m.renderQueue=3000+order*10;AssetDatabase.CreateAsset(m,path);return m;
    }
    static void Support(GameObject root,WaterThunderSlash player)
    {
        var evap=new GameObject("Evaporation - dispersing water ripple flecks");evap.transform.SetParent(root.transform,false);
        evap.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/EvaporationScatter.asset");
        var r=evap.AddComponent<MeshRenderer>();r.sharedMaterial=SupportMaterial("EvaporationWaterScatter","Evap_WaterScatter",null,true,4);
        r.sortingOrder=4;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;player.evaporationMist=r;
        PrefabUtility.SaveAsPrefabAsset(evap,Root+"/Prefabs/WaterEvaporationScatter.prefab");
        var hit=new GameObject("Impact - water and lightning burst");hit.transform.SetParent(root.transform,false);hit.transform.localPosition=WaterThunderMotion.Contact+Vector3.back*.08f;
        var systems=new ParticleSystem[3];
        for(int i=0;i<3;i++)
        {
            var go=new GameObject(i==0?"Hit_WaterSpray":i==1?"Hit_LightningSparks":"Hit_ContactFlash");go.transform.SetParent(hit.transform,false);
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=false;main.duration=1;main.playOnAwake=false;main.startSpeed=0;main.startLifetime=1;main.startSize=1;main.startSize3D=true;main.maxParticles=48;main.simulationSpace=ParticleSystemSimulationSpace.Local;
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;ps.useAutoRandomSeed=false;ps.randomSeed=19037u;
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.renderMode=i==1?ParticleSystemRenderMode.Mesh:ParticleSystemRenderMode.Billboard;
            if(i==1){renderer.mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/ImpactArcShard.asset");renderer.alignment=ParticleSystemRenderSpace.Local;}
            renderer.sharedMaterial=i==0?SupportMaterial("ImpactWater","L2_Splash","L2_SplashMask",false,5):i==1?SupportMaterial("ImpactElectric","L3_Lightning","L3_LightningMask",true,6):SupportMaterial("ImpactFlash","Impact_Flash",null,true,7);
            ConfigureLiquid(renderer.sharedMaterial,i==0);
            renderer.sortingOrder=5+i;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV});systems[i]=ps;
        }
        PrefabUtility.SaveAsPrefabAsset(hit,Root+"/Prefabs/WaterThunderImpactParticles.prefab");
        var impact=hit.AddComponent<WaterThunderImpact>();impact.systems=systems;player.impact=impact;
    }
    public static void Build()
    {
        foreach(string folder in new[]{"Meshes","Materials","Prefabs","Scenes"})Directory.CreateDirectory(Root+"/"+folder);
        AssetDatabase.Refresh();
        string[] rejected={Root+"/Meshes/EvaporationRing.asset",Root+"/Materials/EvaporationWaterRing.mat",Root+"/Prefabs/WaterEvaporationCircles.prefab",Root+"/Textures/Evap_Ring.png"};
        var legacy=new List<string>();foreach(string path in rejected)if(AssetDatabase.LoadMainAssetAtPath(path)!=null)legacy.Add(path);
        if(legacy.Count>0)
        {
            string backup=Path.GetFullPath(Path.Combine(Application.dataPath,"../../history/rejected-visible-water-circles.unitypackage"));Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if(!File.Exists(backup))AssetDatabase.ExportPackage(legacy.ToArray(),backup,ExportPackageOptions.Default);
            foreach(string path in legacy)AssetDatabase.DeleteAsset(path);
        }
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Root+"/Textures"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            bool repeat=path.Contains("Repeat");bool data=repeat||path.Contains("Mask")||path.Contains("Normal")||path.Contains("Evap_WaterScatter");
            bool dirty=importer.sRGBTexture==data||importer.mipmapEnabled||importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.wrapMode!=(repeat?TextureWrapMode.Repeat:TextureWrapMode.Clamp);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=!data;importer.mipmapEnabled=false;
            importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=!data;importer.filterMode=FilterMode.Bilinear;
            importer.wrapMode=repeat?TextureWrapMode.Repeat:TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=1024;
            if(dirty)importer.SaveAndReimport();
        }
        StoreMesh(Ribbon(false),Names[0]);StoreMesh(Splash(),Names[1]);StoreMesh(Lightning(),Names[2]);StoreMesh(Ribbon(true),Names[3]);
        StoreMesh(ScatterSheet(),"EvaporationScatter");StoreMesh(Spark(),"ImpactArcShard");
        var root=new GameObject("Slazeya Water + Thunder Slash");
        var player=root.AddComponent<WaterThunderSlash>();player.autoPlay=false;
        for(int i=0;i<4;i++)
        {
            var child=new GameObject(Names[i]);child.transform.SetParent(root.transform,false);
            child.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/"+Names[i]+".asset");
            var renderer=child.AddComponent<MeshRenderer>();renderer.sharedMaterial=Material(i);
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.sortingOrder=i;player.layers[i]=renderer;
            PrefabUtility.SaveAsPrefabAsset(child,Root+"/Prefabs/"+Names[i]+".prefab");
        }
        Support(root,player);
        // Save before sampling: prefab retains the original asset Mesh, never an instance simulation.
        player.splashSource=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/"+Names[1]+".asset");
        player.sampleTime=.24f;player.autoPlay=true;PrefabUtility.SaveAsPrefabAsset(root,Prefab);Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
    }
    [MenuItem("Steria/Water Thunder/Rebuild authored assets")]
    static void Rebuild(){Build();}
    [MenuItem("Steria/Water Thunder/Open authoring stage")]
    public static void OpenStage()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        if(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab)==null)Build();
        CreateStage(true);EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Root+"/Scenes/WaterThunderAuthoring.unity");
        Selection.activeGameObject=GameObject.Find("Slazeya Water + Thunder Slash");
    }
    public static WaterThunderSlash CreateStage(bool context=false)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;
        camera.orthographicSize=2.60f;camera.transform.position=new Vector3(.65f,2.05f,-15);camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=new Color(.018f,.03f,.052f);camera.allowHDR=false;camera.allowMSAA=true;
        var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        obj.transform.position=new Vector3(0,.10f,0);var player=obj.GetComponent<WaterThunderSlash>();player.autoPlay=false;
        if(context)AddContext();
        if(context){camera.transform.position=new Vector3(.65f,3.2f,-15);camera.transform.LookAt(new Vector3(.65f,2.05f,0));}
        return player;
    }
    public static void AddContext()
    {
        var mat=StageMaterial("StageGround",new Color(.06f,.095f,.13f));
        var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.name="XZ Ground - Y zero";floor.transform.localScale=new Vector3(.9f,1,.5f);floor.GetComponent<Renderer>().sharedMaterial=mat;
        var target=GameObject.CreatePrimitive(PrimitiveType.Capsule);target.name="Hit target - placeholder";target.transform.position=new Vector3(WaterThunderMotion.Contact.x,1.05f,.4f);target.transform.localScale=new Vector3(.6f,1.05f,.6f);
        target.GetComponent<Renderer>().sharedMaterial=StageMaterial("StageTarget",new Color(.17f,.22f,.28f));
    }
    static Material StageMaterial(string name,Color color)
    {
        string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Unlit/Color")){name=name,color=color};AssetDatabase.CreateAsset(m,path);}return m;
    }
}
