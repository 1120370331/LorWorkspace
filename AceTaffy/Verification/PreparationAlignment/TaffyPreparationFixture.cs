using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using AceTaffy;
using HarmonyLib;
using UI;
using UnityEditor;
using UnityEngine;
using Workshop;

public static class TaffyPreparationFixture
{
    private static readonly BindingFlags PrivateInstance = BindingFlags.Instance |
        BindingFlags.NonPublic | BindingFlags.Public;
    private static readonly string Ace = Path.GetFullPath(
        Path.Combine(Application.dataPath, "../../../.."));
    private static readonly string Output = Environment.GetEnvironmentVariable("TAFFY_PREPARATION_OUTPUT_DIR")
        ?? Path.Combine(Ace, "Verification", "PreparationAlignment", "output");
    private const int Size = 512;
    private static readonly Color Background = new Color(0.12f, 0.13f, 0.15f, 1f);

    public static void Run()
    {
        try
        {
            Directory.CreateDirectory(Output);
            AceTaffyMod.RootPath = Path.Combine(Ace, "AceTaffyMod");
            var harmony = new Harmony("lor.acetaffy.preparation.fixture");
            TaffyPreparationAlignment.Install(harmony);
            Check(Harmony.GetPatchInfo(AccessTools.Method(typeof(UICharacterRenderer),
                nameof(UICharacterRenderer.SetCharacter))) != null, "real Harmony patch installed");
            Scenario(0, 170, "AceTaffy");
            Scenario(4, 200, "AceTaffy_Projection");
            Debug.Log("TAFFY_PREPARATION_CHECK source timing: UICharacterRenderer.SetCharacter queues RenderCam only when creating an appearance; RenderCam yields one frame before Camera.Render, so the postfix runs first. Cached-path checks render the same camera explicitly.");
            string previousFailure = Path.Combine(Output, "failure.txt");
            if (File.Exists(previousFailure)) File.Delete(previousFailure);
            Debug.Log("TAFFY_PREPARATION_PASS exact production source, two indices/heights, real transform and Camera.Render");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Directory.CreateDirectory(Output);
            File.WriteAllText(Path.Combine(Output, "failure.txt"), ex.ToString());
            Debug.LogError("TAFFY_PREPARATION_FAIL " + ex);
            EditorApplication.Exit(1);
        }
    }

    private static void Scenario(int index, int height, string skin)
    {
        var uiObject = new GameObject("UICharacterRenderer fixture " + index);
        var ui = uiObject.AddComponent<UICharacterRenderer>();
        ui.characterList = new List<UICharacter>();
        for (int i = 0; i < 11; i++) ui.characterList.Add(new UICharacter(null, null));
        ui.characterRoot = new GameObject("CharacterRoot").transform;
        var appearanceObject = new GameObject("Actual CharacterAppearance");
        appearanceObject.transform.SetParent(ui.characterRoot, false);
        var appearance = appearanceObject.AddComponent<CharacterAppearance>();
        var setter = appearanceObject.AddComponent<WorkshopSkinDataSetter>();
        string png = Path.Combine(AceTaffyMod.RootPath, "Char", skin, "ClothCustom", "Default.png");
        Check(File.Exists(png), skin + " exact package PNG exists");
        setter.dic = new Dictionary<ActionDetail, ClothCustomizeData>
        {
            { ActionDetail.Default, new ClothCustomizeData { spritePath = png } }
        };
        var unit = (UnitDataModel)FormatterServices.GetUninitializedObject(typeof(UnitDataModel));
        var customize = (UnitCustomizingData)FormatterServices.GetUninitializedObject(typeof(UnitCustomizingData));
        customize.height = height;
        typeof(UnitDataModel).GetField("_customizeData", PrivateInstance).SetValue(unit, customize);
        typeof(UnitDataModel).GetField("_defaultBook", PrivateInstance).SetValue(unit, new BookModel());
        ui.characterList[index] = new UICharacter(unit, appearance);
        var originalPosition = new Vector3(10f * index, -2f, 10f);
        // GetRenderTextureByIndexAndSize assigns a Vector2 to localScale;
        // Unity's Vector2->Vector3 conversion keeps z=0 in this native path.
        var originalScale = new Vector3(height * 0.005f, height * 0.005f, 0f);
        appearance.transform.localPosition = originalPosition;
        appearance.transform.localScale = originalScale;

        var spriteObject = new GameObject("Exact Default SpriteRenderer");
        spriteObject.transform.SetParent(appearance.transform, false);
        var spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = SpriteUtil.LoadLargePivotSprite(png,
            new Vector2(0.5f, (512f - 274f) / 1024f));
        Check(spriteRenderer.sprite != null, "native SpriteUtil decoded package PNG");
        var cameraObject = new GameObject("Slot Camera " + index);
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.12f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Background;
        camera.cullingMask = 1 << spriteObject.layer;
        camera.transform.position = new Vector3(10f * index, 0f, -10f);
        var texture = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
        camera.targetTexture = texture;
        ui.cameraList = new List<Camera>();
        for (int i = 0; i < 11; i++) ui.cameraList.Add(camera);
        var native = Render(camera, index + "_" + height + "_native.png");
        // Reproduce the released correction that copied another actor's lean.
        appearance.transform.localPosition = originalPosition +
            Vector3.left * (0.5059f * height * 0.005f);
        var before = Render(camera, index + "_" + height + "_before.png");
        ui.SetCharacter(unit, index, false, false); // Cached appearance path; no resource loader.
        Check(ReferenceEquals(ui.GetRenderTextureByIndexAndSize(index), texture) &&
              appearance.transform.localScale == originalScale,
            index + "/" + height + " native preparation AndSize path retains matching height scale");
        float expectedX = 10f * index - 0.1759f * (height * 0.005f);
        Check(Mathf.Abs(appearance.transform.localPosition.x - expectedX) < 0.0001f,
            index + "/" + height + " production SetCharacter moved actual transform");
        Check(appearance.transform.localPosition.y == originalPosition.y &&
              appearance.transform.localPosition.z == originalPosition.z &&
              appearance.transform.localScale == originalScale,
            index + "/" + height + " y/z/scale unchanged");
        var after = Render(camera, index + "_" + height + "_after.png");
        Check(after.centroidX > before.centroidX + 10f &&
              Mathf.Abs(after.centroidX - Size / 2f) < Mathf.Abs(before.centroidX - Size / 2f),
            index + "/" + height + " Camera.Render pixels moved right toward native slot center");
        Check(Mathf.Abs(after.centroidX - Size / 2f) < 3f,
            index + "/" + height + " visible portrait centered within 3 render pixels");
        ui.SetCharacter(unit, index, false, false);
        Check(Mathf.Abs(appearance.transform.localPosition.x - expectedX) < 0.0001f,
            index + "/" + height + " repeated SetCharacter idempotent");

        // The same appearance now displays an unrelated skin. The patch must
        // undo only its own prior shift, regardless of the unit's key page.
        setter.dic[ActionDetail.Default].spritePath =
            Path.Combine(AceTaffyMod.RootPath, "Char", "Unrelated", "ClothCustom", "Default.png");
        ui.SetCharacter(unit, index, false, false);
        Check(Mathf.Abs(appearance.transform.localPosition.x - 10f * index) < 0.0001f,
            index + "/" + height + " same-instance unrelated skin restored native grid x");
        ui.SetCharacter(unit, index, false, false);
        Check(Mathf.Abs(appearance.transform.localPosition.x - 10f * index) < 0.0001f,
            index + "/" + height + " unrelated skin remains untouched");
        var restored = Render(camera, index + "_" + height + "_restored.png");
        Check(Mathf.Abs(restored.centroidX - native.centroidX) < 1f,
            index + "/" + height + " rendered portrait returns to native slot");
        Check(appearance.transform.localPosition.y == originalPosition.y &&
              appearance.transform.localPosition.z == originalPosition.z &&
              appearance.transform.localScale == originalScale,
            index + "/" + height + " restore preserves y/z/scale");

        UnityEngine.Object.DestroyImmediate(texture);
        UnityEngine.Object.DestroyImmediate(cameraObject);
        UnityEngine.Object.DestroyImmediate(appearanceObject);
        UnityEngine.Object.DestroyImmediate(ui.characterRoot.gameObject);
        UnityEngine.Object.DestroyImmediate(uiObject);
    }

    private static (float centroidX, int pixels) Render(Camera camera, string filename)
    {
        camera.Render();
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = camera.targetTexture;
        var image = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
        image.Apply();
        RenderTexture.active = previous;
        var pixels = image.GetPixels32();
        double sumX = 0;
        int count = 0;
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            Color32 pixel = pixels[y * Size + x];
            if (pixel.r > 75 || pixel.g > 75 || pixel.b > 75)
            {
                sumX += x;
                count++;
            }
        }
        Check(count > 500, filename + " visible pixels in real RenderTexture");
        File.WriteAllBytes(Path.Combine(Output, filename), image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
        float centroid = (float)(sumX / count);
        Debug.Log("TAFFY_PREPARATION_CHECK " + filename + " centroid=" +
                  centroid.ToString("F2") + " pixels=" + count);
        return (centroid, count);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Debug.Log("TAFFY_PREPARATION_CHECK PASS " + message);
    }
}
