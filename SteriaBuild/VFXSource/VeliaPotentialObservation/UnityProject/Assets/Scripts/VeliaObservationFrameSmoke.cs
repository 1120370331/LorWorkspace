using System;
using UnityEngine;

// Preview-only real player-loop check; never included in the prefab bundle or mod DLL.
public sealed class VeliaObservationFrameSmoke : MonoBehaviour
{
    private VeliaPotentialObservationVisual _visual;
    private Action<bool,int,float> _finished;
    private Action _capturePeak;
    private int _frames;
    private float _elapsed;
    private bool _seenGeometry;
    public void Initialize(GameObject particlePrefab,Material material,Action<bool,int,float> finished,Action capturePeak)
    {
        _finished=finished;_capturePeak=capturePeak;_visual=new VeliaPotentialObservationVisual(particlePrefab,material,0,0,0);
        _visual.SetAnchors(new Vector3(-3,5,0),Vector3.zero,4.8f,false);
        _visual.Advance(0);
    }
    private void Update()
    {
        if(_visual==null)return;
        _frames++;_elapsed+=Time.deltaTime;_visual.Advance(Time.deltaTime);
        _seenGeometry|=_visual.LiveParticleCount>0;
        if(_visual.Age>=.55f && _capturePeak!=null) { var capture=_capturePeak;_capturePeak=null;capture(); }
        if(!_visual.Complete)return;
        bool ok=_seenGeometry && _frames>1 && _visual.LiveParticleCount==0 && _visual.HandVertexCount==0 && _elapsed>=VeliaPotentialObservationVisual.Duration;
        float age=_visual.Age;_visual.Dispose();_visual=null;
        _finished(ok,_frames,age);
    }
    private void OnDestroy() {if(_visual!=null)_visual.Dispose();}
}
