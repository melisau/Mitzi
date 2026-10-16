#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Otomatik içerik betikleri yalnız kendi değiştirdikleri asset'leri kaydeder.
/// AssetDatabase.SaveAssets() projedeki her kirli dosyayı yazmaya çalışır; paket
/// klasörleri (ör. Google Mobile Ads prefab'ları) salt okunur olduğu için Unity
/// "Saving Prefab to immutable folder is not allowed" hatası verir.
/// </summary>
public static class EditorSave
{
    public static void Save(params Object[] assets)
    {
        foreach (var asset in assets)
        {
            if (asset == null)
                continue;
            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/"))
                continue;
            AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}
#endif
