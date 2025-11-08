using UnityEngine;

public class LineFeatureDef : Def
{
    public Color Color { get; init; }
    public float Width { get; init; }
    public SurfaceDef Surface { get; init; }
    public LineTexture Texture { get; init; } = LineTexture.Default;
    public bool RoundedCorners { get; init; } = false;

    public Material Material => ResourceManager.LoadMaterial("Materials/LineMaterials/" + Texture.ToString());
}
