using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class HanzhouVfxReferenceDumper
{
    private static readonly string OutputPath = @"C:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\VFXSource\AnhierBlueWhiteSlash\hanzhou_vfx_reference_dump.txt";

    [MenuItem("Steria/Dump Hanzhou VFX Reference")]
    public static void Dump()
    {
        Debug.Log("HZ_DUMP_ENTRY_START");
        var sb = new StringBuilder();
        DumpBundle(sb, @"C:\Users\rog\WorkSpace\projects\games\lor\寒昼事务所V4.0\Assemblies\AB\hz_前三章.ab", new[]
        {
            "寒昼小兵_刀光_斩击",
            "丹顶血魔_刀光_斩击"
        });
        DumpBundle(sb, @"C:\Users\rog\WorkSpace\projects\games\lor\寒昼事务所V4.0\Assemblies\AB\HZ旧特效\HZ_冷雨工坊.ab", new[]
        {
            "冷雨工坊斩击"
        });
        DumpBundle(sb, @"C:\Users\rog\WorkSpace\projects\games\lor\寒昼事务所V4.0\Assemblies\AB\第四章\hz_第四章_额外.ab", new[]
        {
            "灼火刀光-斩击-洛里安"
        });

        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        File.WriteAllText(OutputPath, sb.ToString(), Encoding.UTF8);
        Debug.Log("HZ_DUMP_ENTRY_DONE " + OutputPath);
    }

    private static void DumpBundle(StringBuilder sb, string path, string[] prefabNames)
    {
        sb.AppendLine("===== BUNDLE =====");
        sb.AppendLine(path);
        if (!File.Exists(path))
        {
            sb.AppendLine("MISSING");
            return;
        }

        var bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null)
        {
            sb.AppendLine("LOAD_FAILED");
            return;
        }

        string[] allNames = bundle.GetAllAssetNames();
        sb.AppendLine("AssetCount=" + allNames.Length);
        for (int i = 0; i < allNames.Length; i++)
        {
            sb.AppendLine("ASSET " + allNames[i]);
        }

        foreach (string requestedName in prefabNames)
        {
            GameObject prefab = bundle.LoadAsset<GameObject>(requestedName);
            if (prefab == null)
            {
                sb.AppendLine("--- PREFAB MISSING: " + requestedName);
                foreach (string assetName in allNames)
                {
                    if (assetName.IndexOf(requestedName, StringComparison.OrdinalIgnoreCase) >= 0 || assetName.IndexOf("斩", StringComparison.OrdinalIgnoreCase) >= 0 || assetName.IndexOf("刀光", StringComparison.OrdinalIgnoreCase) >= 0 || assetName.IndexOf("slash", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        sb.AppendLine("CANDIDATE " + assetName);
                    }
                }
                continue;
            }

            DumpPrefab(sb, prefab, requestedName);
        }

        bundle.Unload(true);
    }

    private static void DumpPrefab(StringBuilder sb, GameObject prefab, string label)
    {
        sb.AppendLine("--- PREFAB " + label + " / " + prefab.name + " ---");
        var renderers = prefab.GetComponentsInChildren<Renderer>(true);
        var particles = prefab.GetComponentsInChildren<ParticleSystem>(true);
        var animators = prefab.GetComponentsInChildren<Animator>(true);
        var animations = prefab.GetComponentsInChildren<Animation>(true);
        sb.AppendLine("Summary renderers=" + renderers.Length + " particles=" + particles.Length + " animations=" + animations.Length + " animators=" + animators.Length + " children=" + prefab.GetComponentsInChildren<Transform>(true).Length);

        foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
        {
            string rel = GetPath(t, prefab.transform);
            sb.AppendLine("NODE " + rel + " active=" + t.gameObject.activeSelf + " layer=" + t.gameObject.layer + " localPos=" + V(t.localPosition) + " localRot=" + V(t.localEulerAngles) + " localScale=" + V(t.localScale));

            var r = t.GetComponent<Renderer>();
            if (r != null)
            {
                sb.AppendLine("  RENDERER type=" + r.GetType().Name + " sortingLayer=" + r.sortingLayerName + " sortingOrder=" + r.sortingOrder + " enabled=" + r.enabled + " material=" + MatName(r.sharedMaterial));
                if (r.sharedMaterial != null)
                {
                    Material m = r.sharedMaterial;
                    sb.AppendLine("    MAT shader=" + (m.shader != null ? m.shader.name : "null") + " color=" + ColorString(GetMaterialColor(m)) + " mainTex=" + TexName(m.mainTexture));
                }
            }

            var ps = t.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                var emission = ps.emission;
                var shape = ps.shape;
                var col = ps.colorOverLifetime;
                var size = ps.sizeOverLifetime;
                var vel = ps.velocityOverLifetime;
                var noise = ps.noise;
                var pr = t.GetComponent<ParticleSystemRenderer>();
                sb.AppendLine("  PARTICLE duration=" + main.duration + " loop=" + main.loop + " playOnAwake=" + main.playOnAwake + " delay=" + Curve(main.startDelay) + " lifetime=" + Curve(main.startLifetime) + " speed=" + Curve(main.startSpeed) + " size=" + Curve(main.startSize) + " color=" + Grad(main.startColor));
                sb.AppendLine("    emission enabled=" + emission.enabled + " rate=" + Curve(emission.rateOverTime) + " bursts=" + BurstString(emission));
                sb.AppendLine("    shape enabled=" + shape.enabled + " type=" + shape.shapeType + " radius=" + shape.radius + " box=" + V(shape.box) + " arc=" + shape.arc);
                sb.AppendLine("    modules colorOverLifetime=" + col.enabled + " sizeOverLifetime=" + size.enabled + " velocityOverLifetime=" + vel.enabled + " noise=" + noise.enabled);
                if (pr != null)
                {
                    sb.AppendLine("    psRenderer mode=" + pr.renderMode + " sortingOrder=" + pr.sortingOrder + " material=" + MatName(pr.sharedMaterial) + " velocityScale=" + pr.velocityScale + " lengthScale=" + pr.lengthScale);
                }
            }

            var a = t.GetComponent<Animation>();
            if (a != null)
            {
                sb.AppendLine("  ANIMATION playAutomatically=" + a.playAutomatically + " clip=" + (a.clip != null ? a.clip.name : "null"));
                foreach (AnimationState state in a)
                {
                    sb.AppendLine("    state=" + state.name + " length=" + state.length + " speed=" + state.speed + " wrap=" + state.wrapMode);
                }
            }

            var animator = t.GetComponent<Animator>();
            if (animator != null)
            {
                sb.AppendLine("  ANIMATOR enabled=" + animator.enabled + " controller=" + (animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "null"));
            }
        }
    }

    private static string GetPath(Transform t, Transform root)
    {
        if (t == root) return t.name;
        return GetPath(t.parent, root) + "/" + t.name;
    }

    private static string V(Vector3 v)
    {
        return string.Format("({0:0.###},{1:0.###},{2:0.###})", v.x, v.y, v.z);
    }

    private static string ColorString(Color c)
    {
        return string.Format("({0:0.###},{1:0.###},{2:0.###},{3:0.###})", c.r, c.g, c.b, c.a);
    }

    private static Color GetMaterialColor(Material m)
    {
        if (m.HasProperty("_TintColor")) return m.GetColor("_TintColor");
        if (m.HasProperty("_Color")) return m.GetColor("_Color");
        return m.color;
    }

    private static string MatName(Material m) { return m != null ? m.name : "null"; }
    private static string TexName(Texture t) { return t != null ? t.name + " " + t.width + "x" + t.height : "null"; }

    private static string Curve(ParticleSystem.MinMaxCurve c)
    {
        return c.mode + ":" + c.constantMin.ToString("0.###") + ".." + c.constantMax.ToString("0.###") + "/" + c.constant.ToString("0.###");
    }

    private static string Grad(ParticleSystem.MinMaxGradient g)
    {
        return g.mode + ":" + ColorString(g.colorMin) + ".." + ColorString(g.colorMax) + "/" + ColorString(g.color);
    }

    private static string BurstString(ParticleSystem.EmissionModule emission)
    {
        int count = emission.burstCount;
        string[] parts = new string[count];
        for (int i = 0; i < count; i++)
        {
            ParticleSystem.Burst b = emission.GetBurst(i);
            parts[i] = "t=" + b.time.ToString("0.###") + " count=" + b.minCount + ".." + b.maxCount;
        }
        return string.Join(", ", parts);
    }
}
