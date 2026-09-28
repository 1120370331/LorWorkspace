using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using AceTaffy;
using Battle.DiceAttackEffect;
using HarmonyLib;
using UnityEditor;
using UnityEngine;

// Unity native factory in an isolated scene. Actor art and PPU/pivot are loaded
// with the original workshop formula; the native custom prefab transforms are
// reconstructed from resources.assets path ID 33898, not hand-position guesses.
public static class TaffyUltimatePreview
{
    [Serializable] private class NativeData { public NativeTransform attack_root; public string source_sha256; }
    [Serializable] private class NativeTransform { public NativeVector position; }
    [Serializable] private class NativeVector { public float x, y, z; }
    [Serializable] private class CoverageRecord
    {
        public string facing;
        public float rollFactor;
        public int milliseconds;
        public float ownerFace, targetFace;
    }
    [Serializable] private class AnchorRecord
    {
        public string facing;
        public float rollFactor;
        public int milliseconds;
        public float screenX, screenY;
    }
    [Serializable] private class Metrics
    {
        public string evidenceKind = "Unity native factory isolated simulation; original custom prefab transform data reconstructed; no game MoveRoutine";
        public string nativeResourcesSha256;
        public string nativeHitPivot;
        public List<CoverageRecord> faceCoverage = new List<CoverageRecord>();
        public List<AnchorRecord> emitterAnchors = new List<AnchorRecord>();
    }

    private const string Ultimate = "AceTaffy_SteamDetonation";
    private const string Blunt = "AceTaffy_SteamBlunt";
    private static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly int[] Marks = { 0, 17, 50, 100, 220, 400, 600, 800 };
    private static readonly string Ace = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../.."));
    private static readonly string RunId = Environment.GetEnvironmentVariable("TAFFY_ULTIMATE_RUN_ID");
    private static readonly string Out = Path.Combine(Ace, "Verification", "Vfx", "ultimate-v1", RunId ?? "missing-run-id");
    private static readonly List<string> Checks = new List<string>();
    private static GameObject stage;
    private static Camera camera;
    private static RenderTexture rt;
    private static DiceEffectManager manager;
    private static NativeData native;
    private static Metrics metrics = new Metrics();

    public static void Run()
    {
        try
        {
            Check(!string.IsNullOrEmpty(RunId) && RunId != "baseline", "unique ultimate output directory");
            Directory.CreateDirectory(Out);
            AceTaffyMod.RootPath = Path.Combine(Ace, "AceTaffyMod");
            native = JsonUtility.FromJson<NativeData>(File.ReadAllText(Path.Combine(Ace, "Verification/Vfx/native-hit-pivot.json")));
            Check(native != null && native.attack_root != null && native.attack_root.position != null &&
                  Mathf.Abs(native.attack_root.position.x + 1.92f) < .001f &&
                  Mathf.Abs(native.attack_root.position.y - 2.52f) < .001f, "original Appearance_Custom H fallback pivot data");
            metrics.nativeResourcesSha256 = native.source_sha256;
            metrics.nativeHitPivot = "(-1.92, 2.52, 0), original AtkEffectRoot; H override null";
            TaffyVfx.Prewarm();
            TaffyVfx.Install(new Harmony("lor.acetaffy.ultimate." + RunId));
            ClearBaseModRegistry();
            manager = (DiceEffectManager)FormatterServices.GetUninitializedObject(typeof(DiceEffectManager));

            if (Environment.GetEnvironmentVariable("TAFFY_ULTIMATE_QUICK") == "1")
            {
                Scene();
                var attacker = Actor(true, "Hit");
                var target = Actor(false, "Default");
                var anchors = new List<string>();
                foreach (float roll in new[] { 0f, .5f, 1f })
                {
                    var effect = Spawn(Ultimate, attacker, target, roll);
                    Tick(effect, .10f);
                    string name = "left_roll_" + roll.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "_100.png";
                    Save(name);
                    if (roll == .5f) Save("left_100.png");
                    var emitter = effect.transform.Find("SteamDetonation");
                    Vector3 screen = camera.WorldToScreenPoint(emitter.position);
                    float ownerFace = FaceCoverage(attacker, "Hit");
                    float targetFace = FaceCoverage(target, "Default");
                    anchors.Add("roll=" + roll.ToString("F1") +
                        " emitter_screen=(" + screen.x.ToString("F1") + "," + (540f - screen.y).ToString("F1") + ")" +
                        " owner_face=" + ownerFace.ToString("F3") + " target_face=" + targetFace.ToString("F3"));
                    UnityEngine.Object.DestroyImmediate(effect.gameObject);
                }
                File.WriteAllLines(Path.Combine(Out, "anchor-coverage.txt"), anchors);
                File.WriteAllLines(Path.Combine(Out, "acceptance.txt"), Checks);
                Debug.Log("TAFFY_ULTIMATE_UNITY_PASS quick " + Checks.Count + " checks; " + Out);
                EditorApplication.Exit(0);
                return;
            }

            RunFacing("left", true, true);
            RunFacing("right", false, false);
            File.WriteAllLines(Path.Combine(Out, "acceptance.txt"), Checks);
            File.WriteAllText(Path.Combine(Out, "metrics.json"), JsonUtility.ToJson(metrics, true));
            Debug.Log("TAFFY_ULTIMATE_UNITY_PASS " + Checks.Count + " checks; " + Out);
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Directory.CreateDirectory(Out);
            File.WriteAllText(Path.Combine(Out, "failure.txt"), ex.ToString());
            Debug.LogError("TAFFY_ULTIMATE_UNITY_FAIL " + ex);
            EditorApplication.Exit(1);
        }
    }

    private static void RunFacing(string facing, bool left, bool video)
    {
        Scene();
        var attacker = Actor(left, video ? "Guard" : "Hit");
        var target = Actor(!left, "Default");
        Check(attacker.charAppearance.GetAtkEffectPivot(ActionDetail.Hit) == attacker.charAppearance.atkEffectRoot,
              facing + " native H fallback to extracted AtkEffectRoot");
        if (video)
        {
            // Same native pivot/camera/roll as the new effect. Only the first
            // die has the dedicated resource; ordinary cards retain Blunt.
            SetPose(attacker, "Hit");
            var ordinary = Spawn(Blunt, attacker, target, .5f);
            Tick(ordinary, .10f);
            Save("ordinary_same_context.png");
            UnityEngine.Object.DestroyImmediate(ordinary.gameObject);
            SaveOldMother(attacker);
            SetPose(attacker, "Guard");
        }

        TaffySteamEffect effect = null;
        var frames = Path.Combine(Out, "frames");
        if (video) Directory.CreateDirectory(frames);
        int frameCount = 67; // -0.30 through +0.80 at nominal 60fps.
        for (int frame = 0; frame < frameCount; frame++)
        {
            float seconds = (frame - 18) / 60f;
            if (frame == 18)
            {
                SetPose(attacker, "Hit");
                effect = Spawn(Ultimate, attacker, target, .5f);
                Check(effect.transform.parent == attacker.charAppearance.GetAtkEffectPivot(ActionDetail.Hit),
                      facing + " actual factory attached to native H pivot");
                Check(!effect.HasDamagedEffect, facing + " no target hit effect");
                Check(Mathf.Abs(effect.transform.localScale.x - 3f) < .001f,
                      facing + " native roll .5 applied exactly once");
                Check(effect.transform.Find("PressureArc") != null,
                      facing + " distinct pressure arc child created");
            }
            if (effect != null) Tick(effect, Mathf.Max(0f, seconds));
            if (effect != null && frame <= 42 && frame % 2 == 0)
                RecordFaceCoverage(facing, .5f, Mathf.RoundToInt(seconds * 1000f), attacker, target);
            if (video) Save(Path.Combine("frames", frame.ToString("0000") + ".png"));
        }
        Check(effect != null, facing + " event instance exists");
        UnityEngine.Object.DestroyImmediate(effect.gameObject);
        effect = Spawn(Ultimate, attacker, target, .5f);

        foreach (int ms in Marks)
        {
            Tick(effect, ms / 1000f);
            Save(facing + "_" + ms.ToString("000") + ".png");
            var body = effect.transform.Find("SteamDetonation").GetComponent<SpriteRenderer>();
            var arc = effect.transform.Find("PressureArc").GetComponent<SpriteRenderer>();
            if (ms == 0 || ms == 17) Check(body.enabled && body.color.a >= .94f,
                facing + " complete main body visible at " + ms + "ms");
            if (ms == 100) Check(arc.enabled && arc.color.a >= .69f && body.color.a >= .85f,
                facing + " detached pressure arc and held main at 100ms");
            if (ms == 220) Check(!arc.enabled && body.color.a >= .85f,
                facing + " arc clear and main still held at 220ms");
            if (ms == 400) Check(body.color.a <= .40f,
                facing + " main faded by 400ms");
            if (ms == 600) Check(!body.enabled,
                facing + " main gone by 600ms");
            if (ms == 800) Check(effect.GetComponentsInChildren<SpriteRenderer>(true).All(r => !r.enabled),
                facing + " all visual children gone by 800ms");
            if (ms <= 220) RecordAnchor(facing, .5f, ms, effect);
        }

        UnityEngine.Object.DestroyImmediate(effect.gameObject);
        foreach (float roll in new[] { 0f, 1f })
        {
            var candidate = Spawn(Ultimate, attacker, target, roll);
            for (int i = 0; i <= 12; i += 2)
            {
                float t = i / 60f;
                Tick(candidate, t);
                int ms = Mathf.RoundToInt(t * 1000f);
                RecordFaceCoverage(facing, roll, ms, attacker, target);
                if (i == 0 || i == 6) RecordAnchor(facing, roll, ms, candidate);
                if (i == 6) Save(facing + (roll == 0f ? "_roll0_100.png" : "_high_100.png"));
            }
            Tick(candidate, .22f);
            RecordFaceCoverage(facing, roll, 220, attacker, target);
            RecordAnchor(facing, roll, 220, candidate);
            UnityEngine.Object.DestroyImmediate(candidate.gameObject);
        }
        effect = Spawn(Ultimate, attacker, target, .5f);

        Tick(effect, .10f);
        effect.SetDestroyTime(2f);
        Tick(effect, .20f);
        Check(effect.transform.Find("PressureArc").GetComponent<SpriteRenderer>().color.a < .7f,
              facing + " SetDestroyTime does not restart pressure arc");
        var simultaneous = Spawn(Ultimate, attacker, target, .5f);
        Check(simultaneous.transform.Find("PressureArc").GetComponent<SpriteRenderer>().color.a == 0f,
              facing + " second instance starts from its own clock");
        simultaneous.gameObject.SetActive(false);
        typeof(TaffySteamEffect).GetMethod("OnDisable", Fields).Invoke(simultaneous, null);
        Check(simultaneous.GetComponentsInChildren<SpriteRenderer>(true).All(r => !r.enabled),
              facing + " cancellation clears all children including arc");
        UnityEngine.Object.DestroyImmediate(simultaneous.gameObject);
        UnityEngine.Object.DestroyImmediate(effect.gameObject);
        var fallback = manager.CreateBehaviourEffect("AceTaffy_UltimateUnknown", .5f, attacker, target, 1f);
        Check(fallback == null, facing + " unknown key native fallback");
    }

    private static void RecordFaceCoverage(string facing, float roll, int ms,
        BattleUnitView attacker, BattleUnitView target)
    {
        var record = new CoverageRecord {
            facing = facing, rollFactor = roll, milliseconds = ms,
            ownerFace = FaceCoverage(attacker, "Hit"),
            targetFace = FaceCoverage(target, "Default")
        };
        metrics.faceCoverage.Add(record);
        Check(record.ownerFace < .10f, facing + " owner face below 10% at " + ms + "ms roll " + roll);
    }

    private static void RecordAnchor(string facing, float roll, int ms, TaffySteamEffect effect)
    {
        var screen = camera.WorldToScreenPoint(effect.transform.Find("SteamDetonation").position);
        var record = new AnchorRecord {
            facing = facing, rollFactor = roll, milliseconds = ms,
            screenX = screen.x, screenY = 540f - screen.y
        };
        metrics.emitterAnchors.Add(record);
    }

    private static TaffySteamEffect Spawn(string key, BattleUnitView self, BattleUnitView target, float roll)
    {
        var effect = manager.CreateBehaviourEffect(key, roll, self, target, 1f) as TaffySteamEffect;
        Check(effect != null, key + " real CreateBehaviourEffect returned instance");
        Check(effect.transform.Find(key.Substring("AceTaffy_".Length)) != null, key + " authored body child");
        return effect;
    }

    private static void Tick(TaffySteamEffect effect, float seconds)
    {
        typeof(DiceAttackEffect).GetField("_elapsed", Fields).SetValue(effect, seconds - Time.deltaTime);
        typeof(TaffySteamEffect).GetMethod("Update", Fields).Invoke(effect, null);
    }

    private static void ClearBaseModRegistry()
    {
        var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "BaseMod") ?? Assembly.Load("BaseMod");
        var dictionary = (IDictionary)asm.GetType("BaseMod.Harmony_Patch", true).GetField("CustomEffects", Fields).GetValue(null);
        dictionary.Clear();
        Check(dictionary.Count == 0, "BaseMod CustomEffects empty while private factory handles four exact resources");
    }

    private static void Scene()
    {
        if (stage != null) UnityEngine.Object.DestroyImmediate(stage);
        if (camera != null) UnityEngine.Object.DestroyImmediate(camera.gameObject);
        stage = new GameObject("UltimateNativePivotScene");
        camera = new GameObject("FixedNativeContextCamera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.34f, .36f, .38f);
        camera.transform.position = new Vector3(0f, 2.5f, -20f);
        rt = new RenderTexture(960, 540, 24);
        camera.targetTexture = rt;
    }

    private static BattleUnitView Actor(bool left, string pose)
    {
        var go = new GameObject(left ? "LeftActor" : "RightActor");
        go.transform.SetParent(stage.transform, false);
        go.transform.localPosition = new Vector3(left ? 2.5f : -2.5f, 0f, 0f);
        var view = go.AddComponent<BattleUnitView>();
        Check(view != null, "actual native BattleUnitView component");
        view.enabled = false;
        var rotation = new GameObject("characterRotationCenter").transform;
        rotation.SetParent(go.transform, false);
        rotation.localScale = left ? Vector3.one : new Vector3(-1f, 1f, 1f);
        view.characterRotationCenter = rotation;
        var prefabRoot = new GameObject("[Prefab]Appearance_Custom reconstructed");
        prefabRoot.transform.SetParent(rotation, false);
        var appearance = prefabRoot.AddComponent<CharacterAppearance>();
        Check(appearance != null, "actual native CharacterAppearance component");
        appearance.enabled = false;
        var pivot = new GameObject("AtkEffectRoot original pathID 56365").transform;
        pivot.SetParent(prefabRoot.transform, false);
        pivot.localPosition = new Vector3(native.attack_root.position.x, native.attack_root.position.y, native.attack_root.position.z);
        appearance.atkEffectRoot = pivot;
        // The original custom prefab's action-specific H pointer is null.
        view.charAppearance = appearance;
        view.atkEffectRoot = pivot;
        view.model = (BattleUnitModel)FormatterServices.GetUninitializedObject(typeof(BattleUnitModel));
        view.model.view = view;
        prefabRoot.AddComponent<SpriteRenderer>().sortingOrder = -10;
        SetPose(view, pose);
        return view;
    }

    private static void SetPose(BattleUnitView view, string pose)
    {
        var renderer = view.charAppearance.GetComponent<SpriteRenderer>();
        var xml = new XmlDocument();
        xml.Load(Path.Combine(AceTaffyMod.RootPath, "Resource/CharacterSkin/AceTaffy/ModInfo.xml"));
        var node = xml.SelectSingleNode("/ModInfo/ClothInfo/" + pose);
        Check(node != null, pose + " ModInfo node");
        float ppu = float.Parse(node.Attributes["quality"].Value, System.Globalization.CultureInfo.InvariantCulture);
        float px = float.Parse(node.SelectSingleNode("Pivot").Attributes["pivot_x"].Value, System.Globalization.CultureInfo.InvariantCulture);
        float py = float.Parse(node.SelectSingleNode("Pivot").Attributes["pivot_y"].Value, System.Globalization.CultureInfo.InvariantCulture);
        Check(ppu == 50f, pose + " native loader PPU50");
        string path = Path.Combine(AceTaffyMod.RootPath, "Resource/CharacterSkin/AceTaffy/ClothCustom", pose + ".png");
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Check(ImageConversion.LoadImage(texture, File.ReadAllBytes(path)), pose + " current actor PNG decoded");
        var pivot = new Vector2((px + 512f) / 1024f, (py + 512f) / 1024f);
        renderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, ppu);
    }

    private static void SaveOldMother(BattleUnitView attacker)
    {
        var root = new GameObject("OldMotherAssetOnly");
        root.transform.SetParent(attacker.charAppearance.GetAtkEffectPivot(ActionDetail.Hit), false);
        root.transform.localPosition = new Vector3(-2f, .18f, 0f);
        root.transform.localScale = Vector3.one * 3f;
        var path = Path.Combine(Ace, "Verification/Vfx/ultimate-v1/baseline/AceTaffyMod/Resource/Effects/AceTaffy/SteamDetonation.png");
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Check(ImageConversion.LoadImage(texture, File.ReadAllBytes(path)), "old mother asset decoded for same-camera comparison");
        var child = new GameObject("OldMotherSprite");
        child.transform.SetParent(root.transform, false);
        var renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 790f);
        renderer.sortingLayerName = "Particle";
        renderer.transform.localScale = Vector3.one * 1.25f;
        Save("old_mother_asset_only.png");
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void Save(string name)
    {
        var path = Path.Combine(Out, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var image = new Texture2D(960, 540, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
        image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
        RenderTexture.active = previous;
    }

    // Fixed face regions in the current authored 512px art. The GPU render is
    // effect-only over transparent clear, so actor opacity cannot hide overlap.
    private static float FaceCoverage(BattleUnitView actor, string pose)
    {
        var actorRenderers = stage.GetComponentsInChildren<SpriteRenderer>(true)
            .Where(r => r.GetComponent<CharacterAppearance>() != null)
            .ToArray();
        bool[] original = actorRenderers.Select(r => r.enabled).ToArray();
        for (int i = 0; i < actorRenderers.Length; i++) actorRenderers[i].enabled = false;
        Color background = camera.backgroundColor;
        camera.backgroundColor = Color.clear;
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var image = new Texture2D(960, 540, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
        image.Apply();
        Color32[] pixels = image.GetPixels32();
        UnityEngine.Object.DestroyImmediate(image);
        RenderTexture.active = previous;
        camera.backgroundColor = background;
        for (int i = 0; i < actorRenderers.Length; i++) actorRenderers[i].enabled = original[i];

        var sprite = actor.charAppearance.GetComponent<SpriteRenderer>().sprite;
        int left = pose == "Default" ? 233 : 232;
        int right = pose == "Default" ? 284 : 280;
        int top = pose == "Default" ? 197 : 205;
        int bottom = pose == "Default" ? 244 : 246;
        Vector3 a = FacePixelWorld(actor, sprite, left, top);
        Vector3 b = FacePixelWorld(actor, sprite, right, bottom);
        Vector3 sa = camera.WorldToScreenPoint(a), sb = camera.WorldToScreenPoint(b);
        int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(sa.x, sb.x)), 0, 959);
        int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(sa.x, sb.x)), 0, 959);
        int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(sa.y, sb.y)), 0, 539);
        int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(sa.y, sb.y)), 0, 539);
        int count = 0, covered = 0;
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                count++;
                if (pixels[y * 960 + x].a >= 153) covered++;
            }
        return count == 0 ? 0f : (float)covered / count;
    }

    private static Vector3 FacePixelWorld(BattleUnitView actor, Sprite sprite, int x, int y)
    {
        float mirrored = actor.characterRotationCenter.localScale.x;
        return actor.transform.position + new Vector3(
            mirrored * (x - sprite.pivot.x) / sprite.pixelsPerUnit,
            (sprite.texture.height - y - sprite.pivot.y) / sprite.pixelsPerUnit, 0f);
    }

    private static void Check(bool pass, string label)
    {
        if (!pass) throw new InvalidOperationException("FAIL " + label);
        Checks.Add("PASS " + label);
    }
}
