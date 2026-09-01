using System;
using System.IO;
using MemoryPack;
using UnityEngine;

public static class SaveService
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.mpack");

    /// <summary>序列化并写入文件</summary>
    public static void Save(GameSaveData data)
    {
        data.SavedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // 核心 API：一行完成序列化
        byte[] bytes = MemoryPackSerializer.Serialize(data);

        File.WriteAllBytes(SavePath, bytes);
        Debug.Log($"[Save] 写入成功 → {SavePath}  ({bytes.Length} bytes)");
    }

    /// <summary>从文件读取并反序列化</summary>
    public static GameSaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[Save] 没有存档文件");
            return null;
        }

        byte[] bytes = File.ReadAllBytes(SavePath);

        // 核心 API：一行完成反序列化
        var data = MemoryPackSerializer.Deserialize<GameSaveData>(bytes);
        Debug.Log($"[Save] 读取成功 → Level={data.Level} Gold={data.Gold}");
        return data;
    }

    public static void Delete()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("[Save] 存档已删除");
        }
    }

    public static bool Exists => File.Exists(SavePath);
}