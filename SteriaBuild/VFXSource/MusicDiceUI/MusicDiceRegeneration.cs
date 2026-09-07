#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Steria.VisualAuthoring
{
    public static class MusicDiceRegeneration
    {
        public static void Run()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                int index = Array.IndexOf(args, "-musicDiceOutput");
                if (index < 0 || index + 1 >= args.Length)
                    throw new ArgumentException("Pass -musicDiceOutput with the authoring output directory.");
                if (PlayerSettings.colorSpace != ColorSpace.Gamma)
                    throw new InvalidOperationException("Use the original Unity 2019 Gamma authoring project settings.");
                string output = Path.GetFullPath(args[index + 1]);
                Directory.CreateDirectory(output);
                Export(MusicDiceSpriteGenerator.GetCardSprite(), Path.Combine(output, "Card.png"));
                Export(MusicDiceSpriteGenerator.GetGlyphSprite(), Path.Combine(output, "Glyph.png"));
                Export(MusicDiceSpriteGenerator.GetBlankFrameSprite(), Path.Combine(output, "BlankFrame.png"));
                Debug.Log("Music dice original artwork regenerated: " + output);
                EditorApplication.Exit(0);
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
        }

        private static void Export(Sprite sprite, string path)
        {
            RenderTexture target = RenderTexture.GetTemporary(sprite.texture.width, sprite.texture.height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            Texture2D readable = null;
            try
            {
                Graphics.Blit(sprite.texture, target);
                RenderTexture.active = target;
                readable = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                readable.Apply();
                File.WriteAllBytes(path, readable.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                if (readable != null) UnityEngine.Object.DestroyImmediate(readable);
            }
        }
    }
}
#endif
