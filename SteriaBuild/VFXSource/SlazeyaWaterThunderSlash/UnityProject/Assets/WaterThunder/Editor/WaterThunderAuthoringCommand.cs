using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Steria.WaterThunder;

/// <summary>Explicit local file command for the already-open 2019 Editor, never a network listener.</summary>
[InitializeOnLoad]
public static class WaterThunderAuthoringCommand
{
    [Serializable]class Request{public string revision;}
    static string CommandPath {get{return Path.GetFullPath(Path.Combine(Application.dataPath,"../../authoring-command.json"));}}
    static WaterThunderAuthoringCommand(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(CommandPath))return;
        string request=File.ReadAllText(CommandPath);File.Delete(CommandPath);
        string result=Path.Combine(Path.GetDirectoryName(CommandPath),"authoring-result.json");
        try
        {
            var r=JsonUtility.FromJson<Request>(request);
            if(r==null||String.IsNullOrEmpty(r.revision)||System.Text.RegularExpressions.Regex.IsMatch(r.revision,"[^A-Za-z0-9_-]"))throw new Exception("Invalid revision");
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            bool refresh=scene.path==WaterThunderBuilder.Root+"/Scenes/WaterThunderAuthoring.unity"||UnityEngine.Object.FindObjectOfType<WaterThunderSlash>()!=null;
            string backup="";
            if(refresh)
            {
                string folder=WaterThunderBuilder.Root+"/Scenes/Backups";Directory.CreateDirectory(folder);
                backup=folder+"/Before-"+r.revision+"-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".unity";
                if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Could not back up current authoring scene");
            }
            WaterThunderBuilder.Build();
            if(refresh)
            {
                var player=WaterThunderBuilder.CreateStage(true);player.autoPlay=true;player.sampleTime=.215f;
                EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),WaterThunderBuilder.Root+"/Scenes/WaterThunderAuthoring.unity");
                Selection.activeGameObject=player.gameObject;
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText(result,"{\"status\":\"pass\",\"revision\":\""+r.revision+"\",\"sceneRefreshed\":"+(refresh?"true":"false")+",\"backup\":\""+backup+"\"}");
            Debug.Log("WATER_THUNDER_AUTHORING_UPDATED "+r.revision);
        }
        catch(Exception ex){Debug.LogException(ex);File.WriteAllText(result,"{\"status\":\"fail\",\"error\":\""+ex.Message.Replace("\\","/").Replace("\"","'")+"\"}");}
    }
}
