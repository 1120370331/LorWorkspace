using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Steria.Vfx;
using Object=UnityEngine.Object;

// Geometry-only acceptance: the exact production meshes, opaque neutral clay.
public static class ChristashaStrikeModelStudy
{
    public static void Render(string output)
    {
        Directory.CreateDirectory(output);
        var brush=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
        brush.LoadImage(File.ReadAllBytes("Assets/NativeRuntime/tide-brush-imagegen-v1.png"));
        var strikeShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/NativeRuntime/PlatinumStrike.shader");
        var clay=new Material(Shader.Find("Standard"));clay.color=new Color(.73f,.75f,.77f);
        clay.SetFloat("_Glossiness",.27f);clay.SetFloat("_Metallic",.12f);
        var lightRoot=new GameObject("ClayKey");var light=lightRoot.AddComponent<Light>();
        light.type=LightType.Directional;light.intensity=1.1f;lightRoot.transform.rotation=Quaternion.Euler(30,-30,0);
        var oldAmbient=RenderSettings.ambientLight;RenderSettings.ambientLight=new Color(.35f,.35f,.35f);
        var cameraRoot=new GameObject("ClayCamera");var camera=cameraRoot.AddComponent<Camera>();camera.enabled=false;
        camera.orthographic=true;camera.orthographicSize=.82f;camera.aspect=2.4f;camera.transform.position=new Vector3(1.72f,0,-10);
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.19f,.21f);camera.cullingMask=1<<30;
        var target=new RenderTexture(960,400,24);target.Create();camera.targetTexture=target;
        var image=new Texture2D(960,400,TextureFormat.RGB24,false);
        using(var library=new ChristashaNativePierceRecipes(p=>null,brush,strikeShader))
        foreach(string recipe in new[]{"Pierce","Projectile"})
        {
            var instance=library.CreatePrototype(recipe);instance.SetActive(true);
            foreach(Transform t in instance.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
            foreach(var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.enabled=renderer.name!="ImpactFlash";renderer.sharedMaterial=clay;renderer.SetPropertyBlock(null);
            }
            foreach(int angle in new[]{0,35,75})
            {
                instance.transform.rotation=Quaternion.Euler(angle,0,0);
                camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,960,400),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(output,recipe+"-"+angle+".png"),image.EncodeToPNG());
            }
            Object.DestroyImmediate(instance);
        }
        RenderTexture.active=null;camera.targetTexture=null;RenderSettings.ambientLight=oldAmbient;
        Object.DestroyImmediate(cameraRoot);Object.DestroyImmediate(lightRoot);Object.DestroyImmediate(clay);Object.DestroyImmediate(target);Object.DestroyImmediate(image);Object.DestroyImmediate(brush);
        File.WriteAllText(Path.Combine(output,"readme.txt"),"Exact production meshes, opaque Standard clay, no glow/no particles/no trail. Sword blade 2.55 units, max width .20, guard span .58. Three axial views: 0/35/75 degrees. Geometry preview, not final VFX acceptance.");
    }
}
