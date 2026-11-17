using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using Steria.AntieVfx;
using Object=UnityEngine.Object;

public static class AntieBuilder
{
    public const string Root="Assets/AntieIceIndigo";
    public static readonly string[] Names={"HitVertical","SlashHorizontal","PierceDual"};
    static Mesh Ribbon(AntieAttackKind kind,int blade)
    {
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();const int segments=192,rows=12;
        for(int i=0;i<=segments;i++)
        {
            float t=i/(float)segments;var center=AntieSlash.Curve(kind,blade,t);var normal=AntieSlash.Normal(kind,blade,t);float width=AntieSlash.RibbonWidth(kind,t);
            for(int j=0;j<=rows;j++)
            {
                float v=j/(float)rows;vertices.Add(center+normal*(v*width));uv.Add(new Vector2(t,v));
                if(i<segments&&j<rows){int a=i*(rows+1)+j,b=a+rows+1;triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            }
        }
        for(int i=0;i<triangles.Count;i+=3)if(Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]).z>0){int k=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=k;}
        var mesh=new Mesh{name=Names[(int)kind]+"_"+blade};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    static void Store(Mesh mesh,string name)
    {
        string path=Root+"/Meshes/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);}
    }
    static Material BladeMaterial()
    {
        string path=Root+"/Materials/AntieIceIndigo.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);var shader=Shader.Find("Steria/Antie/IceIndigoSweptSlash");
        if(shader==null||!shader.isSupported)throw new Exception("Antie slash shader unsupported");
        if(m==null){m=new Material(shader){name="AntieIceIndigo"};AssetDatabase.CreateAsset(m,path);}else m.shader=shader;
        m.SetTexture("_ColorTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/User_SlashColor.png"));
        m.SetTexture("_ShapeMask",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/User_SlashMask.png"));
        m.SetTexture("_FlowTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/User_Flow.png"));
        m.SetVector("_TextureRect",new Vector4(.016f,.326f,.904f,.430f));m.SetFloat("_Emission",1.35f);m.SetFloat("_FlowStrength",.32f);m.SetColor("_Tint",Color.white);
        EditorUtility.SetDirty(m);return m;
    }
    static Material GlowMaterial()
    {
        string path=Root+"/Materials/AntieSoftGlow.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);var shader=Shader.Find("Steria/Antie/SoftRadialGlow");
        if(shader==null||!shader.isSupported)throw new Exception("Antie radial shader unsupported");
        if(m==null){m=new Material(shader){name="AntieSoftGlow"};AssetDatabase.CreateAsset(m,path);}else m.shader=shader;
        m.SetTexture("_MaskTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/User_RadialMask.png"));m.SetColor("_Color",new Color(.18f,.72f,1,1));EditorUtility.SetDirty(m);return m;
    }
    static MeshRenderer Part(GameObject parent,string name,Mesh mesh,Material material,int order)
    {
        var go=new GameObject(name);go.transform.SetParent(parent.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.sortingOrder=order;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;return r;
    }
    static RuntimeAnimatorController Animation(string name)
    {
        string clipPath=Root+"/Animations/"+name+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if(clip==null){clip=new AnimationClip{name=name};AssetDatabase.CreateAsset(clip,clipPath);}
        clip.frameRate=60;clip.wrapMode=WrapMode.Once;clip.SetCurve("",typeof(AntieSlash),"sampleTime",AnimationCurve.Linear(0,0,AntieSlash.Duration,AntieSlash.Duration));
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);
        string path=Root+"/Animations/"+name+".controller";var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(c==null){c=AnimatorController.CreateAnimatorControllerAtPath(path);var state=c.layers[0].stateMachine.AddState(name);state.motion=clip;c.layers[0].stateMachine.defaultState=state;}
        else c.layers[0].stateMachine.defaultState.motion=clip;
        EditorUtility.SetDirty(c);return c;
    }
    public static void Build()
    {
        PlayerSettings.colorSpace=ColorSpace.Gamma;PlayerSettings.productName="Antie Ice Indigo Combat";PlayerSettings.companyName="Steria";
        foreach(string name in new[]{"Meshes","Materials","Prefabs","Native","Animations","Scenes"})Directory.CreateDirectory(Root+"/"+name);
        AssetDatabase.Refresh();
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{Root+"/Textures"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);bool data=!path.Contains("SlashColor");
            bool dirty=importer.sRGBTexture==data||importer.mipmapEnabled||importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.wrapMode!=TextureWrapMode.Clamp||importer.npotScale!=TextureImporterNPOTScale.None||importer.maxTextureSize!=2048;
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=!data;importer.alphaIsTransparency=false;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
            if(dirty)importer.SaveAndReimport();
        }
        var temp=GameObject.CreatePrimitive(PrimitiveType.Quad);Store(Object.Instantiate(temp.GetComponent<MeshFilter>().sharedMesh),"GlowQuad");Object.DestroyImmediate(temp);
        var bladeMat=BladeMaterial();var glowMat=GlowMaterial();
        for(int k=0;k<3;k++)
        {
            var kind=(AntieAttackKind)k;string name=Names[k];var go=new GameObject("Antie_"+name);
            for(int i=0;i<(kind==AntieAttackKind.PierceDual?2:1);i++)
            {
                string meshName=name+"_"+i;Store(Ribbon(kind,i),meshName);
                Part(go,"Blade_"+i,AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/"+meshName+".asset"),bladeMat,2+i);
                Part(go,"Glow_"+i,AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/GlowQuad.asset"),glowMat,i);
            }
            PrefabUtility.SaveAsPrefabAsset(go,Root+"/Native/"+name+".prefab");
            var player=go.AddComponent<AntieSlash>();player.kind=kind;player.BindNative();player.autoPlay=true;player.animationDriven=true;
            var animator=go.AddComponent<Animator>();animator.runtimeAnimatorController=Animation(name);
            PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/Antie"+name+".prefab");Object.DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
    }
    public static AntieSlash Stage(AntieAttackKind kind,bool context=false)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=2.4f;camera.transform.position=new Vector3(0,1.95f,-15);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.018f,.03f,.052f);camera.allowHDR=false;
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Antie"+Names[(int)kind]+".prefab"));go.transform.position=new Vector3(0,.08f,0);
        var p=go.GetComponent<AntieSlash>();p.autoPlay=false;p.animationDriven=false;go.GetComponent<Animator>().enabled=false;
        if(context)
        {
            var actor=GameObject.CreatePrimitive(PrimitiveType.Capsule);actor.name="Textureless Antie placeholder";actor.transform.position=new Vector3(-.95f,1.50f,.7f);actor.transform.localScale=new Vector3(.58f,1.50f,.58f);actor.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("Unlit/Color")){color=new Color(.13f,.16f,.21f)};
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Ground";floor.transform.position=new Vector3(0,-.035f,.8f);floor.transform.localScale=new Vector3(10,.04f,4);floor.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("Unlit/Color")){color=new Color(.09f,.11f,.14f)};
        }
        return p;
    }
    [MenuItem("Steria/Antie Ice Indigo/Open Hit Vertical")]
    static void OpenHit(){Build();Open(AntieAttackKind.HitVertical);}
    [MenuItem("Steria/Antie Ice Indigo/Open Slash Horizontal")]
    static void OpenSlash(){Build();Open(AntieAttackKind.SlashHorizontal);}
    [MenuItem("Steria/Antie Ice Indigo/Open Pierce Dual")]
    static void OpenDual(){Build();Open(AntieAttackKind.PierceDual);}
    static void Open(AntieAttackKind kind)
    {
        var p=Stage(kind,true);p.autoPlay=true;p.animationDriven=true;p.GetComponent<Animator>().enabled=true;
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Root+"/Scenes/Antie"+Names[(int)kind]+".unity");Selection.activeGameObject=p.gameObject;
    }
}
