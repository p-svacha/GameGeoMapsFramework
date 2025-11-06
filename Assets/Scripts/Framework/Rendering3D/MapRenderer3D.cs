using System.Linq;
using UltimateNoiseLibrary;
using UnityEngine;

/// <summary>
/// Renderer responsible for rendering a map in 3D.
/// </summary>
public class MapRenderer3D
{
    public Map Map { get; private set; }
    public GameObject MapRoot { get; private set; }

    public MapRenderer3D(Map map)
    {
        Map = map;

        MapRoot = new GameObject("Map3D");
    }

    public void DrawMap()
    {
        float[,] heightMap = new float[100 + 1, 100 + 1];
        PerlinNoise noise = new PerlinNoise(0.01f);
        for (int x = 0; x < heightMap.GetLength(0); x++)
        {
            for (int y = 0; y < heightMap.GetLength(1); y++)
            {
                heightMap[x, y] = 0f; // noise.GetValue(x, y) * 10f;
            }
        }

        ChunkMeshGenerator.GenerateMesh(MapRoot, heightMap, Map.LineFeatures.Values.ToList());
    }


}
