using System.Collections.Generic;
using UnityEngine;

namespace CityPuzzle.Puzzle
{
    // Scanline-fills a piece polygon and samples RGB from the source image, producing a
    // ready-to-upload Color32[] buffer (Y-up, matching Texture2D.SetPixels32's own convention).
    public static class JigsawRasterizer
    {
        public static Color32[] Rasterize(Vector2[] polygon, int canvasW, int canvasH,
            Color32[] sourcePixels, int srcW, int srcH, int srcOriginX, int srcOriginY)
        {
            var pixels = new Color32[canvasW * canvasH];
            int n = polygon.Length;
            var xs = new List<float>(8);

            for (int y = 0; y < canvasH; y++)
            {
                float py = y + 0.5f;
                xs.Clear();
                for (int i = 0; i < n; i++)
                {
                    Vector2 a = polygon[i];
                    Vector2 b = polygon[(i + 1) % n];
                    if ((a.y <= py) != (b.y <= py))
                    {
                        float t = (py - a.y) / (b.y - a.y);
                        xs.Add(a.x + t * (b.x - a.x));
                    }
                }
                xs.Sort();

                int rowBase = y * canvasW;
                int srcRow = srcOriginY + y;
                for (int i = 0; i + 1 < xs.Count; i += 2)
                {
                    int xStart = Mathf.Clamp(Mathf.RoundToInt(xs[i]), 0, canvasW);
                    int xEnd = Mathf.Clamp(Mathf.RoundToInt(xs[i + 1]), 0, canvasW);
                    for (int x = xStart; x < xEnd; x++)
                    {
                        int sx = Mathf.Clamp(srcOriginX + x, 0, srcW - 1);
                        int sy = Mathf.Clamp(srcRow, 0, srcH - 1);
                        Color32 c = sourcePixels[sy * srcW + sx];
                        c.a = 255;
                        pixels[rowBase + x] = c;
                    }
                }
            }
            return pixels;
        }
    }
}
