using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CityPuzzle.Core;

namespace CityPuzzle.Puzzle
{
    public class PuzzleGenerator : MonoBehaviour
    {
        public PuzzlePiece piecePrefab;
        public RectTransform world;
        public RectTransform pieceLayer;
        public RectTransform boardArea;
        public RectTransform slotVisualsContainer;
        public Image slotVisualPrefab;
        public Image blurredBackground;

        const int SamplesPerEdge = 14;
        const int BlurredBgSize = 48;

        readonly List<GameObject> spawnedPieces = new List<GameObject>();
        readonly List<GameObject> spawnedSlotVisuals = new List<GameObject>();
        readonly List<Sprite> spawnedSprites = new List<Sprite>();
        readonly List<Texture2D> spawnedTextures = new List<Texture2D>();

        public PuzzleBoard Build(LevelData level, Difficulty difficulty, int levelIndex)
        {
            Clear();

            var board = boardArea.GetComponent<PuzzleBoard>();
            if (board == null) board = boardArea.gameObject.AddComponent<PuzzleBoard>();

            int cols = DifficultyInfo.Columns(difficulty);
            int rows = DifficultyInfo.Rows(difficulty);
            board.Initialize(cols * rows);

            Texture2D sourceTex = level.cityImage.texture;
            Color32[] sourcePixels = sourceTex.GetPixels32();
            int srcW = sourceTex.width;
            int srcH = sourceTex.height;

            float boardW = boardArea.rect.width;
            float boardH = boardArea.rect.height;
            float cellW = boardW / cols;
            float cellH = boardH / rows;

            float srcCellW = srcW / (float)cols;
            float srcCellH = srcH / (float)rows;
            float srcMarginX = JigsawShapeGenerator.MarginXFactor * srcCellH;
            float srcMarginY = JigsawShapeGenerator.MarginYFactor * srcCellW;

            int seed = levelIndex * 97 + (int)difficulty * 131 + cols * 17 + rows;
            var edges = JigsawShapeGenerator.Generate(cols, rows, seed);

            SetupBlurredBackground(sourceTex);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Vector2 slotLocalPos = new Vector2(
                        (c + 0.5f) * cellW - boardW / 2f,
                        boardH / 2f - (r + 0.5f) * cellH
                    );
                    Vector2 targetPos = boardArea.anchoredPosition + slotLocalPos;

                    if (slotVisualPrefab != null && slotVisualsContainer != null)
                    {
                        Image slotVisual = Instantiate(slotVisualPrefab, slotVisualsContainer);
                        RectTransform svRect = slotVisual.rectTransform;
                        svRect.sizeDelta = new Vector2(cellW - 4f, cellH - 4f);
                        svRect.anchoredPosition = slotLocalPos;
                        spawnedSlotVisuals.Add(slotVisual.gameObject);
                    }

                    int srcOriginX = Mathf.RoundToInt(c * srcCellW - srcMarginX);
                    int srcOriginY = Mathf.RoundToInt(srcH - (r + 1) * srcCellH - srcMarginY);
                    int canvasW = Mathf.Max(2, Mathf.RoundToInt(srcCellW + 2 * srcMarginX));
                    int canvasH = Mathf.Max(2, Mathf.RoundToInt(srcCellH + 2 * srcMarginY));

                    Vector2[] polygon = JigsawShapeGenerator.BuildPolygon(edges, r, c, rows, cols,
                        srcCellW, srcCellH, srcMarginX, srcMarginY, SamplesPerEdge);
                    Color32[] pixels = JigsawRasterizer.Rasterize(polygon, canvasW, canvasH, sourcePixels, srcW, srcH, srcOriginX, srcOriginY);

                    var tex = new Texture2D(canvasW, canvasH, TextureFormat.RGBA32, false);
                    tex.filterMode = FilterMode.Bilinear;
                    tex.SetPixels32(pixels);
                    tex.Apply();
                    spawnedTextures.Add(tex);

                    Sprite pieceSprite = Sprite.Create(tex, new Rect(0, 0, canvasW, canvasH), new Vector2(0.5f, 0.5f), 100f);
                    spawnedSprites.Add(pieceSprite);

                    GameObject pieceGO = Instantiate(piecePrefab.gameObject, pieceLayer);
                    var image = pieceGO.GetComponent<Image>();
                    image.sprite = pieceSprite;
                    image.alphaHitTestMinimumThreshold = 0.5f;
                    var rt = (RectTransform)pieceGO.transform;
                    rt.sizeDelta = new Vector2(cellW * (canvasW / srcCellW), cellH * (canvasH / srcCellH));

                    var piece = pieceGO.GetComponent<PuzzlePiece>();
                    piece.Setup(pieceLayer, board, targetPos, Mathf.Min(cellW, cellH) * 0.4f);
                    spawnedPieces.Add(pieceGO);
                }
            }

            ScatterPiecesAroundBoard();
            return board;
        }

        void SetupBlurredBackground(Texture2D sourceTex)
        {
            if (blurredBackground == null) return;

            var rt = RenderTexture.GetTemporary(BlurredBgSize, BlurredBgSize);
            Graphics.Blit(sourceTex, rt);
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            var smallTex = new Texture2D(BlurredBgSize, BlurredBgSize, TextureFormat.RGBA32, false);
            smallTex.ReadPixels(new Rect(0, 0, BlurredBgSize, BlurredBgSize), 0, 0);
            smallTex.Apply();
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
            smallTex.filterMode = FilterMode.Bilinear;
            spawnedTextures.Add(smallTex);

            Sprite bgSprite = Sprite.Create(smallTex, new Rect(0, 0, BlurredBgSize, BlurredBgSize), new Vector2(0.5f, 0.5f));
            spawnedSprites.Add(bgSprite);
            blurredBackground.sprite = bgSprite;
        }

        void ScatterPiecesAroundBoard()
        {
            float worldW = world.rect.width;
            float worldH = world.rect.height;
            float boardW = boardArea.rect.width;
            float boardH = boardArea.rect.height;
            Vector2 boardCenter = boardArea.anchoredPosition;

            foreach (var go in spawnedPieces)
            {
                var rt = (RectTransform)go.transform;
                Vector2 pos = Vector2.zero;
                for (int attempt = 0; attempt < 24; attempt++)
                {
                    float x = Random.Range(-worldW / 2f + rt.sizeDelta.x * 0.5f, worldW / 2f - rt.sizeDelta.x * 0.5f);
                    float y = Random.Range(-worldH / 2f + rt.sizeDelta.y * 0.5f, worldH / 2f - rt.sizeDelta.y * 0.5f);
                    pos = new Vector2(x, y);
                    bool insideBoard = Mathf.Abs(pos.x - boardCenter.x) < boardW / 2f + 24f &&
                                        Mathf.Abs(pos.y - boardCenter.y) < boardH / 2f + 24f;
                    if (!insideBoard) break;
                }
                rt.anchoredPosition = pos;
            }
        }

        public void Clear()
        {
            foreach (var go in spawnedPieces) if (go != null) Destroy(go);
            foreach (var go in spawnedSlotVisuals) if (go != null) Destroy(go);
            foreach (var s in spawnedSprites) if (s != null) Destroy(s);
            foreach (var t in spawnedTextures) if (t != null) Destroy(t);
            spawnedPieces.Clear();
            spawnedSlotVisuals.Clear();
            spawnedSprites.Clear();
            spawnedTextures.Clear();
        }
    }
}
