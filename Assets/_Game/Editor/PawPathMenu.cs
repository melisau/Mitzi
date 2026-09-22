using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PawPath.Content;
using PawPath.Core;
using PawPath.Data;

namespace PawPath.EditorTools
{
    public static class PawPathMenu
    {
        const string ContentDir = "Assets/_Game/Content";
        const string ScenePath = "Assets/Scenes/PawPath.unity";
        [InitializeOnLoadMethod]
        static void ProtectTextureInspectorWhenPlaying()
        {
            EditorApplication.playModeStateChanged -= ClearTextureSelectionBeforePlay;
            EditorApplication.playModeStateChanged += ClearTextureSelectionBeforePlay;
            EditorApplication.delayCall += EnsureCurrentArtReferences;
        }

        static void EnsureCurrentArtReferences()
        {
            const string catalogPath = "Assets/_Game/Content/PawPathCatalog.asset";
            const string wallpaperPath = "Assets/_Game/Art/home_background_gemini.jpg";
            const string gameplayPath = "Assets/_Game/Art/street_gameplay_summer_v1.png";
            const string gapPath = "Assets/_Game/Art/obstacle_road_gap_v1.png";
            const string moundPath = "Assets/street_mound.png";
            const string streetMoundHighPath = "Assets/street_mound_high.png";
            const string roadPath = "Assets/new_road_terracot.png";
            const string forestBackgroundPath = "Assets/_Game/Art/forest_overcast_gameplay_v1.png";
            const string forestGapPath = "Assets/_Game/Art/forest_road_gap_v1.png";
            const string forestMoundPath = "Assets/_Game/Art/forest_mound_v1.png";
            const string forestUnderfillPath = "Assets/forest_ground_underfill.png";
            const string finishPortalPath = "Assets/_Game/Art/finish_portal_glow_v1.png";
            var catalogAsset = AssetDatabase.LoadAssetAtPath<PawPathCatalog>(catalogPath);
            var wallpaper = AssetDatabase.LoadAssetAtPath<Sprite>(wallpaperPath);
            var gameplay = AssetDatabase.LoadAssetAtPath<Sprite>(gameplayPath);
            var gap = AssetDatabase.LoadAssetAtPath<Sprite>(gapPath);
            var mound = AssetDatabase.LoadAssetAtPath<Sprite>(moundPath);
            var streetMoundHigh = AssetDatabase.LoadAssetAtPath<Sprite>(streetMoundHighPath);
            var road = AssetDatabase.LoadAssetAtPath<Sprite>(roadPath);
            var forestBackground = AssetDatabase.LoadAssetAtPath<Sprite>(forestBackgroundPath);
            var forestGap = AssetDatabase.LoadAssetAtPath<Sprite>(forestGapPath);
            var forestMound = AssetDatabase.LoadAssetAtPath<Sprite>(forestMoundPath);
            var forestUnderfill = AssetDatabase.LoadAssetAtPath<Sprite>(forestUnderfillPath);
            var finishPortal = AssetDatabase.LoadAssetAtPath<Sprite>(finishPortalPath);
            if (catalogAsset == null || wallpaper == null || gameplay == null || gap == null ||
                mound == null || streetMoundHigh == null || road == null ||
                forestBackground == null || forestGap == null || forestMound == null ||
                forestUnderfill == null || finishPortal == null)
                return;
            if (catalogAsset.homeBackground == wallpaper &&
                catalogAsset.shopBackground == wallpaper &&
                catalogAsset.gameplayBackground == gameplay &&
                catalogAsset.roadGapSprite == gap && catalogAsset.streetGapLeftSprite == null &&
                catalogAsset.streetGapRightSprite == null && catalogAsset.moundSprite == mound &&
                catalogAsset.streetMoundHighSprite == streetMoundHigh &&
                catalogAsset.roadPlatformSprite == road && catalogAsset.forestBackground == forestBackground &&
                catalogAsset.forestGapSprite == forestGap && catalogAsset.forestMoundSprite == forestMound &&
                catalogAsset.forestPlatformSprite == null &&
                catalogAsset.forestGroundUnderfillSprite == forestUnderfill &&
                catalogAsset.finishPortalSprite == finishPortal)
                return;

            catalogAsset.homeBackground = wallpaper;
            catalogAsset.shopBackground = wallpaper;
            catalogAsset.gameplayBackground = gameplay;
            catalogAsset.roadGapSprite = gap;
            // Deneme amaçlı iki parçalı kenarlar kaldırıldı; eski tek parça
            // roadGapSprite yeniden kullanılır.
            catalogAsset.streetGapLeftSprite = null;
            catalogAsset.streetGapRightSprite = null;
            catalogAsset.moundSprite = mound;
            catalogAsset.streetMoundHighSprite = streetMoundHigh;
            catalogAsset.roadPlatformSprite = road;
            catalogAsset.forestBackground = forestBackground;
            catalogAsset.forestGapSprite = forestGap;
            catalogAsset.forestMoundSprite = forestMound;
            catalogAsset.forestPlatformSprite = null;
            catalogAsset.forestGroundUnderfillSprite = forestUnderfill;
            catalogAsset.finishPortalSprite = finishPortal;
            EditorUtility.SetDirty(catalogAsset);
            AssetDatabase.SaveAssets();
            Debug.Log("[PawPath] Ev, mağaza, sokak ve orman tema görselleri güncellendi.");
        }

        static void ClearTextureSelectionBeforePlay(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            foreach (var selected in Selection.objects)
            {
                if (selected is Texture2D)
                {
                    Selection.activeObject = null;
                    break;
                }
            }
        }

        [MenuItem("Paw Path/Build Starter Scene")]
        public static void BuildStarterScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game"))
                AssetDatabase.CreateFolder("Assets", "_Game");
            if (!AssetDatabase.IsValidFolder(ContentDir))
                AssetDatabase.CreateFolder("Assets/_Game", "Content");
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            var catalog = CatalogFactory.CreateRuntime();
            var catalogPath = ContentDir + "/PawPathCatalog.asset";
            AssetDatabase.CreateAsset(catalog, catalogPath);
            foreach (var cat in catalog.cats)
                AssetDatabase.AddObjectToAsset(cat, catalog);
            foreach (var item in catalog.shopItems)
                AssetDatabase.AddObjectToAsset(item, catalog);
            foreach (var level in catalog.levels)
                AssetDatabase.AddObjectToAsset(level, catalog);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var bootstrap = new GameObject("RuntimeBootstrap");
            var comp = bootstrap.AddComponent<RuntimeBootstrap>();
            var so = new SerializedObject(comp);
            so.FindProperty("catalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PawPathCatalog>(catalogPath);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            EditorBuildSettings.scenes = scenes;
            AssetDatabase.Refresh();
            Debug.Log("[PawPath] Sahne ve katalog hazır: " + ScenePath + " — Play'e bas.");
        }

        [MenuItem("Paw Path/Reset Save Data")]
        public static void ResetSave()
        {
            PawPath.Core.SaveService.ResetProgressForTesting();
            Debug.Log("[PawPath] İlerleme ve yedek test için sıfırlandı.");
        }
    }
}
