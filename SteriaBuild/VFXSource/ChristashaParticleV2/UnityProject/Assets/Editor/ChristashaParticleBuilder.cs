using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Steria.Vfx;
using Object = UnityEngine.Object;

public static class ChristashaParticleBuilder
{
    const string Root = "Assets/ChristashaV2/";
    const int Width = 1280, Height = 720;
    static readonly string[] Kinds = { "Slash", "Hit", "Pierce", "Projectile", "Cast", "Area", "Point" };
    static readonly string[] Roles = { "Body", "Edge", "Spray", "Mist", "Glint" };
    static string Output;
    static bool FullGpuAudit;
    static string SequenceRootName = "gpu-sequence-ab";
    static readonly List<Object> Temporary = new List<Object>();

    public static void BuildAndPreview()
    {
        try
        {
            Require(Application.unityVersion == "2019.3.15f1", "exact Unity");
            Require(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11, "D3D11 GPU required");
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-v2Revision");
            string revision = index >= 0 ? args[index + 1] : "candidate-r1";
            Output = Path.GetFullPath("../evidence/" + revision);
            Directory.CreateDirectory(Output);
            CreateAssets();
            string[] paths = Kinds.Select(k => Root + "Prefabs/ChristashaV2_" + k + ".prefab").ToArray();
            Directory.CreateDirectory("AssetBundles");
            var manifest = BuildPipeline.BuildAssetBundles("AssetBundles", new[] { new AssetBundleBuild { assetBundleName = ChristashaParticleVisual.BundleName, assetNames = paths } }, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows64);
            Require(manifest != null, "bundle build");
            var bundle = AssetBundle.LoadFromFile(Path.GetFullPath("AssetBundles/" + ChristashaParticleVisual.BundleName));
            Require(bundle != null && bundle.GetAllAssetNames().Length == 7, "seven native bundle roots");
            var prefabs = new Dictionary<string, GameObject>();
            foreach (string kind in Kinds)
            {
                GameObject prefab = bundle.LoadAsset<GameObject>("ChristashaV2_" + kind);
                Require(prefab != null, "bundle prefab " + kind);
                Require(prefab.GetComponentsInChildren<MonoBehaviour>(true).Length == 0, "no custom or missing script");
                Require(prefab.GetComponentsInChildren<ParticleSystem>(true).Length == 5, "five role systems " + kind);
                foreach (var renderer in prefab.GetComponentsInChildren<ParticleSystemRenderer>(true))
                    Require(renderer.sharedMaterial != null && renderer.sharedMaterial.shader.isSupported, "supported material " + kind);
                prefabs[kind] = prefab;
                VerifyPlayback(prefab, kind);
            }
            File.WriteAllText(Path.Combine(Output, "bundle-sha256.txt"), Sha("AssetBundles/" + ChristashaParticleVisual.BundleName));
            ExportAttacks(prefabs);
            ExportProjectilePath(prefabs);
            ExportMass(prefabs);
            ExportGuardExample();
            File.WriteAllText(Path.Combine(Output, "native-readback.txt"), "Unity=" + Application.unityVersion + "\nGPU=" + SystemInfo.graphicsDeviceName + "\nAPI=" + SystemInfo.graphicsDeviceType + "\nExplicit roots=" + string.Join(",", bundle.GetAllAssetNames()) + "\nPreview=Unity proxy battle, not game capture\nClock=canonical ChristashaParticleVisual.Advance, 60fps input, 120Hz particle steps\n");
            bundle.Unload(true);
            AssetDatabase.SaveAssets();
            Debug.Log("CHRISTASHA_V2_NATIVE_PASS");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    static void CreateAssets()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(Root + "Shaders/ParticleAlpha.shader");
        Require(shader != null, "original generic particle shader");
        Texture2D stroke = Texture("StrokeBrush", 512, 128, (u, v) =>
        {
            float irregular = 0.09f * (Mathf.PerlinNoise(u * 19f + 5, 3.5f) - 0.5f) + 0.055f * (Mathf.PerlinNoise(u * 53f + 2, 1.4f) - 0.5f);
            float inner = Mathf.SmoothStep(0, 1, (v - 0.04f - irregular) / 0.11f);
            float outer = Mathf.SmoothStep(0, 1, (1f - v) / 0.035f);
            float flow = v + 0.06f * Mathf.Sin(u * 9f) + 0.022f * Mathf.Sin(u * 23f);
            float grain = Mathf.PerlinNoise(u * 9.2f + 7.2f, flow * 6.1f + 3.7f);
            float fiber = Mathf.PerlinNoise(u * 8.3f + 13f, flow * 28f + 17f);
            Color c = Color.Lerp(new Color(0.49f, 0.30f, 0.10f), new Color(0.93f, 0.745f, 0.33f), Mathf.SmoothStep(0, 1, v * 1.8f));
            c = Color.Lerp(c, new Color(1, 0.957f, 0.77f), Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.84f, 0.97f, v)) * 0.85f);
            c *= Mathf.Lerp(0.76f, 1.07f, Mathf.SmoothStep(0, 1, grain));
            c = Color.Lerp(c, new Color(1f, 0.91f, 0.59f), Mathf.Max(0, fiber - 0.57f) * 0.85f);
            float broken = Mathf.Lerp(Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.32f, 0.58f, grain)), 1, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.23f, 0.80f, v)));
            c.a = inner * outer * broken * 0.97f;
            return c;
        });
        Texture2D leaf = Texture("SprayLeaf", 128, 64, (u, v) =>
        {
            float radius = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 0.85f) * (0.39f - 0.2f * u);
            float axis = 0.49f + 0.06f * Mathf.Sin(u * Mathf.PI);
            float a = Mathf.SmoothStep(0, 1, (radius - Mathf.Abs(v - axis)) / 0.07f);
            return new Color(1, 1, 1, a);
        });
        Texture2D mist = Texture("MistGrain", 128, 128, (u, v) =>
        {
            float x = (u - 0.48f) * 2, y = (v - 0.5f) * 2;
            float a = Mathf.Clamp01(1f - x * x - y * y);
            a = a * a * (0.6f + 0.23f * Mathf.Sin(u * 19 + v * 7) * Mathf.Sin(v * 24 - u * 9));
            return new Color(1, 1, 1, a);
        });
        var mats = new Dictionary<string, Material>();
        foreach (string role in Roles)
        {
            var material = new Material(shader) { name = "V2_" + role };
            material.mainTexture = role == "Body" ? stroke : role == "Mist" ? mist : leaf;
            material.SetFloat("_Stroke", role == "Body" ? 1 : 0);
            string p = Root + "Materials/V2_" + role + ".mat";
            Save(material, p); mats[role] = AssetDatabase.LoadAssetAtPath<Material>(p);
        }
        foreach (string kind in Kinds.Concat(new[] { "Guard" }))
        {
            Mesh mesh = Shape(kind);
            string path = Root + "Meshes/" + kind + ".asset";
            Save(mesh, path);
            mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var root = new GameObject("ChristashaV2_" + kind);
            foreach (string role in Roles)
            {
                var child = new GameObject(role); child.transform.SetParent(root.transform, false);
                var ps = child.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.playOnAwake = false; main.loop = false; main.duration = 1.2f;
                main.startLifetime = 0.4f; main.startSpeed = 0; main.startSize3D = true;
                main.startSizeX = 1; main.startSizeY = 1; main.startSizeZ = 1;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.maxParticles = role == "Body" ? 8 : 96;
                var emission = ps.emission; emission.enabled = false;
                var shape = ps.shape; shape.enabled = false;
                var col = ps.colorOverLifetime; col.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, role == "Body" ? 0.45f : 0.18f), new GradientAlphaKey(0, 1) });
                col.color = new ParticleSystem.MinMaxGradient(gradient);
                var size = ps.sizeOverLifetime; size.enabled = true; size.separateAxes = false;
                size.size = new ParticleSystem.MinMaxCurve(1, role == "Body" ? new AnimationCurve(new Keyframe(0, 0.68f), new Keyframe(0.20f, 1), new Keyframe(0.65f, 1), new Keyframe(1, 0.94f)) : new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, role == "Mist" ? 1.6f : 0.3f)));
                var custom = ps.customData; custom.enabled = true; custom.SetMode(ParticleSystemCustomData.Custom1, ParticleSystemCustomDataMode.Vector); custom.SetVectorComponentCount(ParticleSystemCustomData.Custom1, 1); custom.SetVector(ParticleSystemCustomData.Custom1, 0, new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0, 1, 1)));
                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = mats[role];
                if (kind == "Guard" && role == "Body")
                {
                    string guardMaterialPath = Root + "Materials/V2_GuardExample.mat";
                    var guardMaterial = new Material(mats[role]); guardMaterial.SetFloat("_Guard", 1);
                    Save(guardMaterial, guardMaterialPath); renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(guardMaterialPath);
                }
                renderer.renderMode = role == "Body" ? ParticleSystemRenderMode.Mesh : ParticleSystemRenderMode.Billboard;
                if (role == "Body") renderer.mesh = mesh;
                renderer.alignment = ParticleSystemRenderSpace.Local;
                renderer.sortingLayerName = "Particle";
                renderer.sortingOrder = role == "Mist" ? 0 : role == "Body" ? 2 : role == "Edge" ? 3 : 4;
                renderer.SetActiveVertexStreams(new List<ParticleSystemVertexStream> { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV, ParticleSystemVertexStream.Custom1X });
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            PrefabUtility.SaveAsPrefabAsset(root, Root + "Prefabs/" + root.name + ".prefab");
            Object.DestroyImmediate(root);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static Texture2D Texture(string name, int width, int height, Func<float, float, Color> pixel)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var colors = new Color[width * height];
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) colors[y * width + x] = pixel(x / (float)(width - 1), y / (float)(height - 1));
        tex.SetPixels(colors); tex.Apply();
        string path = Root + "Textures/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true; importer.sRGBTexture = true;
        importer.mipmapEnabled = false; importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.isReadable = false; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Mesh Shape(string kind)
    {
        const int segments = 96;
        var vertices = new Vector3[(segments + 1) * 2]; var uv = new Vector2[vertices.Length]; var triangles = new int[segments * 6];
        for (int i = 0; i <= segments; i++)
        {
            float u = i / (float)segments;
            Vector2 center, normal; float width;
            if (kind == "Slash")
            {
                float a = Mathf.Lerp(91, 266, u) * Mathf.Deg2Rad;
                center = new Vector2(Mathf.Cos(a) * 0.70f - 0.05f, Mathf.Sin(a) * 0.86f);
                normal = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                width = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 0.65f) * Mathf.Lerp(0.035f, 0.27f, Mathf.Pow(u, 0.7f));
            }
            else if (kind == "Guard")
            {
                float a = Mathf.Lerp(95, 265, u) * Mathf.Deg2Rad;
                center = new Vector2(Mathf.Cos(a) * 0.85f, Mathf.Sin(a) * 1.38f); normal = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                width = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 0.85f) * 0.14f;
            }
            else if (kind == "Hit")
            {
                center = new Vector2(Mathf.Lerp(0.40f, -1.02f, u), 0.04f * Mathf.Sin(u * 8f)); normal = Vector2.up;
                width = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 0.38f) * Mathf.Lerp(0.025f, 0.65f, u);
            }
            else if (kind == "Pierce" || kind == "Projectile")
            {
                center = new Vector2(Mathf.Lerp(0.80f, -1.35f, u), 0.016f * Mathf.Sin(u * 7f)); normal = Vector2.up;
                width = Mathf.Sin(Mathf.PI * u) * Mathf.Lerp(0.025f, 0.20f, u);
            }
            else if (kind == "Cast")
            {
                float a = Mathf.Lerp(25, 329, u) * Mathf.Deg2Rad;
                center = new Vector2(Mathf.Cos(a) * 1.3f, Mathf.Sin(a) * 0.25f); normal = new Vector2(Mathf.Cos(a) * 0.45f, Mathf.Sin(a));
                width = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 0.6f) * (0.075f + 0.045f * Mathf.Sin(u * 10));
            }
            else if (kind == "Area")
            {
                center = new Vector2(Mathf.Lerp(2.65f, -2.65f, u), 0.13f + 0.40f * Mathf.Sin(u * Mathf.PI) + 0.14f * Mathf.Sin(u * 11f));
                normal = Vector2.up; width = Mathf.Pow(Mathf.Sin(Mathf.PI * u), 0.7f) * (0.10f + 0.44f * u);
            }
            else
            {
                center = new Vector2(Mathf.Lerp(0.28f, -0.39f, u), 0.02f); normal = Vector2.up;
                width = Mathf.Sin(Mathf.PI * u) * 0.15f;
            }
            float ripple = 0.72f + 0.65f * Mathf.PerlinNoise(u * 17.7f + 3, 4.7f);
            Vector2 inner = center - normal * width * ripple;
            Vector2 outer = center + normal * width * (0.82f + 0.025f * Mathf.Sin(u * 29f));
            vertices[i * 2] = inner; vertices[i * 2 + 1] = outer;
            uv[i * 2] = new Vector2(u, 0); uv[i * 2 + 1] = new Vector2(u, 1);
            if (i < segments)
            {
                int t = i * 6, v = i * 2;
                triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                triangles[t + 3] = v + 1; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
            }
        }
        var mesh = new Mesh { name = "V2_" + kind, vertices = vertices, uv = uv, triangles = triangles };
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }

    static void VerifyPlayback(GameObject prefab, string kind)
    {
        var instance = Object.Instantiate(prefab); int children = instance.GetComponentsInChildren<Transform>().Length;
        var driver = ChristashaParticleVisual.Play(instance, 194);
        Require(driver.LiveParticleCount > 0, "visible event first frame " + kind);
        var bodyRenderer = instance.transform.Find("Body").GetComponent<ParticleSystemRenderer>();
        Require(bodyRenderer.sortingLayerName == "Particle", "sorting name resolved in this runtime " + kind);
        Color sharedTint = bodyRenderer.sharedMaterial.GetColor("_Tint");
        Color testTint = new Color(0.85f, 0.93f, 1f, 1);
        driver.Tint = testTint;
        var block = new MaterialPropertyBlock(); bodyRenderer.GetPropertyBlock(block);
        Require(block.GetColor("_Tint") == testTint && bodyRenderer.sharedMaterial.GetColor("_Tint") == sharedTint, "existing particles use isolated tint " + kind);
        driver.Tint = Color.white;
        driver.Advance(0.1f);
        var body = instance.transform.Find("Body").GetComponent<ParticleSystem>();
        var particles = new ParticleSystem.Particle[8]; int n = body.GetParticles(particles);
        Require(n == 1 && particles[0].remainingLifetime < particles[0].startLifetime, "native particle life advances " + kind);
        int count = driver.LiveParticleCount;
        var first = n > 0 ? particles[0].position : Vector3.zero;
        driver.StopAndClear(); Require(driver.LiveParticleCount == 0, "cancel clears " + kind);
        var replay = ChristashaParticleVisual.Play(instance, 194); replay.Advance(0.1f);
        body.GetParticles(particles);
        Require(replay.LiveParticleCount == count && (particles[0].position - first).sqrMagnitude < 0.000001f, "same seed native replay " + kind);
        replay.Advance(3f); Require(replay.LiveParticleCount == 0, "completion clear " + kind);
        var faster = ChristashaParticleVisual.Play(instance, 194, 2);
        faster.Advance(0.05f);
        Require(faster.LiveParticleCount == count && Mathf.Abs(faster.Duration * 2 - driver.Duration) < 0.000001f, "playbackSpeed uses the same clock " + kind);
        faster.StopAndClear();
        Require(instance.GetComponentsInChildren<Transform>().Length == children, "replay no child growth " + kind);
        File.AppendAllText(Path.Combine(Output, "playback-checks.txt"), kind + ": first frame / count at .1=" + count + "; position=" + first.ToString("F6") + "; cancel / replay / duration clear / no child growth / speed2 / isolated tint / sorting PASS\n");
        Object.DestroyImmediate(instance);
    }

    static Camera Camera(Color background)
    {
        var camera = new GameObject("ProxyCamera").AddComponent<Camera>(); Temporary.Add(camera.gameObject);
        camera.orthographic = true; camera.orthographicSize = 4.6f; camera.aspect = Width / (float)Height;
        camera.transform.position = new Vector3(0, 0.3f, -20); camera.nearClipPlane = 0.1f; camera.farClipPlane = 50;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = background; camera.allowHDR = false; camera.allowMSAA = false;
        camera.cullingMask = 1 << 28;
        foreach (var renderer in Object.FindObjectsOfType<Renderer>())
            if (renderer.gameObject.layer != 28) File.AppendAllText(Path.Combine(Output, "excluded-scene-renderers.txt"), renderer.name + " layer=" + renderer.gameObject.layer + " scene=" + renderer.gameObject.scene.name + "\n");
        var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 }; rt.Create(); Temporary.Add(rt); camera.targetTexture = rt;
        return camera;
    }
    static void ExportAttacks(Dictionary<string, GameObject> prefabs, string[] selectedKinds = null, bool renderEveryFrame = true, bool onlyMid = false)
    {
        foreach (string kind in selectedKinds ?? new[] { "Slash", "Hit", "Pierce", "Projectile", "Point", "Cast", "Area" })
        foreach (bool light in onlyMid ? new[] { true } : new[] { false, true })
        {
            string folder = Path.Combine(Output, kind + (light ? "-mid" : "-dark")); Directory.CreateDirectory(folder);
            var camera = Camera(light ? new Color(0.40f, 0.40f, 0.37f) : new Color(0.09f, 0.12f, 0.15f));
            var visual = new List<ChristashaParticleVisual>();
            bool melee = kind == "Slash" || kind == "SlashV" || kind == "Hit" || kind == "Pierce";
            for (int specimen = 0; specimen < 2; specimen++)
            {
                float x = specimen == 0 ? -3.8f : 3.8f, map = specimen == 0 ? 3f : 2.25f;
                Actor(new Vector2(x + (specimen == 0 ? 1.25f : -1.25f), -1.85f), map / 3f, specimen != 0);
                var instance = Object.Instantiate(prefabs[kind]); Temporary.Add(instance);
                Isolate(instance);
                instance.transform.position = new Vector3(x, kind == "Cast" || kind == "Area" ? -1.85f : -0.35f, -1);
                instance.transform.localScale = melee ? new Vector3(map * 2, map * 2, map * 2) : Vector3.one * (kind == "Area" ? 0.52f : kind == "Guard" ? map : 1f);
                if (specimen != 0) instance.transform.localRotation = Quaternion.Euler(0, 180, 0);
                if (kind == "SlashV") instance.transform.localRotation *= Quaternion.Euler(0, 0, 70);
                visual.Add(ChristashaParticleVisual.Play(instance, specimen == 0 ? 194u : 781u));
            }
            var stats = new StringBuilder("frame,time,leftParticles,rightParticles\n");
            var nativeStats = new StringBuilder("frame,time,specimen,role,count,firstX,firstY,velocityX,velocityY,startLife,remainingLife,rotationZ\n");
            int count = kind == "Area" ? 74 : 46;
            for (int frame = 0; frame < count; frame++)
            {
                if (frame > 0) foreach (var v in visual) v.Advance(1f / 60);
                if (!light || new[] { 0, 1, 3, 6, 11, 21, 36, 45, 66, 73 }.Contains(frame)) Capture(camera, Path.Combine(folder, frame.ToString("D4") + ".png"));
                else if (renderEveryFrame) camera.Render();
                stats.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0},{1:F6},{2},{3}\n", frame, frame / 60f, visual[0].LiveParticleCount, visual[1].LiveParticleCount);
                int ordinal = 0;
                foreach (var instance in Temporary.OfType<GameObject>().Where(o => o != null && o.name.StartsWith("ChristashaV2_")))
                {
                    foreach (var system in instance.GetComponentsInChildren<ParticleSystem>())
                    {
                        var particles = new ParticleSystem.Particle[system.main.maxParticles]; int n = system.GetParticles(particles);
                        var firstParticle = n > 0 ? particles[0] : new ParticleSystem.Particle();
                        nativeStats.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0},{1:F6},{2},{3},{4},{5:F6},{6:F6},{7:F6},{8:F6},{9:F6},{10:F6},{11:F6}\n", frame, frame / 60f, ordinal, system.name, n, firstParticle.position.x, firstParticle.position.y, firstParticle.velocity.x, firstParticle.velocity.y, firstParticle.startLifetime, firstParticle.remainingLifetime, firstParticle.rotation3D.z);
                        if (FullGpuAudit && frame == 3)
                            for (int j = 0; j < n; j++) File.AppendAllText(Path.Combine(folder, "f3-current-particles.txt"), "specimen=" + ordinal + " role=" + system.name + " particle=" + j + " age=" + (particles[j].startLifetime - particles[j].remainingLifetime).ToString("F6") + " startSize=" + particles[j].startSize3D.ToString("F6") + " currentSize=" + particles[j].GetCurrentSize3D(system).ToString("F6") + " startColor=" + particles[j].startColor + " currentColor=" + particles[j].GetCurrentColor(system) + " position=" + particles[j].position.ToString("F6") + "\n");
                    }
                    ordinal++;
                }
            }
            File.WriteAllText(Path.Combine(folder, "particles.csv"), stats.ToString());
            File.WriteAllText(Path.Combine(folder, "native-particles.csv"), nativeStats.ToString());
            Cleanup();
        }
    }
    static void ExportMass(Dictionary<string, GameObject> prefabs)
    {
        string folder = Path.Combine(Output, "Mass-dark"); Directory.CreateDirectory(folder);
        var camera = Camera(new Color(0.09f, 0.12f, 0.15f));
        Actor(new Vector2(6.0f, -1.85f), 1, false);
        float[] targetX = { -5.4f, -2.4f, 0.7f, 3.2f };
        foreach (float x in targetX) Actor(new Vector2(x, -1.85f), 0.88f, true);
        var active = new List<ChristashaParticleVisual>();
        var cast = Object.Instantiate(prefabs["Cast"]); Temporary.Add(cast); Isolate(cast); cast.transform.position = new Vector3(6, -1.85f, -1); active.Add(ChristashaParticleVisual.Play(cast, 194));
        for (int f = 0; f < 108; f++)
        {
            if (f > 0) foreach (var visual in active) visual.Advance(1f / 60);
            if (f == 18)
            {
                var area = Object.Instantiate(prefabs["Area"]); Temporary.Add(area); Isolate(area); area.transform.position = new Vector3(0.3f, -1.85f, -1); area.transform.localScale = new Vector3(13.4f / 13f, 1, 1); active.Add(ChristashaParticleVisual.Play(area, 194));
            }
            if (f == 30)
            {
                for (int i = 0; i < 3; i++)
                {
                    var point = Object.Instantiate(prefabs["Point"]); Temporary.Add(point); Isolate(point); point.transform.position = new Vector3(targetX[i], -0.15f, -1); active.Add(ChristashaParticleVisual.Play(point, (uint)(194 + i)));
                }
            }
            Capture(camera, Path.Combine(folder, f.ToString("D4") + ".png"));
        }
        File.WriteAllText(Path.Combine(folder, "timeline.txt"), "Unity proxy only. Actual adapter timing: Cast t=0; Area t=.3; success Point at earliest manager release t=.5, X=-5.4,-2.4,0.7. Fourth target X=3.2 is unsuccessful and receives no Point. Area root x=.3 scaleX=(6-(-5.4)+2)/13. No game damage is simulated.\n");
        Cleanup();
    }
    static void ExportProjectilePath(Dictionary<string, GameObject> prefabs)
    {
        foreach (bool mirror in new[] { false, true })
        {
            string folder = Path.Combine(Output, mirror ? "ProjectilePath-right-dark" : "ProjectilePath-left-dark"); Directory.CreateDirectory(folder);
            var camera = Camera(new Color(0.09f, 0.12f, 0.15f));
            float sign = mirror ? -1 : 1;
            Actor(new Vector2(5.2f * sign, -1.85f), 1, mirror);
            Actor(new Vector2(-4.3f * sign, -1.85f), 1, !mirror);
            Vector3 source = new Vector3(4.2f * sign, -0.15f, -1), target = new Vector3(-4.3f * sign, -0.15f, -1);
            var instance = Object.Instantiate(prefabs["Projectile"]); Temporary.Add(instance); Isolate(instance); instance.transform.position = source;
            if (mirror) instance.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var driver = ChristashaParticleVisual.Play(instance, 194);
            for (int frame = 0; frame < 46; frame++)
            {
                if (frame > 0) driver.Advance(1f / 60);
                instance.transform.position = Vector3.Lerp(source, target, Mathf.Clamp01(frame / 60f / 0.26f));
                if (frame / 60f >= 0.26f) driver.StopAndClear();
                Capture(camera, Path.Combine(folder, frame.ToString("D4") + ".png"));
            }
            File.WriteAllText(Path.Combine(folder, "timeline.txt"), "Actual adapter visual contract: Fire source -> target atkEffectRoot lerp over .26 seconds; dispose on arrival; no success Point because Initialize cannot prove a successful hit. This is a neutral proxy, not a game capture.\n");
            Cleanup();
        }
    }
    [MenuItem("Steria/Christasha V2/Preview non-shipping Guard recipe")]
    public static void PreviewGuardRecipe()
    {
        Output = Path.GetFullPath("../evidence/guard-example"); Directory.CreateDirectory(Output);
        CreateAssets(); ExportGuardExample();
        File.WriteAllText(Path.Combine(Output, "example-only.txt"), "Runnable guide example, not included in the seven shipping bundle roots and not attached to any game page.\n");
        Debug.Log("CHRISTASHA_V2_GUARD_EXAMPLE_PASS");
    }
    public static void PreviewSlashVOnly()
    {
        try
        {
            Require(Application.unityVersion == "2019.3.15f1", "exact Unity");
            Require(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11, "native GPU");
            Output = Path.GetFullPath("../evidence/candidate-r3-slashv"); Directory.CreateDirectory(Output);
            string path = Path.GetFullPath("AssetBundles/" + ChristashaParticleVisual.BundleName);
            var bundle = AssetBundle.LoadFromFile(path); Require(bundle != null, "reuse existing bundle");
            var slash = bundle.LoadAsset<GameObject>("ChristashaV2_Slash"); Require(slash != null, "same Slash prefab");
            ExportAttacks(new Dictionary<string, GameObject> { { "SlashV", slash } }, new[] { "SlashV" });
            File.WriteAllText(Path.Combine(Output, "native-readback.txt"), "Supplemental actual caller evidence. No assets or bundle rebuilt. Reuses candidate-r3 bundle and unchanged production driver. SlashV=root.localRotation Z70, mirror parent Y180 once; scales=6/4.5.\nBundle SHA256=" + Sha(path) + "\nDriver SHA256=" + Sha("Assets/Scripts/ChristashaParticleVisual.cs") + "\nPreview-only builder SHA256=" + Sha("Assets/Editor/ChristashaParticleBuilder.cs") + "\n");
            bundle.Unload(true); Debug.Log("CHRISTASHA_V2_SLASHV_PASS");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    public static void DiagnoseSparseRender()
    {
        try
        {
            string root = Path.GetFullPath("../evidence/gpu-sparse-ab"); Directory.CreateDirectory(root);
            var bundle = AssetBundle.LoadFromFile(Path.GetFullPath("AssetBundles/" + ChristashaParticleVisual.BundleName));
            var hit = bundle.LoadAsset<GameObject>("ChristashaV2_Hit");
            var prefabs = new Dictionary<string, GameObject> { { "Hit", hit } };
            for (int trial = 0; trial < 3; trial++)
            {
                Output = Path.Combine(root, "trial" + trial + "-sparse"); Directory.CreateDirectory(Output);
                ExportAttacks(prefabs, new[] { "Hit" }, false, true);
                Output = Path.Combine(root, "trial" + trial + "-continuous"); Directory.CreateDirectory(Output);
                ExportAttacks(prefabs, new[] { "Hit" }, true, true);
            }
            bundle.Unload(true);
            File.WriteAllText(Path.Combine(root, "contract.txt"), "Only variable: renderEveryFrame. Same r3 bundle, canonical driver, two seeds 194/781, transforms, mid background and sample save times. Sparse omits Camera.Render at unsaved times. Continuous calls Camera.Render after every Advance, while saving exactly the same samples. No shader/material/particle recipe change.\n");
            Debug.Log("CHRISTASHA_V2_SPARSE_AB_PASS");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    public static void DiagnoseSequence()
    {
        try
        {
            string root = Path.GetFullPath("../evidence/" + SequenceRootName); Directory.CreateDirectory(root);
            var bundle = AssetBundle.LoadFromFile(Path.GetFullPath("AssetBundles/" + ChristashaParticleVisual.BundleName));
            var prefabs = new Dictionary<string, GameObject>();
            foreach (string kind in Kinds) prefabs[kind] = bundle.LoadAsset<GameObject>("ChristashaV2_" + kind);
            FullGpuAudit = true;
            for (int trial = 0; trial < 2; trial++)
            {
                Output = Path.Combine(root, "trial" + trial + "-sparse"); Directory.CreateDirectory(Output);
                foreach (string kind in Kinds) VerifyPlayback(prefabs[kind], kind);
                ExportAttacks(prefabs, new[] { "Slash", "Hit" }, false);
                Output = Path.Combine(root, "trial" + trial + "-continuous"); Directory.CreateDirectory(Output);
                foreach (string kind in Kinds) VerifyPlayback(prefabs[kind], kind);
                ExportAttacks(prefabs, new[] { "Slash", "Hit" }, true);
            }
            bundle.Unload(true);
            Debug.Log("CHRISTASHA_V2_SEQUENCE_AB_PASS");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    public static void DiagnoseSequenceSynchronized()
    {
        SequenceRootName = "gpu-sequence-sync";
        DiagnoseSequence();
    }
    public static void PreviewFrozenBundle()
    {
        try
        {
            Require(Application.unityVersion == "2019.3.15f1", "exact Unity");
            Require(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Direct3D11, "D3D11 GPU");
            Output = Path.GetFullPath("../evidence/candidate-r4"); Directory.CreateDirectory(Output);
            string path = Path.GetFullPath("AssetBundles/" + ChristashaParticleVisual.BundleName);
            Require(Sha(path) == "a0948f97682402b5e50ff3ca38d1f67e0d86a61c523c59b717830ffcf231f690", "unchanged visually approved asset bundle");
            var bundle = AssetBundle.LoadFromFile(path); Require(bundle != null && bundle.GetAllAssetNames().Length == 7, "seven existing bundle roots");
            var prefabs = new Dictionary<string, GameObject>();
            foreach (string kind in Kinds)
            {
                prefabs[kind] = bundle.LoadAsset<GameObject>("ChristashaV2_" + kind); Require(prefabs[kind] != null, "loaded " + kind);
                VerifyPlayback(prefabs[kind], kind);
            }
            prefabs["SlashV"] = prefabs["Slash"];
            ExportAttacks(prefabs, new[] { "Slash", "SlashV", "Hit", "Pierce", "Projectile", "Point", "Cast", "Area" });
            ExportProjectilePath(prefabs); ExportMass(prefabs); ExportGuardExample();
            File.WriteAllText(Path.Combine(Output, "bundle-sha256.txt"), Sha(path));
            File.WriteAllText(Path.Combine(Output, "native-readback.txt"), "Final synchronized-driver preview. No assets/bundle rebuilt. Existing r3 seven-root bundle SHA=" + Sha(path) + "\nProduction driver SHA=" + Sha("Assets/Scripts/ChristashaParticleVisual.cs") + "\nUnity=" + Application.unityVersion + "\nGPU=" + SystemInfo.graphicsDeviceName + "\nAPI=" + SystemInfo.graphicsDeviceType + "\nAll Advance calls have a corresponding camera render even when an image is not saved. Newborn particles receive zero-time simulation before rendering.\nProxy battle, not a game capture. Guard editor example is not bundled.\n");
            bundle.Unload(true); Debug.Log("CHRISTASHA_V2_FROZEN_PREVIEW_PASS");
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static void ExportGuardExample()
    {
        var guard = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/ChristashaV2_Guard.prefab");
        VerifyPlayback(guard, "Guard-example");
        ExportAttacks(new Dictionary<string, GameObject> { { "Guard", guard } }, new[] { "Guard" });
    }
    static void Actor(Vector2 feet, float scale, bool mirror)
    {
        var root = new GameObject("OpaqueNeutralActor"); Temporary.Add(root); root.transform.position = new Vector3(feet.x, feet.y, 2); root.transform.localScale = Vector3.one * scale;
        var material = new Material(Shader.Find("Unlit/Color")); material.color = new Color(0.27f, 0.31f, 0.34f); Temporary.Add(material);
        Part(root.transform, PrimitiveType.Capsule, new Vector3(0, 1.56f, 0), new Vector3(0.75f, 0.66f, 0.3f), material, 0);
        Part(root.transform, PrimitiveType.Sphere, new Vector3(0, 2.68f, 0), Vector3.one * 0.55f, material, 0);
        Part(root.transform, PrimitiveType.Capsule, new Vector3(-0.21f, 0.53f, 0), new Vector3(0.23f, 0.55f, 0.23f), material, -6);
        Part(root.transform, PrimitiveType.Capsule, new Vector3(0.22f, 0.53f, 0), new Vector3(0.23f, 0.55f, 0.23f), material, 9);
        Part(root.transform, PrimitiveType.Capsule, new Vector3(mirror ? 0.58f : -0.58f, 1.55f, -0.1f), new Vector3(0.20f, 0.48f, 0.2f), material, mirror ? -55 : 55);
        var weapon = new Material(Shader.Find("Unlit/Color")); weapon.color = new Color(0.58f, 0.60f, 0.57f); Temporary.Add(weapon);
        Part(root.transform, PrimitiveType.Cube, new Vector3(mirror ? 1.0f : -1.0f, 1.73f, -0.15f), new Vector3(0.07f, 1.52f, 0.07f), weapon, mirror ? -60 : 60);
    }
    static void Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material, float angle)
    {
        var part = GameObject.CreatePrimitive(type); part.transform.SetParent(parent, false); part.transform.localPosition = position; part.transform.localScale = scale; part.transform.localRotation = Quaternion.Euler(0, 0, angle); part.GetComponent<Renderer>().sharedMaterial = material;
        part.layer = 28;
        Object.DestroyImmediate(part.GetComponent<Collider>());
    }
    static void Capture(Camera camera, string path)
    {
        var previous = RenderTexture.active;
        camera.Render(); RenderTexture.active = camera.targetTexture;
        var pixels = new Texture2D(Width, Height, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, Width, Height), 0, 0); pixels.Apply(); File.WriteAllBytes(path, pixels.EncodeToPNG()); Object.DestroyImmediate(pixels); RenderTexture.active = previous;
    }
    static void Cleanup() { foreach (Object o in Temporary) if (o != null) Object.DestroyImmediate(o); Temporary.Clear(); }
    static void Isolate(GameObject instance) { foreach (var t in instance.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 28; }
    static void Save(Object value, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (existing == null) AssetDatabase.CreateAsset(value, path); else { EditorUtility.CopySerialized(value, existing); Object.DestroyImmediate(value); }
    }
    static string Sha(string path) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("V2 gate: " + message); }
}
