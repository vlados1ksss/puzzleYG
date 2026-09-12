using System.Collections.Generic;
using UnityEngine;

namespace CityPuzzle.Puzzle
{
    // Builds classic jigsaw tab/blank edges for a grid and the closed polygon outline for one piece.
    // All shapes are built in a Y-up, pixel-space canvas local to each piece (matches Texture2D's
    // own bottom-left-origin convention, so rasterized pixels never need flipping).
    public static class JigsawShapeGenerator
    {
        const float BumpHalfWidth = 0.16f; // half-width of the tab neck, as a fraction of edge length
        const float BumpHeight = 0.24f;    // how far the tab bulges out, as a fraction of edge length

        public readonly struct EdgeGrid
        {
            public readonly int[,] Horizontal; // [rows-1, cols]: edge between row r (above) and r+1 (below)
            public readonly int[,] Vertical;   // [rows, cols-1]: edge between col c (left) and c+1 (right)

            public EdgeGrid(int[,] h, int[,] v)
            {
                Horizontal = h;
                Vertical = v;
            }
        }

        public static float MarginXFactor => BumpHeight; // multiply by cellH to get horizontal margin
        public static float MarginYFactor => BumpHeight; // multiply by cellW to get vertical margin

        public static EdgeGrid Generate(int cols, int rows, int seed)
        {
            var rng = new System.Random(seed);
            var h = new int[Mathf.Max(0, rows - 1), cols];
            var v = new int[rows, Mathf.Max(0, cols - 1)];
            for (int r = 0; r < rows - 1; r++)
                for (int c = 0; c < cols; c++)
                    h[r, c] = rng.Next(0, 2) == 0 ? 1 : -1;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols - 1; c++)
                    v[r, c] = rng.Next(0, 2) == 0 ? 1 : -1;
            return new EdgeGrid(h, v);
        }

        static float CanonicalOffset(float t)
        {
            float x = Mathf.Abs(t - 0.5f);
            if (x >= BumpHalfWidth) return 0f;
            float u = x / BumpHalfWidth;
            return BumpHeight * Mathf.Sqrt(Mathf.Max(0f, 1f - u * u));
        }

        // Returns the closed polygon (Y-up, pixels) for piece (r,c) inside its own expanded canvas,
        // where the canvas' bottom-left is `margin` below/left of the piece's base-cell bottom-left.
        public static Vector2[] BuildPolygon(EdgeGrid grid, int r, int c, int rows, int cols,
            float cellW, float cellH, float marginX, float marginY, int samplesPerEdge)
        {
            float topSign = r == 0 ? 0 : -grid.Horizontal[r - 1, c];
            float bottomSign = r == rows - 1 ? 0 : grid.Horizontal[r, c];
            float leftSign = c == 0 ? 0 : -grid.Vertical[r, c - 1];
            float rightSign = c == cols - 1 ? 0 : grid.Vertical[r, c];

            Vector2 bottomLeft = new Vector2(marginX, marginY);
            Vector2 bottomRight = bottomLeft + new Vector2(cellW, 0);
            Vector2 topRight = bottomLeft + new Vector2(cellW, cellH);
            Vector2 topLeft = bottomLeft + new Vector2(0, cellH);

            var points = new List<Vector2>(samplesPerEdge * 4);
            AddEdge(points, bottomLeft, bottomRight, new Vector2(0, -1), bottomSign, cellW, samplesPerEdge);
            AddEdge(points, bottomRight, topRight, new Vector2(1, 0), rightSign, cellH, samplesPerEdge);
            AddEdge(points, topRight, topLeft, new Vector2(0, 1), topSign, cellW, samplesPerEdge);
            AddEdge(points, topLeft, bottomLeft, new Vector2(-1, 0), leftSign, cellH, samplesPerEdge);
            return points.ToArray();
        }

        static void AddEdge(List<Vector2> points, Vector2 start, Vector2 end, Vector2 outward, float sign, float edgeLength, int samples)
        {
            Vector2 tangent = end - start;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float offset = sign == 0f ? 0f : CanonicalOffset(t) * sign * edgeLength;
                points.Add(start + tangent * t + outward * offset);
            }
        }
    }
}
