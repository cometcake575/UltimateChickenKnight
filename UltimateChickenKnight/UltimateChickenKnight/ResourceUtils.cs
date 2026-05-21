using System.Reflection;
using UnityEngine;

namespace UltimateChickenKnight;

public static class ResourceUtils
{
    internal static Sprite LoadSpriteResource(
        string spritePath,
        FilterMode filterMode = FilterMode.Bilinear,
        Vector4 border = default,
        float ppu = 100)
    {
        return LoadSpriteResource(spritePath, new Vector2(0.5f, 0.5f), filterMode, border, ppu);
    }

    private static Sprite LoadSpriteResource(
        string spritePath,
        Vector2 pivot, 
        FilterMode filterMode = FilterMode.Bilinear,
        Vector4 border = default,
        float ppu = 100)
    {
        var path = $"UltimateChickenKnight.Resources.{spritePath}.png";

        var asm = Assembly.GetExecutingAssembly();

        using var s = asm.GetManifestResourceStream(path);
        if (s == null) return null;
        var buffer = new byte[s.Length];
        _ = s.Read(buffer, 0, buffer.Length);
        var tex = new Texture2D(2, 2);
        tex.LoadImage(buffer, true);
        tex.wrapMode = TextureWrapMode.Clamp;

        var sprite = Sprite.Create(
            tex, 
            new Rect(0, 0, tex.width, tex.height),
            pivot, 
            ppu, 
            0U, 
            SpriteMeshType.Tight,
            border);
        
        sprite.texture.filterMode = filterMode;
        
        return sprite;
    }
}