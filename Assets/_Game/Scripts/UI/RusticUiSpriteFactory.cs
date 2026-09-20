using UnityEngine;

namespace PawPath.UI
{
    /// <summary>Mobil UI için çalışma anında hafif ahşap/parşömen dokuları üretir.</summary>
    public static class RusticUiSpriteFactory
    {
        static Sprite wood;
        static Sprite parchment;

        public static Sprite WoodButton() => wood != null ? wood : wood = Build(true);
        public static Sprite ParchmentPanel() => parchment != null ? parchment : parchment = Build(false);

        static Sprite Build(bool wooden)
        {
            const int width = 256;
            const int height = 72;
            const int radius = 14;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = wooden ? "RusticWoodButton" : "RusticParchmentPanel",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var random = new System.Random(wooden ? 1847 : 9321);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (!InsideRounded(x, y, width, height, radius))
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                float grain = (float)(random.NextDouble() - 0.5) * (wooden ? 0.075f : 0.035f);
                grain += Mathf.Sin(y * 0.42f + Mathf.Sin(x * 0.055f) * 2f) * (wooden ? 0.035f : 0.012f);
                Color baseColor = wooden
                    ? new Color(0.58f + grain, 0.39f + grain * 0.7f, 0.21f + grain * 0.45f, 0.94f)
                    : new Color(0.84f + grain, 0.74f + grain, 0.61f + grain * 0.8f, 0.88f);

                bool outerBorder = x < 5 || x >= width - 5 || y < 5 || y >= height - 5;
                bool innerBorder = x >= 6 && x < 9 || x < width - 6 && x >= width - 9 ||
                    y >= 6 && y < 9 || y < height - 6 && y >= height - 9;
                if (outerBorder)
                    baseColor = wooden ? new Color(0.42f, 0.27f, 0.13f, 0.96f) : new Color(0.34f, 0.24f, 0.17f, 0.92f);
                else if (innerBorder)
                    baseColor = new Color(0.90f, 0.70f, 0.34f, wooden ? 0.94f : 0.72f);
                texture.SetPixel(x, y, baseColor);
            }

            if (wooden)
            {
                DrawLeaf(texture, 31, 36, false);
                DrawLeaf(texture, width - 31, 36, true);
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(16f, 16f, 16f, 16f));
        }

        static bool InsideRounded(int x, int y, int width, int height, int radius)
        {
            int cx = x < radius ? radius : x >= width - radius ? width - radius - 1 : x;
            int cy = y < radius ? radius : y >= height - radius ? height - radius - 1 : y;
            int dx = x - cx;
            int dy = y - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        static void DrawLeaf(Texture2D texture, int centerX, int centerY, bool mirror)
        {
            Color gold = new Color(0.91f, 0.72f, 0.32f, 0.95f);
            int direction = mirror ? -1 : 1;
            for (int i = -18; i <= 18; i++)
            {
                int x = centerX + i * direction;
                int y = centerY + Mathf.RoundToInt(i * 0.34f);
                if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                    texture.SetPixel(x, y, gold);
            }
            for (int leaf = -2; leaf <= 2; leaf++)
            {
                int lx = centerX + leaf * 7 * direction;
                int ly = centerY + leaf * 2 + (leaf % 2 == 0 ? 5 : -5);
                for (int y = -3; y <= 3; y++)
                for (int x = -6; x <= 6; x++)
                    if (x * x / 36f + y * y / 9f <= 1f)
                        texture.SetPixel(lx + x, ly + y, gold);
            }
        }
    }
}
