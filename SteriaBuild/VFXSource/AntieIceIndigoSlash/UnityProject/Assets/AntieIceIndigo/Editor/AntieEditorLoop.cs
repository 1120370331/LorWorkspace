using UnityEditor;
using UnityEngine;
using Steria.AntieVfx;

[InitializeOnLoad]
public static class AntieEditorLoop
{
    static AntieEditorLoop(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(Application.isBatchMode||Application.isPlaying||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        foreach(var p in Object.FindObjectsOfType<AntieSlash>())if(p.autoPlay){p.Tick();SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();}
    }
}
