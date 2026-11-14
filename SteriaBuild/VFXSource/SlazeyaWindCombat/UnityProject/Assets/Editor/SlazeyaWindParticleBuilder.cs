using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Steria.Vfx;

public static class SlazeyaWindParticleBuilder
{
    public const string Folder="Assets/SlazeyaWind";
    public static string PrefabPath(SlazeyaWindKind kind){return Folder+"/SlazeyaWind"+kind+".prefab";}
    public static void Build()
    {
        Directory.CreateDirectory(Folder);
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/SkillSwordSlash/ContractV1/SwordSlashFlowBuiltInContractV1.shader");
        if(shader==null || !shader.isSupported)throw new Exception("ContractV1 unavailable");
        var material=new Material(shader);material.name="JadeWindFormedArc";
        material.SetColor("_BaseColor",new Color(.13f,.96f,.74f,1));
        material.SetColor("_EmissionColor",new Color(.76f,1,.94f,1));
        material.SetFloat("_Emission",3.2f);material.SetFloat("_BodyStrength",1f);
        material.SetFloat("_HighlightBandCenter",.84f);material.SetFloat("_HighlightBandWidth",.27f);
        material.SetFloat("_HighlightBandSoftness",.13f);material.SetFloat("_HighlightStrength",1);
        material.SetFloat("_FlowStrength",0);material.SetFloat("_DistortStrength",0);
        material.SetFloat("_SrcBlend",5);material.SetFloat("_DstBlend",1);material.SetFloat("_Cull",0);material.SetFloat("_ZWrite",0);
        string brushPath="Assets/Reference/wind-local-brush-v1.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(brushPath);importer.textureType=TextureImporterType.Default;
        importer.sRGBTexture=false;importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=false;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        material.SetTexture("_MaskTex",AssetDatabase.LoadAssetAtPath<Texture2D>(brushPath));
        material=Save(material,Folder+"/JadeWind.mat");
        var particleMat=new Material(Shader.Find("Particles/Standard Unlit"));particleMat.name="WindSilkAlpha";
        particleMat.SetFloat("_Mode",2);particleMat.SetFloat("_ColorMode",0);
        particleMat.SetInt("_SrcBlend",5);particleMat.SetInt("_DstBlend",1);particleMat.SetInt("_ZWrite",0);
        particleMat.SetInt("_Cull",0); // Actor mirroring reverses mesh winding.
        particleMat.EnableKeyword("_ALPHABLEND_ON");particleMat.renderQueue=3000;
        particleMat.SetColor("_Color",Color.white);
        particleMat=Save(particleMat,Folder+"/WindSilk.mat");
        Mesh silk=Save(Ribbon("Silk",SlazeyaWindKind.Pierce,true),Folder+"/Silk.asset");
        foreach(SlazeyaWindKind kind in Enum.GetValues(typeof(SlazeyaWindKind)))
        {
            var root=new GameObject("SlazeyaWind"+kind);
            GameObject space=root;
            if(kind==SlazeyaWindKind.Projectile)
            {
                // World-space projectile: authored for the S=3 gameplay map,
                // independently of the actor hierarchy and its mirror.
                space=new GameObject("WorldVisualSpace");space.transform.SetParent(root.transform,false);
                space.transform.localScale=Vector3.one*3f;
            }
            Mesh mesh=Save(Ribbon(kind.ToString(),kind,false),Folder+"/"+kind+".asset");
            var primary=Add(space,"Main",mesh,material,.52f,0);
            if(kind==SlazeyaWindKind.Slash)primary.transform.localPosition=new Vector3(.12f,.12f,0);
            if(kind==SlazeyaWindKind.Guard)primary.transform.localPosition=new Vector3(.48f,0,0);
            if(kind==SlazeyaWindKind.Slash || kind==SlazeyaWindKind.Pierce || kind==SlazeyaWindKind.Projectile)
            {
                var wakeMesh=Save(Ribbon(kind+"Wake",kind,true),Folder+"/"+kind+"Wake.asset");
                var wake=Add(space,"Wake",wakeMesh,material,.36f,-1);
                wake.transform.localPosition=new Vector3(0,-.12f,.01f);
            }
            Add(space,"Silk",silk,particleMat,.35f,2);
            Add(space,"Fragments",silk,particleMat,.25f,1);
            if(kind==SlazeyaWindKind.Projectile)
            {
                Add(space,"Arrival",silk,particleMat,.25f,3);
                var contact=Save(Ribbon("Contact",SlazeyaWindKind.Hit,false),Folder+"/Contact.asset");
                Add(space,"Contact",contact,material,.18f,4);
            }
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath(kind));UnityEngine.Object.DestroyImmediate(root);
        }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
    }
    private static GameObject Add(GameObject root,string name,Mesh mesh,Material material,float life,int sort)
    {
        var go=new GameObject(name);go.transform.SetParent(root.transform,false);
        var ps=go.AddComponent<ParticleSystem>();var main=ps.main;
        main.playOnAwake=false;main.loop=false;main.duration=.6f;main.startLifetime=life;
        main.startSpeed=0;main.startSize=1;main.maxParticles=32;main.simulationSpace=ParticleSystemSimulationSpace.Local;
        main.scalingMode=ParticleSystemScalingMode.Hierarchy;
        var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
        var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;
        renderer.mesh=mesh;renderer.sharedMaterial=material;renderer.sortingOrder=sort;
        renderer.alignment=ParticleSystemRenderSpace.Local;renderer.enableGPUInstancing=false;
        renderer.sortingLayerName="Particle";
        renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Normal,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV});
        if(name=="Silk" || name=="Fragments" || name=="Arrival")
        {
            var color=ps.colorOverLifetime;color.enabled=true;
            var grad=new Gradient();grad.SetKeys(new[]{new GradientColorKey(new Color(.86f,1f,.96f),0),new GradientColorKey(new Color(.30f,.90f,.72f),1)},new[]{new GradientAlphaKey(1f,0),new GradientAlphaKey(.85f,.35f),new GradientAlphaKey(0,1)});color.color=grad;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,1),new Keyframe(1,0)));
        }
        ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);return go;
    }
    // Purpose-built open mesh. UV.x follows the cutting gesture; UV.y runs inner -> outer.
    // Nonperiodic inlets are actual geometry, not an image of a complete slash.
    private static Mesh Ribbon(string name,SlazeyaWindKind kind,bool fine)
    {
        const int segments=40,rows=6;var verts=new Vector3[(segments+1)*rows];var uv=new Vector2[verts.Length];var colors=new Color[verts.Length];
        var indices=new List<int>();
        for(int i=0;i<=segments;i++)
        {
            float t=i/(float)segments;Vector2 center,normal;float width;
            if(name=="Contact")
            {
                center=new Vector2(.12f-.90f*t,.12f*Mathf.Sin(t*Mathf.PI));normal=Vector2.up;
                width=.60f*Mathf.Pow(Mathf.Sin(t*Mathf.PI),.75f);
            }
            else if(fine && kind==SlazeyaWindKind.Slash)
            {
                float a=(125+82*t)*Mathf.Deg2Rad;
                center=new Vector2(-.1f+1.65f*Mathf.Cos(a),.45f+.68f*Mathf.Sin(a));
                normal=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                width=.24f*Mathf.Pow(Mathf.Sin(t*Mathf.PI),.7f);
            }
            else if(fine && name.EndsWith("Wake",StringComparison.Ordinal))
            {
                // A short returning current, not another copy of the main crescent.
                bool projectile=kind==SlazeyaWindKind.Projectile;
                center=new Vector2(projectile?.1f+1.40f*t:.20f-1.45f*t,
                    (projectile?.12f:.25f)*Mathf.Sin(t*Mathf.PI));
                normal=Vector2.up;
                width=(projectile?.16f:.20f)*Mathf.Sin(t*Mathf.PI)*(1-.45f*t);
            }
            else if(kind==SlazeyaWindKind.Slash || kind==SlazeyaWindKind.Guard || kind==SlazeyaWindKind.Projectile)
            {
                bool guard=kind==SlazeyaWindKind.Guard,projectile=kind==SlazeyaWindKind.Projectile;
                float a=(guard?107+146*t:projectile?62+142*t:52+163*t)*Mathf.Deg2Rad;
                float rx=guard?.62f:projectile?.90f:1.50f,ry=guard?1.65f:projectile?1.08f:1.78f;
                center=new Vector2((guard?-.48f:projectile?-.27f:-.43f)+rx*Mathf.Cos(a),ry*Mathf.Sin(a));
                normal=new Vector2(Mathf.Cos(a),Mathf.Sin(a)).normalized;
                width=(guard?.64f:projectile?.70f:1.02f)*Mathf.Pow(Mathf.Sin(Mathf.PI*t),.72f)*(.34f+1.12f*t);
                if(guard)width=.64f*Mathf.Pow(Mathf.Sin(Mathf.PI*t),.65f);
            }
            else if(kind==SlazeyaWindKind.Hit)
            {
                center=new Vector2(-.2f-2.60f*t,.25f-.65f*t+.18f*Mathf.Sin(t*Mathf.PI));normal=new Vector2(-.2f,1).normalized;
                width=1.08f*Mathf.Pow(Mathf.Sin(Mathf.PI*t),.64f)*(.35f+.9f*t);
            }
            else
            {
                center=new Vector2(.45f-3.7f*t,.08f*Mathf.Sin(t*Mathf.PI));normal=Vector2.up;
                width=(fine?.40f:.60f)*Mathf.Pow(Mathf.Sin(Mathf.PI*t),1.4f)*(1.25f-t*.72f);
            }
            if(fine)width*=.65f;
            // Four asymmetrical swept inlets cut deeply into the inner flank; the outer blade remains continuous.
            float cut=0;
            if(!fine && kind!=SlazeyaWindKind.Guard)
            {
                cut=Mathf.Max(Inlet(t,.215f,.055f,.12f),Inlet(t,.428f,.078f,.17f));
                cut=Mathf.Max(cut,Mathf.Max(Inlet(t,.655f,.047f,.10f),Inlet(t,.795f,.036f,.09f)));
            }
            float rag=.055f*Mathf.Sin(t*49)+.035f*Mathf.Sin(t*101+1.2f);
            for(int j=0;j<rows;j++)
            {
                float v=j==0?0:j==1?.35f:j==2?.70f:j==3?.86f:j==4?1:1.16f;
                float inner=Mathf.Max(cut,Mathf.Clamp01(rag));
                float across=Mathf.LerpUnclamped(inner,1,v);
                Vector2 p=center+normal*width*(across-.44f);
                float authorScale=kind==SlazeyaWindKind.Guard?1.15f:kind==SlazeyaWindKind.Projectile?1:.74f;
                int index=i*rows+j;verts[index]=new Vector3(p.x*authorScale,p.y*authorScale,0);uv[index]=new Vector2(t,v);
                colors[index]=new Color(1,1,1,j==0||j==5?0:j==1?.8f:j==4?.32f:1);
            }
            if(i>0)for(int j=0;j<rows-1;j++)
            {
                int a=(i-1)*rows+j,b=i*rows+j;indices.Add(a);indices.Add(b);indices.Add(a+1);indices.Add(a+1);indices.Add(b);indices.Add(b+1);
            }
        }
        var packedColors=new Color32[colors.Length];for(int i=0;i<colors.Length;i++)packedColors[i]=colors[i];
        // Unity 2019 Shuriken mesh expansion reads COLOR as UNorm8. Float32 colors
        // are misinterpreted as packed bytes (confirmed by native BakeMesh readback).
        var mesh=new Mesh();mesh.name="Slazeya_"+name;mesh.vertices=verts;mesh.uv=uv;mesh.colors32=packedColors;mesh.triangles=indices.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    private static float Inlet(float t,float at,float span,float depth)
    {float u=(t-at)/span;return u<0||u>1?0:depth*Mathf.Pow(Mathf.Sin(u*Mathf.PI),.55f);}

    private static T Save<T>(T candidate,string path) where T:UnityEngine.Object
    {
        var existing=AssetDatabase.LoadAssetAtPath<T>(path);
        if(existing==null){AssetDatabase.CreateAsset(candidate,path);return candidate;}
        EditorUtility.CopySerialized(candidate,existing);EditorUtility.SetDirty(existing);
        UnityEngine.Object.DestroyImmediate(candidate);return existing;
    }
}
