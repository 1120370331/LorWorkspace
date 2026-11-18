using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LOR_DiceSystem;
using UnityEngine;

/// <summary>Scoped per-caster feedback; never selects recipients or settles buffs.</summary>
public sealed class VeliaPotentialObservationEffect : MonoBehaviour
{
    [Serializable] private sealed class Anchors { public float[] rightPalm; }
    private static AssetBundle _bundle;
    private static Material _material;
    private static GameObject _particlePrefab;
    private static Vector2 _palmPixel;
    private static readonly Dictionary<Sprite, Rect> BoundsCache = new Dictionary<Sprite, Rect>();
    private BattleUnitModel _owner, _target;
    private CharacterAppearance _appearance;
    private SpriteRenderer _casterRenderer, _targetRenderer;
    private VeliaPotentialObservationVisual _visual;
    private Coroutine _pending;
    private int _generation, _round, _wave;
    private bool _ownsPose;
    private ActionDetail _priorPose;

    public static void Play(BattleUnitModel caster, BattleUnitModel target)
    { Request(caster, target, false); }

    public static void PlayAfterSpeedDice(BattleUnitModel caster, BattleUnitModel target)
    { Request(caster, target, true); }

    public static void Cancel(BattleUnitModel caster)
    {
        if(caster == null || caster.view == null) return;
        var host=caster.view.GetComponent<VeliaPotentialObservationEffect>();
        if(host!=null) host.Clear();
    }

    private static void Request(BattleUnitModel caster, BattleUnitModel target, bool afterRoll)
    {
        if(!Valid(caster) || !Valid(target)) return;
        var host=caster.view.GetComponent<VeliaPotentialObservationEffect>();
        if(host==null) host=caster.view.gameObject.AddComponent<VeliaPotentialObservationEffect>();
        host.Clear(); host._owner=caster; host._target=target;
        host._round=Singleton<StageController>.Instance.RoundTurn;
        host._wave=Singleton<StageController>.Instance.CurrentWave;
        if(afterRoll) host._pending=host.StartCoroutine(host.AfterDice(host._generation));
        else host.Begin();
    }

    private IEnumerator AfterDice(int generation)
    {
        // ChangeSprite stops roulette; its next Update starts a .2s jitter tail.
        yield return null;
        yield return new WaitForSeconds(0.22f);
        _pending=null;
        if(generation==_generation && CurrentRequest()) Begin();
    }

    private static bool Valid(BattleUnitModel unit)
    { return unit!=null && !unit.IsDead() && unit.view!=null && unit.view.gameObject.activeInHierarchy && unit.view.charAppearance!=null; }

    private bool CurrentRequest()
    {
        return Valid(_owner) && Valid(_target) && Singleton<StageController>.Instance.RoundTurn==_round
            && Singleton<StageController>.Instance.CurrentWave==_wave;
    }

    private static string ModRoot()
    {
        string assembly=Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
        return Directory.GetParent(assembly).FullName;
    }

    private static bool LoadAssets()
    {
        if(_material!=null && _particlePrefab!=null)return true;
        string root=ModRoot();
        string bundlePath=Path.Combine(root,"Assemblies","AB",VeliaPotentialObservationVisual.BundleName);
        string anchorsPath=Path.Combine(root,"Resource","Effects","VeliaPotentialObservation","anchors.json");
        if(!File.Exists(bundlePath) || !File.Exists(anchorsPath)) return false;
        var anchors=JsonUtility.FromJson<Anchors>(File.ReadAllText(anchorsPath));
        if(anchors==null || anchors.rightPalm==null || anchors.rightPalm.Length!=2) return false;
        _palmPixel=new Vector2(anchors.rightPalm[0],anchors.rightPalm[1]);
        if(_bundle==null)_bundle=AssetBundle.LoadFromFile(bundlePath);
        if(_bundle==null)return false;
        _material=_bundle.LoadAsset<Material>(VeliaPotentialObservationVisual.MaterialName);
        _particlePrefab=_bundle.LoadAsset<GameObject>(VeliaPotentialObservationVisual.PrefabName);
        return _particlePrefab!=null && _material!=null && _material.shader!=null && _material.shader.isSupported;
    }

    private static SpriteRenderer Body(CharacterMotion motion)
    {
        if(motion==null)return null;
        var direct=motion.transform.Find("Customize_Renderer");
        var renderer=direct==null?null:direct.GetComponent<SpriteRenderer>();
        if(renderer!=null && renderer.sprite!=null)return renderer;
        if(motion.motionSpriteSet==null)return null;
        foreach(var set in motion.motionSpriteSet)
            if(set.sprType==CharacterAppearanceType.Body && set.sprRenderer!=null && set.sprRenderer.sprite!=null)
                return set.sprRenderer;
        return null;
    }

    private void Begin()
    {
        try
        {
            if(!CurrentRequest() || !LoadAssets()) { Clear(); return; }
            _appearance=_owner.view.charAppearance;
            CharacterMotion pose=_appearance.GetCharacterMotion(ActionDetail.S2);
            _casterRenderer=Body(pose);
            _targetRenderer=Body(_target.view.charAppearance.GetCharacterMotion(ActionDetail.Standing));
            if(_targetRenderer==null) _targetRenderer=Body(_target.view.charAppearance.GetCurrentMotion());
            if(_casterRenderer==null || _targetRenderer==null || _casterRenderer.sprite.rect.width!=512) { Clear(); return; }
            _priorPose=_appearance.GetCurrentMotionDetail();
            _appearance.ChangeMotion(ActionDetail.S2); _ownsPose=true;
            _visual=new VeliaPotentialObservationVisual(_particlePrefab,_material,_targetRenderer.gameObject.layer,
                _targetRenderer.sortingLayerID,_targetRenderer.sortingOrder);
            UpdateAnchors(); _visual.Advance(0);
        }
        catch(Exception ex) { Debug.LogWarning("[Steria] Potential Observation visual: "+ex); Clear(); }
    }

    private static Rect Content(Sprite sprite)
    {
        Rect result;
        if(BoundsCache.TryGetValue(sprite,out result)) return result;
        try
        {
            int w=(int)sprite.rect.width,h=(int)sprite.rect.height;
            Color32[] pixels=sprite.texture.GetPixels32(); int tw=sprite.texture.width;
            int ox=(int)sprite.rect.x,oy=(int)sprite.rect.y,minX=w,minY=h,maxX=0,maxY=0;
            for(int y=0;y<h;y++) for(int x=0;x<w;x++) if(pixels[(oy+y)*tw+ox+x].a>32)
            { minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x+1);maxY=Math.Max(maxY,y+1); }
            result=maxY>minY ? new Rect(minX,minY,maxX-minX,maxY-minY) : new Rect(w*.3f,h*.23f,w*.4f,h*.48f);
        }
        catch { result=new Rect(sprite.rect.width*.3f,sprite.rect.height*.23f,sprite.rect.width*.4f,sprite.rect.height*.48f); }
        BoundsCache[sprite]=result; return result;
    }

    private void UpdateAnchors()
    {
        var sprite=_targetRenderer.sprite; Rect content=Content(sprite);
        // Weapons can widen alpha bounds asymmetrically: use the standing body
        // origin for X, and alpha only to measure vertical extent/sole.
        Vector3 feet=VeliaPotentialObservationVisual.SpritePoint(_targetRenderer,new Vector2(sprite.pivot.x,sprite.rect.height-content.yMin));
        Vector3 crown=VeliaPotentialObservationVisual.SpritePoint(_targetRenderer,new Vector2(sprite.pivot.x,sprite.rect.height-content.yMax));
        float height=Vector3.Distance(feet,crown);
        // Librarian clothing has its own head: the same standing body-to-height ratio as the registered model.
        if(_targetRenderer.sprite.rect.width==512 && content.height<200) height*=1.5f;
        Vector3 palm=VeliaPotentialObservationVisual.SpritePoint(_casterRenderer,_palmPixel);
        _visual.SetAnchors(palm,feet,height,_casterRenderer.transform.lossyScale.x<0 ^ _casterRenderer.flipX);
    }

    private void Update()
    {
        if(_visual==null)return;
        if(!CurrentRequest() || _casterRenderer==null || _targetRenderer==null) { Clear(); return; }
        try
        {
            if(_ownsPose && _appearance.GetCurrentMotionDetail()!=ActionDetail.S2)
            { _ownsPose=false; _visual.HideHand(); }
            UpdateAnchors(); _visual.Advance(Time.deltaTime);
            if(_visual.Age>=VeliaPotentialObservationVisual.PoseDuration) ReleasePose();
            if(_visual.Complete) Clear();
        }
        catch(Exception ex) { Debug.LogWarning("[Steria] Potential Observation update: "+ex.Message); Clear(); }
    }

    private void ReleasePose()
    {
        if(_ownsPose && _appearance!=null && _appearance.GetCurrentMotionDetail()==ActionDetail.S2)
            _appearance.ChangeMotion(_priorPose==ActionDetail.S2 ? ActionDetail.Standing : _priorPose);
        _ownsPose=false;
    }

    private void Clear()
    {
        _generation++;
        if(_pending!=null) { StopCoroutine(_pending); _pending=null; }
        ReleasePose();
        if(_visual!=null) { _visual.Dispose(); _visual=null; }
        _owner=null;_target=null;_appearance=null;_casterRenderer=null;_targetRenderer=null;
    }
    private void OnDisable() { Clear(); }
    private void OnDestroy() { Clear(); }
}
