// extern alias HarmonyLibAlias; // 移除 extern alias
using UnityEngine;
using HarmonyLib; // 恢复简单的 using
using System;
using LOR_DiceSystem; // 添加可能的命名空间
using BaseMod; // Import BaseMod
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Xml.Serialization; // Added for XML Deserialization
using LOR_XML; // Added for BattleEffectTextRoot/BattleEffectText
using System.Linq; // Needed for Linq

namespace MyDLL
{
    // 主要的 Mod 初始化类
    // 改回继承 BaseMod.BaseModInitializer
    public class ModInitializer : BaseMod.BaseModInitializer // Corrected base class
    {
        private static bool _initialized = false;
        // Store the Harmony instance for potential use (e.g., unpatching)
        public static Harmony HarmonyInstance { get; private set; }

        // 改回 override Initialize
        // 注意: BaseModInitializer 提供的初始化方法可能是 OnInitializeMod(), 而不是 Initialize()
        // 需要根据 BaseModInitializer 的实际定义来确定
        public override void OnInitializeMod() // Changed method name based on BaseModInitializer source
        {
            base.OnInitializeMod(); // Call base class initializer FIRST
            Debug.Log("[MyDLL] === ModInitializer.OnInitializeMod() START ==="); // Added START log
            if (_initialized)
            {
                 Debug.Log("[MyDLL] Mod already initialized, skipping."); // Added skip log
                 return; 
            }

            Debug.Log("[MyDLL] Initializing..."); // Added initializing log
            try
            {
                // --- Initialize Harmony and Apply ALL Patches --- 
                try
            {
                    // Create a unique ID for this mod's Harmony instance
                    string harmonyId = "MyDLL.Harmony." + Guid.NewGuid().ToString(); 
                    HarmonyInstance = new Harmony(harmonyId);
                    Debug.Log($"[MyDLL] Created Harmony instance with ID: {harmonyId}");

                    // Apply all patches defined within this assembly (MyDLL.dll)
                    HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                    Debug.Log("[MyDLL] Harmony PatchAll(Assembly) executed.");
                    
                    // --- REMOVED Explicit patch application block ---
                    // try { ... explicit patch for BattleUnitModel_OnWaveStart_Patch ... } catch { ... }
                    // --- End of REMOVED explicit patch application ---
                }
                catch (Exception harmonyEx)
                {
                    Debug.LogError($"[MyDLL] Error during Harmony Initialization or Patching: {harmonyEx}");
                    // Depending on severity, you might want to stop initialization
                    // return; 
                }
                // --- End of Harmony Initialization and Patching ---
                
                // --- Automatically Load All Buff Icons from Resource/ArtWork --- 
                try
                {
                    string modPath = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
                    string modRootPath = Directory.GetParent(modPath)?.FullName;
                    string artworkPath = Path.Combine(modRootPath, "Resource", "ArtWork");
                    Debug.Log($"[MyDLL] Scanning for Buff Icons in: {artworkPath}");

                    if (Directory.Exists(artworkPath))
                    {
                        // Ensure the dictionary exists
                        if (BattleUnitBuf._bufIconDictionary == null) 
                        {
                            BattleUnitBuf._bufIconDictionary = new Dictionary<string, Sprite>();
                        }

                        string[] pngFiles = Directory.GetFiles(artworkPath, "*.png", SearchOption.TopDirectoryOnly);
                        Debug.Log($"[MyDLL] Found {pngFiles.Length} PNG files to load as icons.");

                        foreach (string filePath in pngFiles)
                        {
                            try
                            {
                                string iconId = Path.GetFileNameWithoutExtension(filePath);
                                Debug.Log($"[MyDLL] Loading icon: {iconId} from {filePath}");

                                byte[] fileData = File.ReadAllBytes(filePath);
                                Texture2D texture = new Texture2D(2, 2);
                                
                                if (UnityEngine.ImageConversion.LoadImage(texture, fileData)) 
                                {
                                    Sprite iconSprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);
                                    iconSprite.name = iconId; // Set sprite name for clarity

                                    if (!BattleUnitBuf._bufIconDictionary.ContainsKey(iconId))
                                    {
                                        BattleUnitBuf._bufIconDictionary.Add(iconId, iconSprite);
                                        Debug.Log($"[MyDLL] Successfully loaded and added icon '{iconId}' to BattleUnitBuf dictionary.");
                                    }
                                    else
                                    {
                                        // Overwrite existing icon if needed, or log a warning
                                        BattleUnitBuf._bufIconDictionary[iconId] = iconSprite;
                                        Debug.LogWarning($"[MyDLL] Icon key '{iconId}' already existed. Overwriting with new icon.");
                                    }
                                }
                                else
                                {
                                    Debug.LogError($"[MyDLL] Failed to load texture data from {filePath}");
                                }
                            }
                            catch (Exception fileEx)
                            {
                                Debug.LogError($"[MyDLL] Error processing icon file {filePath}: {fileEx}");
                            }
                        }
                    }
                    else
                    {
                         Debug.LogWarning($"[MyDLL] Artwork directory not found, skipping icon loading: {artworkPath}");
                    }
                }
                catch (Exception autoLoadEx)
                {
                     Debug.LogError($"[MyDLL] Error during automatic icon loading: {autoLoadEx}");
                }
                // --- End of Automatic Icon Loading ---

                // --- REMOVED Manual SteriaFlow icon loading ---
                // try { ... } catch { ... }
                // --- End REMOVED Manual SteriaFlow icon loading ---

                // --- REMOVED Manual MemCrystalPassive icon loading ---
                // try { ... } catch { ... }
                // --- End REMOVED Manual MemCrystalPassive icon loading ---

                // --- REMOVED Manual load custom Effect Text --- 
                /*
                try
                {
                    Debug.Log("[MyDLL] Attempting to manually load EffectTexts_MyDLL.xml");
                    string assemblyPath = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
                    // Path is now relative to the assembly location
                    string xmlPath = Path.Combine(assemblyPath, "Localize", "cn", "EffectTexts", "EffectTexts_MyDLL.xml"); 
                    Debug.Log($"[MyDLL] Effect Text XML path: {xmlPath}");

                    if (File.Exists(xmlPath))
                    {
                        // Read and deserialize the XML
                        XmlSerializer serializer = new XmlSerializer(typeof(BattleEffectTextRoot));
                        using (StreamReader reader = new StreamReader(xmlPath))
                        {
                            BattleEffectTextRoot effectTextRoot = (BattleEffectTextRoot)serializer.Deserialize(reader);
                            
                            if (effectTextRoot != null && effectTextRoot.effectTextList != null)
                            {
                                // Access the singleton instance
                                BattleEffectTextsXmlList textListInstance = Singleton<BattleEffectTextsXmlList>.Instance;
                                
                                foreach (BattleEffectText effectText in effectTextRoot.effectTextList)
                                {
                                    if (effectText != null && !string.IsNullOrEmpty(effectText.ID))
                                    {
                                        Debug.Log($"[MyDLL] Registering Effect Text for ID: {effectText.ID}");
                                        // Attempt to add to the internal dictionaries (common pattern)
                                        // We might need reflection if these are private, or find a public Add method.
                                        // Assuming internal dictionaries named _nameDictionary and _descDictionary for now.
                                        // This is speculative and might need adjustment based on actual BattleEffectTextsXmlList structure.
                                        
                                        // Use reflection to access potentially private dictionaries
                                        var nameDictField = typeof(BattleEffectTextsXmlList).GetField("_nameList", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                                        var descDictField = typeof(BattleEffectTextsXmlList).GetField("_list", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public); // Desc dictionary might be named _list
                                        
                                        if (nameDictField != null && descDictField != null)
                                        {
                                            var nameDict = nameDictField.GetValue(textListInstance) as Dictionary<string, string>;
                                            var descDict = descDictField.GetValue(textListInstance) as Dictionary<string, BattleEffectText>; // The value might be the whole object
                                            
                                            if (nameDict != null && descDict != null)
                                            {
                                                if (!nameDict.ContainsKey(effectText.ID))
                                                {
                                                    nameDict.Add(effectText.ID, effectText.Name);
                                                }
                                                else
                                                {
                                                    Debug.LogWarning($"[MyDLL] Name for ID {effectText.ID} already exists. Overwriting.");
                                                    nameDict[effectText.ID] = effectText.Name;
                                                }

                                                if (!descDict.ContainsKey(effectText.ID))
                                                {
                                                    // Store the whole BattleEffectText object as value seems likely for GetEffectTextDesc(id, param)
                                                    descDict.Add(effectText.ID, effectText); 
                                                }
                                                else
                                                {
                                                    Debug.LogWarning($"[MyDLL] Desc for ID {effectText.ID} already exists. Overwriting.");
                                                    descDict[effectText.ID] = effectText;
                                                }
                                                Debug.Log($"[MyDLL] Successfully registered Name and Desc for {effectText.ID}");
                                            }
                                            else { Debug.LogError("[MyDLL] Failed to cast internal dictionaries."); }
                                        }
                                        else { Debug.LogError("[MyDLL] Could not find internal dictionary fields (_nameList or _list) in BattleEffectTextsXmlList via reflection."); }
                                    }
                                }
                            }
                            else { Debug.LogError("[MyDLL] Failed to deserialize EffectTexts_MyDLL.xml or list is null."); }
                        }
                    }
                    else { Debug.LogError($"[MyDLL] EffectTexts_MyDLL.xml not found at path: {xmlPath}"); }
                }
                */
                // --- End of removed manual Effect Text loading ---

                // Removed manual nested patch application block - relying on BaseMod's automatic discovery

                _initialized = true;

                // --- Other Mod Initialization --- 
                // You can add other setup code here if needed, 
                // like registering custom keywords, singletons, etc.

            }
            catch (Exception ex)
            {
                Debug.LogError($"[MyDLL] Mod Initialization Failed: {ex}");
                 _initialized = false; // Ensure initialized is false on failure
            }
            Debug.Log("[MyDLL] === ModInitializer.OnInitializeMod() END ==="); // Added END log
        }

        // (可选) 用于移除补丁的方法 - 需要 HarmonyPatches 实现 Unpatch()
        // Note: BaseMod might handle unpatching automatically, this might be redundant.
        // Consider removing if BaseMod manages the lifecycle.
        public static void Unload()
        {
             if (!_initialized) return;
             // HarmonyPatches.Unpatch(); // TODO: Implement Unpatch method in HarmonyPatches or handle unpatching differently
             Debug.Log("[MyDLL] Mod Unloaded (Patch removal might be incomplete).");
             _initialized = false;
        }

        // Optional: BaseMod provides other override methods like OnGameStart, OnUpdate, etc.
        // public override void OnGameStart() { ... }
    }

    // --- REMOVE ALL CODE BELOW THIS LINE --- 

} 