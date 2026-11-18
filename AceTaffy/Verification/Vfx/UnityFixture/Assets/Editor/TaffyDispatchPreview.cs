using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using AceTaffy;
using Battle.DiceAttackEffect;
using HarmonyLib;
using UnityEditor;
using UnityEngine;

// Editor-only isolated replay. The factory, Harmony prefix, DiceAttackEffect
// base, ImageConversion loader and SpriteRenderer are actual production paths.
// The two BattleUnitViews and their hand pivots are explicit fixture objects.
public static class TaffyDispatchPreview
{
    private static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.Static |
                                                   BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly string[] Keys = {
        "AceTaffy_SteamBlunt", "AceTaffy_SteamPierce",
        "AceTaffy_SteamGuard", "AceTaffy_SteamDetonation"
    };
    private static readonly string Ace = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../.."));
    private static readonly string Out = Path.Combine(Ace, "Verification", "Vfx", "unity-preview");
    private static Camera camera;
    private static RenderTexture targetTexture;
    private static GameObject stage;
    private static readonly List<string> evidence = new List<string>();

    public static void Run()
    {
        try
        {
            Directory.CreateDirectory(Out);
            AceTaffyMod.RootPath = Path.Combine(Ace, "AceTaffyMod");
            Check(File.Exists(Path.Combine(AceTaffyMod.RootPath, "Resource/Effects/AceTaffy/SteamBlunt.png")), "real package root");
            Scene();
            TaffyVfx.Prewarm();
            var harmony = new Harmony("lor.acetaffy.unity.fixture");
            TaffyVfx.Install(harmony);
            ClearBaseModCustomEffects();
            // The factory does not access instance state. A non-serialized
            // native manager avoids Unity's MonoScript import requirement.
            var manager = (DiceEffectManager)FormatterServices.GetUninitializedObject(typeof(DiceEffectManager));
            var left = Actor(true);
            var right = Actor(false);
            SetPose(left, "Hit");
            SetPose(right, "Default");
            foreach (string key in Keys)
            {
                SetPose(left, key == "AceTaffy_SteamPierce" ? "Penetrate" :
                    key == "AceTaffy_SteamGuard" ? "Guard" : "Hit");
                int before = CountEffects();
                var effect = manager.CreateBehaviourEffect(key, .5f, left, right, 1f);
                Check(effect != null && effect is TaffySteamEffect, key + " real factory returned concrete steam effect");
                Check(CountEffects() == before + 1, key + " one instantiate per factory call");
                Check(!effect.HasDamagedEffect, key + " no invented victim damage effect");
                Check(effect.transform.parent == left.charAppearance.GetAtkEffectPivot(
                    key == "AceTaffy_SteamPierce" ? ActionDetail.Penetrate :
                    key == "AceTaffy_SteamGuard" ? ActionDetail.Guard : ActionDetail.Hit),
                    key + " native action pivot parent");
                var body = effect.transform.Find(key.Substring("AceTaffy_".Length)).GetComponent<SpriteRenderer>();
                Check(body.sprite != null && body.sprite.texture.width > 0 && body.enabled,
                    key + " authored mother loaded and renderer visible at event time");
                Check(body.sortingLayerName == "Particle" && body.gameObject.layer == LayerMask.NameToLayer("Effect"),
                    key + " native sorting and layer");
                float expectedScale = key == "AceTaffy_SteamGuard" ? 1.5f : 3f;
                Check(Mathf.Abs(effect.transform.localScale.x - expectedScale) < .001f,
                    key + " native SetScale(.5) applied exactly once");
                Check(RenderedDelta(effect) > 50, key + " visible GPU pixels from actual SpriteRenderer path");
                Save(key + "_event.png");
                Tick(effect, .4f);
                Save(key + "_fade.png");
                Check(body.color.a < .5f, key + " presentation fades on elapsed clock");
                effect.gameObject.SetActive(false);
                // Edit-mode scripts do not receive every play-mode disable
                // callback. Invoke the production callback explicitly here.
                typeof(TaffySteamEffect).GetMethod("OnDisable", Fields).Invoke(effect, null);
                Check(effect.GetComponentsInChildren<SpriteRenderer>(true).All(x => !x.enabled),
                    key + " cancellation clears child renderers");
                UnityEngine.Object.DestroyImmediate(effect.gameObject);
            }

            int current = CountEffects();
            var unknown = manager.CreateBehaviourEffect("AceTaffy_UnknownSentinel", .5f, left, right, 1f);
            Check(unknown == null && CountEffects() == current, "unknown resource follows native fallback without Taffy instance");
            SetPose(left, "Default");
            SetPose(right, "Hit");
            var mirrored = manager.CreateBehaviourEffect("AceTaffy_SteamDetonation", .5f, right, left, 1f);
            Check(mirrored != null && mirrored.transform.parent == right.charAppearance.GetAtkEffectPivot(ActionDetail.Hit),
                "right-facing real factory attachment");
            Save("AceTaffy_SteamDetonation_right_event.png");
            UnityEngine.Object.DestroyImmediate(mirrored.gameObject);
            File.WriteAllLines(Path.Combine(Out, "dispatch-evidence.txt"), evidence);
            string previousFailure = Path.Combine(Out, "failure.txt");
            if (File.Exists(previousFailure)) File.Delete(previousFailure);
            Debug.Log("TAFFY_UNITY_FACTORY_PASS " + evidence.Count + " checks; " + Out);
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Directory.CreateDirectory(Out);
            File.WriteAllText(Path.Combine(Out, "failure.txt"), ex.ToString());
            Debug.LogError("TAFFY_UNITY_FACTORY_FAIL " + ex);
            EditorApplication.Exit(1);
        }
    }

    private static void ClearBaseModCustomEffects()
    {
        var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "BaseMod");
        if (asm == null) asm = Assembly.Load("BaseMod");
        var type = asm.GetType("BaseMod.Harmony_Patch", true);
        var dictionary = (IDictionary)type.GetField("CustomEffects", Fields).GetValue(null);
        dictionary.Clear();
        Check(dictionary.Count == 0, "BaseMod CustomEffects is empty before dispatch");
    }

    private static BattleUnitView Actor(bool left)
    {
        var unit = new GameObject(left ? "TaffyLeft" : "TaffyRight");
        unit.transform.SetParent(stage.transform, false);
        unit.transform.position = new Vector3(left ? 2.5f : -2.5f, 0f, 0f);
        var view = unit.AddComponent<BattleUnitView>();
        Check(view != null, "native BattleUnitView component available in Editor");
        view.enabled = false;
        var rotation = new GameObject("characterRotationCenter").transform;
        rotation.SetParent(unit.transform, false);
        rotation.localScale = left ? Vector3.one : new Vector3(-1f, 1f, 1f);
        view.characterRotationCenter = rotation;
        var appearanceRoot = new GameObject("NativeCharacterAppearance");
        appearanceRoot.transform.SetParent(rotation, false);
        appearanceRoot.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        var appearance = appearanceRoot.AddComponent<CharacterAppearance>();
        Check(appearance != null, "native CharacterAppearance component available in Editor");
        appearance.enabled = false;
        var hand = new GameObject("handPivot").transform;
        hand.SetParent(appearanceRoot.transform, false);
        hand.localPosition = new Vector3(-1.4f, 0f, 0f);
        appearance.atkEffectRoot = hand;
        foreach (string field in new[] { "atkEffectPivot_H", "atkEffectPivot_Z", "atkEffectPivot_G" })
            typeof(CharacterAppearance).GetField(field, Fields).SetValue(appearance, hand);
        view.charAppearance = appearance;
        view.atkEffectRoot = hand;
        view.model = (BattleUnitModel)FormatterServices.GetUninitializedObject(typeof(BattleUnitModel));
        view.model.view = view;
        var renderer = appearanceRoot.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = -10;
        return view;
    }

    private static void SetPose(BattleUnitView view, string pose)
    {
        var renderer = view.charAppearance.GetComponent<SpriteRenderer>();
        Check(renderer != null, pose + " actor renderer available");
        string actorPath = Path.Combine(AceTaffyMod.RootPath, "Resource/CharacterSkin/AceTaffy/ClothCustom", pose + ".png");
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Check(ImageConversion.LoadImage(texture, File.ReadAllBytes(actorPath)), pose + " actor PNG decoded in Unity");
        renderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
            new Vector2(.5f, .5f), 50f);
    }

    private static void Scene()
    {
        stage = new GameObject("IsolatedTaffyFactoryStage");
        camera = new GameObject("FixtureCamera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7.5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.34f, .36f, .38f);
        camera.transform.position = new Vector3(0f, 2.5f, -20f);
        targetTexture = new RenderTexture(960, 540, 24);
        camera.targetTexture = targetTexture;
    }

    private static int CountEffects() => UnityEngine.Object.FindObjectsOfType<TaffySteamEffect>().Length;

    private static void Tick(DiceAttackEffect effect, float seconds)
    {
        typeof(DiceAttackEffect).GetField("_elapsed", Fields).SetValue(effect, seconds - Time.deltaTime);
        typeof(TaffySteamEffect).GetMethod("Update", Fields).Invoke(effect, null);
    }

    private static void Save(string filename)
    {
        camera.Render();
        var prior = RenderTexture.active;
        RenderTexture.active = targetTexture;
        var texture = new Texture2D(960, 540, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
        texture.Apply();
        File.WriteAllBytes(Path.Combine(Out, filename), texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        RenderTexture.active = prior;
    }

    private static int RenderedDelta(DiceAttackEffect effect)
    {
        var renderers = effect.GetComponentsInChildren<SpriteRenderer>(true);
        var enabled = renderers.Select(x => x.enabled).ToArray();
        var withEffect = Pixels();
        foreach (var renderer in renderers) renderer.enabled = false;
        var withoutEffect = Pixels();
        for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = enabled[i];
        int changed = 0;
        for (int i = 0; i < withEffect.Length; i++)
        {
            int delta = Math.Abs(withEffect[i].r - withoutEffect[i].r) +
                        Math.Abs(withEffect[i].g - withoutEffect[i].g) +
                        Math.Abs(withEffect[i].b - withoutEffect[i].b);
            if (delta > 20) changed++;
        }
        return changed;
    }

    private static Color32[] Pixels()
    {
        camera.Render();
        var prior = RenderTexture.active;
        RenderTexture.active = targetTexture;
        var texture = new Texture2D(960, 540, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
        texture.Apply();
        var pixels = texture.GetPixels32();
        UnityEngine.Object.DestroyImmediate(texture);
        RenderTexture.active = prior;
        return pixels;
    }

    private static void Check(bool pass, string message)
    {
        if (!pass) throw new Exception("FAIL " + message);
        evidence.Add("PASS " + message);
    }
}
