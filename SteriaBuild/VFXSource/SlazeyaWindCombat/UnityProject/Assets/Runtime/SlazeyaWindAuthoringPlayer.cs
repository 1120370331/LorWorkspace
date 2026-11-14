using UnityEngine;
using Steria.Vfx;

[ExecuteInEditMode]
public sealed class SlazeyaWindAuthoringPlayer:MonoBehaviour
{
    public SlazeyaWindKind Kind=SlazeyaWindKind.Slash;
    public bool AutoPlay=true;
    [Range(0,.6f)]public float SampleTime=.12f;
    [Range(.1f,1.5f)]public float PlaybackSpeed=.7f;
    public Material BladeMaterial;
    private SlazeyaWindVisual visual;
    private float lastTime,cycle,lastSample=-1;
    void OnEnable(){lastTime=Time.realtimeSinceStartup;}
    void Update()
    {
        if(visual==null)visual=new SlazeyaWindVisual(gameObject,Kind,19037u);
        float now=Time.realtimeSinceStartup;float dt=Mathf.Clamp(now-lastTime,0,.1f);lastTime=now;
        if(AutoPlay)
        {
            cycle+=dt*PlaybackSpeed;
            if(cycle>.95f){visual.ApplyAt(0);cycle=0;}
            visual.Advance(dt*PlaybackSpeed);SampleTime=visual.Elapsed;lastSample=-1;
        }
        else
        {
            // Reapply at the same sample while tuning material assets in the Inspector.
            visual.ApplyAt(SampleTime);lastSample=SampleTime;
        }
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        UnityEditor.SceneView.RepaintAll();
        #endif
    }
    void OnDisable(){if(visual!=null)visual.StopAndClear();visual=null;}
}
