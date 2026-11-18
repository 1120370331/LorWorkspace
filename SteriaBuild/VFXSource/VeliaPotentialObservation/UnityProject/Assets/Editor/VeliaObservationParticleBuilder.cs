using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

// R6: one anchored light field, one hotspot, and sparse moving straight lights.
public static class VeliaObservationParticleBuilder
{
    public const string PrefabPath="Assets/Prefabs/VeliaObservationParticles.prefab";
    private const string ShaderPath="Assets/Shaders/VeliaObservationParticle.shader";
    private static readonly string[] Names={"CoreFlow","SoftLift","Glints"};
    private static readonly string[] Materials={"VeliaObservationFlow","VeliaObservationMist","VeliaObservationGlint"};
    [Serializable] public sealed class Key { public float time,value; }
    [Serializable] public sealed class BurstReceipt { public float time;public int min,max; }
    [Serializable] public sealed class SystemReceipt
    {
        public string name,simulationSpace,scalingMode,renderMode,texture,material,shader,blend="One One (fixed shader)",maskChannel="R linear";
        public uint seed;public bool playOnAwake,loop,autoSeed,velocityEnabled,atlasEnabled,sizeEnabled;
        public float lifetimeMin,lifetimeMax,widthMin,widthMax,heightMin,heightMax,maskMode,coreEnergy,haloEnergy,emissionGain,faceMask;
        public int sortingOrder;public Vector3 position,shapeScale;
        public string velocityXMode,velocityYMode,velocityZMode;
        public Key[] velocityXMin,velocityXMax,velocityY,sizeX,sizeY,alpha;public BurstReceipt[] bursts;
    }
    [Serializable] private sealed class RecipeReceipt { public string units="H normalized; anchored field shader uses explicit Age";public SystemReceipt[] systems; }
    [Serializable] public sealed class TextureReceipt
    {
        public string name,format,filter,wrap,channel="R",colorSpace;
        public int width,height,nonzeroR,partialR;public float minimumR,maximumR,meanR,borderRMax;public bool mipmaps;
    }
    [Serializable] private sealed class TextureReceipts { public TextureReceipt[] textures; }
    private static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    private static AnimationCurve Curve(params float[] values)
    {
        var keys=new Keyframe[values.Length/2];for(int i=0;i<keys.Length;i++)keys[i]=new Keyframe(values[i*2],values[i*2+1]);
        var curve=new AnimationCurve(keys);for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}return curve;
    }
    private static ParticleSystem.MinMaxCurve Range(float min,float max){return new ParticleSystem.MinMaxCurve(min,max);}
    private static ParticleSystem.MinMaxCurve Same(params float[] values){return new ParticleSystem.MinMaxCurve(1,Curve(values),Curve(values));}
    private static ParticleSystem.MinMaxCurve Spread(params float[] values)
    {var neg=(float[])values.Clone();for(int i=1;i<neg.Length;i+=2)neg[i]=-neg[i];return new ParticleSystem.MinMaxCurve(1,Curve(neg),Curve(values));}
    private static Gradient GlintFade()
    {
        var result=new Gradient();result.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.65f,.15f),new GradientAlphaKey(.4f,.6f),new GradientAlphaKey(0,1)});return result;
    }
    private static Texture2D ImportTexture(string root,string file)
    {
        string path="Assets/Textures/LightMasks/"+file;Directory.CreateDirectory("Assets/Textures/LightMasks");File.Copy(Path.Combine(root,file),path,true);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Default;importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency=false;importer.sRGBTexture=false;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.npotScale=TextureImporterNPOTScale.None;importer.isReadable=true;importer.maxTextureSize=2048;
        var settings=importer.GetPlatformTextureSettings("Standalone");settings.name="Standalone";settings.overridden=true;settings.format=TextureImporterFormat.RGBA32;settings.maxTextureSize=2048;importer.SetPlatformTextureSettings(settings);
        importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static Material Material(Shader shader,string name,Texture2D mask,int role)
    {
        string path="Assets/Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(shader);material.name=name;AssetDatabase.CreateAsset(material,path);}
        material.shader=shader;material.name=name;material.SetTexture("_MainTex",mask);material.SetColor("_Tint",Color.white);
        material.SetFloat("_MaskMode",role);material.SetFloat("_CoreEnergy",role==0?.16f:role==1?.85f:.8f);material.SetFloat("_HaloEnergy",role==0?.18f:role==1?.28f:.15f);material.SetFloat("_EmissionGain",1);
        material.SetFloat("_FaceMask",0);material.renderQueue=(int)RenderQueue.Transparent;EditorUtility.SetDirty(material);return material;
    }
    public static GameObject Build(string repo,string output)
    {
        string source=Path.Combine(repo,"SteriaBuild/VFXSource/VeliaPotentialObservation/source_assets/light-masks");
        var masks=new[]{ImportTexture(source,"surge-mask.png"),ImportTexture(source,"soft-source.png"),ImportTexture(source,"small-streak.png")};
        foreach(var texture in masks)Require(texture.width==512&&texture.height==512,"Preserve original 512 mask dimensions");
        Shader shader=AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);Require(shader!=null&&shader.isSupported,"Light-mask shader unavailable");
        Directory.CreateDirectory("Assets/Materials");Directory.CreateDirectory("Assets/Prefabs");
        var root=new GameObject("VeliaObservationParticles");root.SetActive(false);var body=new GameObject("BodyRoot");body.transform.SetParent(root.transform,false);
        var receipts=new List<SystemReceipt>();
        for(int i=0;i<3;i++)
        {
            var go=new GameObject(Names[i]);go.transform.SetParent(body.transform,false);
            go.transform.localPosition=new Vector3(0,i==0?.70f:i==1?.10f:0,0);
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.duration=1.45f;main.loop=false;main.playOnAwake=false;main.startDelay=0;
            main.simulationSpace=ParticleSystemSimulationSpace.Local;main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.startSpeed=0;main.gravityModifier=0;main.maxParticles=i<2?2:16;
            main.startLifetime=i<2?Range(1.25f,1.25f):Range(.30f,.55f);main.startSize3D=true;
            main.startSizeX=i==0?Range(.70f,.70f):i==1?Range(.62f,.62f):Range(.008f,.020f);
            main.startSizeY=i==0?Range(1.35f,1.35f):i==1?Range(.62f,.62f):Range(.04f,.12f);main.startSizeZ=Range(1,1);
            main.startColor=Color.white;main.startRotation3D=false;main.startRotation=i<2?0:Range(-12*Mathf.Deg2Rad,12*Mathf.Deg2Rad);
            ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(19731+i*101);
            var shape=ps.shape;shape.enabled=i==2;if(i==2){shape.shapeType=ParticleSystemShapeType.Box;shape.position=new Vector3(0,.25f,0);shape.scale=new Vector3(.16f,.30f,.01f);}
            var emission=ps.emission;emission.enabled=true;emission.rateOverTime=0;emission.rateOverDistance=0;
            emission.SetBursts(i<2?new[]{new ParticleSystem.Burst(.16f,1)}:new[]{new ParticleSystem.Burst(.34f,3),new ParticleSystem.Burst(.51f,3),new ParticleSystem.Burst(.68f,2)});
            var velocity=ps.velocityOverLifetime;velocity.enabled=i>0;
            if(i==1){velocity.space=ParticleSystemSimulationSpace.Local;velocity.x=Same(0,0,1,0);velocity.y=Same(0,1.8f,1,.3f);velocity.z=Same(0,0,1,0);velocity.speedModifier=1;}
            if(i==2){velocity.space=ParticleSystemSimulationSpace.Local;velocity.x=Spread(0,.1f,1,.5f);velocity.y=Same(0,1.5f,1,.5f);velocity.z=Same(0,0,1,0);velocity.speedModifier=1;}
            var size=ps.sizeOverLifetime;size.enabled=i==1;
            if(i==1){size.separateAxes=true;size.x=new ParticleSystem.MinMaxCurve(1,Curve(0,.6f,.3f,1,.7f,1.4f,1,1.5f));size.y=new ParticleSystem.MinMaxCurve(1,Curve(0,.6f,.3f,.9f,1,.8f));size.z=new ParticleSystem.MinMaxCurve(1,Curve(0,1,1,1));}
            var color=ps.colorOverLifetime;color.enabled=i==2;if(i==2)color.color=new ParticleSystem.MinMaxGradient(GlintFade());
            var atlas=ps.textureSheetAnimation;atlas.enabled=false;
            var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.alignment=ParticleSystemRenderSpace.View;
            renderer.sharedMaterial=Material(shader,Materials[i],masks[i],i);renderer.sortingOrder=i<2?1:2;
            renderer.sortMode=ParticleSystemSortMode.OldestInFront;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            ps.Pause(false);receipts.Add(Describe(ps));
        }
        PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
        File.WriteAllText(Path.Combine(output,"particle-recipe.json"),JsonUtility.ToJson(new RecipeReceipt{systems=receipts.ToArray()},true));
        var textureReceipts=new List<TextureReceipt>();foreach(var mask in masks)textureReceipts.Add(DescribeTexture(mask));
        File.WriteAllText(Path.Combine(output,"imported-textures.json"),JsonUtility.ToJson(new TextureReceipts{textures=textureReceipts.ToArray()},true));
        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    }
    private static Key[] Keys(AnimationCurve curve)
    {if(curve==null)return new Key[0];var result=new List<Key>();foreach(var k in curve.keys)result.Add(new Key{time=k.time,value=k.value});return result.ToArray();}
    private static SystemReceipt Describe(ParticleSystem ps)
    {
        var m=ps.main;var v=ps.velocityOverLifetime;var renderer=ps.GetComponent<ParticleSystemRenderer>();var mat=renderer.sharedMaterial;
        var bursts=new ParticleSystem.Burst[ps.emission.burstCount];ps.emission.GetBursts(bursts);var b=new List<BurstReceipt>();foreach(var x in bursts)b.Add(new BurstReceipt{time=x.time,min=x.minCount,max=x.maxCount});
        var alpha=new List<Key>();if(ps.colorOverLifetime.enabled)foreach(var k in ps.colorOverLifetime.color.gradient.alphaKeys)alpha.Add(new Key{time=k.time,value=k.alpha});
        return new SystemReceipt{name=ps.name,simulationSpace=m.simulationSpace.ToString(),scalingMode=m.scalingMode.ToString(),renderMode=renderer.renderMode.ToString(),texture=mat.mainTexture.name,material=mat.name,shader=mat.shader.name,
            seed=ps.randomSeed,playOnAwake=m.playOnAwake,loop=m.loop,autoSeed=ps.useAutoRandomSeed,velocityEnabled=v.enabled,atlasEnabled=ps.textureSheetAnimation.enabled,sizeEnabled=ps.sizeOverLifetime.enabled,
            lifetimeMin=m.startLifetime.constantMin,lifetimeMax=m.startLifetime.constantMax,widthMin=m.startSizeX.constantMin,widthMax=m.startSizeX.constantMax,heightMin=m.startSizeY.constantMin,heightMax=m.startSizeY.constantMax,
            maskMode=mat.GetFloat("_MaskMode"),coreEnergy=mat.GetFloat("_CoreEnergy"),haloEnergy=mat.GetFloat("_HaloEnergy"),emissionGain=mat.GetFloat("_EmissionGain"),faceMask=mat.GetFloat("_FaceMask"),sortingOrder=renderer.sortingOrder,
            position=ps.transform.localPosition,shapeScale=ps.shape.scale,velocityXMode=v.x.mode.ToString(),velocityYMode=v.y.mode.ToString(),velocityZMode=v.z.mode.ToString(),
            velocityXMin=Keys(v.x.curveMin),velocityXMax=Keys(v.x.curveMax),velocityY=Keys(v.y.curveMax),sizeX=Keys(ps.sizeOverLifetime.x.curve),sizeY=Keys(ps.sizeOverLifetime.y.curve),alpha=alpha.ToArray(),bursts=b.ToArray()};
    }
    public static TextureReceipt DescribeTexture(Texture2D texture)
    {
        Require(texture!=null&&texture.format==TextureFormat.RGBA32,"RGBA32 mask readback required");
        var pixels=texture.GetPixels32();int nonzero=0,partial=0;byte minimum=255,maximum=0,edge=0;double sum=0;
        for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)
        {
            var p=pixels[y*texture.width+x];byte r=p.r;minimum=Math.Min(minimum,r);maximum=Math.Max(maximum,r);sum+=r;
            if(r>0)nonzero++;if(r>0&&r<255)partial++;if(x==0||y==0||x==texture.width-1||y==texture.height-1)edge=Math.Max(edge,r);
            Require(Math.Abs(p.r-p.g)<=1&&Math.Abs(p.r-p.b)<=1,"Mask must be original grayscale, not colored brush "+texture.name);
        }
        Require(minimum<=2&&maximum>128&&partial>100&&nonzero>100,"Mask lacks useful R-channel energy range "+texture.name);
        Require(texture.filterMode==FilterMode.Bilinear&&texture.wrapMode==TextureWrapMode.Clamp&&texture.mipmapCount==1,"Mask sampling contract");
        bool srgb=UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(texture.graphicsFormat);
        Require(!srgb,"Mask must remain linear in native bundle "+texture.name);
        return new TextureReceipt{name=texture.name,width=texture.width,height=texture.height,format=texture.format.ToString(),filter=texture.filterMode.ToString(),wrap=texture.wrapMode.ToString(),
            colorSpace=srgb?"sRGB":"Linear",nonzeroR=nonzero,partialR=partial,minimumR=minimum/255f,maximumR=maximum/255f,meanR=(float)(sum/pixels.Length/255),borderRMax=edge/255f,mipmaps=texture.mipmapCount>1};
    }
    public static void VerifyLoadedPrefab(GameObject prefab,string output)
    {
        Require(prefab!=null&&prefab.name=="VeliaObservationParticles","Bundled prefab missing");
        Require(prefab.GetComponentsInChildren<ParticleSystem>(true).Length==3,"R6 requires exactly three roles, no GoldWisps");
        foreach(var component in prefab.GetComponentsInChildren<Component>(true))Require(component is Transform||component is ParticleSystem||component is ParticleSystemRenderer,"Unexpected prefab component "+component.GetType().Name);
        var receipts=new List<SystemReceipt>();var textures=new List<TextureReceipt>();
        for(int i=0;i<3;i++)
        {
            var child=prefab.transform.Find("BodyRoot/"+Names[i]);Require(child!=null,"Missing role "+Names[i]);var ps=child.GetComponent<ParticleSystem>();var r=child.GetComponent<ParticleSystemRenderer>();
            Require(ps!=null&&r!=null&&r.sharedMaterial!=null&&r.sharedMaterial.shader.isSupported,"Invalid system/material "+Names[i]);
            Require(!ps.main.playOnAwake&&!ps.main.loop&&!ps.useAutoRandomSeed&&ps.main.simulationSpace==ParticleSystemSimulationSpace.Local&&ps.main.scalingMode==ParticleSystemScalingMode.Hierarchy,"Clock/scale contract "+Names[i]);
            Require(r.renderMode==ParticleSystemRenderMode.Billboard&&!ps.textureSheetAnimation.enabled,"Plain mask quad required "+Names[i]);
            Require(r.sharedMaterial.name==Materials[i]&&r.sharedMaterial.GetFloat("_MaskMode")==i,"Mask role material mismatch");
            Require(Mathf.Approximately(r.sharedMaterial.GetFloat("_CoreEnergy"),i==0?.16f:i==1?.85f:.8f)&&Mathf.Approximately(r.sharedMaterial.GetFloat("_HaloEnergy"),i==0?.18f:i==1?.28f:.15f),"Per-role light energy mismatch");
            Require(r.sharedMaterial.GetFloat("_FaceMask")==0,"Horizontal face band must remain disabled");
            Require(r.sortingOrder==(i<2?1:2),"Trailing field and primary moving hotspot must remain foreground");
            if(i==0)Require(!ps.velocityOverLifetime.enabled&&!ps.colorOverLifetime.enabled&&!ps.sizeOverLifetime.enabled&&ps.emission.burstCount==1,"Anchored trailing field must preserve geometry and particle alpha");
            else {var v=ps.velocityOverLifetime;Require(v.x.mode==ParticleSystemCurveMode.TwoCurves&&v.y.mode==ParticleSystemCurveMode.TwoCurves&&v.z.mode==ParticleSystemCurveMode.TwoCurves,"Moving light velocity axes must share mode");}
            if(i==1)Require(ps.sizeOverLifetime.enabled&&!ps.colorOverLifetime.enabled&&ps.emission.burstCount==1,"Single moving hotspot must use size curve and shader envelope");
            var mask=(Texture2D)r.sharedMaterial.mainTexture;Require(mask.name==(i==0?"surge-mask":i==1?"soft-source":"small-streak"),"Old colored atlas dependency detected");
            textures.Add(DescribeTexture(mask));receipts.Add(Describe(ps));
        }
        File.WriteAllText(Path.Combine(output,"bundled-particle-recipe.json"),JsonUtility.ToJson(new RecipeReceipt{systems=receipts.ToArray()},true));
        File.WriteAllText(Path.Combine(output,"bundled-textures.json"),JsonUtility.ToJson(new TextureReceipts{textures=textures.ToArray()},true));
    }
}
