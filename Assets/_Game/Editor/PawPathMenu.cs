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
