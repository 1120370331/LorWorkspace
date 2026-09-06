using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

/// <summary>Visual-only FarAreaEach session; the manager remains the sole settlement owner.</summary>
public class FarAreaEffect_Steria_VeliaTideMist : FarAreaEffect
{
    private const float CallbackWatchdog = 8f;
    private static FarAreaEffect_Steria_VeliaTideMist Active;
    private BattlePlayingCardDataInUnitModel _originCard;
    private BattleDiceBehavior _dice;
    private BattleFarAreaPlayManager _originManager;
    private bool _managerOwned, _closed, _callback, _tailHandled;
    private int _ordinal = -1;
    private float _waitAge;
    private Camera _camera;
    private AssetBundle _bundle;
    private VeliaTideMistVisualController _visual;
    private VeliaTideMistScreenFilter _filter;

    public override bool HasIndependentAction { get { return false; } }

    public static FarAreaEffect_Steria_VeliaTideMist GetOrCreate(BattleUnitModel owner)
    {
        var card = owner == null ? null : owner.currentDiceAction;
        if (Active != null && !Active._closed && Active.isActiveAndEnabled && Active._self == owner
            && card != null && ReferenceEquals(Active._originCard, card) && !Active._visual.IsFinishing)
        {
            Active.BeginDice();
            return Active;
        }
        if (Active != null) Active.Complete();
        var root = new GameObject("FarAreaEffect_Steria_VeliaTideMist");
        var effect = root.AddComponent<FarAreaEffect_Steria_VeliaTideMist>();
        effect.Init(owner);
        return effect;
    }

    public override void Init(BattleUnitModel self, params object[] args)
    {
        base.Init(self, args);
        if (Active != null && !ReferenceEquals(Active, this)) Active.Complete();
        Active = this;
        _originCard = self == null ? null : self.currentDiceAction;
        _originManager = Singleton<BattleFarAreaPlayManager>.Instance;
        _managerOwned = _originManager != null && _originManager.attacker == self;
        _visual = new VeliaTideMistVisualController();
        BeginDice();
        try
        {
            var cameras = SingletonBehavior<BattleCamManager>.Instance;
            _camera = cameras == null ? null : cameras.EffectCam;
            if (_camera != null)
            {
                Material template = LoadMaterial();
                if (template != null)
                {
                    _filter = _camera.gameObject.AddComponent<VeliaTideMistScreenFilter>();
                    _filter.Initialize(_visual, template);
                }
                RefreshProtection();
            }
        }
        catch (Exception ex) { Debug.LogWarning("VeliaTideMist visual unavailable: " + ex.Message); ReleaseRendering(); }
    }

    private void BeginDice()
    {
        var next = _originCard == null ? null : _originCard.currentBehavior;
        if (_ordinal >= 0 && ReferenceEquals(_dice, next) && !_isDoneEffect) return;
        _dice = next;
        _ordinal++;
        _callback = _tailHandled = false;
        _waitAge = 0f;
        _isDoneEffect = false;
        _visual.BeginDice(_ordinal);
        isRunning = !_visual.DiceReady;
    }

    public override void GiveDamageFromManager(List<BattleUnitModel> damagedUnitList)
    {
        if (_closed || _callback || _isDoneEffect || _visual == null) return;
        // Reject callbacks to an old owner/card session. An empty successful list still pulses.
        if (OwnerEnded()) { Complete(); return; }
        _callback = true;
        var points = new List<Vector2>();
        if (_camera != null && damagedUnitList != null)
        {
            var seen = new HashSet<BattleUnitModel>();
            foreach (var unit in damagedUnitList)
            {
                if (unit == null || unit.view == null || !seen.Add(unit)) continue;
                try
                {
                    Bounds bounds;
                    Vector3 world = TryBodyBounds(unit, out bounds) ? bounds.center : FallbackHitPosition(unit);
                    Vector3 viewport = _camera.WorldToViewportPoint(world);
                    if (Finite(viewport) && viewport.z > 0f) points.Add(new Vector2(viewport.x, viewport.y));
                }
                catch (Exception) { /* A removed victim view must not stop the single global pulse. */ }
            }
        }
        _visual.TriggerPulse(points.ToArray());
        // Do not inspect remaining dice here: the manager calls OnEnd immediately after us.
    }

    protected override void Update()
    {
        // IsDoneEffect is a per-die gate, NOT this session's lifetime.
        if (_closed || _visual == null) return;
        if (OwnerEnded()) { Complete(); return; }
        float dt = Time.deltaTime;
        _visual.Advance(dt);
        if (dt > 0f && !float.IsNaN(dt) && !float.IsInfinity(dt)) _waitAge += dt;
        if (_visual.DiceReady) isRunning = false;
        // Includes no callback and a manager that never advances after a completed die.
        if ((!_callback || _tailHandled) && !_visual.IsFinishing && _waitAge > CallbackWatchdog + VeliaTideMistVisualController.IntroDuration)
        {
            Complete();
            return;
        }
        if (_callback && !_tailHandled && _visual.PulseFinished)
        {
            _tailHandled = true;
            _waitAge = 0f;
            // Read in Update after settlement hooks may have cleared or appended the queue.
            bool hasNextDice = _originCard != null && _originCard.cardBehaviorQueue != null && _originCard.cardBehaviorQueue.Count > 0;
            if (hasNextDice) _isDoneEffect = true;
            else _visual.Finish();
        }
        if (_visual.IsComplete) { Complete(); return; }
        if (_camera != null)
        {
            try { RefreshProtection(); }
            catch (Exception) { _visual.SetProtectionRects(null); }
        }
    }

    private bool OwnerEnded()
    {
        if (_self == null || _self.IsDead() || _self.IsExtinction() || _self.view == null
            || _originCard == null || !ReferenceEquals(_self.currentDiceAction, _originCard)) return true;
        var manager = Singleton<BattleFarAreaPlayManager>.Instance;
        return _managerOwned && (manager == null || !ReferenceEquals(manager, _originManager) || !manager.isRunning || manager.attacker != _self);
    }

    private void RefreshProtection()
    {
        var rects = new List<Rect>();
        var units = BattleObjectManager.instance == null ? new List<BattleUnitModel> { _self } : BattleObjectManager.instance.GetAliveList();
        foreach (var unit in units)
        {
            Bounds bounds;
            if (!TryBodyBounds(unit, out bounds)) continue;
            Vector2 low = new Vector2(float.MaxValue, float.MaxValue), high = new Vector2(float.MinValue, float.MinValue);
            bool visible = true;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 world = bounds.center + Vector3.Scale(bounds.extents, new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                Vector3 p = _camera.WorldToViewportPoint(world);
                if (!Finite(p) || p.z <= 0f) { visible = false; break; }
                low = Vector2.Min(low, new Vector2(p.x, p.y)); high = Vector2.Max(high, new Vector2(p.x, p.y));
            }
            if (visible) rects.Add(Rect.MinMaxRect(low.x - 0.006f, low.y - 0.006f, high.x + 0.006f, high.y + 0.016f));
            if (rects.Count == VeliaTideMistVisualController.MaxProtectionRects) break;
        }
        _visual.SetProtectionRects(rects.ToArray());
    }

    // Private copy of the registered Skin SpriteRenderer read pattern; no dependency on Slazeya.
    private static bool TryBodyBounds(BattleUnitModel unit, out Bounds bounds)
    {
        bounds = new Bounds();
        if (unit == null || unit.view == null || unit.view.charAppearance == null) return false;
        bool found = false;
        var appearance = unit.view.charAppearance;
        var renderers = new HashSet<SpriteRenderer>();
        var motion = appearance.GetCurrentMotion();
        if (motion != null && motion.motionSpriteSet != null)
            foreach (var sprite in motion.motionSpriteSet)
                if (sprite.sprType != CharacterAppearanceType.Effect && sprite.sprRenderer != null) renderers.Add(sprite.sprRenderer);
        if (appearance.CustomAppearance != null)
            foreach (var renderer in appearance.CustomAppearance.allSpriteList) if (renderer != null) renderers.Add(renderer);
        foreach (var renderer in renderers)
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.sprite == null || renderer.color.a <= 0.01f || !Finite(renderer.bounds.size)) continue;
            if (!found) { bounds = renderer.bounds; found = true; } else bounds.Encapsulate(renderer.bounds);
        }
        return found && bounds.size.y > 0f && Finite(bounds.center) && Finite(bounds.size);
    }

    private static bool Finite(Vector3 v)
    {
        return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
    }

    private static Vector3 FallbackHitPosition(BattleUnitModel unit)
    {
        if (unit.view.atkEffectRoot != null && Finite(unit.view.atkEffectRoot.position)) return unit.view.atkEffectRoot.position;
        if (unit.view.charAppearance != null && unit.view.charAppearance.atkEffectRoot != null
            && Finite(unit.view.charAppearance.atkEffectRoot.position)) return unit.view.charAppearance.atkEffectRoot.position;
        // Reference H=6 only when registered body bounds and both attack anchors are absent.
        return unit.view.WorldPosition + Vector3.up * 3f;
    }

    private Material LoadMaterial()
    {
        string assemblyDir = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
        string modRoot = Directory.GetParent(assemblyDir).FullName;
        foreach (string folder in new[] { Path.Combine(modRoot, "Resource", "AssetBundle"), Path.Combine(modRoot, "Assemblies", "AB") })
        foreach (string extension in new[] { "", ".ab" })
        {
            string path = Path.Combine(folder, VeliaTideMistVisualController.BundleName + extension);
            if (!File.Exists(path)) continue;
            try
            {
                _bundle = AssetBundle.LoadFromFile(path);
                Material material = _bundle == null ? null : _bundle.LoadAsset<Material>(VeliaTideMistVisualController.MaterialName);
                if (material != null && material.shader != null && material.shader.isSupported && material.GetTexture("_MistAtlas") != null) return material;
            }
            catch (Exception ex) { Debug.LogWarning("VeliaTideMist bundle read failed: " + ex.Message); }
            if (_bundle != null) { _bundle.Unload(true); _bundle = null; }
        }
        return null;
    }

    private void Complete()
    {
        if (_closed) return;
        _closed = true;
        isRunning = false;
        _isDoneEffect = true;
        if (_visual != null) _visual.Cancel();
        ReleaseRendering();
        if (ReferenceEquals(Active, this)) Active = null;
        Destroy(gameObject); // This module's root only; never the EffectCam GameObject.
    }

    private void ReleaseRendering()
    {
        if (_filter != null) { _filter.Release(); Destroy(_filter); _filter = null; }
        if (_bundle != null) { _bundle.Unload(true); _bundle = null; }
        _camera = null;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Complete();
    }
    private void OnDestroy()
    {
        _closed = true;
        isRunning = false;
        _isDoneEffect = true;
        if (_visual != null) _visual.Cancel();
        ReleaseRendering();
        if (ReferenceEquals(Active, this)) Active = null;
    }
}
