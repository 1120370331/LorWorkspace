using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ChristashaFrameLoopTest
{
    const string Pending = "Christasha.FrameLoop.Pending";
    static ChristashaFrameLoopTest() { EditorApplication.playModeStateChanged += Changed; }
    static string Arg(string key)
    {
        var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, key);
        if (i < 0 || i + 1 == args.Length) throw new ArgumentException(key);
        return args[i + 1];
    }
    public static void Run()
    {
        ChristashaStrikeShaderBuilder.Build();
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-modelOnly")>=0)
        {
            ChristashaStrikeModelStudy.Render(Path.Combine(Arg("-candidateOutput"),"model-study"));
            EditorApplication.Exit(0);return;
        }
        SessionState.SetBool(Pending, true);
        SessionState.SetString(Pending + "Output", Arg("-candidateOutput"));
        SessionState.SetString(Pending + "Bundle", Arg("-referenceBundle"));
        SessionState.SetBool(Pending + "Stable", Array.IndexOf(Environment.GetCommandLineArgs(), "-requireStable") >= 0);
        EditorApplication.isPlaying = true;
    }
    static void Changed(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        var probe = new GameObject("FrameLoopProbe").AddComponent<ChristashaFrameLoopProbe>();
        probe.Output = SessionState.GetString(Pending + "Output", "");
        probe.BundlePath = SessionState.GetString(Pending + "Bundle", "");
        probe.BrushPath = Path.GetFullPath("Assets/NativeRuntime/tide-brush-imagegen-v1.png");
        probe.SkinPath = Arg("-characterSkin");
        probe.RequireStable = SessionState.GetBool(Pending + "Stable", false);
        probe.Finished = success => { Debug.Log("CHRISTASHA_FRAME_LOOP_" + (success ? "PASS" : "FAIL")); EditorApplication.Exit(success ? 0 : 1); };
    }
}
