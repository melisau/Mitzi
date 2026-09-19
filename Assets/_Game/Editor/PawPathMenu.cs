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
            EditorApplication.delayCall += EnsureWallpaper2References;
        }

        static void EnsureWallpaper2References()
        {
            const string catalogPath = "Assets/_Game/Content/PawPathCatalog.asset";
            const string wallpaperPath = "Assets/_Game/Art/home_wallpaper_cat_scale_v1.png";
            const string gameplayPath = "Assets/_Game/Art/street_gameplay_summer_v1.png";
            var catalogAsset = AssetDatabase.LoadAssetAtPath<PawPathCatalog>(catalogPath);
            var wallpaper = AssetDatabase.LoadAssetAtPath<Sprite>(wallpaperPath);
            var gameplay = AssetDatabase.LoadAssetAtPath<Sprite>(gameplayPath);
            if (catalogAsset == null || wallpaper == null || gameplay == null)
                return;
            if (catalogAsset.homeBackground == wallpaper &&
                catalogAsset.shopBackground == wallpaper &&
                catalogAsset.gameplayBackground == gameplay)
                return;

            catalogAsset.homeBackground = wallpaper;
            catalogAsset.shopBackground = wallpaper;
            catalogAsset.gameplayBackground = gameplay;
            EditorUtility.SetDirty(catalogAsset);
            AssetDatabase.SaveAssets();
            Debug.Log("[PawPath] Ev, mağaza ve sokak arka planları güncellendi.");
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
            PlayerPrefs.DeleteKey("PawPath.Save.v1");
            PlayerPrefs.Save();
            Debug.Log("[PawPath] Kayıt silindi.");
        }
    }
}
