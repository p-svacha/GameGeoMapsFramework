using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// IMPORTANT:
/// - Source sprites need to be the same size as cell Size
/// - Sprite Mode of source sprites need to be set to Single - Cell Size - FULL RECT 
/// </summary>
public static class RuntimeSpriteAssetBuilder
{
    /*
    /// <summary>
    /// Builds and returns a TMP_SpriteAsset at runtime containing the sprites of the provided defs, and referenced by name by their DefName.
    /// </summary>
    public static TMP_SpriteAsset BuildResourceSpriteAsset(Def[] defs, int cellSize, FilterMode filterMode)
    {
        // Global sprite sheet attributes that can be set in inspector
        float OX = 0f; // offset x
        float OY = 19f; // offset y
        float ADV = 16f; // x advance
        float SF = 1f; // scale factor

        // Validate that all defs have a sprite
        foreach (Def def in defs)
        {
            if (def.Sprite == null) throw new System.Exception($"Cannot create sprite sheet. Def {def.DefName} has no sprite.");
        }

        // Collect all sprites that will go into sprite sheet
        Sprite[] sprites = defs.Select(d => d.Sprite).ToArray();
        Texture2D[] cellTextures = ExtractSpritesToCells(sprites, cellSize);

        // Build atlas
        RectInt[] rectsPx;
        Texture2D atlasTex = BuildAtlas(cellTextures, cellSize, filterMode, out rectsPx);

        // Create the TMP_SpriteAsset
        TMP_SpriteAsset spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        spriteAsset.name = "ResourceSpriteAsset";
        spriteAsset.spriteSheet = atlasTex;

        // Assign material
        Material spriteMat = new Material(Shader.Find("TextMeshPro/Sprite"));
        spriteMat.mainTexture = atlasTex;
        spriteMat.name = "ResourceSpriteAsset Material";
        spriteAsset.material = spriteMat;

        // Prepare tables
        spriteAsset.spriteInfoList = new List<TMP_Sprite>();
        spriteAsset.spriteGlyphTable.Clear();
        spriteAsset.spriteCharacterTable.Clear();

        spriteAsset.faceInfo = new FaceInfo()
        {
            pointSize = cellSize,
            scale = 1,
            ascentLine = cellSize,
            baseline = 0,
            descentLine = 0,
        };

        for (int i = 0; i < sprites.Length; i++)
        {
            var def = defs[i];
            var pr = rectsPx[i];

            var info = new TMP_Sprite
            {
                name = def.DefName,
                hashCode = def.DefName.GetHashCode(),
                x = (short)pr.x,
                y = (short)pr.y,
                width = (short)pr.width,
                height = (short)pr.height,
                pivot = new Vector2(0.5f, 0.5f),
                xOffset = OX,
                yOffset = OY,
                xAdvance = ADV,
            };
            spriteAsset.spriteInfoList.Add(info);

            var glyphRect = new GlyphRect(pr.x, pr.y, pr.width, pr.height);
            var metrics = new GlyphMetrics(info.width, info.height, info.xOffset, info.yOffset, info.xAdvance);

            var glyph = new TMP_SpriteGlyph((uint)info.hashCode, metrics, glyphRect, SF, 0, sprites[i]);
            spriteAsset.spriteGlyphTable.Add(glyph);

            var character = new TMP_SpriteCharacter((uint)info.hashCode, glyph) { name = def.DefName };
            spriteAsset.spriteCharacterTable.Add(character);
        }

        // Build lookup tables
        spriteAsset.UpdateLookupTables();

        return spriteAsset;
    }
    */

    private static Texture2D[] ExtractSpritesToCells(Sprite[] sprites, int cellSize)
    {
        var cells = new Texture2D[sprites.Length];

        for (int i = 0; i < sprites.Length; i++)
        {
            var s = sprites[i];
            var srcTex = s.texture;

            // Full size of the sprite (should include original transparent padding if Mesh Type = Full Rect)
            int fullW = Mathf.RoundToInt(s.rect.width);
            int fullH = Mathf.RoundToInt(s.rect.height);
            if (fullW != cellSize || fullH != cellSize)
                throw new System.Exception($"{s.name}: sprite rect {fullW}x{fullH} != cellSize {cellSize}");

            // The (possibly trimmed) region that actually contains pixels on the texture
            Rect tr = s.textureRect;
            int srcX = Mathf.RoundToInt(tr.x);
            int srcY = Mathf.RoundToInt(tr.y);
            int srcW = Mathf.RoundToInt(tr.width);
            int srcH = Mathf.RoundToInt(tr.height);

            // Where that trimmed region belongs inside the full (padded) 22×22 rect
            Vector2 off = s.textureRectOffset; // offset created by trimming
            int dstX = Mathf.RoundToInt(off.x);
            int dstY = Mathf.RoundToInt(off.y);

            var dst = new Texture2D(cellSize, cellSize, TextureFormat.RGBA32, false);
            dst.filterMode = FilterMode.Point;
            dst.wrapMode = TextureWrapMode.Clamp;

            // Clear (fully transparent)
            var clear = new Color32[cellSize * cellSize];
            dst.SetPixels32(clear);

            // GPU copy: exact pixels, no resampling, keeps padding
            Graphics.CopyTexture(srcTex, 0, 0, srcX, srcY, srcW, srcH, dst, 0, 0, dstX, dstY);
            dst.Apply(false, false);

            cells[i] = dst;
        }

        return cells;
    }

    /// <summary>
    /// Creates the atlas texture packing all given textures into a single texture.
    /// </summary>
    private static Texture2D BuildAtlas(Texture2D[] cells, int cellSize, FilterMode filterMode,
                                         out RectInt[] pixelRects)
    {
        int count = cells.Length;
        int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt((float)count / cols);

        int atlasW = cols * cellSize;
        int atlasH = rows * cellSize;

        var atlas = new Texture2D(atlasW, atlasH, TextureFormat.RGBA32, false);
        atlas.filterMode = filterMode;
        atlas.wrapMode = TextureWrapMode.Clamp;

        pixelRects = new RectInt[count];

        for (int i = 0; i < count; i++)
        {
            int col = i % cols;
            int row = i / cols;

            // If you want row 0 at the top, flip Y like this; otherwise use row * cellSize.
            int x = col * cellSize;
            int y = atlasH - (row + 1) * cellSize;

            // Exact GPU copy, no resample:
            Graphics.CopyTexture(cells[i], 0, 0, 0, 0, cellSize, cellSize, atlas, 0, 0, x, y);

            pixelRects[i] = new RectInt(x, y, cellSize, cellSize);
        }

        atlas.Apply(false, false);
        return atlas;
    }
}
