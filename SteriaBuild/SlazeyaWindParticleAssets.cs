using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Steria.Vfx
{
    internal static class SlazeyaWindParticleAssets
    {
        internal const string BundleName = "steria_slazeya_wind_combat";
        private static AssetBundle _bundle;
        private static readonly Dictionary<SlazeyaWindKind, GameObject> Prefabs = new Dictionary<SlazeyaWindKind, GameObject>();

        internal static GameObject Instantiate(SlazeyaWindKind kind, Transform parent)
        {
            GameObject prefab;
            if (!Prefabs.TryGetValue(kind, out prefab) || prefab == null)
            {
                if (_bundle == null)
                {
                    string assembly = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
                    string root = Directory.GetParent(assembly).FullName;
                    string path = Path.Combine(root, "Assemblies", "AB", BundleName);
                    // Fixture uses the same assembly-relative layout; no scene scans or fallback search.
                    if (!File.Exists(path)) throw new FileNotFoundException("Slazeya wind AssetBundle missing", path);
                    _bundle = AssetBundle.LoadFromFile(path);
                    if (_bundle == null) throw new InvalidOperationException("Slazeya wind AssetBundle load failed: " + path);
                }
                string name = "SlazeyaWind" + kind;
                prefab = _bundle.LoadAsset<GameObject>(name);
                if (prefab == null) throw new InvalidOperationException("Slazeya wind prefab missing: " + name);
                Prefabs[kind] = prefab;
            }
            GameObject instance = UnityEngine.Object.Instantiate(prefab, parent, false);
            instance.SetActive(false);
            return instance;
        }
    }
}
