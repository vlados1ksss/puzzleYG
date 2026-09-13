using UnityEngine;

namespace CityPuzzle.Core
{
    // Cheap blur via repeated half-size downsampling (GPU bilinear averaging), which reads much
    // smoother than a single huge downscale. Also center-crops to a square so non-square source
    // photos never get stretched when used behind the (square) puzzle board or level cards.
    public static class ImageBlur
    {
        public static Texture2D CreateBlurredTexture(Texture2D source, int finalSize)
        {
            int size = Mathf.Min(source.width, source.height);
            float sx = (float)size / source.width;
            float sy = (float)size / source.height;
            var scale = new Vector2(sx, sy);
            var offset = new Vector2((1f - sx) * 0.5f, (1f - sy) * 0.5f);

            // Start at a moderately large intermediate size so the first pass isn't a single
            // brutal downsample (which looks blocky), then halve repeatedly down to finalSize.
            int startSize = Mathf.Clamp(Mathf.NextPowerOfTwo(size / 2), finalSize * 2, 1024);

            RenderTexture current = RenderTexture.GetTemporary(startSize, startSize);
            Graphics.Blit(source, current, scale, offset);

            int w = startSize;
            while (w > finalSize)
            {
                int nw = Mathf.Max(finalSize, w / 2);
                var next = RenderTexture.GetTemporary(nw, nw);
                Graphics.Blit(current, next);
                RenderTexture.ReleaseTemporary(current);
                current = next;
                w = nw;
            }

            var prevActive = RenderTexture.active;
            RenderTexture.active = current;
            var tex = new Texture2D(finalSize, finalSize, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, finalSize, finalSize), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(current);
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        public static Sprite CreateBlurredSprite(Texture2D source, int size)
        {
            var tex = CreateBlurredTexture(source, size);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
