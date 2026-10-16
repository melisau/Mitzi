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
    /// Altı kedinin (Mitzi, Pamuk, Kömür, Tarçın, Moka, Ada) yeni gerçekçi karelerini
    /// Sprite olarak içe aktarır, her kedi için Animator Controller kurar ve katalogdaki
    /// kedi tanımına bağlar.
    ///
    /// Kareler: Assets/_Game/Art/Cats/{id}/{id}_{poz}.png
    /// Tüm kareler aynı ölçekte ve aynı zemin çizgisiyle hazırlandığı için hepsine
    /// AYNI Pixels Per Unit verilir: yürürken/zıplarken büyüyüp küçülme olmaz.
    /// </summary>
    [InitializeOnLoad]
    internal static class CatBreedAnimationBuilder
    {
        const string CatalogPath = "Assets/_Game/Content/PawPathCatalog.asset";
        const string Root = "Assets/_Game/Art/Cats";
        const string SessionKey = "PawPath.CatBreedAnimationBuilder.v8";
        // Zemin karelerinin tuvali 1120 px yüksek; diğer oyun kareleriyle aynı 11.5 birim.
        const float PixelsPerUnit = 1120f / 11.5f;
        // Zemin/hava karelerinde patiler tuvalin altından %4.5 yukarıda. Pivotu, eski
        // Mitzi karelerindeki ayak hizasıyla (%9.3) aynı dünya noktasına gelecek
        // şekilde ayarlıyoruz; böylece kedi zemine gömülmez ve havada da durmaz.
        static readonly Vector2 GroundPivot = new Vector2(0.5f, 0.5f - 0.093f + 0.0446f);

        static readonly string[] CatIds = { "mitzi", "pamuk", "komur", "tarcin", "moka", "ada" };

        // Yürüme: 24 kare, 30 fps (0.8 sn'lik döngü). Dört bacak sırayla, eklemli adım atar;
        // yerdeki pati zeminde sabit kalır, öne savrulan pati kalkar.
        static readonly string[] WalkFrames =
            Enumerable.Range(1, 24).Select(i => $"walk_{i:00}").ToArray();

        // durum adı -> (kareler, fps, döngü)
        static readonly (string state, string[] frames, float fps, bool loop)[] States =
        {
            ("Walk",   WalkFrames, 30f, true),
            ("Idle",   new[] { "idle_01", "idle_02" }, 1.5f, true),
            ("Jump",   new[] { "jump_01", "jump_02", "jump_03", "jump_04_land" }, 7f, false),
            ("Crouch", new[] { "crouch_01" }, 1f, false),   // gövde yere yaklaşır, bacaklar bükülür
            ("Sit",    new[] { "sit_01", "sit_02" }, 4f, false),
            ("Lie",    new[] { "lie_01" }, 1f, false),
            ("Sleep",  new[] { "sleep_01" }, 1f, false),
            ("Eat",    new[] { "eat_01" }, 1f, false),
            ("Climb",  new[] { "climb_01", "climb_02", "climb_03", "climb_04" }, 6f, true),
        };

        static CatBreedAnimationBuilder()
        {
            EditorApplication.delayCall += BuildOnce;
        }

        [MenuItem("Paw Path/Kedi Irk Animasyonlarını Yeniden Kur")]
        static void BuildFromMenu() => Build(true);

        static void BuildOnce()
        {
            if (SessionState.GetBool(SessionKey, false))
                return;
            // Altı kedinin hepsi kurulana kadar her derlemede yeniden dene
            // (yeni dosyaların bir kısmı henüz içe aktarılmamış olabilir).
            if (Build(false) >= CatIds.Length)
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

            int built = 0;
            foreach (var id in CatIds)
            {
                var cat = catalog.cats.Find(c => c != null && c.id == id);
                if (cat == null)
                    continue;
                string folder = $"{Root}/{id}";
                if (!AssetDatabase.IsValidFolder(folder))
                    continue;

                var sprites = new Dictionary<string, Sprite>();
                foreach (var s in States)
                    foreach (var f in s.frames)
                        if (!sprites.ContainsKey(f))
                            sprites[f] = ImportSprite($"{folder}/{id}_{f}.png",
                                f.StartsWith("climb") ? new Vector2(0.5f, 0.5f) : GroundPivot);
                if (sprites["walk_01"] == null)
                {
                    Debug.LogWarning($"{id}: kareler bulunamadı ({folder}).");
                    continue;
                }

                string controllerPath = $"{folder}/{id}.controller";
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)
                                 ?? AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var sm = controller.layers[0].stateMachine;
                AnimatorState walkState = null;
                foreach (var s in States)
                {
                    var frames = s.frames.Select(f => sprites[f]).Where(sp => sp != null).ToArray();
                    if (frames.Length == 0)
                        continue;
                    var clip = CreateClip($"{folder}/{id}_{s.state.ToLowerInvariant()}.anim",
                        $"{id}_{s.state.ToLowerInvariant()}", frames, s.fps, s.loop);
                    var state = sm.states.Select(c => c.state).FirstOrDefault(st => st.name == s.state)
                                ?? sm.AddState(s.state);
                    state.motion = clip;
                    if (s.state == "Walk")
                        walkState = state;
                }
                if (walkState != null)
                    sm.defaultState = walkState;
                EditorUtility.SetDirty(controller);

                cat.animator = controller;
                cat.idleSprite = sprites["idle_01"] ?? sprites["walk_01"];
                cat.portrait = sprites["sit_02"] ?? cat.idleSprite;
                cat.sleepFrames = new[] { sprites["lie_01"], sprites["sleep_01"] }
                    .Where(sp => sp != null).ToArray();
                cat.walkFrames = Frames(sprites, WalkFrames);
                cat.idleFrames = Frames(sprites, "idle_01", "idle_02");
                cat.jumpFrames = Frames(sprites, "jump_01", "jump_02", "jump_03", "jump_04_land");
                // Tırmanma: her kedi kendi renginde (ağaçta ve evdeki kedi ağacında).
                cat.climbFrames = Frames(sprites, "climb_01", "climb_02", "climb_03", "climb_04");
                // Mama yeme: kedinin kendi renginde 4 kare (yoksa mama kabının kareleri kullanılır).
                cat.eatFrames = Enumerable.Range(1, 4)
                    .Select(i => ImportSprite($"{folder}/{id}_eatanim_{i:00}.png", new Vector2(0.5f, 0.5f)))
                    .Where(sp => sp != null).ToArray();
                // Kum kabı kareleri tools/build_litter_frames.py ile üretilir (giriş, içeride, kumda).
                var litter = new[] { "litter_out", "litter_in2", "litter_in" }
                    .Select(f => AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{id}_{f}.png"))
                    .ToArray();
                if (litter.All(sp => sp != null))
                    cat.litterFrames = litter;
                // Evde uyuma (yatma -> uyuma) geçişini bütün kediler için aç.
                cat.useDetailedHomeAnimations = true;
                EditorUtility.SetDirty(cat);
                built++;
            }

            // Kazanma ekranı arka planı boşsa proje kökündeki görseli bağla.
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
            // Yalnız katalog ve kedi tanımlarını kaydet (paket klasörlerine yazmaya çalışma).
            EditorSave.Save(catalog.cats.Where(c => c != null).Cast<Object>().Append(catalog).ToArray());
            Debug.Log($"{built}/{CatIds.Length} kedi için yürüme, zıplama, oturma, uyuma, yeme ve tırmanma animasyonları kuruldu.");
            return built;
        }

        static Sprite[] Frames(Dictionary<string, Sprite> sprites, params string[] keys) =>
            keys.Select(k => sprites.TryGetValue(k, out var s) ? s : null).Where(s => s != null).ToArray();

        static Sprite ImportSprite(string path, Vector2 pivot)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null && System.IO.File.Exists(path))
            {
                // Dosya diske yeni yazıldıysa henüz içe aktarılmamış olabilir.
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }
            if (importer == null)
                return null;
            bool dirty = importer.textureType != TextureImporterType.Sprite
                         || importer.spriteImportMode != SpriteImportMode.Single
                         || !importer.alphaIsTransparency
                         || importer.mipmapEnabled
                         || Mathf.Abs(importer.spritePixelsPerUnit - PixelsPerUnit) > 0.01f
                         || importer.maxTextureSize < 2048
                         || (importer.spritePivot - pivot).sqrMagnitude > 1e-6;
            if (dirty)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                // Tuvaller zaten hizalı: tüm karelerde aynı pivot noktası.
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
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
            // döngüde son kare de tam süre görünsün
            if (loop)
                keys.Add(new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[0] });
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }
    }
}
#endif
