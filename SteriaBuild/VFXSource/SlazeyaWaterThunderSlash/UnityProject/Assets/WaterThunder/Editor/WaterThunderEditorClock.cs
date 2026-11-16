using UnityEditor;
using UnityEngine;
using Steria.WaterThunder;

// Keep the authoring scene moving outside Play mode; no custom UI system is required.
[InitializeOnLoad]
public static class WaterThunderEditorClock
{
    static double last;
    static WaterThunderEditorClock(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(Application.isBatchMode||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.timeSinceStartup-last<1.0/60)return;
        last=EditorApplication.timeSinceStartup;
        var players=Object.FindObjectsOfType<WaterThunderSlash>();bool repaint=false;
        foreach(var player in players)
        {
            if(!player.isActiveAndEnabled||!player.gameObject.scene.IsValid()||!player.NeedsRefresh)continue;
            player.TickPreview();repaint=true;
        }
        if(repaint){SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();}
    }
}

public sealed class WaterThunderSaveGuard : UnityEditor.AssetModificationProcessor
{
    static string[] OnWillSaveAssets(string[] paths)
    {
        // Never serialize the DontSave simulation clone into a scene or prefab.
        foreach(var player in Object.FindObjectsOfType<WaterThunderSlash>())player.RestoreSourceForSave();
        return paths;
    }
}
