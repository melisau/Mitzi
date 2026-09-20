using System;
using System.Linq;
using PawPath.Data;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PawPath.EditorTools
{
    [InitializeOnLoad]
    internal static class SarmanWalkAnimationBuilder
    {
        private const int FrameCount = 31;
        private const float FrameRate = 12f;
        private const float NormalizedSpriteHeight = 11.5f;
        private const string CatalogPath = "Assets/_Game/Content/PawPathCatalog.asset";
        private const string ClipPath = "Assets/_Game/Art/cat_walk_sarman.anim";
        private const string ControllerPath = "Assets/_Game/Art/cat_walk_sarman.controller";
        private const string SessionKey = "PawPath.SarmanWalkAnimationBuilder.v2";

        static SarmanWalkAnimationBuilder()
        {
            EditorApplication.delayCall += BuildOncePerEditorSession;
        }

        [MenuItem("Paw Path/Rebuild Sarman Walk Animation")]
        private static void BuildFromMenu()
        {
            BuildAndAssign(true);
        }

        private static void BuildOncePerEditorSession()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);
            BuildAndAssign(false);
        }

        private static void BuildAndAssign(bool logSuccess)
        {
            var frames = new Sprite[FrameCount];
            for (var index = 1; index <= FrameCount; index++)
            {
                var path = GetFramePath(index);
                ConfigureAsSprite(path);
                frames[index - 1] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            if (frames.Any(frame => frame == null))
            {
                Debug.LogWarning("Sarman yürüyüş animasyonu kurulamadı: 31 karenin tamamı Sprite olarak yüklenemedi.");
                return;
            }

            var clip = GetOrCreateClip();
            var binding = new EditorCurveBinding
            {
                path = string.Empty,
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            var keys = frames
                .Select((frame, index) => new ObjectReferenceKeyframe
                {
                    time = index / FrameRate,
                    value = frame
                })
                .ToArray();

            clip.frameRate = FrameRate;
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
            EditorUtility.SetDirty(clip);

            var controller = GetOrCreateController(clip);
            AssignToSecondCat(controller, frames[0]);
            AssetDatabase.SaveAssets();

            if (logSuccess)
            {
                Debug.Log("31 karelik sarman yürüyüş animasyonu ikinci kediye bağlandı.");
            }
        }

        private static string GetFramePath(int index)
        {
            return $"Assets/cat_walk_sarman ({index}).png";
        }

        private static void ConfigureAsSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            var importedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            float importedHeight = importedTexture != null ? importedTexture.height : 1150f;
            float normalizedPixelsPerUnit = importedHeight / NormalizedSpriteHeight;
            var requiresReimport = importer.textureType != TextureImporterType.Sprite
                                   || importer.spriteImportMode != SpriteImportMode.Single
                                   || !importer.alphaIsTransparency
                                   || importer.mipmapEnabled
                                   || Mathf.Abs(importer.spritePixelsPerUnit - normalizedPixelsPerUnit) > 0.01f;
            if (!requiresReimport)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            // Karelerin tuval ölçüleri birbirinden farklı. Her sprite'ın dünyadaki
            // yüksekliğini eşitleyerek yürürken büyüyüp küçülme/titreme oluşmasını önleriz.
            importer.spritePixelsPerUnit = normalizedPixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static AnimationClip GetOrCreateClip()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip != null)
            {
                return clip;
            }

            clip = new AnimationClip { name = "cat_walk_sarman" };
            AssetDatabase.CreateAsset(clip, ClipPath);
            return clip;
        }

        private static AnimatorController GetOrCreateController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == "Sarman Walk");
            if (state == null)
            {
                state = stateMachine.AddState("Sarman Walk");
            }

            state.motion = clip;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void AssignToSecondCat(RuntimeAnimatorController controller, Sprite idleSprite)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PawPathCatalog>(CatalogPath);
            if (catalog == null || catalog.cats == null || catalog.cats.Count < 2 || catalog.cats[1] == null)
            {
                Debug.LogWarning("Sarman yürüyüş animasyonu kurulamadı: katalogdaki ikinci kedi bulunamadı.");
                return;
            }

            var secondCat = catalog.cats[1];
            secondCat.idleSprite = idleSprite;
            secondCat.animator = controller;
            EditorUtility.SetDirty(secondCat);
            EditorUtility.SetDirty(catalog);
        }
    }
}
