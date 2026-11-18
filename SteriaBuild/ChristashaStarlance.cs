using System;
using System.IO;
using Battle.DiceAttackEffect;
using HarmonyLib;
using LOR_DiceSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace Steria.Vfx
{
    internal sealed class ChristashaStarlance : IDisposable
    {
        internal const float Duration = .75f;
        private const float ContactTime = .16f;
        private const float ReferencePixel = 34f / 720f;
        private const string AtlasFolder = "ChristashaStarlanceV3";
        private static Mesh quad;
        private static Material[] materials;
        private static bool reportedLoadError;
        private static readonly int AtlasRect = Shader.PropertyToID("_MainTex_ST");
        private static readonly Layer[] Layers =
        {
            new Layer("Charge", 0, 7, -46, -52, 66, 52),
            new Layer("Lead", 3, 11, -40, -43, 40, 43),
            new Layer("Travel", 2, 30, 0, -44, 256, 44),
            new Layer("Exit", 9, 26, 0, -24, 256, 24),
            new Layer("Impact", 9, 44, -146, -160, 176, 160),
        };

        private readonly GameObject root;
        private readonly MeshRenderer[] renderers;
        private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        private readonly float pixel;
        private bool confirmed, latched;
        private float age, impactStart;
        private Vector3 struck, strikeExit, strikeAxis, strikeNormal;

        private ChristashaStarlance(float actorScale)
        {
            pixel = ReferencePixel * Mathf.Max(.1f, actorScale) / 3f;
            root = new GameObject("ChristashaStarlanceV3");
            renderers = new MeshRenderer[Layers.Length];
            try
            {
                for (int i = 0; i < Layers.Length; i++)
                {
                    var part = new GameObject(Layers[i].Name);
                    part.transform.SetParent(root.transform, false);
                    part.AddComponent<MeshFilter>().sharedMesh = quad;
                    var renderer = part.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = materials[i];
                    renderer.sortingLayerName = "Particle";
                    renderer.sortingOrder = 10 + i;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.enabled = false;
                    renderers[i] = renderer;
                }
            }
            catch
            {
                UnityEngine.Object.Destroy(root);
                throw;
            }
        }

        internal static ChristashaStarlance Create(float actorScale)
        {
            return Prepare() ? new ChristashaStarlance(actorScale) : null;
        }

        internal static bool Prepare()
        {
            if (materials != null) return true;
            var prepared = new Material[Layers.Length];
            var textures = new Texture2D[Layers.Length];
            try
            {
                string assembly = Path.GetDirectoryName(new Uri(typeof(ChristashaStarlance).Assembly.CodeBase).LocalPath);
                string directory = Path.Combine(Directory.GetParent(assembly).FullName, "Resource", "Effects", AtlasFolder);
                var shader = Shader.Find("Legacy Shaders/Particles/Additive (Soft)");
                if (shader == null)
                {
                    // This built-in shader can be cold before its first native battle effect.
                    Resources.Load<GameObject>("Prefabs/Battle/CreatureEffect/1/Fairy_Care");
                    shader = Shader.Find("Legacy Shaders/Particles/Additive (Soft)");
                }
                if (shader == null || !shader.isSupported) throw new InvalidOperationException("Starlance's native soft-additive shader is unavailable");
                for (int i = 0; i < Layers.Length; i++)
                {
                    var layer = Layers[i];
                    var texture = textures[i] = new Texture2D(2, 2, TextureFormat.RGB24, false, true)
                    {
                        name = "Starlance " + layer.Name,
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp
                    };
                    int expectedHeight = layer.StrideY * ((layer.End - layer.Start + layer.Columns) / layer.Columns);
                    if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(Path.Combine(directory, layer.Name + ".png")), true) ||
                        texture.width != layer.StrideX * layer.Columns || texture.height != expectedHeight)
                        throw new InvalidOperationException("Starlance atlas dimensions changed: " + layer.Name);
                    prepared[i] = new Material(shader) { name = "Starlance " + layer.Name, mainTexture = texture };
                }
                quad = new Mesh { name = "Starlance radiance quad" };
                quad.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up, new Vector3(1f, 1f, 0f) };
                quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
                quad.colors32 = new[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255),
                    new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) };
                quad.triangles = new[] { 0, 2, 1, 1, 2, 3 };
                quad.UploadMeshData(true);
                materials = prepared;
                return true;
            }
            catch (Exception error)
            {
                foreach (var material in prepared)
                    if (material != null) UnityEngine.Object.Destroy(material);
                foreach (var texture in textures)
                    if (texture != null) UnityEngine.Object.Destroy(texture);
                if (quad != null) UnityEngine.Object.Destroy(quad);
                quad = null;
                if (!reportedLoadError) SteriaLogger.LogError("ChristashaStarlance preparation: " + error);
                reportedLoadError = true;
                return false;
            }
        }

        internal void SetLayer(string name)
        {
            int layer = LayerMask.NameToLayer(name);
            if (layer < 0) return;
            root.layer = layer;
            foreach (var renderer in renderers) renderer.gameObject.layer = layer;
        }

        internal void Confirm()
        {
            if (confirmed || age >= Duration) return;
            confirmed = true;
            impactStart = Mathf.Max(ContactTime, age);
        }

        internal float Lifetime { get { return Duration + Mathf.Max(0f, impactStart - ContactTime); } }
        internal bool Complete { get { return age >= Lifetime; } }

        internal void Advance(float dt, Vector3 source, Vector3 contact, Vector3 exit, Camera camera)
        {
            age += Mathf.Max(0f, dt);
            Vector3 axis = contact - source;
            float distance = axis.magnitude;
            if (distance < .001f)
            {
                for (int i = 0; i < 3; i++) renderers[i].enabled = false;
                if (latched) DrawImpact();
                return;
            }
            axis /= distance;
            Vector3 depth = camera == null ? Vector3.forward : camera.transform.forward;
            Vector3 up = camera == null ? Vector3.up : camera.transform.up;
            Vector3 normal = Vector3.Cross(depth, axis).normalized;
            if (normal.sqrMagnitude < .0001f) normal = Vector3.ProjectOnPlane(up, axis).normalized;
            if (Vector3.Dot(normal, up) < 0f) normal = -normal;

            float front = Smooth(.048f, .163f, age);
            Draw(0, age, source, axis, normal, 0f);
            Draw(1, age, source + axis * (distance * front), axis, normal, 0f);
            Draw(2, age, source, axis, normal, distance * front);
            if (!confirmed || age < impactStart) return;

            if (!latched)
            {
                latched = true;
                struck = contact;
                strikeExit = exit;
                strikeAxis = axis;
                strikeNormal = normal;
            }
            DrawImpact();
        }

        private void DrawImpact()
        {
            float impactAge = age - impactStart + ContactTime;
            Vector3 exitVector = strikeExit - struck;
            float exitLength = exitVector.magnitude;
            Draw(3, impactAge, struck, exitLength < .001f ? strikeAxis : exitVector / exitLength, strikeNormal,
                exitLength * Smooth(.164f, .234f, impactAge));
            Draw(4, impactAge, struck, strikeAxis, strikeNormal, 0f);
        }

        private void Draw(int index, float time, Vector3 origin, Vector3 axis, Vector3 normal, float length)
        {
            var renderer = renderers[index];
            var layer = Layers[index];
            float frame = time * 60f;
            bool beam = index == 2 || index == 3;
            renderer.enabled = frame >= layer.Start && frame < layer.End && (!beam || length > .001f);
            if (!renderer.enabled) return;
            int from = Mathf.RoundToInt(frame);
            var texture = renderer.sharedMaterial.mainTexture;
            properties.SetVector(AtlasRect, layer.Rect(from, texture));
            renderer.SetPropertyBlock(properties);
            var tx = renderer.transform;
            tx.SetPositionAndRotation(origin + axis * (layer.Left * pixel) - normal * (layer.Bottom * pixel),
                Quaternion.LookRotation(Vector3.Cross(axis, normal), normal));
            tx.localScale = new Vector3(beam ? length : layer.Width * pixel, layer.Height * pixel, 1f);
        }

        private static float Smooth(float start, float end, float time)
        {
            float f = Mathf.InverseLerp(start, end, time);
            return f * f * (3f - 2f * f);
        }

        public void Dispose()
        {
            if (root == null) return;
            root.SetActive(false);
            UnityEngine.Object.Destroy(root);
        }

        private sealed class Layer
        {
            internal readonly string Name;
            internal readonly int Start, End, Left, Bottom, Width, Height, Columns;
            internal int StrideX { get { return Width + 4; } }
            internal int StrideY { get { return Height + 4; } }

            internal Layer(string name, int start, int end, int left, int top, int right, int bottom)
            {
                Name = name;
                Start = start;
                End = end;
                Left = left;
                Bottom = bottom;
                Width = right - left;
                Height = bottom - top;
                Columns = Math.Min(8, end - start + 1);
            }

            internal Vector4 Rect(int frame, Texture texture)
            {
                int offset = frame - Start;
                return new Vector4((float)Width / texture.width, (float)Height / texture.height,
                    (offset % Columns * StrideX + 2f) / texture.width,
                    1f - (offset / Columns * StrideY + 2f + Height) / texture.height);
            }
        }
    }

    public static class ChristashaStarlanceDefaults
    {
        internal const string Resource = "Steria_ChristashaV2PierceNear";

        internal static bool Matches(BattleUnitModel unit, DiceBehaviour dice, CardRange range, ActionDetail action, string custom)
        {
            if (dice == null || dice.Detail != BehaviourDetail.Penetrate ||
                (dice.Type != BehaviourType.Atk && dice.Type != BehaviourType.Standby) ||
                (range != CardRange.Near && range != CardRange.Far) || dice.MotionDetail == MotionDetail.F ||
                action == ActionDetail.Fire || custom == "None" || dice.EffectRes == "None" ||
                custom == "Steria_ChristashaV2PierceFar" || dice.EffectRes == "Steria_ChristashaV2PierceFar") return false;
            var id = unit == null || unit.equipment == null || unit.Book == null ? null : unit.Book.BookId;
            return id != null && id.packageId == "SteriaBuilding" &&
                (id.id == 10 || id.id == 11 || id.id == 99000009 || id.id == 99000010);
        }

        internal static string Resolve(BattleUnitView view, BattleCardBehaviourResult result, ActionDetail action, string custom)
        {
            if (result == null || !result.hasBehaviour) return custom;
            var range = result.cardModel == null ? result.diceBehaviourResult.range : result.cardModel.GetSpec().Ranged;
            return Matches(view == null ? null : view.model, result.behaviourRawData, range, action, custom) ? Resource : custom;
        }
    }

    [HarmonyPatch(typeof(RencounterManager), "PrintEffect")]
    public static class ChristashaStarlancePresentationPatch
    {
        private static readonly System.Reflection.FieldInfo Librarian = AccessTools.Field(typeof(RencounterManager), "_librarian"),
            Enemy = AccessTools.Field(typeof(RencounterManager), "_enemy"),
            LibrarianResult = AccessTools.Field(typeof(RencounterManager), "_currentLibrarianBehaviourResult"),
            EnemyResult = AccessTools.Field(typeof(RencounterManager), "_currentEnemyBehaviourResult");

        public static void Prefix(RencounterManager __instance, ActionDetail librarianActionDetail, ActionDetail enemyActionDetail,
            ref string liCustomRes, ref string enCustomRes)
        {
            liCustomRes = ChristashaStarlanceDefaults.Resolve((BattleUnitView)Librarian.GetValue(__instance),
                (BattleCardBehaviourResult)LibrarianResult.GetValue(__instance), librarianActionDetail, liCustomRes);
            enCustomRes = ChristashaStarlanceDefaults.Resolve((BattleUnitView)Enemy.GetValue(__instance),
                (BattleCardBehaviourResult)EnemyResult.GetValue(__instance), enemyActionDetail, enCustomRes);
        }
    }

    [HarmonyPatch(typeof(DiceEffectManager), "CreateAttackEffect")]
    public static class ChristashaStarlanceDirectPatch
    {
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(ref DiceAttackEffect __result, BattleUnitModel unit, float destroyTime)
        {
            var playing = unit == null ? null : unit.currentDiceAction;
            if (playing == null || playing.card == null || playing.currentBehavior == null || unit.view == null ||
                playing.target == null || playing.target.view == null) return true;
            var dice = playing.currentBehavior.behaviourInCard;
            if (!ChristashaStarlanceDefaults.Matches(unit, dice, playing.card.GetSpec().Ranged, ActionDetail.Default, null)) return true;
            float ratio = Mathf.Clamp01((float)playing.currentBehavior.DiceResultValue / Mathf.Max(1, dice.Dice));
            __result = SingletonBehavior<DiceEffectManager>.Instance.CreateBehaviourEffect(ChristashaStarlanceDefaults.Resource,
                ratio, unit.view, playing.target.view, destroyTime);
            return __result == null;
        }
    }
}
