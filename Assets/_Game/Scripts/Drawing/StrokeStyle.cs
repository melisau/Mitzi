using UnityEngine;

namespace PawPath.Drawing
{
    /// <summary>
    /// Çizgilerin görünümü: oyunun sıcak, boyalı paletine uygun renkler ve
    /// çalışma anında üretilen dokular. Çizerken pastel boya dokusu kullanılır;
    /// parmak kalkınca çizgi fırçanın malzemesine (tahta, jöle, buz) dönüşür.
    /// Dokular 1 dünya birimi uzunluğunda tasarlanır ve LineRenderer'ın
    /// Tile modunda birim başına bir kez tekrarlanır.
    /// </summary>
    public static class StrokeStyle
    {
        static Material crayonMaterial;
        static Material glowMaterial;
        static readonly Material[] surfaceMaterials = new Material[4];

        // ── Renkler ────────────────────────────────────────────────
        /// <summary>Çizim sırasındaki pastel boya rengi.</summary>
        public static Color CrayonColor(PathSurfaceType type) => type switch
        {
            PathSurfaceType.Bounce => new Color(0.36f, 0.63f, 0.90f, 1f),  // gök mavisi
            PathSurfaceType.Ice => new Color(0.60f, 0.80f, 0.94f, 1f),     // buz mavisi
            PathSurfaceType.Hazard => new Color(0.88f, 0.38f, 0.33f, 1f),  // mercan
            _ => new Color(0.45f, 0.30f, 0.20f, 1f)                        // koyu kahve
        };

        /// <summary>Katılaşmış malzemenin boya tonu (beyaz doku bununla çarpılır).</summary>
        public static Color SolidColor(PathSurfaceType type) => type switch
        {
            PathSurfaceType.Bounce => new Color(0.42f, 0.70f, 0.96f, 1f),
            PathSurfaceType.Ice => new Color(0.84f, 0.94f, 1f, 1f),
            PathSurfaceType.Hazard => new Color(0.92f, 0.42f, 0.36f, 1f),
            _ => new Color(0.80f, 0.57f, 0.36f, 1f)                        // sıcak ahşap
        };

        /// <summary>Çizginin çevresindeki koyu kenar; açık zeminde de seçilir.</summary>
        public static Color OutlineColor(PathSurfaceType type) => type switch
        {
            PathSurfaceType.Bounce => new Color(0.15f, 0.34f, 0.60f, 0.85f),
            PathSurfaceType.Ice => new Color(0.30f, 0.50f, 0.72f, 0.80f),
            PathSurfaceType.Hazard => new Color(0.50f, 0.16f, 0.14f, 0.85f),
            _ => new Color(0.28f, 0.17f, 0.10f, 0.85f)
        };

        public static Color HighlightColor(PathSurfaceType type) =>
            type == PathSurfaceType.Ice ? new Color(1f, 1f, 1f, 0.65f) : new Color(1f, 1f, 1f, 0.28f);

        // ── Malzemeler ─────────────────────────────────────────────
        public static Material Crayon()
        {
            if (crayonMaterial == null)
                crayonMaterial = Create("PawPath Crayon Stroke", BuildCrayon());
            return crayonMaterial;
        }

        public static Material Surface(PathSurfaceType type)
        {
            int index = (int)type;
            if (index < 0 || index >= surfaceMaterials.Length)
                index = 0;
            if (surfaceMaterials[index] == null)
            {
                Texture2D texture = type switch
                {
                    PathSurfaceType.Bounce => BuildJelly(),
                    PathSurfaceType.Ice => BuildIce(),
                    PathSurfaceType.Hazard => BuildHazard(),
                    _ => BuildPlanks()
                };
                surfaceMaterials[index] = Create("PawPath Stroke " + type, texture);
            }
            return surfaceMaterials[index];
        }

        /// <summary>Katılaşma dalgası ve buz parıltısı için kenarları yumuşak ışık.</summary>
        public static Material Glow()
        {
            if (glowMaterial == null)
            {
                const int h = 32;
                var texture = NewTexture("StrokeGlow", 4, h, TextureWrapMode.Clamp);
                var pixels = new Color[4 * h];
                for (int y = 0; y < h; y++)
                {
                    float v = (y + 0.5f) / h;
                    float a = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(v - 0.5f) * 2f), 1.6f);
                    for (int x = 0; x < 4; x++)
                        pixels[y * 4 + x] = new Color(1f, 1f, 1f, a);
                }
                texture.SetPixels(pixels);
                texture.Apply();
                glowMaterial = Create("PawPath Stroke Glow", texture);
            }
            return glowMaterial;
        }

        static Material Create(string name, Texture2D texture)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("[PawPath] Sprites/Default shader bulunamadı.");
                return null;
            }
            return new Material(shader) { name = name, mainTexture = texture };
        }

        static Texture2D NewTexture(string name, int w, int h, TextureWrapMode wrap)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = wrap
            };
        }

        // ── Dokular (beyaz tabanlı; renk LineRenderer rengiyle verilir) ──
        // u: çizgi boyunca (1 dünya birimi), v: çizginin enine (0 alt, 1 üst).

        /// <summary>Pastel boya: pütürlü, hafif çizgili kenarlar.</summary>
        static Texture2D BuildCrayon()
        {
            const int w = 128, h = 32;
            var texture = NewTexture("CrayonStroke", w, h, TextureWrapMode.Repeat);
            var pixels = new Color[w * h];
            var random = new System.Random(4127);
            float[] edgeNoise = new float[w];
            for (int x = 0; x < w; x++)
                edgeNoise[x] = (float)random.NextDouble();
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = (y + 0.5f) / h;
                float grain = (float)random.NextDouble();
                float streak = 0.5f + 0.5f * Mathf.Sin(v * 46f + Mathf.Sin(x * 0.21f) * 1.6f);
                float shade = 0.86f + 0.10f * streak + 0.04f * grain;
                float edge = Mathf.Min(v, 1f - v);
                float rough = 0.05f + 0.12f * edgeNoise[(x + (v < 0.5f ? 0 : 37)) % w];
                float alpha = Mathf.Clamp01((edge - rough) / 0.07f);
                // Boyanın kâğıtta bıraktığı küçük boşluklar.
                if (grain < 0.05f && edge < 0.3f)
                    alpha *= 0.35f;
                pixels[y * w + x] = new Color(shade, shade, shade, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>Tahta köprü: birim başına üç tahta, aralarında koyu ek yeri ve çiviler.</summary>
        static Texture2D BuildPlanks()
        {
            const int w = 144, h = 32, plank = w / 3;
            var texture = NewTexture("PlankStroke", w, h, TextureWrapMode.Repeat);
            var pixels = new Color[w * h];
            var random = new System.Random(911);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int local = x % plank;
                int plankIndex = x / plank;
                float v = (y + 0.5f) / h;
                float grain = 0.90f + 0.05f * Mathf.Sin(v * 22f + local * 0.12f + plankIndex * 1.7f)
                    + 0.03f * (float)random.NextDouble();
                if (v > 0.80f) grain = Mathf.Min(1f, grain + 0.10f);   // üst kenar ışığı
                if (v < 0.20f) grain *= 0.78f;                          // alt kenar gölgesi
                if (local < 3) grain *= 0.50f;                          // tahtalar arası ek
                // Her tahtanın başında iki küçük çivi.
                float nail1 = (local - 8f) * (local - 8f) + (y - 9f) * (y - 9f);
                float nail2 = (local - 8f) * (local - 8f) + (y - 23f) * (y - 23f);
                if (nail1 < 3.2f || nail2 < 3.2f) grain *= 0.55f;
                float edge = Mathf.Min(v, 1f - v);
                float alpha = Mathf.Clamp01(edge / 0.06f);
                pixels[y * w + x] = new Color(grain, grain, grain, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>Jöle şerit: ortası parlak, üstte ıslak parlama bandı, küçük kabarcıklar.</summary>
        static Texture2D BuildJelly()
        {
            const int w = 96, h = 32;
            var texture = NewTexture("JellyStroke", w, h, TextureWrapMode.Repeat);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = (y + 0.5f) / h;
                float body = 0.78f + 0.22f * (1f - Mathf.Abs(v - 0.55f) * 2f);
                if (v > 0.68f && v < 0.80f) body = 1f;                  // parlama bandı
                float bx = x % 48f;
                float b1 = (bx - 14f) * (bx - 14f) + (y - 13f) * (y - 13f);
                float b2 = (bx - 34f) * (bx - 34f) + (y - 18f) * (y - 18f);
                if (b1 < 7f || b2 < 4f) body = Mathf.Min(1f, body + 0.18f);
                float edge = Mathf.Min(v, 1f - v);
                float alpha = Mathf.Clamp01(edge / 0.08f);
                pixels[y * w + x] = new Color(body, body, body, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>Buz: hafif mavi zemin üzerinde çapraz beyaz parıltı çizgileri.</summary>
        static Texture2D BuildIce()
        {
            const int w = 128, h = 32;
            var texture = NewTexture("IceStroke", w, h, TextureWrapMode.Repeat);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = (y + 0.5f) / h;
                float shade = 0.90f + 0.06f * v;
                float diagonal = (x + y * 0.9f) % 43f;
                if (diagonal < 3f) shade = 1f;
                if (diagonal > 20f && diagonal < 21.5f) shade = 0.98f;
                float edge = Mathf.Min(v, 1f - v);
                float alpha = Mathf.Clamp01(edge / 0.06f);
                pixels[y * w + x] = new Color(shade, shade, shade, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>Tehlike: çapraz uyarı şeritleri.</summary>
        static Texture2D BuildHazard()
        {
            const int w = 128, h = 32;
            var texture = NewTexture("HazardStroke", w, h, TextureWrapMode.Repeat);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = (y + 0.5f) / h;
                float stripe = ((x + y) / 16) % 2 == 0 ? 1f : 0.70f;
                float edge = Mathf.Min(v, 1f - v);
                float alpha = Mathf.Clamp01(edge / 0.06f);
                pixels[y * w + x] = new Color(stripe, stripe, stripe, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
