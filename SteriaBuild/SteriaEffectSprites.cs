using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Steria
{
    /// <summary>
    /// 管理Steria mod的特效图片资源
    /// </summary>
    public static class SteriaEffectSprites
    {
        private static Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        private static Dictionary<string, Material> _additiveMaterials = new Dictionary<string, Material>();
        private static bool _initialized = false;
        private static string _artworkPath;

        /// <summary>
        /// 初始化特效图片系统
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                string modPath = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
                string modRootPath = Directory.GetParent(modPath)?.FullName;
                _artworkPath = Path.Combine(modRootPath, "Resource", "ArtWork");

                SteriaLogger.Log($"SteriaEffectSprites ArtWork path: {_artworkPath}");
                SteriaLogger.Log($"ArtWork path exists: {Directory.Exists(_artworkPath)}");

                if (Directory.Exists(_artworkPath))
                {
                    var files = Directory.GetFiles(_artworkPath, "*.png");
                    SteriaLogger.Log($"Found {files.Length} PNG files in ArtWork folder");
                    foreach (var f in files)
                    {
                        SteriaLogger.Log($"  - {Path.GetFileName(f)}");
                    }
                }

                _initialized = true;
            }
            catch (Exception ex)
            {
                SteriaLogger.Log($"ERROR: Failed to initialize SteriaEffectSprites: {ex}");
            }
        }

        /// <summary>
        /// 获取指定名称的Texture2D（不含扩展名）
        /// </summary>
        public static Texture2D GetTexture(string name, bool removeBackground = true, float brightnessThreshold = 0.15f)
        {
            Initialize();

            string cacheKey = removeBackground ? name + "_nobg" : name;
            if (_textures.TryGetValue(cacheKey, out Texture2D cached))
            {
                SteriaLogger.Log($"GetTexture: Using cached texture for {name}");
                return cached;
            }

            string filePath = Path.Combine(_artworkPath, name + ".png");
            SteriaLogger.Log($"GetTexture: Looking for {filePath}");

            if (!File.Exists(filePath))
            {
                SteriaLogger.Log($"ERROR: Texture not found: {filePath}");
                return null;
            }

            try
            {
                SteriaLogger.Log($"GetTexture: Loading {name}...");
                byte[] fileData = File.ReadAllBytes(filePath);
                SteriaLogger.Log($"GetTexture: Read {fileData.Length} bytes");

                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;

                if (ImageConversion.LoadImage(texture, fileData))
                {
                    SteriaLogger.Log($"GetTexture: Loaded {name} ({texture.width}x{texture.height})");

                    if (removeBackground)
                    {
                        texture = RemoveBackgroundByBrightness(texture, brightnessThreshold);
                    }

                    texture.name = name;
                    _textures[cacheKey] = texture;
                    return texture;
                }
                else
                {
                    SteriaLogger.Log($"ERROR: Failed to decode texture data: {name}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                SteriaLogger.Log($"ERROR: Exception loading texture {name}: {ex}");
                return null;
            }
        }

        private static Texture2D RemoveBackgroundByBrightness(Texture2D source, float threshold)
        {
            Color[] pixels = source.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i];
                float brightness = pixel.r * 0.299f + pixel.g * 0.587f + pixel.b * 0.114f;

                if (brightness < threshold)
                {
                    pixels[i] = new Color(pixel.r, pixel.g, pixel.b, 0f);
                }
                else
                {
                    float alpha = (brightness - threshold) / (1f - threshold);
                    alpha = Mathf.Clamp01(alpha * alpha * 1.5f);
                    pixels[i] = new Color(pixel.r, pixel.g, pixel.b, alpha);
                }
            }

            source.SetPixels(pixels);
            source.Apply();
            return source;
        }

        /// <summary>
        /// 获取特效材质
        /// </summary>
        public static Material GetEffectMaterial(string textureName, bool useAdditive = true, float brightnessThreshold = 0.15f)
        {
            SteriaLogger.Log($"GetEffectMaterial: {textureName}, additive={useAdditive}");

            string cacheKey = textureName + (useAdditive ? "_add" : "_alpha");
            if (_additiveMaterials.TryGetValue(cacheKey, out Material cached))
            {
                SteriaLogger.Log($"GetEffectMaterial: Using cached material for {textureName}");
                return cached;
            }

            Texture2D texture = GetTexture(textureName, true, brightnessThreshold);
            if (texture == null)
            {
                SteriaLogger.Log($"ERROR: GetEffectMaterial failed - texture is null for {textureName}");
                return null;
            }

            try
            {
                Material material;

                if (useAdditive)
                {
                    Shader shader = Shader.Find("Particles/Additive");
                    SteriaLogger.Log($"GetEffectMaterial: Particles/Additive shader found: {shader != null}");
                    if (shader == null)
                    {
                        shader = Shader.Find("Sprites/Default");
                        SteriaLogger.Log($"GetEffectMaterial: Fallback to Sprites/Default: {shader != null}");
                    }
                    material = new Material(shader);
                    material.mainTexture = texture;
                    material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
                }
                else
                {
                    Shader shader = Shader.Find("Sprites/Default");
                    if (shader == null)
                    {
                        shader = Shader.Find("Unlit/Transparent");
                    }
                    material = new Material(shader);
                    material.mainTexture = texture;
                }

                material.name = cacheKey;
                _additiveMaterials[cacheKey] = material;
                SteriaLogger.Log($"GetEffectMaterial: Created material for {textureName}");
                return material;
            }
            catch (Exception ex)
            {
                SteriaLogger.Log($"ERROR: Exception creating material for {textureName}: {ex}");
                return null;
            }
        }

        public static Material GetAdditiveMaterial(string textureName)
        {
            return GetEffectMaterial(textureName, true, 0.15f);
        }

        public static GameObject CreateEffectQuad(string textureName, Transform parent, float scale = 1f)
        {
            SteriaLogger.Log($"CreateEffectQuad: {textureName}, scale={scale}");

            Material material = GetAdditiveMaterial(textureName);
            if (material == null)
            {
                SteriaLogger.Log($"ERROR: CreateEffectQuad failed - material is null for {textureName}");
                return null;
            }

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Effect_" + textureName;

            var collider = quad.GetComponent("MeshCollider") as Component;
            if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }

            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.material = new Material(material);
            renderer.sortingOrder = 100;

            if (parent != null)
            {
                quad.transform.SetParent(parent);
            }
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localRotation = Quaternion.identity;

            float aspectRatio = 16f / 9f;
            quad.transform.localScale = new Vector3(scale * aspectRatio, scale, 1f);

            SteriaLogger.Log($"CreateEffectQuad: Created quad for {textureName}");
            return quad;
        }

        public static GameObject CreateEffectSprite(string textureName, Transform parent, float pixelsPerUnit = 100f)
        {
            Texture2D texture = GetTexture(textureName);
            if (texture == null) return null;

            GameObject spriteObj = new GameObject("EffectSprite_" + textureName);

            if (parent != null)
            {
                spriteObj.transform.SetParent(parent);
            }
            spriteObj.transform.localPosition = Vector3.zero;
            spriteObj.transform.localRotation = Quaternion.identity;

            SpriteRenderer sr = spriteObj.AddComponent<SpriteRenderer>();

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit
            );
            sr.sprite = sprite;

            sr.material = new Material(Shader.Find("Sprites/Default"));
            sr.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            sr.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);

            sr.sortingOrder = 100;

            return spriteObj;
        }
    }
}
