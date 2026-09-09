using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Steria
{
    // This complete production file is copied byte-for-byte into the Unity preview fixture.
    [Serializable]
    public sealed class AnhierTextureProfile
    {
        public string name;
        public float duration, pixelsPerUnit, pivotX, pivotY;
        public float splashPixelsPerUnit = 70f, splashPivotX = .48f, splashPivotY = .49f;
        public float[] frameTimes;
        public string[] body, accent, splash;
    }

    public sealed class AnhierTextureAssets
    {
        public AnhierTextureProfile Profile { get; private set; }
        public Sprite[] Body { get; private set; }
        public Sprite[] Accent { get; private set; }
        public Sprite[] Splash { get; private set; }
        private static readonly Dictionary<string, AnhierTextureAssets> Cache = new Dictionary<string, AnhierTextureAssets>();
        private static Material _alphaMaterial;

        public static Material AlphaMaterial
        {
            get
            {
                if (_alphaMaterial == null)
                {
                    Shader shader = Shader.Find("Sprites/Default");
                    if (shader == null) throw new InvalidOperationException("AnhierTexture: Sprites/Default shader unavailable");
                    _alphaMaterial = new Material(shader) { name = "AnhierTexture_SharedRGBA" };
                }
                return _alphaMaterial;
            }
        }

        public static AnhierTextureAssets Load(string directory, string profile)
        {
            string root = Path.GetFullPath(directory);
            string key = Path.Combine(root, profile + ".json");
            AnhierTextureAssets cached;
            if (Cache.TryGetValue(key, out cached)) return cached;
            var config = JsonUtility.FromJson<AnhierTextureProfile>(File.ReadAllText(key));
            Validate(config, profile);
            var created = new List<Sprite>();
            try
            {
                var assets = new AnhierTextureAssets { Profile = config };
                assets.Body = LoadFrames(root, config.body, config, created, false);
                assets.Accent = LoadFrames(root, config.accent, config, created, false);
                assets.Splash = LoadFrames(root, config.splash, config, created, true);
                // Force material validation while still inside the rollback boundary.
                var material = AlphaMaterial;
                Cache.Add(key, assets);
                return assets;
            }
            catch
            {
                foreach (Sprite sprite in created)
                {
                    UnityEngine.Object.Destroy(sprite.texture);
                    UnityEngine.Object.Destroy(sprite);
                }
                throw;
            }
        }

        private static void Validate(AnhierTextureProfile p, string name)
        {
            if (p == null || p.name != name || p.frameTimes == null || p.body == null || p.accent == null ||
                p.frameTimes.Length < 3 || p.body.Length != p.frameTimes.Length || p.accent.Length != p.body.Length ||
                p.duration <= 0 || p.duration > 1f || p.pixelsPerUnit <= 0 ||
                p.pivotX < 0 || p.pivotX > 1 || p.pivotY < 0 || p.pivotY > 1 ||
                p.frameTimes[0] != 0 || p.frameTimes[p.frameTimes.Length - 1] >= 1f ||
                (p.splash != null && p.splash.Length != 0 && p.splash.Length != p.body.Length))
                throw new InvalidDataException("AnhierTexture: invalid profile " + name);
            for (int i = 1; i < p.frameTimes.Length; i++)
                if (!(p.frameTimes[i] > p.frameTimes[i - 1])) throw new InvalidDataException("AnhierTexture: unordered timeline " + name);
        }

        private static Sprite[] LoadFrames(string root, string[] names, AnhierTextureProfile p, List<Sprite> created, bool splash)
        {
            if (names == null || names.Length == 0) return new Sprite[0];
            var result = new Sprite[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                if (string.IsNullOrEmpty(names[i]) || Path.GetFileName(names[i]) != names[i])
                    throw new InvalidDataException("AnhierTexture: invalid frame filename");
                string path = Path.Combine(root, names[i]);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    // Preserve the authored RGBA bytes. Dark pigment is not background.
                    if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), true))
                        throw new InvalidDataException("PNG decode failed: " + path);
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    texture.name = names[i];
                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                        splash ? new Vector2(p.splashPivotX, p.splashPivotY) : new Vector2(p.pivotX, p.pivotY), splash ? p.splashPixelsPerUnit : p.pixelsPerUnit,
                        0, SpriteMeshType.FullRect);
                    sprite.name = names[i];
                    result[i] = sprite;
                    created.Add(sprite);
                }
                catch { UnityEngine.Object.Destroy(texture); throw; }
            }
            return result;
        }
    }

    // Samples native normalized event time. Every renderer stays in its action hierarchy.
    public sealed class AnhierTextureTimeline : MonoBehaviour
    {
        private AnhierTextureAssets _assets;
        private SpriteRenderer _body, _accent, _splash;
        private Transform _contactAnchor;
        public int CurrentFrame { get; private set; }
        public bool Complete { get; private set; }
        public bool AccentsEnabled { get; set; } = true;
        public float Alpha { get; private set; }

        public void Initialize(AnhierTextureAssets assets, int layer, Transform targetPivot)
        {
            _assets = assets;
            bool particleExists = false;
            foreach (SortingLayer sorting in SortingLayer.layers)
                if (sorting.name == "Particle") particleExists = true;
            if (!particleExists) throw new InvalidOperationException("AnhierTexture: native Particle sorting layer unavailable");
            _body = CreateRenderer("Pigment", layer, 0, transform);
            _accent = CreateRenderer("FoamOrFire", layer, 1, transform);
            if (assets.Splash.Length > 0)
            {
                if (targetPivot == null) throw new InvalidOperationException("AnhierTexture: water contact target missing");
                _contactAnchor = targetPivot;
                _splash = CreateRenderer("TargetWaterSplash", layer, 1, transform);
                // A sibling of the muzzle in the neutral scene effect: pressure direction
                // and native power scale are inherited once; only its world contact point
                // follows the target (the native Gun Fire/Hit separation).
            }
        }

        private static SpriteRenderer CreateRenderer(string name, int layer, int order, Transform parent)
        {
            var child = new GameObject(name) { layer = layer };
            child.transform.SetParent(parent, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = AnhierTextureAssets.AlphaMaterial;
            renderer.sortingLayerName = "Particle";
            renderer.sortingOrder = order;
            return renderer;
        }

        public void SetLayer(int layer)
        {
            _body.gameObject.layer = _accent.gameObject.layer = layer;
            if (_splash != null) _splash.gameObject.layer = layer;
        }

        public static float NativeAlpha(float normalizedTime, bool far)
        {
            // Anim_Default 7048: full through .25, smooth cubic to .666667.
            // Anim_GunFarAtk 7095 at speed 2: full through 2/3 of .5s lifetime.
            float hold = far ? 2f / 3f : .25f;
            float end = far ? 1f : 2f / 3f;
            float u = Mathf.Clamp01((normalizedTime - hold) / (end - hold));
            return 1f - u * u * (3f - 2f * u);
        }

        public void Sample(float seconds, float lifetime)
        {
            float normalized = seconds / Mathf.Max(.001f, lifetime);
            bool far = _assets.Profile.name == "SeaFarHit";
            Alpha = NativeAlpha(normalized, far);
            Complete = Alpha <= 0f;
            CurrentFrame = 0;
            for (int i = 1; i < _assets.Profile.frameTimes.Length; i++)
                if (normalized >= _assets.Profile.frameTimes[i]) CurrentFrame = i;
            SetFrame(_body, _assets.Body[CurrentFrame], Alpha);
            SetFrame(_accent, _assets.Accent[CurrentFrame], AccentsEnabled ? Alpha : 0f);
            if (_splash != null)
            {
                if (_contactAnchor != null) _splash.transform.position = _contactAnchor.position;
                SetFrame(_splash, _assets.Splash[CurrentFrame], Alpha);
            }
        }

        private static void SetFrame(SpriteRenderer renderer, Sprite sprite, float alpha)
        {
            renderer.sprite = sprite;
            renderer.color = new Color(1f, 1f, 1f, alpha);
            renderer.enabled = alpha > 0f;
        }

    }
}
