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
        public float duration, pixelsPerUnit, pivotX, pivotY, actorScale;
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
                p.duration < .35f || p.duration > .6f || p.pixelsPerUnit <= 0 ||
                p.pivotX < 0 || p.pivotX > 1 || p.pivotY < 0 || p.pivotY > 1 || p.actorScale <= 0 ||
                p.frameTimes[0] != 0 || p.frameTimes[p.frameTimes.Length - 1] >= p.duration ||
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

    // Deliberately no Update: the game adapter and fixture both sample this same timeline.
    public sealed class AnhierTextureTimeline : MonoBehaviour
    {
        private AnhierTextureAssets _assets;
        private SpriteRenderer _body, _accent, _splash;
        public float Duration { get { return _assets.Profile.duration; } }
        public int CurrentFrame { get; private set; }
        public bool Complete { get; private set; }
        public bool AccentsEnabled { get; set; } = true;

        public void Initialize(AnhierTextureAssets assets, int layer, int sortingOrder)
        {
            _assets = assets;
            _body = CreateRenderer("Pigment", layer, sortingOrder);
            _accent = CreateRenderer("FoamOrFire", layer, sortingOrder + 1);
            if (assets.Splash.Length > 0) _splash = CreateRenderer("TargetWaterSplash", layer, sortingOrder + 2);
        }

        private SpriteRenderer CreateRenderer(string name, int layer, int order)
        {
            var child = new GameObject(name) { layer = layer };
            child.transform.SetParent(transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = AnhierTextureAssets.AlphaMaterial;
            renderer.sortingOrder = order;
            return renderer;
        }

        public void Sample(float seconds, Vector3 origin, Vector3 target, bool faceLeft, float actorScale, float scaleFactor)
        {
            Complete = seconds >= Duration;
            if (Complete) { _body.enabled = _accent.enabled = false; if (_splash != null) _splash.enabled = false; return; }
            var p = _assets.Profile;
            CurrentFrame = 0;
            for (int i = 1; i < p.frameTimes.Length; i++) if (seconds >= p.frameTimes[i]) CurrentFrame = i;
            float factor = p.actorScale * Mathf.Clamp(actorScale, .55f, 1.75f) * (1f + .12f * Mathf.Clamp(scaleFactor, 0f, 2f));
            // Keep world facing independent of characterRotationCenter's inherited mirror.
            Vector3 delta = target - origin;
            float angle = Mathf.Atan2(delta.y, Mathf.Max(.001f, Mathf.Abs(delta.x))) * Mathf.Rad2Deg;
            if (p.name == "MemoryGuard") angle = 0f;
            Quaternion rotation = Quaternion.Euler(0f, 0f, faceLeft ? -angle : angle);
            // MemoryHit is an impact drawing: its authored hot-point pivot lands
            // at the same stable target root as native damaged effects.
            Vector3 location = p.name == "MemoryHit" ? target : origin;
            if (p.name == "SeaFarHit")
            {
                // A compact water head crosses the actual distance in two render frames.
                // Its PNG width stays fixed; it is never stretched into a laser.
                float travel = Mathf.Clamp01(seconds / (2f / 60f));
                location = Vector3.Lerp(origin, target, 1f - (1f - travel) * (1f - travel));
            }
            SetPose(_body, _assets.Body[CurrentFrame], location, rotation, faceLeft, factor);
            SetPose(_accent, _assets.Accent[CurrentFrame], location, rotation, faceLeft, factor);
            _accent.enabled = AccentsEnabled;
            if (_splash != null)
            {
                SetPose(_splash, _assets.Splash[CurrentFrame], target, rotation, faceLeft, factor);
                _splash.enabled = seconds >= 1f / 60f && AccentsEnabled;
            }
        }

        private static void SetPose(SpriteRenderer renderer, Sprite sprite, Vector3 position, Quaternion rotation, bool left, float scale)
        {
            renderer.enabled = true;
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.transform.position = position;
            renderer.transform.rotation = rotation;
            renderer.transform.localScale = new Vector3(left ? -scale : scale, scale, 1f);
        }
    }
}
