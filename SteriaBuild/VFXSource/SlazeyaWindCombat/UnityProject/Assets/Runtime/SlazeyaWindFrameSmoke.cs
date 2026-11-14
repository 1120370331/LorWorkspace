using System;
using UnityEngine;
using Steria.Vfx;
public sealed class SlazeyaWindFrameSmoke:MonoBehaviour
{
    SlazeyaWindVisual visual;Action<bool> done;int frames;bool seen;
    public void Initialize(GameObject prefab,Action<bool> callback){visual=new SlazeyaWindVisual(Instantiate(prefab),SlazeyaWindKind.Slash,19037u);done=callback;}
    void Update(){if(visual==null)return;frames++;visual.Advance(Time.deltaTime);seen|=visual.LiveParticleCount>0;if(!visual.IsComplete)return;Debug.Log("[CASE] RealUpdate PASS frames="+frames+" elapsed="+visual.Elapsed);done(seen&&frames>1&&visual.LiveParticleCount==0);visual=null;}
    void OnDestroy(){if(visual!=null)visual.StopAndClear();}
}
