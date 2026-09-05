using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Steria;
using UnityEngine;

/// <summary>Visual/audio only: the default far-area manager retains all combat settlement.</summary>
public class FarAreaEffect_Steria_OceanWave : FarAreaEffect
{
    private const float CallbackWatchdog = 8f;
    private static AssetBundle _bundle;
    private static GameObject _prefab;
    private SlazeyaStormVisualController _visual;
    private SlazeyaStormAudioController _audio;
    private GameObject _instance;
    private bool _burstTriggered;
    private bool _managerOwned;
    private BattlePlayingCardDataInUnitModel _originCard;

    public override void Init(BattleUnitModel self, params object[] args)
    {
        base.Init(self, args);
        isRunning = true;
        _burstTriggered = false;
        _originCard = self == null ? null : self.currentDiceAction;
        var manager = Singleton<BattleFarAreaPlayManager>.Instance;
        _managerOwned = manager != null && manager.attacker == self;
        var footprint = new SlazeyaStormVisualController.Footprint();
        Vector3? audioPosition = null;
        try
        {
            List<BattleUnitModel> recipients = FindRecipients(self);
            if (recipients.Count > 0) audioPosition = recipients[0].view.WorldPosition;
            float height = RepresentativeHeight(recipients, self);
            if (recipients.Count > 0 && height > 0f)
            {
                var feet = new Vector3[recipients.Count];
                for (int i = 0; i < feet.Length; i++) feet[i] = recipients[i].view.WorldPosition;
                footprint = SlazeyaStormVisualController.FitFootprint(feet, height);
                // This component and the visual are world roots, never children of the caster.
                transform.SetParent(null, true);
                transform.position = footprint.Center;
                GameObject prefab = LoadPrefab();
                if (prefab != null)
                {
                    _instance = Instantiate(prefab);
                    int layer = LayerMask.NameToLayer("Character");
                    if (layer >= 0)
                        foreach (Transform child in _instance.GetComponentsInChildren<Transform>(true))
                            child.gameObject.layer = layer;
                }
                SteriaLogger.Log(string.Format("SlazeyaStormMass frozen XZ center={0} Rx={1:F3} Rz={2:F3} visible H={3:F3} recipients={4}",
                    footprint.Center, footprint.RadiusX, footprint.RadiusZ, height, recipients.Count));
            }
            else SteriaLogger.Log("SlazeyaStormMass: no valid recipient/body bounds; completing without visible resources.");
            _visual = new SlazeyaStormVisualController(_instance, footprint);
        }
        catch (Exception ex)
        {
            SteriaLogger.Log("SlazeyaStormMass visual initialization failed: " + ex);
            if (_instance != null) Destroy(_instance);
            _instance = null;
            _visual = new SlazeyaStormVisualController(null, footprint);
        }
        if (audioPosition.HasValue) _audio = SlazeyaStormAudioController.TryCreate(audioPosition.Value, ReadEffectVolume);
    }

    public override void GiveDamageFromManager(List<BattleUnitModel> damagedUnitList)
    {
        if (_isDoneEffect || _burstTriggered) return;
        _burstTriggered = true; // Empty lists (all defenses) still burst exactly once.
        if (_visual != null) _visual.TriggerBurst();
        if (_audio != null) _audio.TriggerBurst();
    }

    protected override void Update()
    {
        if (_isDoneEffect || _visual == null) return;
        // Manager can enter End without a damage callback when its attacker is disabled.
        var manager = Singleton<BattleFarAreaPlayManager>.Instance;
        bool cancelled = _self == null || _self.IsDead() || _self.IsExtinction()
            || _self.IsBreakLifeZero() || _self.IsKnockout()
            || (_originCard != null && _self.currentDiceAction != _originCard)
            || (_managerOwned && (manager == null || !manager.isRunning || manager.attacker != _self));
        if (cancelled || (!_burstTriggered && _visual.Elapsed >= SlazeyaStormVisualController.GatherDuration + CallbackWatchdog))
        {
            SteriaLogger.Log("SlazeyaStormMass cancelled: owner ended or callback wait expired; no synthetic burst.");
            Complete();
            return;
        }
        _visual.Advance(Time.deltaTime);
        if (_audio != null) _audio.Advance(Time.deltaTime);
        if (_visual.GatherReady) isRunning = false; // Only opens the default manager's gate.
        if (_visual.IsComplete) Complete();
    }

    private void Complete()
    {
        isRunning = false;
        _isDoneEffect = true;
        ReleaseVisual();
        Destroy(gameObject);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _isDoneEffect = true;
        ReleaseVisual();
    }

    private void OnDestroy() { ReleaseVisual(); }

    private void ReleaseVisual()
    {
        if (_audio != null) { _audio.Dispose(); _audio = null; }
        if (_visual != null) { _visual.Dispose(); _visual = null; }
        if (_instance != null) { Destroy(_instance); _instance = null; }
    }

    private static float ReadEffectVolume()
    {
        var game = GlobalGameManager.Instance;
        var option = game != null ? game.CurrentOption : null;
        return option != null ? option.GetVolumeEffect() : 1f;
    }

    private static bool IsRecipient(BattleUnitModel unit, BattleUnitModel self)
    {
        return self != null && unit != null && unit != self && unit.faction != self.faction
            && !unit.IsDead() && !unit.IsExtinction() && unit.view != null
            && SlazeyaStormVisualController.IsFinite(unit.view.WorldPosition);
    }

    private static List<BattleUnitModel> FindRecipients(BattleUnitModel self)
    {
        var result = new List<BattleUnitModel>();
        if (self == null) return result;
        var manager = Singleton<BattleFarAreaPlayManager>.Instance;
        if (manager != null && manager.attacker == self && manager.victims != null)
            foreach (var victim in manager.victims)
                if (victim != null && IsRecipient(victim.unitModel, self) && !result.Contains(victim.unitModel))
                    result.Add(victim.unitModel);
        if (result.Count == 0 && BattleObjectManager.instance != null)
            foreach (var unit in BattleObjectManager.instance.GetAliveList_opponent(self.faction))
                if (IsRecipient(unit, self) && !result.Contains(unit)) result.Add(unit);
        if (result.Count == 0 && self.currentDiceAction != null && IsRecipient(self.currentDiceAction.target, self))
            result.Add(self.currentDiceAction.target);
        return result;
    }

    private static float RepresentativeHeight(List<BattleUnitModel> units, BattleUnitModel self)
    {
        var heights = new List<float>();
        foreach (var unit in units)
        {
            float value = BodyHeight(unit);
            if (value > 0f) heights.Add(value);
        }
        if (heights.Count == 0) return BodyHeight(self);
        heights.Sort();
        int middle = heights.Count / 2;
        return heights.Count % 2 == 1 ? heights[middle] : (heights[middle - 1] + heights[middle]) * 0.5f;
    }

    private static float BodyHeight(BattleUnitModel unit)
    {
        if (unit == null || unit.view == null || unit.view.charAppearance == null) return 0f;
        bool found = false;
        Bounds bounds = new Bounds();
        // Active body sprites only; excludes inactive motions, attached particle effects and UI.
        foreach (SpriteRenderer renderer in unit.view.charAppearance.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!renderer.enabled || renderer.sprite == null || renderer.color.a <= 0.01f) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return found && SlazeyaStormVisualController.IsFinite(bounds.size) ? bounds.size.y : 0f;
    }

    private static GameObject LoadPrefab()
    {
        if (_prefab != null) return _prefab;
        string assemblyDir = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
        string modRoot = Directory.GetParent(assemblyDir).FullName;
        foreach (string folder in new[] { Path.Combine(modRoot, "Resource", "AssetBundle"), Path.Combine(modRoot, "Assemblies", "AB") })
        foreach (string extension in new[] { "", ".ab" })
        {
            string path = Path.Combine(folder, SlazeyaStormVisualController.BundleName + extension);
            if (!File.Exists(path)) continue;
            try
            {
                _bundle = AssetBundle.LoadFromFile(path);
                if (_bundle == null) continue;
                _prefab = _bundle.LoadAsset<GameObject>(SlazeyaStormVisualController.PrefabName);
                if (_prefab != null)
                {
                    foreach (Renderer renderer in _prefab.GetComponentsInChildren<Renderer>(true))
                        if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null || !renderer.sharedMaterial.shader.isSupported)
                            throw new InvalidOperationException("Missing or unsupported storm material/shader");
                    SteriaLogger.Log("SlazeyaStormMass: loaded " + path + " version=" + SlazeyaStormVisualController.Version);
                    return _prefab;
                }
            }
            catch (Exception ex) { SteriaLogger.Log("SlazeyaStormMass bundle read failed: " + path + " " + ex.Message); }
            _prefab = null;
            if (_bundle != null) { _bundle.Unload(true); _bundle = null; }
        }
        SteriaLogger.Log("SlazeyaStormMass: bundle unavailable; lifecycle remains active without visuals.");
        return null;
    }
}
