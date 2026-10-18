using System;
using UnityEngine;

namespace PawPath.UI
{
    /// <summary>
    /// Yazı tipine bağlı olmayan, okuma bilmeyen çocukların da anlayacağı
    /// arayüz ikonlarını çalışma anında kenar yumuşatmalı (SDF) olarak çizer.
    /// Unity'nin yerleşik yazı tipi emoji çizemediği için 🍗 💧 🌙 🪙 yerine
    /// bu sprite'lar kullanılır. Beyaz siluet ikonlar Image.color ile boyanır.
    /// </summary>
    public static class CozyIconFactory
    {
        const int IconSize = 128;

        static Sprite coin, gear, food, water, moon, heart, paw, circle, pill, candy, pencil;
        static Sprite playIcon, bag, catFace, pillButton, bubble;
        static Sprite softEllipse, softGlow;
        static Sprite starIcon, cloudIcon, ringIcon, handIcon;
        static Sprite[] handFrames;
        static Sprite treeIcon, buildingIcon, boltIcon, gamepadIcon, flagIcon;
        static Sprite arrowUp, arrowDown, arrowLeft, arrowRight, pauseIcon, restartIcon, house;
        static Sprite eraserIcon, snowflakeIcon, unpackIcon, dotIcon, closeIcon, touchFinger;

        public static Sprite TouchFinger() => touchFinger != null ? touchFinger : touchFinger = BuildIcon("IconTouchFinger", p =>
            Mathf.Min(RoundBox(p, new Vector2(60f, 85f), new Vector2(10f, 33f), 9f),
                Mathf.Min(RoundBox(p, new Vector2(70f, 43f), new Vector2(29f, 28f), 18f),
                    RoundBox(p, new Vector2(36f, 45f), new Vector2(12f, 20f), 10f))));

        public static Sprite Close() => closeIcon != null ? closeIcon : closeIcon = BuildIcon("IconClose", p =>
        {
            var v = p - new Vector2(64f, 64f);
            var q = new Vector2((v.x + v.y) * 0.7071f, (v.y - v.x) * 0.7071f);
            return Mathf.Min(RoundBox(q, Vector2.zero, new Vector2(10f, 43f), 6f),
                RoundBox(q, Vector2.zero, new Vector2(43f, 10f), 6f));
        });

        public static Sprite Dot() => dotIcon != null ? dotIcon : dotIcon = BuildIcon("IconDot", p => Circle(p, new Vector2(64f, 64f), 52f));

        public static Sprite Unpack() => unpackIcon != null ? unpackIcon : unpackIcon = BuildIcon("IconUnpack", UnpackSdf);

        static float UnpackSdf(Vector2 p)
        {
            float box = RoundBox(p, new Vector2(64f, 36f), new Vector2(43f, 23f), 6f);
            float inside = RoundBox(p, new Vector2(64f, 42f), new Vector2(33f, 21f), 3f);
            float rim = Mathf.Max(box, -inside);
            Vector2 q = (p - new Vector2(64f, 85f)) / 0.55f + new Vector2(64f, 64f);
            return Mathf.Min(rim, ArrowSdf(q, 0f) * 0.55f);
        }

        public static Sprite Eraser() => eraserIcon != null ? eraserIcon : eraserIcon = BuildIcon("IconEraser", EraserSdf);
        public static Sprite Snowflake() => snowflakeIcon != null ? snowflakeIcon : snowflakeIcon = BuildIcon("IconSnowflake", SnowflakeSdf);

        static float EraserSdf(Vector2 p)
        {
            Vector2 q = p - new Vector2(64f, 66f);
            q = new Vector2((q.x + q.y) * 0.7071f, (q.y - q.x) * 0.7071f) + new Vector2(64f, 64f);
            float body = RoundBox(q, new Vector2(64f, 64f), new Vector2(23f, 43f), 9f);
            float seam = RoundBox(q, new Vector2(64f, 56f), new Vector2(28f, 3f), 1f);
            return Mathf.Max(body, -seam);
        }

        static float SnowflakeSdf(Vector2 p)
        {
            float d = 1000f;
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                Vector2 v = p - new Vector2(64f, 64f);
                Vector2 q = new Vector2(v.x * Mathf.Cos(a) + v.y * Mathf.Sin(a),
                    -v.x * Mathf.Sin(a) + v.y * Mathf.Cos(a));
                d = Mathf.Min(d, RoundBox(q, new Vector2(0f, 24f), new Vector2(4f, 25f), 3f));
                Vector2 branch = q - new Vector2(0f, 28f);
                branch = new Vector2((branch.x - branch.y) * 0.7071f, (branch.x + branch.y) * 0.7071f);
                d = Mathf.Min(d, RoundBox(branch, new Vector2(0f, 9f), new Vector2(3f, 12f), 2f));
            }
            return d;
        }

        /// <summary>Altın renkli, ortasında pati olan para. Boyanmadan kullanılır.</summary>
        public static Sprite Coin() => coin != null ? coin : coin = BuildCoin();
        public static Sprite Gear() => gear != null ? gear : gear = BuildIcon("IconGear", GearSdf);
        public static Sprite FoodBowl() => food != null ? food : food = BuildIcon("IconFoodBowl", FoodBowlSdf);
        public static Sprite WaterDrop() => water != null ? water : water = BuildIcon("IconWaterDrop", DropSdf);
        public static Sprite Moon() => moon != null ? moon : moon = BuildIcon("IconMoon", MoonSdf);
        public static Sprite Heart() => heart != null ? heart : heart = BuildIcon("IconHeart", HeartSdf);
        public static Sprite Paw() => paw != null ? paw : paw = BuildIcon("IconPaw", PawSdf);
        public static Sprite Pencil() => pencil != null ? pencil : pencil = BuildIcon("IconPencil", PencilSdf);
        public static Sprite Play() => playIcon != null ? playIcon : playIcon = BuildIcon("IconPlay", PlaySdf);
        /// <summary>Çizgili tenteli küçük dükkân: Dükkan düğmesinin ikonu.</summary>
        public static Sprite Storefront() => bag != null ? bag : bag = BuildIcon("IconStorefront", StorefrontSdf);
        /// <summary>Okşama eli: kahve kenarlı, açık ten rengi, avucu pembe (boyanmadan kullanılır).</summary>
        public static Sprite PettingHand() => handIcon != null ? handIcon : handIcon = BuildHand(null);

        /// <summary>
        /// Parmakların sırayla hafifçe kıvrılıp açıldığı döngü kareleri
        /// (serçe parmaktan işaret parmağına doğru dalga gibi).
        /// </summary>
        public static Sprite[] PettingHandFrames()
        {
            if (handFrames != null)
                return handFrames;
            const int count = 8;
            handFrames = new Sprite[count];
            var curls = new float[4];
            for (int f = 0; f < count; f++)
            {
                for (int i = 0; i < 4; i++)
                    curls[i] = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * f / count - i * 0.9f);
                handFrames[f] = BuildHand(curls);
            }
            return handFrames;
        }

        static Sprite BuildHand(float[] curls)
        {
            var pixels = new Color[IconSize * IconSize];
            Paint(pixels, IconSize, IconSize, p => HandSdf(p, curls) - 4f, new Color(0.43f, 0.27f, 0.20f, 1f));
            Paint(pixels, IconSize, IconSize, p => HandSdf(p, curls), new Color(1f, 0.88f, 0.78f, 1f));
            Paint(pixels, IconSize, IconSize, p => Ellipse(p, new Vector2(58f, 50f), 14f, 10f),
                new Color(1f, 0.78f, 0.75f, 0.6f));
            return ToSprite("IconPettingHand", pixels, IconSize, IconSize, Vector4.zero);
        }

        // Parmaklar: x merkezi, taban yüksekliği, yarı uzunluk, yarı genişlik.
        static readonly Vector4[] HandFingers =
        {
            new Vector4(42f, 63f, 17f, 6.5f), new Vector4(57f, 67f, 22f, 6.5f),
            new Vector4(72f, 66f, 21f, 6.5f), new Vector4(86f, 64f, 15f, 6f)
        };

        static float HandSdf(Vector2 p, float[] curls)
        {
            float palm = RoundBox(p, new Vector2(64f, 44f), new Vector2(29f, 27f), 14f);
            float fingers = float.MaxValue;
            for (int i = 0; i < HandFingers.Length; i++)
            {
                Vector4 f = HandFingers[i];
                float curl = curls != null ? curls[i] : 0f;
                // Kıvrılan parmak kısalır ve tabanı etrafında hafifçe eğilir.
                float half = f.z - 5f * curl;
                Vector2 basePoint = new Vector2(f.x, f.y);
                Vector2 q = curls != null ? Rotate(p, basePoint, (curl - 0.5f) * 10f * Mathf.Deg2Rad) : p;
                fingers = Mathf.Min(fingers, RoundBox(q, new Vector2(f.x, f.y + half), new Vector2(f.w, half), f.w));
            }
            Vector2 t = Rotate(p, new Vector2(30f, 52f), -35f * Mathf.Deg2Rad);
            float thumb = RoundBox(t, new Vector2(30f, 52f), new Vector2(8f, 17f), 8f);
            return SmoothUnion(SmoothUnion(palm, fingers, 3f), thumb, 4f);
        }

        // Köpeğe basma efekti için çizgi film şekilleri.
        public static Sprite Star() => starIcon != null ? starIcon : starIcon = BuildIcon("IconStar", StarSdf);
        public static Sprite Cloud() => cloudIcon != null ? cloudIcon : cloudIcon = BuildIcon("IconCloud", CloudSdf);
        public static Sprite Ring() => ringIcon != null ? ringIcon : ringIcon = BuildIcon("IconRing", RingSdf);

        static Vector2[] starPoints;

        static float StarSdf(Vector2 p)
        {
            if (starPoints == null)
            {
                starPoints = new Vector2[10];
                for (int i = 0; i < 10; i++)
                {
                    float angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                    float radius = i % 2 == 0 ? 54f : 24f;
                    starPoints[i] = new Vector2(64f + radius * Mathf.Cos(angle), 62f + radius * Mathf.Sin(angle));
                }
            }
            return Polygon(p, starPoints) - 4f;
        }

        static float CloudSdf(Vector2 p)
        {
            float d = Circle(p, new Vector2(40f, 52f), 26f);
            d = Mathf.Min(d, Circle(p, new Vector2(66f, 66f), 34f));
            d = Mathf.Min(d, Circle(p, new Vector2(92f, 52f), 24f));
            return Mathf.Min(d, RoundBox(p, new Vector2(64f, 40f), new Vector2(44f, 16f), 14f));
        }

        static float RingSdf(Vector2 p) => Mathf.Abs(Circle(p, new Vector2(64f, 64f), 52f)) - 6f;

        /// <summary>Dünya uzayı için 1x1 birimlik yumuşak kenarlı oval (gölge). Ölçekle boyutlandırılır.</summary>
        public static Sprite SoftEllipse() => softEllipse != null ? softEllipse : softEllipse = BuildRadial("SoftEllipse", 64, 2.2f);
        /// <summary>Dünya uzayı için 1x1 birimlik merkezden dışa sönen ışık halesi.</summary>
        public static Sprite SoftGlow() => softGlow != null ? softGlow : softGlow = BuildRadial("SoftGlow", 64, 1.6f);

        static Sprite BuildRadial(string name, int size, float falloffPower)
        {
            var pixels = new Color[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / half;
                float dy = (y + 0.5f - half) / half;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Pow(Mathf.Clamp01(1f - r), falloffPower);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                size, 0, SpriteMeshType.FullRect);
        }

        // Oyun modu ve mekân seçimi.
        public static Sprite Tree() => treeIcon != null ? treeIcon : treeIcon = BuildIcon("IconTree", TreeSdf);
        public static Sprite Buildings() => buildingIcon != null ? buildingIcon : buildingIcon = BuildIcon("IconBuildings", BuildingsSdf);
        public static Sprite Bolt() => boltIcon != null ? boltIcon : boltIcon = BuildIcon("IconBolt", BoltSdf);
        public static Sprite Gamepad() => gamepadIcon != null ? gamepadIcon : gamepadIcon = BuildIcon("IconGamepad", GamepadSdf);
        /// <summary>Damalı bitiş bayrağı (dama kareleri oyuk, zemin rengi görünür).</summary>
        public static Sprite FinishFlag() => flagIcon != null ? flagIcon : flagIcon = BuildIcon("IconFinishFlag", FlagSdf);

        // Bölüm içi kontroller ve duraklat menüsü.
        public static Sprite ArrowUp() => arrowUp != null ? arrowUp : arrowUp = BuildIcon("IconArrowUp", p => ArrowSdf(p, 0f));
        public static Sprite ArrowRight() => arrowRight != null ? arrowRight : arrowRight = BuildIcon("IconArrowRight", p => ArrowSdf(p, Mathf.PI * 0.5f));
        public static Sprite ArrowDown() => arrowDown != null ? arrowDown : arrowDown = BuildIcon("IconArrowDown", p => ArrowSdf(p, Mathf.PI));
        public static Sprite ArrowLeft() => arrowLeft != null ? arrowLeft : arrowLeft = BuildIcon("IconArrowLeft", p => ArrowSdf(p, -Mathf.PI * 0.5f));
        public static Sprite Pause() => pauseIcon != null ? pauseIcon : pauseIcon = BuildIcon("IconPause", PauseSdf);
        public static Sprite Restart() => restartIcon != null ? restartIcon : restartIcon = BuildIcon("IconRestart", RestartSdf);
        public static Sprite House() => house != null ? house : house = BuildIcon("IconHouse", HouseSdf);

        /// <summary>Gözleri gülen kedi yüzü: duygu balonunda "mutluyum" anlamında.</summary>
        public static Sprite HappyCatFace() => catFace != null ? catFace : catFace = BuildIcon("IconHappyCat", CatFaceSdf);

        /// <summary>
        /// Yuvarlak ikon düğmeyle aynı dilde (koyu halka, hafif degrade, üst parlama)
        /// hap biçimli düğme zemini. Oyna ve Dükkan bunu kullanır (9-dilim).
        /// </summary>
        public static Sprite PillButton() => pillButton != null ? pillButton : pillButton = BuildPillButton();

        /// <summary>Altta kuyruğu olan beyaz konuşma balonu (kuyruk ucu sprite'ın altında).</summary>
        public static Sprite SpeechBubble() => bubble != null ? bubble : bubble = BuildSpeechBubble();

        /// <summary>Yuvarlak ikon düğme zemini: üstten alta hafif gölgeli dolgu ve koyu halka.</summary>
        public static Sprite CircleButton() => circle != null ? circle : circle = BuildCircleButton();

        /// <summary>Sayaç ve rozetler için yumuşak köşeli hap zemini (9-dilim).</summary>
        public static Sprite Pill() => pill != null ? pill : pill = BuildPill();

        /// <summary>Ana aksiyon düğmesi: parlak üst, koyu alt dudak (9-dilim, renkle boyanır).</summary>
        public static Sprite CandyButton() => candy != null ? candy : candy = BuildCandyButton();

        // ---------- Çizim altyapısı ----------

        delegate float Sdf(Vector2 p);

        static Sprite BuildIcon(string name, Sdf sdf)
        {
            var pixels = new Color[IconSize * IconSize];
            for (int y = 0; y < IconSize; y++)
            for (int x = 0; x < IconSize; x++)
            {
                float d = sdf(new Vector2(x + 0.5f, y + 0.5f));
                float a = Mathf.Clamp01(0.5f - d);
                pixels[y * IconSize + x] = new Color(1f, 1f, 1f, a);
            }
            return ToSprite(name, pixels, IconSize, IconSize, Vector4.zero);
        }

        static Sprite ToSprite(string name, Color[] pixels, int width, int height, Vector4 border)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, border);
        }

        static void Paint(Color[] pixels, int width, int height, Sdf sdf, Color color)
        {
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float coverage = Mathf.Clamp01(0.5f - sdf(new Vector2(x + 0.5f, y + 0.5f))) * color.a;
                if (coverage <= 0f)
                    continue;
                int i = y * width + x;
                Color dst = pixels[i];
                float outA = coverage + dst.a * (1f - coverage);
                if (outA <= 0f)
                    continue;
                Color result = (color * coverage + dst * dst.a * (1f - coverage)) / outA;
                result.a = outA;
                pixels[i] = result;
            }
        }

        // ---------- Temel SDF yardımcıları (negatif = içeride) ----------

        static float Circle(Vector2 p, Vector2 c, float r) => Vector2.Distance(p, c) - r;

        static float Ellipse(Vector2 p, Vector2 c, float rx, float ry)
        {
            Vector2 q = new Vector2((p.x - c.x) / rx, (p.y - c.y) / ry);
            return (q.magnitude - 1f) * Mathf.Min(rx, ry);
        }

        static float RoundBox(Vector2 p, Vector2 c, Vector2 half, float r)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + new Vector2(r, r);
            Vector2 outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            return outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
        }

        static float Polygon(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                Vector2 e = v[j] - v[i];
                Vector2 w = p - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3))
                    s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        static float SmoothUnion(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }

        static Vector2 Rotate(Vector2 p, Vector2 center, float radians)
        {
            Vector2 d = p - center;
            float c = Mathf.Cos(radians), s = Mathf.Sin(radians);
            return center + new Vector2(d.x * c - d.y * s, d.x * s + d.y * c);
        }

        // ---------- İkon şekilleri (y yukarı, 128x128 alan) ----------

        static float GearSdf(Vector2 p)
        {
            Vector2 c = new Vector2(64f, 64f);
            float d = Circle(p, c, 38f);
            for (int k = 0; k < 8; k++)
            {
                Vector2 q = Rotate(p, c, -k * Mathf.PI / 4f);
                d = Mathf.Min(d, RoundBox(q, new Vector2(64f, 104f), new Vector2(11f, 14f), 4f));
            }
            return Mathf.Max(d, -Circle(p, c, 16f));
        }

        static float FoodBowlSdf(Vector2 p)
        {
            // Kase gövdesi: elipsin alt yarısı.
            float body = Mathf.Max(Ellipse(p, new Vector2(64f, 62f), 56f, 46f), p.y - 60f);
            float rim = RoundBox(p, new Vector2(64f, 62f), new Vector2(60f, 7f), 6f);
            float foot = RoundBox(p, new Vector2(64f, 14f), new Vector2(26f, 6f), 3f);
            float bowl = Mathf.Min(Mathf.Min(body, rim), foot);
            // Kasenin üstünde mama taneleri; kenarla arasında ince boşluk bırakılır.
            float kibble = Circle(p, new Vector2(38f, 80f), 13f);
            kibble = Mathf.Min(kibble, Circle(p, new Vector2(64f, 88f), 16f));
            kibble = Mathf.Min(kibble, Circle(p, new Vector2(90f, 80f), 13f));
            kibble = Mathf.Min(kibble, Circle(p, new Vector2(51f, 100f), 11f));
            kibble = Mathf.Min(kibble, Circle(p, new Vector2(78f, 101f), 11f));
            kibble = Mathf.Max(kibble, -(rim - 5f));
            return Mathf.Min(bowl, kibble);
        }

        static readonly Vector2[] DropTip =
        {
            new Vector2(64f, 120f), new Vector2(31f, 60f), new Vector2(97f, 60f)
        };

        static float DropSdf(Vector2 p)
        {
            float round = Circle(p, new Vector2(64f, 50f), 38f);
            float tip = Polygon(p, DropTip);
            float drop = SmoothUnion(round, tip, 8f);
            // Parlama: damlanın içinde küçük bir boşluk, siluete hacim verir.
            float shine = Ellipse(p, new Vector2(48f, 48f), 7f, 13f);
            return Mathf.Max(drop, -shine);
        }

        static float MoonSdf(Vector2 p)
        {
            float moon = Mathf.Max(Circle(p, new Vector2(58f, 60f), 48f), -Circle(p, new Vector2(86f, 80f), 42f));
            float star1 = Circle(p, new Vector2(100f, 100f), 6f);
            float star2 = Circle(p, new Vector2(113f, 58f), 4f);
            return Mathf.Min(moon, Mathf.Min(star1, star2));
        }

        static float HeartSdf(Vector2 p)
        {
            const float scale = 100f;
            Vector2 q = new Vector2((p.x - 64f) / scale, (p.y - 12f) / scale);
            q.x = Mathf.Abs(q.x);
            float d;
            if (q.y + q.x > 1f)
                d = (q - new Vector2(0.25f, 0.75f)).magnitude - Mathf.Sqrt(2f) / 4f;
            else
            {
                float a = (q - new Vector2(0f, 1f)).sqrMagnitude;
                float t = 0.5f * Mathf.Max(q.x + q.y, 0f);
                float b = (q - new Vector2(t, t)).sqrMagnitude;
                d = Mathf.Sqrt(Mathf.Min(a, b)) * Mathf.Sign(q.x - q.y);
            }
            return d * scale;
        }

        static float PawSdf(Vector2 p)
        {
            float pad = Ellipse(p, new Vector2(64f, 42f), 32f, 26f);
            float toes = Circle(p, new Vector2(27f, 76f), 13f);
            toes = Mathf.Min(toes, Circle(p, new Vector2(49f, 100f), 14f));
            toes = Mathf.Min(toes, Circle(p, new Vector2(79f, 100f), 14f));
            toes = Mathf.Min(toes, Circle(p, new Vector2(101f, 76f), 13f));
            return Mathf.Min(pad, toes);
        }

        static readonly Vector2[] PencilTip =
        {
            new Vector2(64f, 8f), new Vector2(79f, 34f), new Vector2(49f, 34f)
        };

        static float PencilSdf(Vector2 p)
        {
            // Kalem ekseni q-uzayında dikeydir; 45° döndürülünce uç sol alta bakar.
            Vector2 q = Rotate(p, new Vector2(64f, 64f), Mathf.PI / 4f);
            float shaft = RoundBox(q, new Vector2(64f, 78f), new Vector2(15f, 40f), 4f);
            float band = RoundBox(q, new Vector2(64f, 102f), new Vector2(17f, 2.5f), 0f);
            float body = Mathf.Max(shaft, -band);
            float tip = Polygon(q, PencilTip);
            return Mathf.Min(body, tip);
        }

        static float TreeSdf(Vector2 p)
        {
            float canopy = SmoothUnion(Circle(p, new Vector2(64f, 82f), 30f), Circle(p, new Vector2(40f, 66f), 22f), 6f);
            canopy = SmoothUnion(canopy, Circle(p, new Vector2(88f, 66f), 22f), 6f);
            canopy = SmoothUnion(canopy, Circle(p, new Vector2(64f, 104f), 18f), 6f);
            float trunk = RoundBox(p, new Vector2(64f, 28f), new Vector2(9f, 22f), 3f);
            return Mathf.Min(canopy, trunk);
        }

        static float BuildingsSdf(Vector2 p)
        {
            float tall = RoundBox(p, new Vector2(44f, 52f), new Vector2(24f, 44f), 4f);
            float low = RoundBox(p, new Vector2(88f, 40f), new Vector2(22f, 32f), 4f);
            float windows = float.MaxValue;
            foreach (float cx in new[] { 34f, 54f })
            foreach (float cy in new[] { 30f, 52f, 74f })
                windows = Mathf.Min(windows, RoundBox(p, new Vector2(cx, cy), new Vector2(5f, 6f), 1.5f));
            foreach (float cx in new[] { 80f, 96f })
            foreach (float cy in new[] { 28f, 48f })
                windows = Mathf.Min(windows, RoundBox(p, new Vector2(cx, cy), new Vector2(4f, 5f), 1.5f));
            return Mathf.Max(Mathf.Min(tall, low), -windows);
        }

        static readonly Vector2[] BoltShape =
        {
            new Vector2(76f, 120f), new Vector2(30f, 58f), new Vector2(60f, 58f),
            new Vector2(46f, 8f), new Vector2(98f, 74f), new Vector2(68f, 74f)
        };

        static float BoltSdf(Vector2 p) => Polygon(p, BoltShape) - 3f;

        static float GamepadSdf(Vector2 p)
        {
            float grips = SmoothUnion(Circle(p, new Vector2(32f, 48f), 20f), Circle(p, new Vector2(96f, 48f), 20f), 4f);
            float body = SmoothUnion(RoundBox(p, new Vector2(64f, 62f), new Vector2(50f, 26f), 20f), grips, 6f);
            float dpad = Mathf.Min(RoundBox(p, new Vector2(38f, 64f), new Vector2(12f, 4f), 1.5f),
                RoundBox(p, new Vector2(38f, 64f), new Vector2(4f, 12f), 1.5f));
            float buttons = Mathf.Min(Circle(p, new Vector2(86f, 70f), 6f), Circle(p, new Vector2(98f, 58f), 6f));
            return Mathf.Max(body, -Mathf.Min(dpad, buttons));
        }

        static float FlagSdf(Vector2 p)
        {
            float pole = RoundBox(p, new Vector2(26f, 62f), new Vector2(5f, 54f), 3f);
            float cloth = RoundBox(p, new Vector2(70f, 90f), new Vector2(42f, 26f), 4f);
            float cells = float.MaxValue;
            for (int i = 0; i < 4; i++)
            for (int j = 0; j < 2; j++)
                if ((i + j) % 2 == 0)
                    cells = Mathf.Min(cells, RoundBox(p, new Vector2(38.5f + 21f * i, 77f + 26f * j),
                        new Vector2(10.5f, 13f), 0f));
            return Mathf.Min(pole, Mathf.Max(cloth, -cells));
        }

        static readonly Vector2[] ArrowHead =
        {
            new Vector2(64f, 104f), new Vector2(28f, 64f), new Vector2(100f, 64f)
        };

        /// <summary>Yukarı bakan kalın ok; angle ile saat yönünde döndürülür.</summary>
        static float ArrowSdf(Vector2 p, float angle)
        {
            Vector2 q = Rotate(p, new Vector2(64f, 64f), angle);
            float head = Polygon(q, ArrowHead) - 7f;
            float shaft = RoundBox(q, new Vector2(64f, 44f), new Vector2(13f, 26f), 5f);
            return Mathf.Min(head, shaft);
        }

        static float PauseSdf(Vector2 p)
        {
            return Mathf.Min(RoundBox(p, new Vector2(46f, 64f), new Vector2(13f, 38f), 7f),
                RoundBox(p, new Vector2(82f, 64f), new Vector2(13f, 38f), 7f));
        }

        static Vector2[] restartGap, restartHead;

        static float RestartSdf(Vector2 p)
        {
            Vector2 c = new Vector2(64f, 60f);
            const float r = 34f;
            if (restartGap == null)
            {
                float a0 = 40f * Mathf.Deg2Rad, a1 = 105f * Mathf.Deg2Rad;
                restartGap = new[]
                {
                    c, c + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * 90f,
                    c + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * 90f
                };
                Vector2 n = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
                Vector2 t = new Vector2(-n.y, n.x);
                Vector2 end = c + n * r;
                restartHead = new[] { end + t * 24f, end - t * 4f + n * 22f, end - t * 4f - n * 22f };
            }
            float ring = Mathf.Abs(Circle(p, c, r)) - 9f;
            ring = Mathf.Max(ring, -Polygon(p, restartGap));
            float head = Polygon(p, restartHead) - 2f;
            return Mathf.Min(ring, head);
        }

        static readonly Vector2[] HouseRoof =
        {
            new Vector2(64f, 114f), new Vector2(10f, 64f), new Vector2(118f, 64f)
        };

        static float HouseSdf(Vector2 p)
        {
            float roof = Polygon(p, HouseRoof) - 5f;
            float body = RoundBox(p, new Vector2(64f, 40f), new Vector2(38f, 32f), 5f);
            float chimney = RoundBox(p, new Vector2(94f, 96f), new Vector2(7f, 14f), 2f);
            float door = RoundBox(p, new Vector2(64f, 26f), new Vector2(12f, 20f), 4f);
            return Mathf.Max(Mathf.Min(Mathf.Min(roof, body), chimney), -door);
        }

        static readonly Vector2[] PlayTriangle =
        {
            new Vector2(40f, 22f), new Vector2(40f, 106f), new Vector2(108f, 64f)
        };

        static float PlaySdf(Vector2 p) => Polygon(p, PlayTriangle) - 8f;

        static readonly float[] AwningScallops = { 21f, 43f, 64f, 85f, 107f };
        static readonly float[] AwningStripes = { 32f, 54f, 75f, 96f };

        static float StorefrontSdf(Vector2 p)
        {
            // Tente: üstte bant, altta yarım daire fistonlar, aralarında çizgi boşlukları.
            float band = RoundBox(p, new Vector2(64f, 96f), new Vector2(54f, 14f), 6f);
            float scallops = float.MaxValue;
            foreach (float cx in AwningScallops)
                scallops = Mathf.Min(scallops, Circle(p, new Vector2(cx, 82f), 11f));
            float awning = Mathf.Min(band, Mathf.Max(scallops, p.y - 86f));
            float gaps = float.MaxValue;
            foreach (float cx in AwningStripes)
                gaps = Mathf.Min(gaps, RoundBox(p, new Vector2(cx, 92f), new Vector2(2f, 24f), 0f));
            awning = Mathf.Max(awning, -gaps);
            float roof = RoundBox(p, new Vector2(64f, 114f), new Vector2(48f, 5f), 4f);
            // Bina: pencere ve kapı oyulur.
            float body = RoundBox(p, new Vector2(64f, 38f), new Vector2(46f, 34f), 6f);
            float door = RoundBox(p, new Vector2(82f, 30f), new Vector2(13f, 26f), 4f);
            float window = RoundBox(p, new Vector2(42f, 44f), new Vector2(14f, 12f), 3f);
            body = Mathf.Max(body, -Mathf.Min(door, window));
            body = Mathf.Max(body, p.y - 66f);
            return Mathf.Min(Mathf.Min(awning, roof), body);
        }

        static readonly Vector2[] EarLeft = { new Vector2(18f, 62f), new Vector2(26f, 118f), new Vector2(58f, 90f) };
        static readonly Vector2[] EarRight = { new Vector2(110f, 62f), new Vector2(102f, 118f), new Vector2(70f, 90f) };
        static readonly Vector2[] CatNose = { new Vector2(58f, 48f), new Vector2(70f, 48f), new Vector2(64f, 41f) };

        static float CatFaceSdf(Vector2 p)
        {
            float head = Ellipse(p, new Vector2(64f, 54f), 50f, 42f);
            head = SmoothUnion(SmoothUnion(head, Polygon(p, EarLeft), 6f), Polygon(p, EarRight), 6f);
            // Gülen gözler (^ ^), "w" ağız ve burun siluetten oyulur.
            float eyeL = Mathf.Max(Mathf.Abs(Circle(p, new Vector2(44f, 58f), 10f)) - 3.5f, -(p.y - 60f));
            float eyeR = Mathf.Max(Mathf.Abs(Circle(p, new Vector2(84f, 58f), 10f)) - 3.5f, -(p.y - 60f));
            float mouthL = Mathf.Max(Mathf.Abs(Circle(p, new Vector2(57f, 40f), 7f)) - 3f, p.y - 40f);
            float mouthR = Mathf.Max(Mathf.Abs(Circle(p, new Vector2(71f, 40f), 7f)) - 3f, p.y - 40f);
            float nose = Polygon(p, CatNose) - 1f;
            float cut = Mathf.Min(Mathf.Min(Mathf.Min(eyeL, eyeR), Mathf.Min(mouthL, mouthR)), nose);
            return Mathf.Max(head, -cut);
        }

        // ---------- Renkli / dokulu zeminler ----------

        static Sprite BuildCoin()
        {
            var pixels = new Color[IconSize * IconSize];
            Vector2 c = new Vector2(64f, 64f);
            Paint(pixels, IconSize, IconSize, p => Circle(p, c, 58f), new Color(0.72f, 0.46f, 0.10f, 1f));
            Paint(pixels, IconSize, IconSize, p => Circle(p, c, 51f), new Color(1f, 0.79f, 0.24f, 1f));
            Paint(pixels, IconSize, IconSize, p => Mathf.Abs(Circle(p, c, 42f)) - 2.2f, new Color(0.86f, 0.58f, 0.13f, 1f));
            Paint(pixels, IconSize, IconSize, p =>
            {
                Vector2 q = (p - c) / 0.5f + c + new Vector2(0f, 4f);
                return PawSdf(q) * 0.5f;
            }, new Color(0.80f, 0.52f, 0.10f, 1f));
            Paint(pixels, IconSize, IconSize, p => Ellipse(Rotate(p, c, 0.6f), new Vector2(42f, 92f), 9f, 16f),
                new Color(1f, 1f, 0.92f, 0.55f));
            return ToSprite("IconCoin", pixels, IconSize, IconSize, Vector4.zero);
        }

        static Sprite BuildCircleButton()
        {
            const int size = 256;
            var pixels = new Color[size * size];
            Vector2 c = new Vector2(128f, 128f);
            // Koyu dış halka (boya rengiyle çarpılınca aynı rengin koyusu olur).
            Paint(pixels, size, size, p => Circle(p, c, 124f), new Color(0.62f, 0.62f, 0.62f, 1f));
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float a = Mathf.Clamp01(0.5f - Circle(p, c, 112f));
                if (a <= 0f)
                    continue;
                float shade = Mathf.Lerp(0.86f, 1f, y / (float)size);
                int i = y * size + x;
                pixels[i] = Color.Lerp(pixels[i], new Color(shade, shade, shade, 1f), a);
            }
            // Üstte yumuşak parlama.
            Paint(pixels, size, size, p => Ellipse(p, new Vector2(128f, 196f), 62f, 24f),
                new Color(1f, 1f, 1f, 0.22f));
            return ToSprite("UiCircleButton", pixels, size, size, Vector4.zero);
        }

        static Sprite BuildPillButton()
        {
            const int w = 208, h = 104;
            const float radius = h / 2f;
            const float ring = 8f;
            var pixels = new Color[w * h];
            Vector2 c = new Vector2(w / 2f, h / 2f);
            Vector2 half = new Vector2(w / 2f - 1f, h / 2f - 1f);
            Paint(pixels, w, h, p => RoundBox(p, c, half, radius - 1f), new Color(0.62f, 0.62f, 0.62f, 1f));
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float a = Mathf.Clamp01(0.5f - RoundBox(p, c, half - new Vector2(ring, ring), radius - 1f - ring));
                if (a <= 0f)
                    continue;
                float shade = Mathf.Lerp(0.86f, 1f, y / (float)h);
                int i = y * w + x;
                pixels[i] = Color.Lerp(pixels[i], new Color(shade, shade, shade, 1f), a);
            }
            Paint(pixels, w, h, p => RoundBox(p, new Vector2(w / 2f, h * 0.72f), new Vector2(w / 2f - radius * 0.9f, h * 0.10f), h * 0.10f),
                new Color(1f, 1f, 1f, 0.22f));
            return ToSprite("UiPillButton", pixels, w, h, new Vector4(radius, radius, radius, radius));
        }

        static readonly Vector2[] BubbleTail =
        {
            new Vector2(64f, 44f), new Vector2(96f, 44f), new Vector2(70f, 4f)
        };

        static Sprite BuildSpeechBubble()
        {
            const int w = 160, h = 176;
            var pixels = new Color[w * h];
            Sdf shape = p => Mathf.Min(RoundBox(p, new Vector2(80f, 106f), new Vector2(76f, 66f), 56f),
                Polygon(p, BubbleTail) - 2f);
            Paint(pixels, w, h, shape, new Color(0.60f, 0.47f, 0.37f, 1f));
            Paint(pixels, w, h, p => shape(p) + 4f, Color.white);
            return ToSprite("UiSpeechBubble", pixels, w, h, Vector4.zero);
        }

        static Sprite BuildPill()
        {
            const int w = 96, h = 64;
            var pixels = new Color[w * h];
            Paint(pixels, w, h, p => RoundBox(p, new Vector2(w / 2f, h / 2f), new Vector2(w / 2f - 1f, h / 2f - 1f), 30f),
                Color.white);
            return ToSprite("UiPill", pixels, w, h, new Vector4(32f, 31f, 32f, 31f));
        }

        static Sprite BuildCandyButton()
        {
            const int w = 128, h = 96;
            var pixels = new Color[w * h];
            Vector2 c = new Vector2(w / 2f, h / 2f);
            // Alt dudak: daha koyu ton, düğmeye basılabilir hacim verir.
            Paint(pixels, w, h, p => RoundBox(p, c, new Vector2(w / 2f - 1f, h / 2f - 1f), 28f),
                new Color(0.66f, 0.66f, 0.66f, 1f));
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float a = Mathf.Clamp01(0.5f - RoundBox(p, c + new Vector2(0f, 4f),
                    new Vector2(w / 2f - 5f, h / 2f - 9f), 24f));
                if (a <= 0f)
                    continue;
                float shade = Mathf.Lerp(0.90f, 1f, y / (float)h);
                int i = y * w + x;
                pixels[i] = Color.Lerp(pixels[i], new Color(shade, shade, shade, 1f), a);
            }
            Paint(pixels, w, h, p => RoundBox(p, new Vector2(w / 2f, h - 22f), new Vector2(w / 2f - 22f, 6f), 6f),
                new Color(1f, 1f, 1f, 0.30f));
            return ToSprite("UiCandyButton", pixels, w, h, new Vector4(32f, 32f, 32f, 32f));
        }
    }
}
