#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using PawPath.Data;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PawPath.EditorTools
{
    /// <summary>
    /// Binds all six cats to transparent, registered side-view reference animations.
    /// </summary>
    [InitializeOnLoad]
    internal static class CatBreedAnimationBuilder
    {
        const string CatalogPath = "Assets/_Game/Content/PawPathCatalog.asset";
        const string PackRoot = "Assets/_Game/Art/AnimalsRealistic/Cats";
        const string SessionKey = "PawPath.CatBreedAnimationBuilder.realistic.v11.bodyproportions";
        // Common canvas and PPU keep scale stable between poses.
        const float PixelsPerUnit = 100f;
        // Extraction registers paws at 92% of the canvas height.
        static readonly Vector2 GroundPivot = new Vector2(0.5f, 0.08f);

        static readonly (string catId, string packColor)[] CatPackMap =
        {
            ("mitzi", "silver"),
            ("tarcin", "calico"),
            ("pamuk", "greywhite"),
            ("ada", "greywhite"),
            ("komur", "black"),
            ("moka", "black"),
        };

        static readonly string[] WalkFrames =
            Enumerable.Range(1, 12).Select(i => $"walk_{i:00}").ToArray();
        // Video poses cover a complete stride, unlike the older near-duplicate atlas.
        static string[] WalkKeys(string color)
        {
            var video = System.IO.Directory.GetFiles($"{PackRoot}/{color}", "videowalk_*.png");
            var paths = video;
            return paths.Select(System.IO.Path.GetFileNameWithoutExtension).OrderBy(key => key).ToArray();
        }

        static readonly (string state, string[] frames, float fps, bool loop)[] States =
        {
            ("Walk",   WalkFrames, 16f, true),
            ("Idle",   new[] { "idle_01", "idle_02", "idle_03", "idle_04" }, 2f, true),
            ("Jump",   new[] { "jump_01", "jump_02", "jump_03" }, 12f, false),
            ("Crouch", new[] { "crouch_01" }, 1f, false),
            ("CrouchWalk", new[] { "crouchwalk_01", "crouchwalk_02", "crouchwalk_03", "crouchwalk_04" }, 8f, true),
            ("Sit",    new[] { "sit_01", "sit_02" }, 4f, false),
            ("Lie",    new[] { "lie_01" }, 1f, false),
            ("Sleep",  new[] { "sleep_01", "sleep_02", "sleep_03", "sleep_04" }, 2f, true),
            ("Eat",    new[] { "eatanim_01", "eatanim_02", "eatanim_03", "eatanim_04" }, 8f, true),
            ("Drink",  new[] { "drink_01", "drink_02", "drink_03", "drink_04" }, 8f, true),
            ("Climb",  Enumerable.Range(1, 12).Select(i => $"climb_{i:00}").ToArray(), 24f, true),
        };

        static CatBreedAnimationBuilder()
        {
            EditorApplication.delayCall += BuildOnce;
        }

        [MenuItem("Paw Path/Gerçekçi Kedi Animasyonlarını Yeniden Kur")]
        public static void BuildFromMenu() => Build(true);

        static void BuildOnce()
        {
            if (SessionState.GetBool(SessionKey, false))
                return;
            if (Build(false) >= CatPackMap.Length)
                SessionState.SetBool(SessionKey, true);
        }

        static int Build(bool log)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogWarning("Kedi animasyonları kurulamadı: katalog bulunamadı.");
                return 0;
            }

            if (!AssetDatabase.IsValidFolder(PackRoot))
            {
                Debug.LogWarning($"Pack klasörü yok: {PackRoot}. Önce sheet'leri dilimleyin.");
                return 0;
            }

            // Renk başına bir controller; aynı rengi paylaşan kediler onu kullanır.
            var controllersByColor = new Dictionary<string, AnimatorController>();
            var spritesByColor = new Dictionary<string, Dictionary<string, Sprite>>();

            foreach (var color in CatPackMap.Select(m => m.packColor).Distinct())
            {
                string folder = $"{PackRoot}/{color}";
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    Debug.LogWarning($"Pack rengi eksik: {folder}");
                    continue;
                }

                var sprites = LoadPackSprites(folder);
                if (!sprites.TryGetValue("videowalk_01", out var walk0) || walk0 == null)
                {
                    Debug.LogWarning($"{color}: videowalk_01 bulunamadı ({folder}).");
                    continue;
                }

                spritesByColor[color] = sprites;
                controllersByColor[color] = BuildController(folder, color, sprites);
            }

            int built = 0;
            foreach (var (id, color) in CatPackMap)
            {
                var cat = catalog.cats.Find(c => c != null && c.id == id);
                if (cat == null || !spritesByColor.TryGetValue(color, out var sprites))
                    continue;
                if (!controllersByColor.TryGetValue(color, out var controller))
                    continue;

                cat.animator = controller;
                cat.isStarterCat = id == "mitzi";
                if (cat.isStarterCat) cat.unlockAfterLevel = 0;
                cat.idleSprite = SpriteOr(sprites, "idle_01") ?? SpriteOr(sprites, "videowalk_01");
                cat.portrait = SpriteOr(sprites, "sit_02")
                               ?? SpriteOr(sprites, "sit_01")
                               ?? cat.idleSprite;
                cat.sleepFrames = new[]
                    {
                        SpriteOr(sprites, "lie_01"),
                        SpriteOr(sprites, "sleep_01"), SpriteOr(sprites, "sleep_02"),
                        SpriteOr(sprites, "sleep_03"), SpriteOr(sprites, "sleep_04")
                    }
                    .Where(sp => sp != null).ToArray();
                cat.walkFrames = Frames(sprites, WalkKeys(color));
                cat.crouchWalkFrames = Frames(sprites, "crouchwalk_01", "crouchwalk_02", "crouchwalk_03", "crouchwalk_04");
                cat.idleFrames = Frames(sprites, "idle_01", "idle_02", "idle_03", "idle_04");
                cat.drinkFrames = Frames(sprites, "drink_01", "drink_02", "drink_03", "drink_04");
                cat.jumpApexSprite = SpriteOr(sprites, "jump_apex");
                cat.jumpFrames = Frames(sprites, "jump_01", "jump_02", "jump_03", "jump_04_land");
                cat.climbFrames = Frames(sprites, Enumerable.Range(1, 12).Select(i => $"climb_{i:00}").ToArray());
                cat.eatFrames = Enumerable.Range(1, 4)
                    .Select(i => SpriteOr(sprites, $"eatanim_{i:00}"))
                    .Where(sp => sp != null).ToArray();
                cat.useDetailedHomeAnimations = true;
                EditorUtility.SetDirty(cat);
                built++;
            }

            if (catalog.completionBackground == null)
            {
                const string completionPath = "Assets/complete_screen.jpg";
                var bg = AssetDatabase.LoadAssetAtPath<Sprite>(completionPath);
                if (bg == null)
                {
                    var importer = AssetImporter.GetAtPath(completionPath) as TextureImporter;
                    if (importer != null)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spriteImportMode = SpriteImportMode.Single;
                        importer.SaveAndReimport();
                        bg = AssetDatabase.LoadAssetAtPath<Sprite>(completionPath);
                    }
                }
                catalog.completionBackground = bg;
            }

            EditorUtility.SetDirty(catalog);
            catalog.cats = catalog.cats.OrderBy(c => c != null && c.id == "mitzi" ? 0 : 1).ToList();
            catalog.completionFaces = catalog.cats.Where(c => c != null)
                .Select(c => c.portrait).Where(s => s != null).ToArray();
            EditorSave.Save(catalog.cats.Where(c => c != null).Cast<Object>().Append(catalog).ToArray());
            if (log || built > 0)
                Debug.Log(
                    $"{built}/{CatPackMap.Length} kedi gerçekçi animasyonlara bağlandı.");
            return built;
        }

        static Dictionary<string, Sprite> LoadPackSprites(string folder)
        {
            var sprites = new Dictionary<string, Sprite>();
            float walkPpu = CalibratedWalkPpu(folder);
            foreach (var key in WalkKeys(System.IO.Path.GetFileName(folder)))
                sprites[key] = ImportSprite($"{folder}/{key}.png", GroundPivot, walkPpu);
            sprites["jump_apex"] = ImportSprite($"{folder}/jump_apex.png", GroundPivot);
            sprites["jump_04_land"] = ImportSprite($"{folder}/jump_04_land.png", GroundPivot);
            foreach (var s in States.Where(state => state.state != "Walk"))
            foreach (var f in s.frames)
                if (!sprites.ContainsKey(f))
                    sprites[f] = ImportSprite($"{folder}/{f}.png",
                        f.StartsWith("climb") ? new Vector2(0.5f, 0.5f) : GroundPivot);

            for (int i = 1; i <= 4; i++)
            {
                string key = $"eatanim_{i:00}";
                sprites[key] = ImportSprite($"{folder}/{key}.png", new Vector2(0.5f, 0.5f));
            }

            return sprites;
        }

        static AnimatorController BuildController(
            string folder, string color, Dictionary<string, Sprite> sprites)
        {
            string controllerPath = $"{folder}/{color}.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)
                             ?? AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var sm = controller.layers[0].stateMachine;
            AnimatorState walkState = null;
            foreach (var s in States)
            {
                var keys = s.state == "Walk" ? WalkKeys(color) : s.frames;
                var frames = keys.Select(f => SpriteOr(sprites, f)).Where(sp => sp != null).ToArray();
                if (frames.Length == 0)
                    continue;
                var clip = CreateClip($"{folder}/{color}_{s.state.ToLowerInvariant()}.anim",
                    $"{color}_{s.state.ToLowerInvariant()}", frames,
                    s.state == "Walk" ? frames.Length / 0.5f : s.fps, s.loop);
                var state = sm.states.Select(c => c.state).FirstOrDefault(st => st.name == s.state)
                            ?? sm.AddState(s.state);
                state.motion = clip;
                EditorUtility.SetDirty(state);
                if (s.state == "Walk")
                    walkState = state;
            }
            if (walkState != null)
                sm.defaultState = walkState;
            EditorUtility.SetDirty(sm);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssetIfDirty(controller);
            return controller;
        }

        static Sprite SpriteOr(Dictionary<string, Sprite> sprites, string key) =>
            sprites.TryGetValue(key, out var s) ? s : null;

        static Sprite[] Frames(Dictionary<string, Sprite> sprites, params string[] keys) =>
            keys.Select(k => SpriteOr(sprites, k)).Where(s => s != null).ToArray();

        static float CalibratedWalkPpu(string folder)
        {
            // Raised tails must not make the walking torso smaller than idle.
            // Use ear-to-ground height on the head side, with one scale per loop.
            float standing = BodyHeightPixels($"{folder}/idle_01.png");
            var heights = WalkKeys(System.IO.Path.GetFileName(folder))
                .Select(key => BodyHeightPixels($"{folder}/{key}.png")).Where(h => h > 0f).OrderBy(h => h).ToArray();
            if (standing <= 0f || heights.Length == 0) return PixelsPerUnit;
            float median = (heights[(heights.Length - 1) / 2] + heights[heights.Length / 2]) * 0.5f;
            // A lowered head during a stride must not enlarge the entire torso.
            // Keep the stronger of the head-height and torso-thickness constraints,
            // with a single import scale for the whole loop (no per-frame pulsing).
            float torso = TorsoThicknessPixels($"{folder}/idle_01.png");
            var thicknesses = WalkKeys(System.IO.Path.GetFileName(folder))
                .Select(key => TorsoThicknessPixels($"{folder}/{key}.png"))
                .Where(h => h > 0f).OrderBy(h => h).ToArray();
            float ratio = median / standing;
            if (torso > 0f && thicknesses.Length > 0)
            {
                float walkingTorso = (thicknesses[(thicknesses.Length - 1) / 2] + thicknesses[thicknesses.Length / 2]) * .5f;
                ratio = Mathf.Max(ratio, walkingTorso / torso);
            }
            return PixelsPerUnit * Mathf.Clamp(ratio, 0.65f, 1.35f);
        }

        static float TorsoThicknessPixels(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(System.IO.File.ReadAllBytes(path))) return 0f;
                var pixels = texture.GetPixels32();
                int left = texture.width, right = -1;
                for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                    if (pixels[y * texture.width + x].a > 96)
                    { left = Mathf.Min(left, x); right = Mathf.Max(right, x); }
                if (right <= left) return 0f;
                var thicknesses = new List<int>();
                // Mid-body columns exclude head and tail. The longest solid run
                // ignores detached paws and the tail without counting empty pixels.
                for (int x = Mathf.FloorToInt(left + (right - left) * .35f);
                    x < Mathf.FloorToInt(left + (right - left) * .60f); x++)
                {
                    int run = 0, longest = 0;
                    for (int y = 0; y < texture.height; y++)
                    {
                        run = pixels[y * texture.width + x].a > 96 ? run + 1 : 0;
                        longest = Mathf.Max(longest, run);
                    }
                    if (longest > 0) thicknesses.Add(longest);
                }
                thicknesses.Sort();
                return thicknesses.Count == 0 ? 0f :
                    (thicknesses[(thicknesses.Count - 1) / 2] + thicknesses[thicknesses.Count / 2]) * .5f;
            }
            finally { Object.DestroyImmediate(texture); }
        }

        static float BodyHeightPixels(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!texture.LoadImage(System.IO.File.ReadAllBytes(path))) return 0f;
                var pixels = texture.GetPixels32();
                int left = texture.width, right = -1, floor = texture.height, head = -1;
                for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                    if (pixels[y * texture.width + x].a > 96)
                    { left = Mathf.Min(left, x); right = Mathf.Max(right, x); floor = Mathf.Min(floor, y); }
                int headSide = Mathf.FloorToInt(left + (right - left) * 0.45f);
                for (int y = 0; y < texture.height; y++)
                for (int x = headSide; x < texture.width; x++)
                    if (pixels[y * texture.width + x].a > 96) head = Mathf.Max(head, y);
                return head >= floor ? head - floor + 1 : 0f;
            }
            finally { Object.DestroyImmediate(texture); }
        }

        static Sprite ImportSprite(string path, Vector2 pivot, float ppu = PixelsPerUnit)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null && System.IO.File.Exists(path))
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }
            if (importer == null)
                return null;
            bool dirty = importer.textureType != TextureImporterType.Sprite
                         || importer.spriteImportMode != SpriteImportMode.Single
                         || !importer.alphaIsTransparency
                         || importer.mipmapEnabled
                         || importer.filterMode != FilterMode.Bilinear
                         || Mathf.Abs(importer.spritePixelsPerUnit - ppu) > 0.01f
                         || importer.maxTextureSize < 512
                         || (importer.spritePivot - pivot).sqrMagnitude > 1e-6;
            if (dirty)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 512;
                importer.spritePixelsPerUnit = ppu;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static AnimationClip CreateClip(string path, string name, Sprite[] frames, float fps, bool loop)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = name };
                AssetDatabase.CreateAsset(clip, path);
            }
            clip.frameRate = fps;
            var binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            var keys = frames.Select((sp, i) => new ObjectReferenceKeyframe { time = i / fps, value = sp }).ToList();
            // Each sprite is held for one frame. Looping wraps to the first key;
            // adding that first key again would extend the clip by another frame.
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            if (loop)
            {
                // Unity otherwise adds a held frame after the closing loop key.
                settings.startTime = 0f;
                settings.stopTime = frames.Length / fps;
            }
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssetIfDirty(clip);
            return clip;
        }
    }
}
#endif
