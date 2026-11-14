using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Steria.Vfx;

public static class SlazeyaWindAuthoring
{
    [MenuItem("Steria/Wind/Open authoring stage")]
    public static void Open()
    {
        SlazeyaWindSweptBuilder.Build();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        SlazeyaWindPreview.Actor(4,false,4.5f);
        PersistActorSprites();
        var camera=SlazeyaWindPreview.MakeCamera(false);camera.gameObject.name="Main Camera";
        camera.tag="MainCamera";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(SlazeyaWindParticleBuilder.PrefabPath(SlazeyaWindKind.Slash));
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name="Wind Slash - Live Authoring";
        instance.transform.position=new Vector3(-2.1f,7.02f,0);instance.transform.localScale=Vector3.one*6;
        var playback=instance.AddComponent<SlazeyaWindAuthoringPlayer>();
        playback.BladeMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/SlazeyaWind/WindSweptBlade.mat");
        Selection.activeGameObject=instance;
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/WindAuthoring.unity");
        var game=System.Type.GetType("UnityEditor.GameView,UnityEditor");
        if(game!=null)EditorWindow.GetWindow(game).Focus();
        Debug.Log("SLAZEYA_AUTHORING_READY Assets/WindAuthoring.unity");
    }

    static void PersistActorSprites()
    {
        // Game art stays in the ignored local reference folder. Saving these
        // references makes the authoring scene survive an Editor restart.
        const string folder="Assets/Reference/AuthoringSpritesV2";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Reference","AuthoringSpritesV2");
        foreach(var renderer in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())
        {
            if(renderer.sprite==null)continue;
            string path=folder+"/"+renderer.gameObject.name.Replace(".png","")+".asset";
            var persistent=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(persistent==null)
            {
                var original=renderer.sprite;
                string texturePath="Assets/Reference/"+renderer.gameObject.name;
                var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
                if(importer!=null)
                {
                    importer.npotScale=TextureImporterNPOTScale.None;
                    importer.maxTextureSize=4096;importer.isReadable=true;
                    importer.textureCompression=TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if(texture==null)throw new System.InvalidOperationException("Missing local actor texture "+renderer.gameObject.name);
                persistent=Sprite.Create(texture,original.rect,new Vector2(original.pivot.x/original.rect.width,original.pivot.y/original.rect.height),original.pixelsPerUnit);
                AssetDatabase.CreateAsset(persistent,path);
            }
            renderer.sprite=persistent;
        }
        AssetDatabase.SaveAssets();
    }
}
