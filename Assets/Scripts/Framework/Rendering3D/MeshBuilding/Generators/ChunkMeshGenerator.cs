using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class ChunkMeshGenerator
{
    public static int RESOLUTION = 2; // world-units per vertex step

    private static MeshBuilder MeshBuilder;
    private static List<LineFeature> Lines;
    private static List<Triangle2D> LineMeshTriangles;

    /// <summary>
    /// Terrain-only mesh at configurable resolution (one quad per RESOLUTION×RESOLUTION block).
    /// </summary>
    public static void GenerateMesh(GameObject mapRoot, float[,] heightmap, List<LineFeature> lines)
    {
        Lines = lines;
        LineMeshTriangles = new List<Triangle2D>();

        if (mapRoot == null || heightmap == null) return;

        // Create lines
        MeshBuilder = new MeshBuilder("Chunk", "Default", mapRoot.transform);
        CreateLineMesh();

        int width = heightmap.GetLength(0);
        int height = heightmap.GetLength(1);
        if (width < 2 || height < 2) return;

        // Create mesh builder + grass submesh
        int grassSubmesh = MeshBuilder.GetSubmesh("Materials/3D/Grass");

        int maxX = width - 1; // world extent in X
        int maxZ = height - 1; // world extent in Z

        // Step over the grid in RESOLUTION-sized blocks.
        for (int x = 0; x < maxX; x += RESOLUTION)
        {
            int x1 = Mathf.Min(x + RESOLUTION, maxX);

            for (int z = 0; z < maxZ; z += RESOLUTION)
            {
                int z1 = Mathf.Min(z + RESOLUTION, maxZ);

                // Check if this square intersects any LineFeature meshes (partially or fully)
                
                Vector2 v1_2d = new Vector2(x, z);
                Vector2 v2_2d = new Vector2(x1, z);
                Vector2 v3_2d = new Vector2(x1, z1);
                Vector2 v4_2d = new Vector2(x, z1);
                bool b1Covered = LineMeshTriangles.Any(t => t.ContainsPoint(v1_2d));
                bool b2Covered = LineMeshTriangles.Any(t => t.ContainsPoint(v2_2d));
                bool b3Covered = LineMeshTriangles.Any(t => t.ContainsPoint(v3_2d));
                bool b4Covered = LineMeshTriangles.Any(t => t.ContainsPoint(v4_2d));
                bool isFullyCoveredByLineMesh = b1Covered && b2Covered && b3Covered && b4Covered;
                if (isFullyCoveredByLineMesh) continue;

                // Heights from the heightmap corners (integer grid)
                float h00 = heightmap[x, z];
                float h10 = heightmap[x1, z];
                float h11 = heightmap[x1, z1];
                float h01 = heightmap[x, z1];

                // World positions (X,Z are grid coords; Y is height)
                Vector3 v1 = new Vector3(x, h00, z);
                Vector3 v2 = new Vector3(x1, h10, z);
                Vector3 v3 = new Vector3(x1, h11, z1);
                Vector3 v4 = new Vector3(x, h01, z1);

                // 123
                // 134

                // UVs normalized to the whole chunk [0..1]
                Vector2 uvStart = new Vector2((float)x / maxX, (float)z / maxZ);
                Vector2 uvEnd = new Vector2((float)x1 / maxX, (float)z1 / maxZ);

                MeshBuilder.BuildPlane(grassSubmesh, v1, v2, v3, v4, uvStart, uvEnd);
            }
        }

        MeshBuilder.ApplyMesh(addCollider: true, applyMaterials: true, castShadows: true);
    }

    /// <summary>
    /// Adds all submeshes for all LineFeatures to the MeshBuilder.
    /// </summary>
    private static void CreateLineMesh()
    {
        int roadSumbesh = MeshBuilder.GetSubmesh("Materials/3D/Road");

        foreach (LineFeature line in Lines)
        {
            // Iterate through each point in the line to create the corresponding (left, right) MeshVertices.
            List<MeshVertex> leftVertices = new List<MeshVertex>();
            List<MeshVertex> rightVertices = new List<MeshVertex>();
            for (int i = 0; i < line.Points.Count; i++)
            {
                Vector2 prevPos = (i == 0) ? line.Points[i].Position : line.Points[i - 1].Position;
                Vector2 thisPos = line.Points[i].Position;
                Vector2 nextPos = (i == line.Points.Count - 1) ? line.Points[i].Position : line.Points[i + 1].Position;

                Vector2 left = HelperFunctions.GetOffsetIntersection(prevPos, thisPos, nextPos, -line.Def.Width, -line.Def.Width);
                Vector2 right = HelperFunctions.GetOffsetIntersection(prevPos, thisPos, nextPos, line.Def.Width, line.Def.Width);

                MeshVertex mvLeft = MeshBuilder.AddVertex(new Vector3(left.x, 0f, left.y), Vector2.zero);
                MeshVertex mvRight = MeshBuilder.AddVertex(new Vector3(right.x, 0f, right.y), Vector2.zero);

                leftVertices.Add(mvLeft);
                rightVertices.Add(mvRight);
            }

            // Create mesh
            for (int i = 0; i < line.Points.Count - 1; i++)
            {
                // Debug.Log($"Building plane {leftVertices[i].Position}, {leftVertices[i + 1].Position}, {rightVertices[i + 1].Position}, {rightVertices[i].Position}");
                List<MeshTriangle> t = MeshBuilder.AddPlane(roadSumbesh, leftVertices[i], leftVertices[i + 1], rightVertices[i + 1], rightVertices[i]);
                LineMeshTriangles.Add(new Triangle2D(new Vector2(t[0].Vertex1.Position.x, t[0].Vertex1.Position.z), new Vector2(t[0].Vertex2.Position.x, t[0].Vertex2.Position.z), new Vector2(t[0].Vertex3.Position.x, t[0].Vertex3.Position.z)));
                LineMeshTriangles.Add(new Triangle2D(new Vector2(t[1].Vertex1.Position.x, t[1].Vertex1.Position.z), new Vector2(t[1].Vertex2.Position.x, t[1].Vertex2.Position.z), new Vector2(t[1].Vertex3.Position.x, t[1].Vertex3.Position.z)));
            }
        }
    }

    private struct Triangle2D
    {
        public Vector2 A, B, C;

        public Triangle2D(Vector2 a, Vector2 b, Vector2 c)
        {
            A = a;
            B = b;
            C = c;
        }

        /// <summary>
        /// Returns true if p is inside the triangle (edges inclusive). Uses edge functions (cross products), no division.
        /// </summary>
        public bool ContainsPoint(Vector2 p)
        {
            float c1 = Cross(B - A, p - A);
            float c2 = Cross(C - B, p - B);
            float c3 = Cross(A - C, p - C);

            // Inside if all have the same sign (or zero)
            return (c1 >= 0f && c2 >= 0f && c3 >= 0f) || (c1 <= 0f && c2 <= 0f && c3 <= 0f);
        }

        /// <summary>
        /// Returns true if p is inside with tolerance. epsilon absorbs tiny numerical flips.
        /// </summary>
        public bool ContainsPoint(Vector2 p, float epsilon)
        {
            float c1 = Cross(B - A, p - A);
            float c2 = Cross(C - B, p - B);
            float c3 = Cross(A - C, p - C);

            bool hasNeg = (c1 < -epsilon) || (c2 < -epsilon) || (c3 < -epsilon);
            bool hasPos = (c1 > epsilon) || (c2 > epsilon) || (c3 > epsilon);
            return !(hasNeg && hasPos);
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }
    }
}
