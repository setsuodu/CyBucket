using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 资源门面：对齐 AssetBundleFramework。
/// 当前全部走 Editor AssetDatabase；真机再走 ABManager。
/// </summary>
public static class ResManager
{
    public const string EditorRoot = "Assets/Bundles";

    public static async UniTask InitializeAsync(CancellationToken ct = default)
    {
#if UNITY_EDITOR
        Debug.Log("[ResManager] Editor AssetDatabase mode");
        await UniTask.Yield(ct);
#else
        await ABManager.Instance.InitializeAsync(ct);
#endif
    }

    public static async UniTask<T> LoadAsync<T>(
        string abNameOrEditorPath,
        string assetName,
        CancellationToken ct = default) where T : Object
    {
#if UNITY_EDITOR
        await UniTask.Yield(ct);
        return LoadFromAssetDatabase<T>(abNameOrEditorPath, assetName);
#else
        using (await ABManager.Instance.LoadBundleHandleAsync(abNameOrEditorPath, ct))
        {
            return await ABManager.Instance.LoadAssetAsync<T>(abNameOrEditorPath, assetName, ct);
        }
#endif
    }

    public static async UniTask<GameObject> InstantiateAsync(
        string abNameOrEditorPath,
        string assetName,
        Transform parent = null,
        CancellationToken ct = default)
    {
        var prefab = await LoadAsync<GameObject>(abNameOrEditorPath, assetName, ct);
        if (prefab == null)
        {
            Debug.LogWarning($"[ResManager] null prefab {abNameOrEditorPath}/{assetName}");
            return null;
        }
        return Object.Instantiate(prefab, parent);
    }

#if UNITY_EDITOR
    static T LoadFromAssetDatabase<T>(string abNameOrPath, string assetName) where T : Object
    {
        if (abNameOrPath.StartsWith("Assets/"))
        {
            var direct = AssetDatabase.LoadAssetAtPath<T>(abNameOrPath);
            if (direct != null) return direct;
        }

        var folder = $"{EditorRoot}/{abNameOrPath}".Replace("\\", "/");
        if (AssetDatabase.IsValidFolder(folder))
        {
            var guids = AssetDatabase.FindAssets($"{assetName} t:{typeof(T).Name}", new[] { folder });
            if (guids != null && guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) return asset;
            }
        }

        var all = AssetDatabase.FindAssets($"{assetName} t:{typeof(T).Name}");
        foreach (var g in all)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null && asset.name == assetName)
                return asset;
        }

        Debug.LogWarning($"[ResManager] AssetDatabase miss: {abNameOrPath} / {assetName}");
        return null;
    }
#endif
}
