using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class SlazeyaStormMassBundleBuilder
{
    private const string PrefabPath="Assets/Prefabs/SlazeyaStormMassPrefab.prefab";
    private const float H=6f;
    private const int CallbackStep=93;
    private static float ActualCallbackTime;
    private static readonly Vector3[] Feet={new Vector3(4,0,-3),new Vector3(10,0,3),new Vector3(16,0,-2),new Vector3(7,0,5),new Vector3(15,0,5)};
    private static readonly Vector3 Caster=new Vector3(-15,0,0);
    private static readonly string[] SampleNames={"01_bubble_established","02_initial_envelope","02a_collapse_start","02b_collapse_inflow","02c_half_collapse","03_core_charge","03a_core_arrived","03b_core_peak","03c_core_hold","03d_callback_before","04_core_B0","04a_front_B1","04b_front_B2","04c_gap_B4","04d_flash_B7","05_front_B11","06_waterfall_B14","07_falling_fragments","08_low_mist","09_clear"};
    private static readonly float[] GatherSamples={0.25f,0.80f,0.82f,0.95f,1.10f,1.23f,1.28f,80f/60f,1.35f,92f/60f};
    private static readonly float[] BurstSamples={0,1f/60f,2f/60f,4f/60f,7f/60f,11f/60f,14f/60f,0.45f,0.90f,1.25f};
    private static readonly List<float> ActualSampleTimes=new List<float>();
    private static readonly string[] TextureFiles={"water_body_rgba.png","water_normal_roughness.png","water_flow_rg.png","foam_spray_4x4.png","mist_density_lighting_4x4.png","droplet_spindrift_4x2.png"};
    private static readonly List<string> TextureHashes=new List<string>();

    public static void BuildBundle()
    {
        try
        {
            Require(Application.unityVersion=="2019.3.15f1","Unity 2019.3.15f1");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            PlayerSettings.colorSpace=ColorSpace.Gamma; QualitySettings.antiAliasing=4;
            foreach(string dir in new[]{"Assets/Textures/Round2","Assets/Materials/Round3","Assets/Meshes/Round3","Assets/Prefabs"}) Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
            Shader waterShader=Shader.Find(SlazeyaStormVisualController.ShaderName), particleShader=Shader.Find(SlazeyaStormVisualController.ParticleShaderName);
            Shader lightningShader=Shader.Find(SlazeyaStormVisualController.LightningShaderName);
            Require(waterShader!=null&&waterShader.isSupported&&!ShaderUtil.ShaderHasError(waterShader),"R4 deformed water shader supported");
            Require(particleShader!=null&&particleShader.isSupported&&!ShaderUtil.ShaderHasError(particleShader),"R4 native age atlas shader supported");
            Require(lightningShader!=null&&lightningShader.isSupported&&!ShaderUtil.ShaderHasError(lightningShader),"finite local lightning shader supported");
            Texture2D[] textures=ImportWorkerTextures();
            Material water=WaterMaterial("BreakerWater",waterShader,textures,false);
            Material low=WaterMaterial("InflowWater",waterShader,textures,true);
            Material spray=ParticleMaterial("FoamSheets",particleShader,textures[3],new Vector4(4,4,1,0));
            // The revised worker atlas supplies an open asymmetric membrane; consume its full A.
            spray.SetFloat("_SheetBreakup",0);
            Material mist=ParticleMaterial("VolumeMist",particleShader,textures[4],new Vector4(4,4,1,1));
            Material returnMist=ParticleMaterial("ReturnMist",particleShader,textures[4],new Vector4(4,4,1,1));
            returnMist.SetColor("_MistDark",new Color(0.208f,0.275f,0.322f));returnMist.SetColor("_MistLight",new Color(0.604f,0.671f,0.722f));
            Material liftMist=ParticleMaterial("CrestLiftMist",particleShader,textures[4],new Vector4(4,4,1,1));
            liftMist.SetColor("_MistDark",new Color(0.208f,0.275f,0.322f));liftMist.SetColor("_MistLight",new Color(0.784f,0.835f,0.875f));
            Material drops=ParticleMaterial("Spindrift",particleShader,textures[5],new Vector4(4,2,0,0));
            Material gatheredFoam=ParticleMaterial("GatheredWhitewater",particleShader,textures[4],new Vector4(4,4,1,0));
            Material parcelFoam=ParticleMaterial("TransportFoamDetail",particleShader,textures[4],new Vector4(4,4,1,0));
            parcelFoam.SetFloat("_DetailOpacity",0.30f);
            var root=new GameObject(SlazeyaStormVisualController.PrefabName);
            Transform ground=Child(root.transform,"AB_GroundRoot"),storm=Child(root.transform,"AB_StormRoot");
            Transform lightning=Child(Child(root.transform,"AB_BurstRoot"),"LightningRoot");
            var lightningMaterial=new Material(lightningShader){name="FiniteDischarge"};lightningMaterial.SetColor("_Core",new Color(0.953f,0.973f,1));lightningMaterial.SetColor("_Edge",new Color(0.659f,0.749f,0.820f));SaveAsset(lightningMaterial,"Assets/Materials/Round3/FiniteDischarge.mat");
            MakeLightning(lightning,"PrimaryBolt",lightningMaterial,0);MakeLightning(lightning,"SecondaryBolt",lightningMaterial,1);
            Transform particles=Child(root.transform,"AB_ParticleRoot");
            MakeBubbleSurface(storm,"BubbleEnvelope",false,water);
            MakeBubbleSurface(root.transform.Find("AB_BurstRoot"),"RingJet",true,water);
            MakeParticles(particles,"GatherMist",mist,40,false);
            MakeParticles(particles,"CrestSpindrift",drops,64,true);
            MakeParticles(particles,"BurstSheets",spray,48,false);
            MakeParticles(particles,"BurstDroplets",drops,180,true);
            MakeParticles(particles,"ReturnMist",returnMist,32,false);
            MakeParticles(particles,"CrestLiftMist",liftMist,24,false);
            MakeParticles(particles,"GatherFoam",parcelFoam,168,false);
            MakeParticles(particles,"WaterfallStreaks",gatheredFoam,72,true);
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath); Object.DestroyImmediate(root); AssetDatabase.SaveAssets();
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../AssetBundles")); Directory.CreateDirectory(output);
            var build=BuildPipeline.BuildAssetBundles(output,new[]{new AssetBundleBuild{assetBundleName=SlazeyaStormVisualController.BundleName,assetNames=new[]{PrefabPath}}},
                BuildAssetBundleOptions.ChunkBasedCompression|BuildAssetBundleOptions.ForceRebuildAssetBundle,BuildTarget.StandaloneWindows64);
            Require(build!=null,"Windows64 LZ4 R4 bundle");
            string bundlePath=Path.Combine(output,SlazeyaStormVisualController.BundleName);
            AssetBundle bundle=AssetBundle.LoadFromFile(bundlePath); Require(bundle!=null,"R4 bundle readback");
            try
            {
                GameObject prefab=bundle.LoadAsset<GameObject>(SlazeyaStormVisualController.PrefabName);
                VerifyPrefab(prefab); VerifyController(prefab);
                string previews=PreviewDirectory(); Directory.CreateDirectory(previews);
                ActualSampleTimes.Clear(); CaptureSet(prefab,previews,"black",false); CaptureSet(prefab,previews,"battlefield",true);
                CaptureAtlasDiagnostics(prefab,previews);
                WriteManifest(previews,bundlePath);
            }
            finally {bundle.Unload(true);}
            Debug.Log("SLAZEYA_ROUND4_BUILD_PREVIEW_PASS "+bundlePath);
        }
        catch(Exception ex) {Debug.LogException(ex);EditorApplication.Exit(1);}
    }

    private static void Require(bool ok,string text)
    {
        if(!ok) throw new Exception("FAIL "+text);
        Debug.Log("PASS "+text);
    }
    private static Transform Child(Transform parent,string name)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);return t;
    }
    private static void SaveAsset(Object asset,string path)
    {
        if(AssetDatabase.LoadAssetAtPath<Object>(path)!=null) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(asset,path);
    }
    private static Texture2D[] ImportWorkerTextures()
    {
        string source=Path.GetFullPath(Path.Combine(Application.dataPath,"../../source_assets/round2"));
        var textures=new Texture2D[TextureFiles.Length];TextureHashes.Clear();
        for(int i=0;i<TextureFiles.Length;i++)
        {
            string file=Path.Combine(source,TextureFiles[i]);
            Require(File.Exists(file),"approved worker source asset "+TextureFiles[i]);
            string imported="Assets/Textures/Round2/"+TextureFiles[i];
            File.Copy(file,imported,true);TextureHashes.Add(Digest(file));
            Require(Digest(file)==Digest(imported),"exact worker PNG copy "+TextureFiles[i]);
            AssetDatabase.ImportAsset(imported,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(imported);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=false;
            importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.wrapModeU=i<3?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
            importer.wrapModeV=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.maxTextureSize=1024;
            importer.SaveAndReimport();
            textures[i]=AssetDatabase.LoadAssetAtPath<Texture2D>(imported);
            int width=i==2?512:1024, height=i==3||i==4?1024:512;
            Require(textures[i]!=null&&textures[i].width==width&&textures[i].height==height,"frozen PNG dimensions "+TextureFiles[i]);
        }
        return textures;
    }
    private static Material WaterMaterial(string name,Shader shader,Texture2D[] textures,bool low)
    {
        var material=new Material(shader){name=name};
        material.SetTexture("_BodyTex",textures[0]);material.SetTexture("_NormalRoughness",textures[1]);material.SetTexture("_FlowTex",textures[2]);
        material.SetFloat("_Alpha",0);
        SaveAsset(material,"Assets/Materials/Round3/"+name+".mat");return material;
    }
    private static Material ParticleMaterial(string name,Shader shader,Texture texture,Vector4 grid)
    {
        var material=new Material(shader){name=name};material.SetTexture("_Atlas",texture);material.SetVector("_Grid",grid);
        SaveAsset(material,"Assets/Materials/Round3/"+name+".mat");return material;
    }
    private static void MakeBubbleSurface(Transform parent,string name,bool ring,Material material)
    {
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var patch=new List<Vector4>();var triangles=new List<int>();
        const int sectors=SlazeyaStormVisualController.SectorCount,around=8,vertical=12;
        float[] latitude={0,0.38f,0.72f,1};
        for(int sector=0;sector<sectors;sector++)
        {
            float u0=(float)sector/sectors,u1=(float)(sector+1)/sectors,angle=(u0+u1)*Mathf.PI;
            if(ring)AddPatch(u0,u1,0,1,angle,2,16,24,vertices,uv,patch,triangles);
            else
            {
                for(int region=0;region<3;region++)AddPatch(u0,u1,latitude[region],latitude[region+1],angle,0,around,vertical,vertices,uv,patch,triangles);
                // Joined bottom cap: each sector fan is coincident before rupture and can peel separately afterwards.
                AddPatch(u0,u1,0,1,angle,1,around,6,vertices,uv,patch,triangles);
            }
        }
        var mesh=new Mesh{name=name+"_R4",vertices=vertices.ToArray(),uv=uv.ToArray(),triangles=triangles.ToArray()};mesh.SetUVs(1,patch);
        mesh.bounds=new Bounds(Vector3.zero,Vector3.one*8);SaveAsset(mesh,"Assets/Meshes/Round3/"+name+".asset");
        Transform node=Child(parent,name);node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=node.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
    }
    private static void AddPatch(float u0,float u1,float low,float high,float sector,int kind,int around,int rows,List<Vector3> vertices,List<Vector2> uv,List<Vector4> patch,List<int> triangles)
    {
        int start=vertices.Count;bool top=kind==0&&high==1;
        int regularRows=top?rows:rows+1;
        for(int i=0;i<=around;i++)for(int j=0;j<regularRows;j++)
        {
            float u=Mathf.Lerp(u0,u1,(float)i/around),v=Mathf.Lerp(low,high,(float)j/rows);
            vertices.Add(new Vector3(Mathf.Cos(u*Mathf.PI*2),v,Mathf.Sin(u*Mathf.PI*2)));
            uv.Add(new Vector2(u,v));patch.Add(new Vector4(sector,low,high,kind));
        }
        for(int i=0;i<around;i++)for(int j=0;j<regularRows-1;j++)
        {
            int k=start+i*regularRows+j,n=k+regularRows;
            if(kind==2){triangles.Add(k);triangles.Add(n);triangles.Add(k+1);triangles.Add(k+1);triangles.Add(n);triangles.Add(n+1);}
            else{triangles.Add(k);triangles.Add(k+1);triangles.Add(n);triangles.Add(k+1);triangles.Add(n+1);triangles.Add(n);}
        }
        if(top)
        {
            // A single pole vertex per fracture-sector fan: identical position/normal across sectors until B.
            int pole=vertices.Count;vertices.Add(Vector3.up);uv.Add(new Vector2((u0+u1)*0.5f,1));patch.Add(new Vector4(sector,low,high,kind));
            for(int i=0;i<around;i++){int k=start+i*regularRows+regularRows-1;triangles.Add(k);triangles.Add(pole);triangles.Add(k+regularRows);}
        }
    }
    private static void MakeParticles(Transform parent,string name,Material material,int capacity,bool stretch)
    {
        var node=Child(parent,name);var ps=node.gameObject.AddComponent<ParticleSystem>();
        ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.duration=4f;main.loop=false;main.playOnAwake=false;main.startSpeed=0;
        main.startLifetime=1f;main.maxParticles=capacity;main.startSize=0.1f;
        main.simulationSpace=ParticleSystemSimulationSpace.Local;main.scalingMode=ParticleSystemScalingMode.Local;
        main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
        main.gravityModifier=name=="WaterfallStreaks"?2.8f:name=="BurstDroplets"?1.8f:name=="BurstSheets"?0.7f:0f;
        if(name=="BurstDroplets"||name=="BurstSheets")
        {
            var velocity=ps.velocityOverLifetime;velocity.enabled=true;
            velocity.speedModifier=new ParticleSystem.MinMaxCurve(1f,new AnimationCurve(new Keyframe(0,1),new Keyframe(0.35f,0.55f),new Keyframe(1,0.10f)));
        }
        var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
        var sheet=ps.textureSheetAnimation;sheet.enabled=false;
        ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(capacity+1945);
        bool mist=name=="GatherMist"||name=="ReturnMist"||name=="CrestLiftMist";
        var color=ps.colorOverLifetime;color.enabled=!mist;
        color.color=new Gradient{colorKeys=new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            alphaKeys=new[]{new GradientAlphaKey(0.4f,0),new GradientAlphaKey(1,0.10f),new GradientAlphaKey(0.8f,0.65f),new GradientAlphaKey(0,1)}};
        if(name=="GatherFoam")color.enabled=false;
        if(name=="CrestLiftMist"||name=="ReturnMist")
        {
            var size=ps.sizeOverLifetime;size.enabled=true;
            size.size=new ParticleSystem.MinMaxCurve(1f,new AnimationCurve(new Keyframe(0,1),new Keyframe(0.7f,name=="CrestLiftMist"?2f:1.5f),new Keyframe(1,name=="CrestLiftMist"?2.1f:1.55f)));
            var velocity=ps.velocityOverLifetime;velocity.enabled=true;
            velocity.speedModifier=new ParticleSystem.MinMaxCurve(1f,new AnimationCurve(new Keyframe(0,1),new Keyframe(1,0.35f)));
        }
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
        renderer.renderMode=stretch?ParticleSystemRenderMode.Stretch:ParticleSystemRenderMode.Billboard;
        renderer.lengthScale=1.7f;renderer.velocityScale=0.025f;renderer.cameraVelocityScale=0;
        if(name=="WaterfallStreaks"){renderer.lengthScale=3.1f;renderer.velocityScale=0.055f;}
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Color,
            ParticleSystemVertexStream.UV,ParticleSystemVertexStream.AgePercent,ParticleSystemVertexStream.StableRandomX});
    }

    private static void MakeLightning(Transform parent,string name,Material material,int variant)
    {
        float sign=variant==0?1:-1;
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
        var path=new[]{new Vector3(0,0,0),new Vector3(0.15f,0.055f*sign,0),new Vector3(0.32f,-0.06f*sign,0),
            new Vector3(0.51f,0.047f*sign,0),new Vector3(0.72f,-0.043f*sign,0),new Vector3(0.87f,0.038f*sign,0),new Vector3(1,0,0)};
        AddBoltStroke(path,0.023f,vertices,uv,triangles);
        AddBoltStroke(new[]{path[3],new Vector3(0.56f,-0.09f*sign,0),new Vector3(0.63f,-0.13f*sign,0),new Vector3(0.60f,-0.22f*sign,0)},0.010f,vertices,uv,triangles);
        var mesh=new Mesh{name=name,vertices=vertices.ToArray(),uv=uv.ToArray(),triangles=triangles.ToArray()};mesh.RecalculateBounds();
        SaveAsset(mesh,"Assets/Meshes/Round3/"+name+".asset");
        Transform node=Child(parent,name);node.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=node.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.enabled=false;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
    }
    private static void AddBoltStroke(Vector3[] points,float halfWidth,List<Vector3> vertices,List<Vector2> uv,List<int> triangles)
    {
        int offset=vertices.Count;
        for(int i=0;i<points.Length;i++)
        {
            Vector3 direction=(points[Mathf.Min(points.Length-1,i+1)]-points[Mathf.Max(0,i-1)]).normalized;
            Vector3 across=new Vector3(-direction.y,direction.x,0)*halfWidth;
            vertices.Add(points[i]-across);vertices.Add(points[i]+across);
            uv.Add(new Vector2((float)i/(points.Length-1),0));uv.Add(new Vector2((float)i/(points.Length-1),1));
            if(i==points.Length-1)continue;
            int k=offset+i*2;triangles.Add(k);triangles.Add(k+2);triangles.Add(k+1);triangles.Add(k+1);triangles.Add(k+2);triangles.Add(k+3);
        }
    }

    private static void VerifyPrefab(GameObject prefab)
    {
        Require(prefab!=null,"fixed-name R4 prefab readback");
        foreach(Transform node in prefab.GetComponentsInChildren<Transform>(true))
            foreach(Component c in node.GetComponents<Component>())
                Require(c!=null&&!(c is MonoBehaviour),"native component "+node.name);
        foreach(Renderer r in prefab.GetComponentsInChildren<Renderer>(true))
        {
            Material m=r.sharedMaterial;
            Require(m!=null&&m.shader!=null&&m.shader.isSupported,"readback supported shader "+r.name);
            bool water=m.shader.name==SlazeyaStormVisualController.ShaderName;
            bool particle=m.shader.name==SlazeyaStormVisualController.ParticleShaderName;
            Require(water||particle||m.shader.name==SlazeyaStormVisualController.LightningShaderName,"R4 shader whitelist "+r.name);
            foreach(string prop in water?new[]{"_BodyTex","_NormalRoughness","_FlowTex"}:particle?new[]{"_Atlas"}:new string[0])
                Require(m.GetTexture(prop)!=null,"real texture dependency "+r.name+prop);
        }
        foreach(MeshFilter f in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            Require(f.sharedMesh!=null,"morph mesh "+f.name);
            if(f.name.EndsWith("Bolt"))continue;
            var patchData=new List<Vector4>();f.sharedMesh.GetUVs(1,patchData);
            Require(patchData.Count==f.sharedMesh.vertexCount,"bubble / ring patch topology "+f.name);
        }
        foreach(ParticleSystem p in prefab.GetComponentsInChildren<ParticleSystem>(true))
        {
            Require(!p.main.loop&&!p.main.playOnAwake&&!p.emission.enabled&&p.main.maxParticles>0,"finite explicit system "+p.name);
            var streams=new List<ParticleSystemVertexStream>();p.GetComponent<ParticleSystemRenderer>().GetActiveVertexStreams(streams);
            Require(streams.Contains(ParticleSystemVertexStream.AgePercent),"native particle-age atlas stream "+p.name);
        }
    }
    private static Bounds[] PreviewBodies(Vector3[] feet,float height)
    {
        var bodies=new Bounds[feet.Length];
        for(int i=0;i<feet.Length;i++)bodies[i]=new Bounds(feet[i]+Vector3.up*height*0.5f,new Vector3(2.18f,height,0.70f));
        return bodies;
    }
    [Serializable] private class RecipientGeometry
    {
        public Vector3 foot,head,min,max;
        public float maxNormalizedRadius;
        public Vector2 sectionAtFoot;
    }
    [Serializable] private class EnvelopeCase
    {
        public string name;
        public SlazeyaStormVisualController.Footprint footprint;
        public RecipientGeometry[] recipients;
        public float topY,poleGap,seamGap,bottomJoinGap;
        public float[] initialSampleTimes={0.25f,0.80f};
    }
    [Serializable] private class EnvelopeReport {public List<EnvelopeCase> cases=new List<EnvelopeCase>();}
    private static void VerifyEnvelopeCases(GameObject prefab)
    {
        var report=new EnvelopeReport();string output=PreviewDirectory();Directory.CreateDirectory(output);
        for(int scenario=0;scenario<4;scenario++)
        {
            string name=new[]{"single","five","tall_1_8H","raised_foot_0_35H"}[scenario];
            Vector3[] feet=scenario==0?new[]{Feet[1]}:(Vector3[])Feet.Clone();
            if(scenario==3)feet[2].y+=0.35f*H;
            Bounds[] bodies=PreviewBodies(feet,H);
            if(scenario==2)bodies[2]=new Bounds(feet[2]+Vector3.up*H*0.9f,new Vector3(2.18f,H*1.8f,0.7f));
            var f=SlazeyaStormVisualController.FitFootprint(feet,H,bodies);
            var item=new EnvelopeCase{name=name,footprint=f,topY=f.EnvelopeCenter.y+f.EnvelopeRadii.y*1.14f,recipients=new RecipientGeometry[feet.Length]};
            for(int i=0;i<feet.Length;i++)
            {
                var samples=SlazeyaStormVisualController.BodySamples(feet[i],bodies[i]);float maximum=0;
                foreach(var p in samples){maximum=Mathf.Max(maximum,SlazeyaStormVisualController.NormalizedRadius(f,p));Require(p.y>=f.GroundY,"above closed bottom "+name+"/"+i);}
                Require(maximum<=0.90001f,"all foot/head/eight body corners enclosed "+name+"/"+i+" norm="+maximum);
                item.recipients[i]=new RecipientGeometry{foot=feet[i],head=samples[1],min=bodies[i].min,max=bodies[i].max,maxNormalizedRadius=maximum,sectionAtFoot=SlazeyaStormVisualController.GroundSection(f,feet[i].y)};
            }
            for(int i=0;i<=32;i++)
            {
                float theta=i/32f*Mathf.PI*2;
                var pole=SlazeyaStormVisualController.BubblePoint(f,theta,1,theta,0.72f,1,false,0.8f/1.35f,2.4f,-1);
                item.poleGap=Mathf.Max(item.poleGap,Vector3.Distance(pole,new Vector3(f.EnvelopeCenter.x,item.topY,f.EnvelopeCenter.z)));
                var bottom=SlazeyaStormVisualController.BubblePoint(f,theta,0,theta,0,0.38f,false,0.8f/1.35f,2.4f,-1);
                var cap=SlazeyaStormVisualController.BubblePoint(f,theta,1,theta,0,1,true,0.8f/1.35f,2.4f,-1);
                item.bottomJoinGap=Mathf.Max(item.bottomJoinGap,Vector3.Distance(bottom,cap));
                var seam0=SlazeyaStormVisualController.BubblePoint(f,0,i/32f,0,0,1,false,0.8f/1.35f,2.4f,-1);
                var seam1=SlazeyaStormVisualController.BubblePoint(f,Mathf.PI*2,i/32f,Mathf.PI*2,0,1,false,0.8f/1.35f,2.4f,-1);
                item.seamGap=Mathf.Max(item.seamGap,Vector3.Distance(seam0,seam1));
                Require(SlazeyaStormVisualController.IsFinite(pole)&&SlazeyaStormVisualController.IsFinite(seam0),"finite pole / shell "+name);
            }
            Require(item.poleGap<0.0001f&&item.seamGap<0.0001f&&item.bottomJoinGap<0.0001f,"closed pole / UV seam / cap "+name);
            report.cases.Add(item);CaptureGeometryWire(prefab,f,feet,bodies,output,name,false);
            if(scenario==1)CaptureGeometryWire(prefab,f,feet,bodies,output,name,true);
        }
        File.WriteAllText(Path.Combine(output,"envelope-containment.json"),JsonUtility.ToJson(report,true));
    }
    private static void CaptureGeometryWire(GameObject prefab,SlazeyaStormVisualController.Footprint f,Vector3[] feet,Bounds[] bodies,string output,string name,bool side)
    {
        var root=Object.Instantiate(prefab);var markers=new GameObject("EnvelopeSampleMarkers");Camera camera=CreateCamera(false);
        if(side){camera.transform.position=f.EnvelopeCenter+new Vector3(45,4,0);camera.transform.LookAt(f.EnvelopeCenter);}
        var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;
        using(var driver=new SlazeyaStormVisualController(root,f))
        {
            driver.Advance(0.80f);foreach(var r in root.GetComponentsInChildren<Renderer>())r.enabled=false;
            root.transform.Find("AB_StormRoot/BubbleEnvelope").GetComponent<Renderer>().enabled=true;
            var bodyMaterial=StageMaterial(new Color(0.50f,0.47f,0.23f));var footMaterial=StageMaterial(new Color(1,0.85f,0.20f));var headMaterial=StageMaterial(new Color(1,0.35f,0.72f));
            for(int i=0;i<feet.Length;i++)
            {
                Cube(markers.transform,bodies[i].center,bodies[i].size,bodyMaterial);
                foreach(var p in new[]{feet[i],new Vector3(bodies[i].center.x,bodies[i].max.y,bodies[i].center.z)})
                {
                    var dot=GameObject.CreatePrimitive(PrimitiveType.Sphere);dot.transform.SetParent(markers.transform,false);dot.transform.position=p;dot.transform.localScale=Vector3.one*0.36f;
                    dot.GetComponent<Renderer>().sharedMaterial=p==feet[i]?footMaterial:headMaterial;
                }
            }
            try{GL.wireframe=true;File.WriteAllBytes(Path.Combine(output,"geometry_"+name+(side?"_side":"")+".png"),Render(camera,rt,pixels));}
            finally{GL.wireframe=false;}
        }
        Object.DestroyImmediate(root);Object.DestroyImmediate(markers);camera.targetTexture=null;Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);
    }

    private static void VerifyController(GameObject prefab)
    {
        var footprint=SlazeyaStormVisualController.FitFootprint(Feet,H,PreviewBodies(Feet,H));
        VerifyFoamTransport(prefab,footprint);
        foreach(Vector3 foot in Feet)
        {
            float x=(foot.x-footprint.Center.x)/footprint.RadiusX,z=(foot.z-footprint.Center.z)/footprint.RadiusZ;
            Require(x*x+z*z<=1.00001f,"frozen ellipse covers "+foot);
        }
        VerifyEnvelopeCases(prefab);
        var root=Object.Instantiate(prefab);
        using(var driver=new SlazeyaStormVisualController(root,footprint))
        {
            for(int i=0;i<120;i++)driver.Advance(1f/60f);
            Require(driver.GatherReady&&!driver.BurstTriggered&&!driver.IsComplete,"hold cannot invent burst");
            Require(driver.TriggerBurst()&&!driver.TriggerBurst(),"burst callback idempotent");
            Require(driver.AliveParticles>0,"native crest emission");
            for(int i=0;i<79;i++)driver.Advance(1f/60f);
            Require(driver.IsComplete&&driver.AliveParticles==0,"R4 tail clears native particles");
            foreach(Renderer r in root.GetComponentsInChildren<Renderer>())Require(!r.enabled,"clear renderer "+r.name);
        }
        Object.DestroyImmediate(root);
        using(var missing=new SlazeyaStormVisualController(null,footprint))
        {
            missing.Advance(1.4f);missing.TriggerBurst();missing.Advance(1.3f);Require(missing.IsComplete,"missing bundle lifecycle");
        }
    }
    private static float InspectCoreSupport(GameObject root,SlazeyaStormVisualController driver,SlazeyaStormVisualController.Footprint f,StringBuilder csv)
    {
        float maximum=0;
        foreach(Renderer renderer in root.GetComponentsInChildren<Renderer>())
        {
            if(!renderer.enabled)continue;float support=0;
            var ps=renderer.GetComponent<ParticleSystem>();
            if(ps!=null)
            {
                var particles=new ParticleSystem.Particle[ps.main.maxParticles];int count=ps.GetParticles(particles);
                var pr=(ParticleSystemRenderer)renderer;
                for(int i=0;i<count;i++)
                {
                    float half=particles[i].GetCurrentSize(ps)*0.707107f;
                    if(pr.renderMode==ParticleSystemRenderMode.Stretch)half=half*(1+pr.lengthScale)+particles[i].velocity.magnitude*pr.velocityScale;
                    support=Mathf.Max(support,Vector3.Distance(ps.transform.TransformPoint(particles[i].position),driver.CoreCenter)+half);
                }
            }
            else
            {
                Mesh mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                if(renderer.name.EndsWith("Bolt"))
                {foreach(Vector3 v in mesh.vertices)support=Mathf.Max(support,Vector3.Distance(renderer.transform.TransformPoint(v),driver.CoreCenter));}
                else
                {
                    var patches=new List<Vector4>();mesh.GetUVs(1,patches);Vector2[] uv=mesh.uv;
                    for(int i=0;i<uv.Length;i++)
                    {
                        Vector4 patch=patches[i];float theta=uv[i].x*Mathf.PI*2;
                        Vector3 point=patch.w>1.5f?SlazeyaStormVisualController.RingPoint(f,theta,uv[i].y,driver.BurstAge):SlazeyaStormVisualController.BubblePoint(f,theta,uv[i].y,patch.x,patch.y,patch.z,patch.w>0.5f,Mathf.Clamp01(driver.Elapsed/1.35f),driver.ShapePhase,driver.BurstAge);
                        RequireFinite(point,renderer.name);support=Mathf.Max(support,Vector3.Distance(point,driver.CoreCenter));
                    }
                }
            }
            csv.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0:F6},{1:F6},{2},{3:F6},{4:F6}",driver.Elapsed,driver.BurstAge,renderer.name,support,driver.CoreVisibleLimit));
            if(support>driver.CoreVisibleLimit+H*0.0001f)throw new Exception("Core support exceeded by "+renderer.name+": "+support);
            maximum=Mathf.Max(maximum,support);
        }
        return maximum;
    }
    private static void RequireFinite(Vector3 point,string name)
    {if(!SlazeyaStormVisualController.IsFinite(point))throw new Exception("Nonfinite deformed point "+name);}
    private static void VerifyFoamTransport(GameObject prefab,SlazeyaStormVisualController.Footprint footprint)
    {
        Directory.CreateDirectory(PreviewDirectory());var root=Object.Instantiate(prefab);
        var parcels=new ParticleSystem.Particle[168];var previousRadii=new float[168];for(int i=0;i<168;i++)previousRadii[i]=float.MaxValue;
        var csv=new StringBuilder("time,parcel,x,y,z,core_distance,collapse,charge\n");
        var support=new StringBuilder("time,burst_age,renderer,support_radius,limit\n");
        using(var driver=new SlazeyaStormVisualController(root,footprint))
        {
            var ps=root.transform.Find("AB_ParticleRoot/GatherFoam").GetComponent<ParticleSystem>();
            float[] samples={0.82f,0.95f,1.10f,1.23f,1.28f,1.333333f,1.35f,2.0f};float previous=0;
            foreach(float time in samples)
            {
                while(previous<time-0.000001f){float dt=Mathf.Min(1f/120f,time-previous);driver.Advance(dt);previous+=dt;}
                int count=ps.GetParticles(parcels);Require(count==168,"persistent native foam identities "+time);
                for(int i=0;i<count;i++)
                {
                    Vector3 world=ps.transform.TransformPoint(parcels[i].position);float distance=Vector3.Distance(world,driver.CoreCenter);
                    if(Vector3.Distance(world,driver.FoamParcelPosition(i))>0.0001f)throw new Exception("Native parcel mismatch "+i);
                    if(distance>previousRadii[i]+H*0.0001f)throw new Exception("3D contraction reversed seed "+i);
                    previousRadii[i]=distance;
                    if(time>=1.28f&&distance>driver.CoreRadius+H*0.0001f)throw new Exception("Seed outside core "+i);
                    csv.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0:F5},{1},{2:F5},{3:F5},{4:F5},{5:F5},{6:F5},{7:F5}",time,i,world.x,world.y,world.z,distance,driver.CollapseProgress,driver.Charge));
                }
                if(time>=1.32f)InspectCoreSupport(root,driver,footprint,support);
            }
            Require(!driver.BurstTriggered&&driver.Charge>=0.75f,"holds ONLY the lit core until manager callback");
            Camera camera=CreateCamera(true);var stage=CreateStage(true);var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;
            File.WriteAllBytes(Path.Combine(PreviewDirectory(),"battlefield_core_hold_G2.png"),Render(camera,rt,pixels));
            camera.targetTexture=null;Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(stage);Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);
            driver.TriggerBurst();InspectCoreSupport(root,driver,footprint,support);
            Require(driver.Propagation==0,"B0 starts inside core with zero propagation");
        }
        Object.DestroyImmediate(root);
        File.WriteAllText(Path.Combine(PreviewDirectory(),"native-foam-transport.csv"),csv.ToString());
        File.WriteAllText(Path.Combine(PreviewDirectory(),"native-core-support.csv"),support.ToString());
        var wave=new StringBuilder("burst_age,w,water_x,water_y,water_z,water_radius,adjacent_step\n");Vector3 previousPoint=SlazeyaStormVisualController.RingPoint(footprint,2.25f,0.72f,0);
        float previousRadius=0;
        for(int i=0;i<=56;i++)
        {
            float b=i/240f;Vector3 point=SlazeyaStormVisualController.RingPoint(footprint,2.25f,0.72f,b);
            float radius=Vector3.Distance(point,footprint.EnvelopeCenter),step=Vector3.Distance(point,previousPoint);
            if(step>H*0.15f||radius+0.0001f<previousRadius)throw new Exception("Wavefront discontinuity "+b);
            wave.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0:F6},{1:F6},{2:F6},{3:F6},{4:F6},{5:F6},{6:F6}",b,SlazeyaStormVisualController.WaveProgress(b),point.x,point.y,point.z,radius,step));
            previousRadius=radius;previousPoint=point;
        }
        File.WriteAllText(Path.Combine(PreviewDirectory(),"native-wavefront.csv"),wave.ToString());
        Require(true,"R4 3D core seeds, all rendered support, B0 and continuous wavefront");
    }
    private static byte[] Render(Camera camera,RenderTexture rt,Texture2D pixels)
    {
        RenderTexture old=RenderTexture.active;
        camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();RenderTexture.active=old;
        return pixels.EncodeToPNG();
    }
    private static void CaptureSet(GameObject prefab,string output,string label,bool stage)
    {
        string sequence=Path.Combine(output,label+"_sequence"),peak=Path.Combine(output,label+"_peak60");
        Directory.CreateDirectory(sequence);Directory.CreateDirectory(peak);
        GameObject scenery=CreateStage(stage);Camera camera=CreateCamera(stage);
        var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32){antiAliasing=4};
        var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;
        GameObject root=Object.Instantiate(prefab);var trace=new StringBuilder("time,burst_age,driver_burst_age,phase,foam_gather,radius_ratio,alive_particles,lightning_energy,ready,burst,complete\n");
        int nextSample=0,frame=0,maximumAlive=0;
        using(var controller=new SlazeyaStormVisualController(root,SlazeyaStormVisualController.FitFootprint(Feet,H,PreviewBodies(Feet,H))))
        {
            for(int step=0;step<=180;step++)
            {
                float time=step/60f;
                if(step>0)controller.Advance(1f/60f);
                if(step==CallbackStep){ActualCallbackTime=time;controller.TriggerBurst();}
                float burstAge=controller.BurstTriggered?time-ActualCallbackTime:-1f;
                maximumAlive=Mathf.Max(maximumAlive,controller.AliveParticles);
                trace.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0:F4},{1:F4},{2:F4},{3:F4},{4:F4},{5:F4},{6},{7:F4},{8},{9},{10}",time,burstAge,controller.BurstAge,controller.Phase,controller.FoamGather,controller.RadiusRatio,controller.AliveParticles,controller.LightningEnergy,controller.GatherReady,controller.BurstTriggered,controller.IsComplete));
                if(burstAge>0.155f||(burstAge>0.055f&&burstAge<0.095f))
                    if(controller.LightningEnergy>0.001f)throw new Exception("Lightning escaped finite pulse windows");
                bool sample=nextSample<SampleNames.Length&&(nextSample<GatherSamples.Length?time+0.0001f>=GatherSamples[nextSample]:burstAge+0.0001f>=BurstSamples[nextSample-GatherSamples.Length]);
                bool peakFrame=step>=CallbackStep-1&&step<=CallbackStep+14;
                if(step%2==0||sample||peakFrame)
                {
                    byte[] png=Render(camera,rt,pixels);
                    if(step%2==0)File.WriteAllBytes(Path.Combine(sequence,"frame_"+(frame++).ToString("D4")+".png"),png);
                    if(peakFrame)File.WriteAllBytes(Path.Combine(peak,"frame_"+(step-CallbackStep+1).ToString("D3")+".png"),png);
                    if(sample)
                    {
                        string name=label+"_"+SampleNames[nextSample];File.WriteAllBytes(Path.Combine(output,name+".png"),png);
                        if(label=="black")ActualSampleTimes.Add(time);
                        Debug.Log("Native R4 "+name+" t="+time+" B="+burstAge+" particles="+controller.AliveParticles);
                        if(SampleNames[nextSample]=="03b_core_peak"||SampleNames[nextSample]=="06_waterfall_B14")
                        {
                            var particleRenderers=root.GetComponentsInChildren<ParticleSystemRenderer>();
                            foreach(var r in particleRenderers)r.enabled=false;
                            File.WriteAllBytes(Path.Combine(output,name+"_main_shape.png"),Render(camera,rt,pixels));
                            foreach(var r in particleRenderers)r.enabled=true;
                        }
                        nextSample++;
                    }
                }
            }
            Require(nextSample==SampleNames.Length,"all actual callback-relative representative frames "+label);
            Require(controller.IsComplete&&maximumAlive>0,"finite native sequence "+label+" maximumAlive="+maximumAlive);
        }
        File.WriteAllText(Path.Combine(output,label+"_timeline.csv"),trace.ToString());
        Object.DestroyImmediate(root);camera.targetTexture=null;Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(scenery);Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);
    }
    private static void CaptureAtlasDiagnostics(GameObject prefab,string output)
    {
        var evidence=new StringBuilder("atlas,sample,burst_age,native_min_age,native_max_age,rendered_median_age,pixels\n");
        for(int atlas=0;atlas<2;atlas++)
        {
            var root=Object.Instantiate(prefab);
            Camera camera=CreateCamera(false);
            var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);
            var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=rt;
            string name=atlas==0?"foam":"mist";
            ParticleSystem target=root.transform.Find("AB_ParticleRoot/"+(atlas==0?"BurstSheets":"ReturnMist")).GetComponent<ParticleSystem>();
            var renderer=target.GetComponent<ParticleSystemRenderer>();
            int[] steps=atlas==0?new[]{4,12,24}:new[]{24,40,61};
            var particleData=new ParticleSystem.Particle[target.main.maxParticles];
            using(var controller=new SlazeyaStormVisualController(root,SlazeyaStormVisualController.FitFootprint(Feet,H,PreviewBodies(Feet,H))))
            {
                controller.Advance(1.55f);controller.TriggerBurst();int sample=0;
                for(int step=0;step<=steps[2];step++)
                {
                    if(step>0)controller.Advance(1f/60f);
                    if(step!=steps[sample])continue;
                    foreach(Renderer r in root.GetComponentsInChildren<Renderer>())r.enabled=false;
                    renderer.enabled=true;
                    File.WriteAllBytes(Path.Combine(output,"atlas_"+name+"_"+sample+".png"),Render(camera,rt,pixels));
                    int count=target.GetParticles(particleData);Require(count>0,"native "+name+" sample particles");
                    float min=1,max=0;
                    for(int i=0;i<count;i++){float age=1-particleData[i].remainingLifetime/particleData[i].startLifetime;min=Mathf.Min(min,age);max=Mathf.Max(max,age);}
                    var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetFloat("_DebugAge",1);renderer.SetPropertyBlock(block);
                    Render(camera,rt,pixels);
                    var observed=new List<byte>();
                    foreach(Color32 pixel in pixels.GetPixels32())if(pixel.b==0&&pixel.g>3)observed.Add(pixel.r);
                    Require(observed.Count>16,"native age probe visible "+name);
                    observed.Sort();float median=observed[observed.Count/2]/255f;
                    Require(median>=min-0.04f&&median<=max+0.04f,"native TEXCOORD UV.xy AgePercent.z StableRandomX.w "+name+" sample="+sample+" age="+median+" expected="+min+".."+max);
                    evidence.AppendLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2:F4},{3:F4},{4:F4},{5:F4},{6}",name,sample,step/60f,min,max,median,observed.Count));
                    block.SetFloat("_DebugAge",0);renderer.SetPropertyBlock(block);
                    sample++;if(sample==3)break;
                }
            }
            Object.DestroyImmediate(root);camera.targetTexture=null;Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);
        }
        File.WriteAllText(Path.Combine(output,"native-atlas-age-verification.csv"),evidence.ToString());
    }

    [Serializable] private class Manifest
    {
        public string version=SlazeyaStormVisualController.Version;
        public string unityVersion,graphicsDevice,colorSpace,bundle,bundleSha256,controllerSha256;
        public string source="AssetBundle.LoadFromFile -> script-free prefab -> canonical controller -> native shader and particles";
        public int width=1280,height=720,sequenceFps=30,simulationHz=60;
        public float referenceBodyHeight=H,callbackTime,orthographicSize=13.8f;
        public float coreRadius=H*0.06f,coreVisibleLimit=H*0.12f;
        public string propagation="smoothstep(B/0.23); B0,+1,+2,+4,+7,+11,+14 native 60Hz samples";
        public float gatherDuration=SlazeyaStormVisualController.GatherDuration,tailDuration=SlazeyaStormVisualController.TailDuration;
        public Vector3 cameraPosition=new Vector3(2,27,-56),cameraLookAt=new Vector3(2,2,1),casterFoot=Caster;
        public Vector3[] recipientFeet=Feet;
        public string[] representativeFiles=SampleNames;
        public float[] representativeTimes;
        public string[] textureFiles=TextureFiles,textureSha256;
        public SlazeyaStormVisualController.Footprint footprint;
        public string normals="Finite parametric ellipsoid/rupture derivatives; finite pole fan normal; closed body coverage independent of texture A gutter. Fixed effect-owned illumination.";
        public string atlas="PNG top-left row-major, bilinear tile clamp plus authored 12px gutters, real AgePercent.z, dual-frame interpolation, stable random droplet variant.";
        public string lightning="B[0,.05) primary; B[.05,.10) dark; B[.10,.15) secondary 60%; then zero. One branched mesh per pulse, same-surface core/edge; bounded local water illumination.";
        public string mist="Authored A coverage only, R lighting, no particle color-lifetime double fade; lift clusters B.04/.11; return clusters B.36/.50, finite explicit Simulate.";
        public string limitations="Native Unity visual proof only. Independent art and game PlayMode acceptance belong to main thread.";
    }
    private static string Digest(string path)
    {
        using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");
    }
    private static void WriteManifest(string output,string bundle)
    {
        var manifest=new Manifest{unityVersion=Application.unityVersion,graphicsDevice=SystemInfo.graphicsDeviceName,colorSpace=QualitySettings.activeColorSpace.ToString(),
            bundle=bundle,bundleSha256=Digest(bundle),controllerSha256=Digest(Path.Combine(Application.dataPath,"Scripts/SlazeyaStormVisualController.cs")),callbackTime=ActualCallbackTime,
            representativeTimes=ActualSampleTimes.ToArray(),textureSha256=TextureHashes.ToArray(),footprint=SlazeyaStormVisualController.FitFootprint(Feet,H,PreviewBodies(Feet,H))};
        File.WriteAllText(Path.Combine(output,"manifest.json"),JsonUtility.ToJson(manifest,true));
    }
    private static string PreviewDirectory()
    {
        var dir=new DirectoryInfo(Application.dataPath);
        while(dir!=null&&!File.Exists(Path.Combine(dir.FullName,"AGENTS.md")))dir=dir.Parent;
        if(dir==null)throw new Exception("Cannot locate repository preview directory");
        return Path.Combine(dir.FullName,"preview_exports/slazeya_storm_mass/round4");
    }

    private static Camera CreateCamera(bool stage)
    {
        Camera camera = new GameObject("NativeStormPreviewCamera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = stage ? new Color(0.095f,0.084f,0.077f) : Color.black;
        camera.orthographic = true; camera.orthographicSize = 13.8f;
        camera.nearClipPlane = 0.1f; camera.farClipPlane = 160;
        camera.transform.position = new Vector3(2f,27f,-56f);
        camera.transform.LookAt(new Vector3(2f,2f,1f));
        camera.allowHDR = false;
        return camera;
    }

    private static Material StageMaterial(Color color)
    {
        var material = new Material(Shader.Find("Unlit/Color"));
        material.color = color; material.hideFlags = HideFlags.DontSave;
        return material;
    }

    private static GameObject CreateStage(bool stage)
    {
        var root = new GameObject("ScaleReferences");
        if (stage)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.SetParent(root.transform,false);
            floor.transform.localScale = new Vector3(8,1,6);
            floor.GetComponent<Renderer>().sharedMaterial = StageMaterial(new Color(0.19f,0.17f,0.15f));
            var gridMaterial = StageMaterial(new Color(0.23f,0.21f,0.185f));
            for (int i = -4; i <= 4; i++)
            {
                Cube(root.transform,new Vector3(i*6f,0.009f,0),new Vector3(0.025f,0.01f,40),gridMaterial);
                Cube(root.transform,new Vector3(0,0.01f,i*5f),new Vector3(65,0.01f,0.025f),gridMaterial);
            }
        }
        var victimMaterial = StageMaterial(stage ? new Color(0.43f,0.39f,0.32f) : new Color(0.21f,0.23f,0.24f));
        foreach (var foot in Feet) Character(root.transform,foot,victimMaterial);
        Character(root.transform,Caster,StageMaterial(new Color(0.22f,0.37f,0.43f)));
        return root;
    }

    private static void Cube(Transform root, Vector3 position, Vector3 scale, Material material)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(root,false); cube.transform.localPosition = position; cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void Character(Transform parent, Vector3 foot, Material material)
    {
        var root = Child(parent,"ReferenceCharacter_H6"); root.localPosition = foot;
        Cube(root,new Vector3(-0.35f,H*0.22f,0),new Vector3(0.42f,H*0.44f,0.55f),material);
        Cube(root,new Vector3(0.35f,H*0.22f,0),new Vector3(0.42f,H*0.44f,0.55f),material);
        Cube(root,new Vector3(0,H*0.62f,0),new Vector3(1.45f,H*0.36f,0.7f),material);
        Cube(root,new Vector3(-0.94f,H*0.58f,0),new Vector3(0.30f,H*0.34f,0.4f),material);
        Cube(root,new Vector3(0.94f,H*0.58f,0),new Vector3(0.30f,H*0.34f,0.4f),material);
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(root,false); head.transform.localPosition = new Vector3(0,H*0.90f,0);
        head.transform.localScale = Vector3.one * H*0.20f;
        head.GetComponent<Renderer>().sharedMaterial = material;
    }


}
