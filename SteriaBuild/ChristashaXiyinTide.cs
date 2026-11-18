using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Steria.Vfx
{
    internal sealed class ChristashaXiyinTide : IDisposable
    {
        internal const float ReleaseTime = .75f;
        internal const float FieldEnd = 1.67f;
        internal const float HitEnd = .635f;
        private const string Folder = "ChristashaXiyinV3";
        private static AtlasPack cached;
        private readonly GameObject root;
        private readonly AtlasPack pack;
        private readonly SpriteRenderer behind, breaker, score;
        private readonly List<Inflow> inflows = new List<Inflow>();
        private readonly List<Impact> impacts = new List<Impact>();
        private readonly Dictionary<Sprite, BodyShape> bodyShapes = new Dictionary<Sprite, BodyShape>();
        private readonly Vector3 center;
        private readonly float radius, heightRadius;
        private readonly int layer, characterLayer, lowerOrder, upperOrder;

        private ChristashaXiyinTide(Transform owner, BattleUnitView caster, List<BattleUnitView> subjects, AtlasPack art)
        {
            pack = art;
            root = new GameObject("Xiyin V3 - common tide");
            root.transform.SetParent(owner, false);
            layer = LayerMask.NameToLayer("Effect");
            if (layer < 0) layer = owner.gameObject.layer;
            characterLayer = SortingLayerOf(caster);
            lowerOrder = upperOrder = SortingOrderOf(caster);

            Vector3 min = caster.WorldPosition, max = min;
            foreach (BattleUnitView view in subjects)
            {
                min = Vector3.Min(min, view.WorldPosition);
                max = Vector3.Max(max, view.WorldPosition);
                lowerOrder = Mathf.Min(lowerOrder, SortingOrderOf(view));
                upperOrder = Mathf.Max(upperOrder, SortingOrderOf(view));
            }

            LocalBody actorBody = FindBody(caster);
            radius = Mathf.Max(10f, actorBody.Height * 1.1f, (max.x - min.x) * .57f + 2f);
            heightRadius = Mathf.Clamp(radius, actorBody.Height * 1.35f, actorBody.Height * 2.55f);
            center = (min + max) * .5f;
            center.y = Mathf.Lerp(min.y, max.y, .38f);

            behind = MakeSprite("Bed, rear staff and rear breaker", characterLayer, lowerOrder - 4);
            breaker = MakeSprite("Front breaker", characterLayer, upperOrder + 4);
            score = MakeSprite("Front staff and glints", characterLayer, upperOrder + 8);
            foreach (SpriteRenderer renderer in new[] { behind, breaker, score })
            {
                renderer.transform.position = center;
                renderer.transform.localScale = new Vector3(radius * 1280f / (453f * 500f), heightRadius * 736f / (453f * 288f), 1f);
            }
            for (int i = 0; i < subjects.Count; i++)
                inflows.Add(new Inflow(this, subjects[i], i));
            Present(0f);
        }

        internal static void Prewarm()
        {
            if (cached != null) return;
            try { cached = new AtlasPack(); }
            catch (Exception error) { SteriaLogger.LogError("Xiyin V3 atlas: " + error.Message); }
        }

        internal static ChristashaXiyinTide Create(Transform root, BattleUnitView caster, List<BattleUnitView> subjects)
        {
            Prewarm();
            return cached == null ? null : new ChristashaXiyinTide(root, caster, subjects, cached);
        }

        internal bool Complete(float time)
        {
            if (time < FieldEnd) return false;
            foreach (Impact impact in impacts)
                if (time < impact.Birth + HitEnd) return false;
            return true;
        }

        internal void Confirm(BattleUnitView view, float time)
        {
            LocalBody contact = FindBody(view);
            impacts.Add(new Impact
            {
                View = view, Birth = time, Variant = impacts.Count % 2, Contact = contact, Flip = InwardSide(contact.Foot) < 0f,
                Behind = MakeSprite("Target water back", characterLayer, lowerOrder - 1),
                Front = MakeSprite("Target water front", characterLayer, upperOrder + 2)
            });
            Present(time);
        }

        internal void Present(float time)
        {
            SetFrame(behind, pack.Field[0], time, FieldEnd);
            SetFrame(breaker, pack.Field[1], time, FieldEnd);
            SetFrame(score, pack.Field[2], time, FieldEnd);
            foreach (Inflow branch in inflows) branch.Present(time);
            foreach (Impact impact in impacts)
            {
                float age = time - impact.Birth;
                SetFrame(impact.Behind, pack.Hits[impact.Variant * 2], age, HitEnd);
                SetFrame(impact.Front, pack.Hits[impact.Variant * 2 + 1], age, HitEnd);
                if (age >= HitEnd) continue;
                LocalBody body = impact.Contact;
                if (impact.View != null && impact.View.gameObject.activeInHierarchy)
                {
                    body = FindBody(impact.View);
                    impact.Contact = body;
                }
                Vector3 scale = new Vector3(body.Width * 288f / (120f * 192f), body.Height * 240f / (150f * 160f), 1f);
                foreach (SpriteRenderer renderer in impact.Renderers)
                {
                    renderer.transform.position = body.Foot;
                    renderer.transform.localScale = scale;
                    renderer.flipX = impact.Flip;
                }
            }
        }

        public void Dispose()
        {
            if (root == null) return;
            root.SetActive(false);
            Object.Destroy(root);
        }

        private SpriteRenderer MakeSprite(string name, int sortingLayer, int order)
        {
            var obj = MakeObject(name);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = pack.Material;
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = order;
            return renderer;
        }

        private GameObject MakeObject(string name)
        {
            var obj = new GameObject(name);
            obj.layer = layer;
            obj.transform.SetParent(root.transform, false);
            return obj;
        }

        private static int SortingLayerOf(BattleUnitView view)
        {
            if (view.charSortingGroup != null) return view.charSortingGroup.sortingLayerID;
            var sprite = MainSprite(view);
            return sprite == null ? SortingLayer.NameToID("Character") : sprite.sortingLayerID;
        }

        private static int SortingOrderOf(BattleUnitView view)
        {
            if (view.charSortingGroup != null) return view.charSortingGroup.sortingOrder;
            var sprite = MainSprite(view);
            return sprite == null ? 0 : sprite.sortingOrder;
        }

        private static SpriteRenderer MainSprite(BattleUnitView view)
        {
            CharacterMotion motion = view.charAppearance == null ? null : view.charAppearance.GetCurrentMotion();
            SpriteRenderer best = null;
            float height = 0f;
            if (motion != null && motion.motionSpriteSet != null)
            {
                foreach (SpriteSet set in motion.motionSpriteSet)
                {
                    var renderer = set.sprRenderer;
                    if (renderer == null || !renderer.enabled || renderer.sprite == null) continue;
                    if (set.sprType == CharacterAppearanceType.Skin) return renderer;
                    if (renderer.bounds.size.y <= height) continue;
                    height = renderer.bounds.size.y;
                    best = renderer;
                }
            }
            return best;
        }

        private LocalBody FindBody(BattleUnitView view)
        {
            var renderer = MainSprite(view);
            if (renderer == null)
            {
                float unit = view.characterRotationCenter == null ? 3f : Mathf.Abs(view.characterRotationCenter.lossyScale.y);
                unit = Mathf.Clamp(unit, 1f, 6f);
                return new LocalBody(view.WorldPosition, unit * 1.95f, unit * 3.25f);
            }

            Sprite sprite = renderer.sprite;
            if (!bodyShapes.TryGetValue(sprite, out BodyShape shape))
            {
                shape = BodyShape.FromSprite(sprite);
                bodyShapes.Add(sprite, shape);
            }
            Vector3 scale = renderer.transform.lossyScale;
            Vector3 anchor = renderer.transform.TransformPoint(shape.X * (renderer.flipX ? -1f : 1f),
                shape.Bottom * (renderer.flipY ? -1f : 1f), 0f);
            anchor.z = view.WorldPosition.z;
            return new LocalBody(anchor, Mathf.Max(.65f, Mathf.Abs(shape.Width * scale.x)), Mathf.Max(1f, Mathf.Abs(shape.Height * scale.y)));
        }

        private float InwardSide(Vector3 position) { return position.x < center.x ? 1f : -1f; }

        private static void SetFrame(SpriteRenderer renderer, Sprite[] frames, float age, float end)
        {
            renderer.enabled = age < end;
            if (renderer.enabled) renderer.sprite = frames[Mathf.Clamp(Mathf.RoundToInt(age * 30f), 0, frames.Length - 1)];
        }

        private readonly struct LocalBody
        {
            internal readonly Vector3 Foot;
            internal readonly float Width, Height;
            internal LocalBody(Vector3 foot, float width, float height) { Foot = foot; Width = width; Height = height; }
        }

        private readonly struct BodyShape
        {
            internal readonly float X, Bottom, Width, Height;
            private BodyShape(float x, float bottom, float width, float height) { X = x; Bottom = bottom; Width = width; Height = height; }

            internal static BodyShape FromSprite(Sprite sprite)
            {
                Bounds bounds = sprite.bounds;
                var fallback = new BodyShape(bounds.center.x, bounds.min.y, bounds.size.x * .64f, bounds.size.y);
                Texture2D texture = sprite.texture;
                if (!texture.isReadable || sprite.packed || sprite.rect.width < 20f || sprite.rect.height < 20f) return fallback;

                Rect rect = sprite.rect;
                var pixels = texture.GetPixels32();
                int stride = Mathf.Max(1, (int)Mathf.Max(rect.width, rect.height) / 140);
                int x0 = (int)rect.x, y0 = (int)rect.y, width = (int)rect.width, height = (int)rect.height;
                int low = height, high = 0;
                for (int y = 0; y < height; y += stride)
                for (int x = 0; x < width; x += stride)
                {
                    if (pixels[(y + y0) * texture.width + x + x0].a < 90) continue;
                    low = Mathf.Min(low, y);
                    high = Mathf.Max(high, y);
                }
                if (high <= low) return fallback;

                var torso = new List<int>();
                int torsoBottom = Mathf.RoundToInt(Mathf.Lerp(low, high, .37f));
                int torsoTop = Mathf.RoundToInt(Mathf.Lerp(low, high, .66f));
                for (int y = torsoBottom; y <= torsoTop; y += stride)
                for (int x = 0; x < width; x += stride)
                {
                    if (pixels[(y + y0) * texture.width + x + x0].a >= 90) torso.Add(x);
                }
                if (torso.Count == 0) return fallback;
                torso.Sort();
                int left = torso[torso.Count / 16], right = torso[torso.Count * 15 / 16];
                float ppu = sprite.pixelsPerUnit;
                return new BodyShape((torso[torso.Count / 2] - sprite.pivot.x) / ppu,
                    (low - sprite.pivot.y) / ppu, (right - left + stride) / ppu, (high - low + stride) / ppu);
            }
        }

        private sealed class Impact
        {
            internal BattleUnitView View;
            internal float Birth;
            internal int Variant;
            internal bool Flip;
            internal LocalBody Contact;
            internal SpriteRenderer Behind, Front;
            internal IEnumerable<SpriteRenderer> Renderers { get { yield return Behind; yield return Front; } }
        }

        private sealed class Inflow
        {
            private const int Rows = 32, Columns = 7;
            private readonly ChristashaXiyinTide field;
            private readonly BattleUnitView view;
            private readonly Mesh mesh;
            private readonly MeshRenderer renderer;
            private readonly float birth, side;
            private readonly int seed;
            private readonly Vector3[] vertices = new Vector3[Rows * Columns];
            private readonly Color32[] colors = new Color32[Rows * Columns];
            private LocalBody incoming;

            internal Inflow(ChristashaXiyinTide field, BattleUnitView view, int index)
            {
                this.field = field;
                this.view = view;
                birth = index % 2 == 0 ? .206f : .155f;
                incoming = field.FindBody(view);
                side = field.InwardSide(incoming.Foot);
                seed = index;
                var obj = field.MakeObject("Incoming shallows " + index);
                mesh = new Mesh { name = "Moving shallow water" };
                mesh.MarkDynamic();
                var filter = obj.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                obj.AddComponent<XiyinOwnedMesh>().Resource = mesh;
                renderer = obj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = field.pack.Material;
                renderer.sortingLayerID = field.characterLayer;
                renderer.sortingOrder = field.upperOrder + 1;
                mesh.vertices = vertices;
                mesh.colors32 = colors;
                mesh.triangles = StripTriangles();
            }

            internal void Present(float time)
            {
                renderer.enabled = view != null && view.gameObject.activeInHierarchy && time > birth && time < 1.13f;
                if (!renderer.enabled) return;

                if (time < ReleaseTime - .00005f) incoming = field.FindBody(view);
                var body = incoming;
                Vector3 touch = body.Foot + new Vector3(-side * body.Width * .39f, body.Height * .44f, 0f);
                Vector3 start = body.Foot + new Vector3(-side * body.Width * .7f, -.055f * field.heightRadius, 0f);
                float outer = field.center.x - side * field.radius * 1.02f;
                start.x = side > 0f ? Mathf.Min(start.x, outer) : Mathf.Max(start.x, outer);
                Vector3 throat = field.center + new Vector3(-.035f * side * field.radius, -.018f * field.heightRadius, 0f);
                float head = (time - birth) / (ReleaseTime - birth);
                float tail = Mathf.Max(0f, head - .43f);
                float strength = Ramp(birth, birth + .13f, time) * (1f - Ramp(.79f, 1.13f, time));

                for (int i = 0; i < Rows; i++)
                {
                    float u = i / (Rows - 1f);
                    float parcel = Mathf.Lerp(tail, head, u);
                    Vector3 center = FlowPoint(start, touch, throat, parcel, body.Height);
                    Vector3 direction = (FlowPoint(start, touch, throat, parcel + .008f, body.Height) - center).normalized;
                    Vector3 across = new Vector3(-direction.y, direction.x, 0f);
                    float cap = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * u)), .68f);
                    float thickness = body.Height * (.013f + .026f * Ramp(.31f, 1f, parcel)) * cap;
                    float ripple = Mathf.Sin(15f * parcel - 11f * time + seed);
                    center += across * (thickness * .26f * ripple);
                    for (int j = 0; j < Columns; j++)
                    {
                        float acrossU = 2f * j / (Columns - 1) - 1f;
                        float white = Mathf.Pow(Mathf.Max(0f, .5f + .5f * Mathf.Cos(42f * parcel + 11f * acrossU - 13f * time + seed)), 5f);
                        float edge = Mathf.Max(0f, 1f - acrossU * acrossU);
                        float foam = white * Mathf.Exp(-Mathf.Pow((acrossU - .36f) * 2.8f, 2f));
                        int k = i * Columns + j;
                        vertices[k] = center + across * (thickness * acrossU);
                        colors[k] = new Color(1f, .66f + .30f * foam, .33f + .61f * foam, strength * cap * edge * (.23f + .47f * foam));
                    }
                }
                mesh.vertices = vertices;
                mesh.colors32 = colors;
                mesh.RecalculateBounds();
            }

            private static Vector3 FlowPoint(Vector3 start, Vector3 touch, Vector3 throat, float parcel, float height)
            {
                if (parcel <= 1f)
                {
                    float climb = parcel * parcel;
                    return new Vector3(Mathf.Lerp(start.x, touch.x, parcel), Mathf.Lerp(start.y, touch.y, climb), touch.z);
                }
                float drift = Mathf.Clamp01((parcel - 1f) / .75f);
                return new Vector3(Mathf.Lerp(touch.x, throat.x, drift),
                    Mathf.Lerp(touch.y, throat.y, drift) + height * .07f * Mathf.Sin(Mathf.PI * drift), Mathf.Lerp(touch.z, throat.z, drift));
            }

            private static float Ramp(float a, float b, float time)
            {
                float s = Mathf.InverseLerp(a, b, time);
                return s * s * (3f - 2f * s);
            }

            private static int[] StripTriangles()
            {
                var indices = new int[(Rows - 1) * (Columns - 1) * 6];
                int i = 0;
                for (int row = 0; row < Rows - 1; row++)
                for (int column = 0; column < Columns - 1; column++)
                {
                    int n = row * Columns + column;
                    indices[i++] = n; indices[i++] = n + 1; indices[i++] = n + Columns;
                    indices[i++] = n + 1; indices[i++] = n + Columns + 1; indices[i++] = n + Columns;
                }
                return indices;
            }
        }

        private sealed class AtlasPack
        {
            internal readonly Sprite[][] Field = new Sprite[3][];
            internal readonly Sprite[][] Hits = new Sprite[4][];
            internal readonly Material Material;
            private readonly List<Object> loaded = new List<Object>();

            internal AtlasPack()
            {
                string assembly = Path.GetDirectoryName(new Uri(typeof(ChristashaXiyinTide).Assembly.CodeBase).LocalPath);
                string path = Path.Combine(Directory.GetParent(assembly).FullName, "Resource", "Effects", Folder);
                var shader = Shader.Find("Sprites/Default");
                if (shader == null || !shader.isSupported) throw new InvalidOperationException("Native sprite shader unavailable");
                try
                {
                    Material = new Material(shader) { name = "Xiyin transparent water" };
                    loaded.Add(Material);
                    var worldPivot = new Vector2(646f / 1280f, 300f / 736f);
                    string[] field = { "Behind", "Breaker", "Score" };
                    for (int i = 0; i < field.Length; i++)
                        Field[i] = Read(path, field[i], 500, 288, 4, 7, 51, worldPivot);
                    var hitPivot = new Vector2(126f / 288f, 62f / 240f);
                    string[] hits = { "HitBack0", "HitFront0", "HitBack1", "HitFront1" };
                    for (int i = 0; i < hits.Length; i++)
                        Hits[i] = Read(path, hits[i], 192, 160, 6, 4, 20, hitPivot);
                }
                catch
                {
                    foreach (Object resource in loaded) Object.Destroy(resource);
                    throw;
                }
            }

            private Sprite[] Read(string path, string name, int width, int height, int cols, int rows, int frames, Vector2 pivot)
            {
                var result = new Sprite[frames];
                Texture2D texture = null;
                int capacity = cols * rows;
                for (int i = 0; i < frames; i++)
                {
                    int cell = i % capacity;
                    if (cell == 0)
                    {
                        texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                        {
                            name = name + " page " + i / capacity,
                            filterMode = FilterMode.Bilinear,
                            wrapMode = TextureWrapMode.Clamp
                        };
                        loaded.Add(texture);
                        if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(Path.Combine(path, name + "-" + i / capacity + ".png")), true))
                            throw new InvalidDataException("Cannot decode " + name);
                    }
                    var bounds = new Rect(cell % cols * (width + 4) + 2,
                        texture.height - cell / cols * (height + 4) - height - 2, width, height);
                    result[i] = Sprite.Create(texture, bounds, pivot, 1f, 0, SpriteMeshType.FullRect);
                    loaded.Add(result[i]);
                }
                return result;
            }
        }
    }

    internal sealed class XiyinOwnedMesh : MonoBehaviour
    {
        internal Mesh Resource;
        private void OnDestroy() { if (Resource != null) Object.Destroy(Resource); }
    }
}
