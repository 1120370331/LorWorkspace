using System;
using System.Collections.Generic;
using System.IO;
using Battle.DiceAttackEffect;
using HarmonyLib;
using UnityEngine;

namespace AceTaffy
{
    // One immutable texture/sprite pair per authored mother. Unity objects are loaded on
    // the main thread at mod initialization; instances only own their small renderers.
    public static class TaffyVfx
    {
        private static readonly string[] Names = { "SteamBlunt", "SteamPierce", "SteamGuard", "SteamDetonation", "SteamWisp" };
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static readonly HashSet<string> Reported = new HashSet<string>();
        private static Material _material;
        private static bool _installed;

        // BaseMod scans only DLLs at the package root for CustomEffects, while
        // playable assembly discovery loads Assemblies/AceTaffy.dll. Patch the
        // actual RencounterManager effect factory with an exact resource scope.
        public static void Install(Harmony harmony)
        {
            if (_installed) return;
            if (harmony == null) throw new ArgumentNullException("harmony");
            var factory = AccessTools.Method(typeof(DiceEffectManager), "CreateBehaviourEffect",
                new[] { typeof(string), typeof(float), typeof(BattleUnitView), typeof(BattleUnitView), typeof(float) });
            var prefix = AccessTools.Method(typeof(TaffyVfx), "CreateBehaviourEffectPrefix");
            if (factory == null || prefix == null)
                throw new MissingMethodException("AceTaffy steam: real CreateBehaviourEffect hook unavailable");
            harmony.Patch(factory, prefix: new HarmonyMethod(prefix) { priority = Priority.First });
            _installed = true;
            AceTaffyMod.Log("steam effect factory installed for four exact resources");
        }

        private static Type EffectType(string resource)
        {
            switch (resource)
            {
                case "AceTaffy_SteamBlunt": return typeof(DiceAttackEffect_AceTaffy_SteamBlunt);
                case "AceTaffy_SteamPierce": return typeof(DiceAttackEffect_AceTaffy_SteamPierce);
                case "AceTaffy_SteamGuard": return typeof(DiceAttackEffect_AceTaffy_SteamGuard);
                case "AceTaffy_SteamDetonation": return typeof(DiceAttackEffect_AceTaffy_SteamDetonation);
                default: return null;
            }
        }

        private static bool CreateBehaviourEffectPrefix(string resource, float scaleFactor,
            BattleUnitView self, BattleUnitView target, float time, ref DiceAttackEffect __result)
        {
            Type type = EffectType(resource);
            if (type == null) return true; // Includes null and all other mods.
            GameObject instance = null;
            try
            {
                instance = new GameObject(resource);
                var effect = (TaffySteamEffect)instance.AddComponent(type);
                effect.Initialize(self, target, time);
                if (!effect.Ready) throw new InvalidOperationException("effect initialization did not produce its renderer");
                effect.SetScale(scaleFactor);
                __result = effect;
                return false;
            }
            catch (Exception ex)
            {
                if (instance != null) UnityEngine.Object.Destroy(instance);
                AceTaffyMod.Log("steam effect factory failed for " + resource + ": " + ex);
                return true; // Let native resolve its own resource or return null.
            }
        }

        public static void Prewarm()
        {
            foreach (string name in Names) Get(name);
            try { var material = Material; }
            catch (Exception ex)
            {
                if (Reported.Add("Sprites/Default")) AceTaffyMod.Log("steam material unavailable: " + ex.Message);
            }
        }

        internal static Sprite Get(string name)
        {
            Sprite found;
            if (Sprites.TryGetValue(name, out found)) return found;
            try
            {
                string root = AceTaffyMod.RootPath;
                if (string.IsNullOrEmpty(root)) throw new InvalidOperationException("mod root unavailable");
                string path = Path.Combine(root, "Resource", "Effects", "AceTaffy", name + ".png");
                byte[] png = File.ReadAllBytes(path);
                if (png.Length > 16 * 1024 * 1024)
                    throw new InvalidDataException("effect PNG exceeds 16 MiB: " + path);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (!ImageConversion.LoadImage(texture, png, true))
                        throw new InvalidDataException("PNG decode failed: " + path);
                    if (texture.width > 2048 || texture.height > 2048)
                        throw new InvalidDataException("effect texture exceeds 2048 px: " + path);
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    texture.name = "AceTaffy_" + name;
                    // Calibrated from visible (alpha >= 8) pixels of the authored
                    // masters against the v3 Taffy action's 261 px / quality 50.
                    // The native roll multiplier is 1..2; these targets are at 1.5.
                    found = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                        new Vector2(.5f, .5f), PixelsPerUnit(name), 0, SpriteMeshType.FullRect);
                    found.name = texture.name;
                    Sprites.Add(name, found);
                    return found;
                }
                catch { UnityEngine.Object.Destroy(texture); throw; }
            }
            catch (Exception ex)
            {
                Sprites[name] = null;
                if (Reported.Add(name)) AceTaffyMod.Log("effect asset " + name + " unavailable: " + ex.Message);
                return null;
            }
        }

        internal static float PixelsPerUnit(string name)
        {
            switch (name)
            {
                case "SteamBlunt": return 1170f;      // .65 actor height wide
                case "SteamPierce": return 1050f;     // 1.05 wide, .19 high
                case "SteamGuard": return 1000f;      // .75 high after G native scale
                case "SteamDetonation": return 790f;  // 1.25 wide, .80 high
                case "SteamWisp": return 900f;        // individual loose steam flecks
                default: throw new ArgumentOutOfRangeException("name");
            }
        }

        internal static Material Material
        {
            get
            {
                if (_material == null)
                {
                    Shader shader = Shader.Find("Sprites/Default");
                    if (shader == null) throw new InvalidOperationException("Sprites/Default unavailable");
                    _material = new Material(shader) { name = "AceTaffy_SteamRGBA" };
                }
                return _material;
            }
        }
    }
}

// The loader discovers this exact DiceAttackEffect_ prefix and XML suffix.
public abstract class TaffySteamEffect : DiceAttackEffect
{
    internal bool Ready { get; private set; }
    protected abstract string Mother { get; }
    protected abstract string NativeSuffix { get; }
    protected virtual int WispCount { get { return 3; } }
    protected virtual float BodyScale { get { return 1f; } }
    protected virtual float ForwardOffset { get { return -1.2f; } }

    private SpriteRenderer _body;
    private readonly List<SpriteRenderer> _wisps = new List<SpriteRenderer>(8);
    private readonly List<SpriteRenderer> _sparks = new List<SpriteRenderer>(6);

    public override void Initialize(BattleUnitView self, BattleUnitView target, float destroyTime)
    {
        try
        {
            if (self == null || self.model == null || self.charAppearance == null || target == null)
                throw new InvalidOperationException("combat appearance unavailable");
            gameObject.name = "AceTaffySteam_" + NativeSuffix;
            additionalScale = Vector3.one * (NativeSuffix == "G" ? -.5f : 0f);
            // Move the authored left-leading edge ahead of the gauntlet so its
            // broad trailing cloud does not veil the character's face. This is
            // an attachment offset, separate from the <= .12-height animation.
            offset = new Vector2(ForwardOffset, .18f);
            addedDestroyTime = 0f;
            // BaseMod creates a bare GameObject then AddComponent(effectType).
            // RequireComponent normally supplies these, but make the native
            // base.Initialize contract explicit even if metadata inheritance
            // differs between installed Unity/Mono versions.
            if (spr == null) spr = GetComponent<SpriteRenderer>() ?? gameObject.AddComponent<SpriteRenderer>();
            if (animator == null) animator = GetComponent<Animator>() ?? gameObject.AddComponent<Animator>();
            base.Initialize(self, target, Mathf.Max(.001f, destroyTime));
            _bHasDamagedEffect = false; // The native hit path alone confirms damage.
            spr.enabled = false;
            animator.enabled = false;

            Sprite mother = AceTaffy.TaffyVfx.Get(Mother);
            if (mother == null) { UnityEngine.Object.Destroy(gameObject); return; }
            int layer = LayerMask.NameToLayer("Effect");
            if (layer < 0) throw new InvalidOperationException("Effect layer unavailable");
            _body = Child(Mother, mother, layer, 0);
            _body.transform.localScale = Vector3.one * BodyScale;
            Sprite wisp = AceTaffy.TaffyVfx.Get("SteamWisp");
            if (wisp != null)
                for (int i = 0; i < WispCount; i++) _wisps.Add(Child("Wisp" + i, wisp, layer, -1));
            if (Mother == "SteamDetonation" && wisp != null)
                for (int i = 0; i < 6; i++) _sparks.Add(Child("BrassFleck" + i, wisp, layer, 1));
            Sample();
            Ready = true;
        }
        catch (Exception ex)
        {
            AceTaffy.AceTaffyMod.Log("steam effect failed: " + ex);
            UnityEngine.Object.Destroy(gameObject);
        }
    }

    private SpriteRenderer Child(string name, Sprite sprite, int layer, int order)
    {
        var child = new GameObject(name) { layer = layer };
        child.transform.SetParent(transform, false);
        var renderer = child.AddComponent<SpriteRenderer>();
        renderer.sharedMaterial = AceTaffy.TaffyVfx.Material;
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Particle";
        renderer.sortingOrder = order;
        return renderer;
    }

    public override void SetLayer(string layerName)
    {
        base.SetLayer(layerName);
    }

    protected override void Update()
    {
        base.Update();
        Sample();
    }

    private static float Fade(float time, float hold, float end)
    {
        float u = Mathf.Clamp01((time - hold) / (end - hold));
        return 1f - u * u * (3f - 2f * u);
    }

    private void Sample()
    {
        if (_body == null) return;
        // One scaled combat-seconds clock. Native lifetime changes object ownership,
        // but SetDestroyTime must not restart or stretch the steam presentation.
        float t = _elapsed;
        float bodyEnd = Mother == "SteamDetonation" ? .60f : Mother == "SteamBlunt" ? .55f : .45f;
        float alpha = Fade(t, .16f, bodyEnd);
        _body.color = new Color(1f, 1f, 1f, alpha * (Mother == "SteamGuard" ? .90f : .96f));
        _body.enabled = alpha > .001f;
        float expand = Mother == "SteamDetonation" ? 1f + .15f * Mathf.Clamp01(t / .25f) :
            Mother == "SteamGuard" ? 1f - .04f * Mathf.Clamp01(t / .22f) : 1f;
        _body.transform.localScale = Vector3.one * (BodyScale * expand);
        _body.transform.localPosition = new Vector3(-.06f * Mathf.Clamp01(t / .05f), 0f, 0f);
        float wispEnd = Mother == "SteamDetonation" ? .85f : Mother == "SteamBlunt" ? .65f : .55f;
        for (int i = 0; i < _wisps.Count; i++)
        {
            var r = _wisps[i];
            float start = .025f + i * .018f;
            float a = Mathf.Clamp01((t - start) / .045f) * Fade(t, .19f, wispEnd);
            r.color = new Color(1f, 1f, 1f, a * (.22f + .04f * (i % 3)));
            r.enabled = a > .001f;
            r.transform.localScale = Vector3.one * (.16f + .035f * (i % 3));
            r.transform.localPosition = new Vector3(.18f + i * .095f + t * .28f,
                ((i % 2 == 0) ? 1f : -1f) * (.07f + i * .045f) + t * .22f, 0f);
        }
        for (int i = 0; i < _sparks.Count; i++)
        {
            var r = _sparks[i];
            float a = Mathf.Clamp01((t - .025f) / .025f) * Fade(t, .08f, .22f);
            r.color = new Color(.9f, .61f, .29f, .75f * a);
            r.enabled = a > .001f;
            r.transform.localScale = new Vector3(.038f, .012f, 1f);
            r.transform.localPosition = new Vector3(-.19f - i * .055f - t * .26f,
                (i - 2.5f) * .09f + t * (i % 2 == 0 ? .12f : -.08f), 0f);
        }
    }

    private void OnDisable()
    {
        // A canceled attack must leave no renderer, even if its native object persists.
        if (_body != null) _body.enabled = false;
        foreach (var r in _wisps) if (r != null) r.enabled = false;
        foreach (var r in _sparks) if (r != null) r.enabled = false;
    }
}

public sealed class DiceAttackEffect_AceTaffy_SteamBlunt : TaffySteamEffect
{ protected override string Mother { get { return "SteamBlunt"; } } protected override string NativeSuffix { get { return "H"; } } protected override int WispCount { get { return 4; } } }
public sealed class DiceAttackEffect_AceTaffy_SteamPierce : TaffySteamEffect
{ protected override string Mother { get { return "SteamPierce"; } } protected override string NativeSuffix { get { return "Z"; } } protected override float ForwardOffset { get { return -1.4f; } } }
public sealed class DiceAttackEffect_AceTaffy_SteamGuard : TaffySteamEffect
{ protected override string Mother { get { return "SteamGuard"; } } protected override string NativeSuffix { get { return "G"; } } protected override int WispCount { get { return 0; } } protected override float BodyScale { get { return 2f; } } protected override float ForwardOffset { get { return -.3f; } } }
public sealed class DiceAttackEffect_AceTaffy_SteamDetonation : TaffySteamEffect
{ protected override string Mother { get { return "SteamDetonation"; } } protected override string NativeSuffix { get { return "H"; } } protected override int WispCount { get { return 8; } } protected override float BodyScale { get { return 1.25f; } } protected override float ForwardOffset { get { return -2f; } } }
