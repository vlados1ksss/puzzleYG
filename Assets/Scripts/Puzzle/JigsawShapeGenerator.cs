using System.Collections.Generic;
using UnityEngine;

namespace CityPuzzle.Puzzle
{
    // Builds classic jigsaw tab/blank edges for a grid and the closed polygon outline for one piece.
    // Each tab is a proper "peg": a narrow neck flaring into a round head, traced as an arc of a
    // circle whose center sits off the edge line — not just a smooth bump — so it reads as a real
    // interlocking puzzle piece rather than a wavy rectangle.
    // All shapes are built in a Y-up, pixel-space canvas local to each piece (matches Texture2D's
    // own bottom-left-origin convention, so rasterized pixels never need flipping).
    public static class JigsawShapeGenerator
    {
        const float NeckHalfWidth = 0.09f;  // half-width of the neck, as a fraction of edge length
        const float CenterOffset = 0.12f;   // distance from the edge line to the head's circle center
        static readonly float HeadRadius = Mathf.Sqrt(CenterOffset * CenterOffset + NeckHalfWidth * NeckHalfWidth);
        static readonly float ApexHeight = CenterOffset + HeadRadius; // how far the tab bulges out overall

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

        public static float MarginXFactor => ApexHeight + 0.02f; // multiply by cellH to get horizontal margin
        public static float MarginYFactor => ApexHeight + 0.02f; // multiply by cellW to get vertical margin

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

        // Canonical (t, n) points for one edge of length 1, tab bulging toward +n, shared by every
        // edge/piece (only the sign flips per edge). t can move backwards during the head's arc —
        // that back-and-forth is exactly what makes the head wider than its neck.
        static List<Vector2> BuildCanonicalTabPoints(int arcSamples)
        {
            var pts = new List<Vector2>(arcSamples + 3)
            {
                new Vector2(0f, 0f),
                new Vector2(0.5f - NeckHalfWidth, 0f)
            };

            float thetaA = Mathf.Atan2(-CenterOffset, -NeckHalfWidth);
            float thetaB = Mathf.Atan2(-CenterOffset, NeckHalfWidth) - 2f * Mathf.PI;
            for (int i = 1; i <= arcSamples; i++)
            {
                float s = (float)i / arcSamples;
                float angle = Mathf.Lerp(thetaA, thetaB, s);
                pts.Add(new Vector2(0.5f + HeadRadius * Mathf.Cos(angle), CenterOffset + HeadRadius * Mathf.Sin(angle)));
            }

            pts.Add(new Vector2(1f, 0f));
            return pts;
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

            var canonical = BuildCanonicalTabPoints(samplesPerEdge);
            var points = new List<Vector2>(canonical.Count * 4);
            AddEdge(points, canonical, bottomLeft, bottomRight, new Vector2(0, -1), bottomSign, cellW);
            AddEdge(points, canonical, bottomRight, topRight, new Vector2(1, 0), rightSign, cellH);
            AddEdge(points, canonical, topRight, topLeft, new Vector2(0, 1), topSign, cellW);
            AddEdge(points, canonical, topLeft, bottomLeft, new Vector2(-1, 0), leftSign, cellH);
            return points.ToArray();
        }

        // Both the tangential and perpendicular components scale by the SAME edge length, so the
        // tab's proportions stay correct on that edge regardless of the cell's other dimension.
        static void AddEdge(List<Vector2> points, List<Vector2> canonical, Vector2 start, Vector2 end,
            Vector2 outward, float sign, float edgeLength)
        {
            Vector2 tangent = end - start;
            for (int i = 0; i < canonical.Count; i++)
            {
                Vector2 cp = canonical[i];
                points.Add(start + tangent * cp.x + outward * (sign * cp.y * edgeLength));
            }
        }
    }
}
