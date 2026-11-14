using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Steria.Vfx;

// Second art direction. Materials are authored assets: rebuilding preserves Inspector edits.
public static class SlazeyaWindSweptBuilder
{
    const string Folder="Assets/SlazeyaWind";
    const string Inputs="Assets/SlazeyaWindInputs";
    public static void Build()
    {
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        var shader=AssetDatabase.LoadAssetAtPath<Shader>(Inputs+"/SwordSlashSweptWedgeBuiltInV1.shader");
        if(shader==null||!shader.isSupported)throw new InvalidOperationException("SweptWedge shader is unavailable");
        string texturePath=Inputs+"/wind-wedge-v3.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType=TextureImporterType.Default;importer.sRGBTexture=false;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;
        importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
        importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        var flowImporter=(TextureImporter)AssetImporter.GetAtPath(Inputs+"/wind-flow-cc0.png");
        flowImporter.sRGBTexture=false;flowImporter.wrapMode=TextureWrapMode.Repeat;flowImporter.mipmapEnabled=true;
        flowImporter.textureCompression=TextureImporterCompression.Uncompressed;flowImporter.SaveAndReimport();
        Material blade=Material("WindSweptBlade",shader,texture);
        Material pressure=PressureMaterial(false,texture);
        Material guard=PressureMaterial(true,null);
        Material motes=MoteMaterial();
        Material pierce=PierceMaterial();
        Material impactRing=ImpactRingMaterial();
        foreach(SlazeyaWindKind kind in Enum.GetValues(typeof(SlazeyaWindKind)))
        {
            var root=new GameObject("SlazeyaWind"+kind);
            // Chosen by inspecting and rotating the live Unity authoring stage.
            if(kind==SlazeyaWindKind.Slash)root.transform.localRotation=Quaternion.Euler(55,0,25);
            GameObject space=root;
            if(kind==SlazeyaWindKind.Projectile)
            {
                space=new GameObject("WorldVisualSpace");space.transform.SetParent(root.transform,false);
                space.transform.localScale=Vector3.one*3;
            }
            var main=Add(space,"Main",Save(Shape(kind,false),Folder+"/Swept"+kind+".asset"),kind==SlazeyaWindKind.Guard?guard:kind==SlazeyaWindKind.Slash?blade:kind==SlazeyaWindKind.Pierce?pierce:pressure,kind==SlazeyaWindKind.Pierce?.64f:.5f,0);
            if(kind==SlazeyaWindKind.Slash)main.transform.localPosition=new Vector3(.35f,.05f,0);
            if(kind==SlazeyaWindKind.Guard)main.transform.localPosition=new Vector3(.45f,0,0);
            if(kind==SlazeyaWindKind.Slash||kind==SlazeyaWindKind.Pierce||kind==SlazeyaWindKind.Projectile)
            {
                var wake=Add(space,"Wake",Save(Shape(kind,true),Folder+"/Swept"+kind+"Wake.asset"),kind==SlazeyaWindKind.Slash?blade:kind==SlazeyaWindKind.Pierce?pierce:pressure,kind==SlazeyaWindKind.Pierce?.52f:.32f,1);
                wake.transform.localPosition=new Vector3(.2f,-.22f,.015f);
            }
            if(kind==SlazeyaWindKind.Projectile)
            {
                var contact=Add(space,"Contact",Save(Shape(SlazeyaWindKind.Hit,true),Folder+"/SweptContact.asset"),pressure,.20f,2);
                contact.transform.localScale=Vector3.one;
                AddMotes(space,motes,"Arrival");
            }
            AddMotes(space,motes);
            if(kind==SlazeyaWindKind.Pierce)
            {
                AddImpactSprite(space,"ImpactRing",impactRing,.44f,4);
                AddImpactSprite(space,"ImpactFlash",motes,.14f,5);
                AddMotes(space,motes,"ImpactMotes");
            }
            PrefabUtility.SaveAsPrefabAsset(root,SlazeyaWindParticleBuilder.PrefabPath(kind));
            UnityEngine.Object.DestroyImmediate(root);
        }
        AssetDatabase.SaveAssets();
    }
    static Material Material(string name,Shader shader,Texture texture)
    {
        string path=Folder+"/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material!=null)return material;
        material=new Material(shader){name=name};
        material.SetTexture("_WedgeTex",texture);
        material.SetTexture("_HighlightTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Inputs+"/wind-flow-cc0.png"));
        material.SetColor("_BaseColor",new Color(.18f,.58f,.41f,1));
        material.SetColor("_EmissionColor",new Color(.70f,1f,.88f,1));
        material.SetFloat("_Emission",.7f);material.SetFloat("_Alpha",1);
        material.SetFloat("_Sweep",1);material.SetFloat("_WedgeSpan",.78f);
        // The fixed shader already premultiplies coverage into RGB.
        material.SetFloat("_SrcBlend",1);material.SetFloat("_DstBlend",1);
        material.SetFloat("_Cull",0);material.SetFloat("_ZWrite",0);
        AssetDatabase.CreateAsset(material,path);return material;
    }
    static Material PressureMaterial(bool guard,Texture shape)
    {
        string path=Folder+(guard?"/WindGuardFormed.mat":"/WindPressureFormed.mat");
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/SkillSwordSlash/ContractV1/SwordSlashFlowBuiltInContractV1.shader");
        m=new Material(shader){name=guard?"WindGuardFormed":"WindPressureFormed"};
        if(shape!=null)m.SetTexture("_ShapeTex",shape);
        m.SetTexture("_HighlightTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Inputs+"/wind-flow-cc0.png"));
        m.SetColor("_BaseColor",new Color(.21f,.65f,.53f,1));m.SetColor("_EmissionColor",new Color(.8f,1,.92f,1));
        m.SetFloat("_Emission",1.2f);m.SetFloat("_BodyStrength",.85f);
        m.SetFloat("_HighlightBandCenter",.62f);m.SetFloat("_HighlightBandWidth",.55f);m.SetFloat("_HighlightBandSoftness",.20f);
        m.SetFloat("_FlowStrength",0);m.SetFloat("_DistortStrength",0);
        m.SetFloat("_SrcBlend",5);m.SetFloat("_DstBlend",1);m.SetFloat("_Cull",0);m.SetFloat("_ZWrite",0);
        AssetDatabase.CreateAsset(m,path);return m;
    }
    static Material MoteMaterial()
    {
        string path=Folder+"/WindMotes.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
        m=new Material(Shader.Find("Particles/Standard Unlit")){name="WindMotes"};
        m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Inputs+"/wind-mote-cc0.png"));
        m.SetFloat("_Mode",2);m.SetInt("_SrcBlend",5);m.SetInt("_DstBlend",1);m.SetInt("_ZWrite",0);m.SetInt("_Cull",0);
        m.EnableKeyword("_ALPHABLEND_ON");m.renderQueue=3000;m.SetColor("_Color",Color.white);
        AssetDatabase.CreateAsset(m,path);return m;
    }
    static Material PierceMaterial()
    {
        string path=Folder+"/WindPierceCore.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/SkillSwordSlash/ContractV1/SwordSlashFlowBuiltInContractV1.shader");
        m=new Material(shader){name="WindPierceCore"};
        m.SetColor("_BaseColor",new Color(.10f,.65f,.51f,1));m.SetColor("_EmissionColor",new Color(.9f,1,.97f,1));
        m.SetFloat("_Emission",1.35f);m.SetFloat("_BodyStrength",.65f);
        m.SetFloat("_HighlightBandCenter",.5f);m.SetFloat("_HighlightBandWidth",.4f);m.SetFloat("_HighlightBandSoftness",.14f);
        m.SetTexture("_HighlightTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Inputs+"/wind-flow-cc0.png"));
        m.SetFloat("_FlowStrength",0);m.SetFloat("_DistortStrength",0);
        m.SetFloat("_SrcBlend",5);m.SetFloat("_DstBlend",1);m.SetFloat("_Cull",0);m.SetFloat("_ZWrite",0);
        AssetDatabase.CreateAsset(m,path);return m;
    }
    static Material ImpactRingMaterial()
    {
        string texture=Inputs+"/wind-impact-ring-mit.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(texture);
        importer.textureType=TextureImporterType.Default;importer.wrapMode=TextureWrapMode.Clamp;
        importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.maxTextureSize=512;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        string path=Folder+"/WindPierceImpactRing.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        bool create=m==null;if(create)m=new Material(Shader.Find("Particles/Standard Unlit")){name="WindPierceImpactRing"};
        m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));m.SetColor("_Color",Color.white);
        m.SetFloat("_Mode",2);m.SetInt("_SrcBlend",5);m.SetInt("_DstBlend",1);m.SetInt("_ZWrite",0);m.SetInt("_Cull",0);
        m.EnableKeyword("_ALPHABLEND_ON");m.renderQueue=3000;
        if(create)AssetDatabase.CreateAsset(m,path);else EditorUtility.SetDirty(m);return m;
    }
    static void AddImpactSprite(GameObject parent,string name,Material material,float lifetime,int sorting)
    {
        var go=AddMotes(parent,material,name);var ps=go.GetComponent<ParticleSystem>();var main=ps.main;
        main.startLifetime=lifetime;main.startSize3D=true;main.startSizeX=2;main.startSizeY=2;main.startSizeZ=1;main.maxParticles=1;
        var color=ps.colorOverLifetime;var g=new Gradient();
        g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(.4f,.95f,.82f),1)},
            new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.85f,.3f),new GradientAlphaKey(.3f,.7f),new GradientAlphaKey(0,1)});
        color.color=g;go.GetComponent<ParticleSystemRenderer>().sortingOrder=sorting;
    }
    static GameObject AddMotes(GameObject parent,Material material,string name="Motes")
    {
        var go=new GameObject(name);go.transform.SetParent(parent.transform,false);
        var ps=go.AddComponent<ParticleSystem>();var m=ps.main;
        m.playOnAwake=false;m.loop=false;m.startSpeed=0;m.startLifetime=.3f;m.maxParticles=16;
        m.scalingMode=ParticleSystemScalingMode.Hierarchy;m.simulationSpace=ParticleSystemSimulationSpace.Local;
        var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
        var color=ps.colorOverLifetime;color.enabled=true;
        var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(new Color(.64f,1f,.86f),0),new GradientColorKey(new Color(.25f,.7f,.58f),1)},new[]{new GradientAlphaKey(.9f,0),new GradientAlphaKey(0,1)});color.color=g;
        var r=go.GetComponent<ParticleSystemRenderer>();r.renderMode=ParticleSystemRenderMode.Billboard;r.sharedMaterial=material;
        r.sortingLayerName="Particle";r.sortingOrder=3;
        ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);return go;
    }
    static GameObject Add(GameObject root,string name,Mesh mesh,Material mat,float lifetime,int order)
    {
        var go=new GameObject(name);go.transform.SetParent(root.transform,false);
        var ps=go.AddComponent<ParticleSystem>();var m=ps.main;
        m.playOnAwake=false;m.loop=false;m.duration=.6f;m.startLifetime=lifetime;
        m.startSpeed=0;m.startSize=1;m.maxParticles=4;m.simulationSpace=ParticleSystemSimulationSpace.Local;
        m.scalingMode=ParticleSystemScalingMode.Hierarchy;
        var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
        var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;
        renderer.mesh=mesh;renderer.sharedMaterial=mat;renderer.alignment=ParticleSystemRenderSpace.Local;
        renderer.sortingLayerName="Particle";renderer.sortingOrder=order;renderer.enableGPUInstancing=false;
        renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Normal,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV});
        ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);return go;
    }
    static Mesh Shape(SlazeyaWindKind kind,bool secondary)
    {
        const int segments=62,rows=4;
        var vertices=new Vector3[(segments+1)*rows];var uv=new Vector2[vertices.Length];var colors=new Color32[vertices.Length];
        var triangles=new List<int>();
        for(int i=0;i<=segments;i++)
        {
            float t=i/(float)segments;Vector2 centre,normal;float width;
            if(kind==SlazeyaWindKind.Slash)
            {
                float a=(secondary?105+115*t:40+205*t)*Mathf.Deg2Rad;
                centre=new Vector2(-.2f+(secondary?1.35f:1.55f)*Mathf.Cos(a),(secondary?.72f:1.55f)*Mathf.Sin(a));
                normal=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                width=(secondary?.27f:.86f)*Mathf.Pow(Mathf.Sin(Mathf.PI*t),.45f);
            }
            else if(kind==SlazeyaWindKind.Guard)
            {
                float a=(110+140*t)*Mathf.Deg2Rad;
                centre=new Vector2(-.38f+.7f*Mathf.Cos(a),1.7f*Mathf.Sin(a));normal=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                width=.52f*Mathf.Sin(t*Mathf.PI);
            }
            else if(kind==SlazeyaWindKind.Projectile&&!secondary)
            {
                float a=(75+150*t)*Mathf.Deg2Rad;
                centre=new Vector2(-.25f+.8f*Mathf.Cos(a),1.0f*Mathf.Sin(a));normal=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                width=.58f*Mathf.Sin(t*Mathf.PI);
            }
            else if(kind==SlazeyaWindKind.Pierce)
            {
                // Unit axis: the runtime extends this from the hand through the target.
                centre=new Vector2(-t,0);normal=Vector2.up;
                width=Mathf.Pow(Mathf.Sin(Mathf.PI*t),.85f)*(1-.60f*t);
            }
            else
            {
                float length=kind==SlazeyaWindKind.Pierce?3.3f:kind==SlazeyaWindKind.Projectile?1.5f:2.1f;
                float direction=kind==SlazeyaWindKind.Projectile?1:-1;
                centre=new Vector2(.2f+direction*length*t,(kind==SlazeyaWindKind.Hit?-.4f*t:0)+.16f*Mathf.Sin(t*Mathf.PI));
                normal=Vector2.up;
                width=(kind==SlazeyaWindKind.Hit?.9f:kind==SlazeyaWindKind.Pierce?.5f:.18f)*Mathf.Pow(Mathf.Sin(t*Mathf.PI),.65f);
                if(secondary)width*=.55f;
            }
            float scale=kind==SlazeyaWindKind.Guard?1.1f:kind==SlazeyaWindKind.Projectile||kind==SlazeyaWindKind.Pierce?1:.80f;
            for(int j=0;j<rows;j++)
            {
                float v=j==0?0:j==1?.28f:j==2?.72f:1;
                Vector2 p=centre+normal*(v-.48f)*width;
                int index=i*rows+j;vertices[index]=new Vector3(p.x*scale,p.y*scale,0);uv[index]=new Vector2(t,v);
                float a=j==0||j==3?0:1;
                colors[index]=new Color(.85f,1f,.95f,a);
            }
            if(i==0)continue;
            for(int j=0;j<rows-1;j++)
            {
                int a=(i-1)*rows+j,b=i*rows+j;
                triangles.Add(a);triangles.Add(b);triangles.Add(a+1);triangles.Add(a+1);triangles.Add(b);triangles.Add(b+1);
            }
        }
        var mesh=new Mesh{name="WindSwept_"+kind+(secondary?"Return":"")};
        mesh.vertices=vertices;mesh.uv=uv;mesh.colors32=colors;mesh.triangles=triangles.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static T Save<T>(T value,string path) where T:UnityEngine.Object
    {
        var existing=AssetDatabase.LoadAssetAtPath<T>(path);
        if(existing==null){AssetDatabase.CreateAsset(value,path);return value;}
        EditorUtility.CopySerialized(value,existing);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(value);return existing;
    }
}
