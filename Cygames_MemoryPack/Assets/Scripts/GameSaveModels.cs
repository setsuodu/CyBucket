using System;
using System.Collections.Generic;
using MemoryPack;
using UnityEngine;

namespace Roguelite.SaveSystem
{
    /// <summary>
    /// 仅承载可持久化状态，不直接引用 GameObject、MonoBehaviour、ScriptableObject 或 UnityEngine.Object。
    /// 版本容忍模式用于让新客户端读取旧版本存档。
    /// </summary>
    [MemoryPackable(GenerateType.VersionTolerant)]
    public partial class GameSaveData
    {
        // 编号是二进制协议的一部分：后续不可变更或复用。
        [MemoryPackOrder(0)] public string Magic { get; set; } = "ROGUELITE_SAVE";
        [MemoryPackOrder(1)] public int ApplicationSaveVersion { get; set; } = 1;
        [MemoryPackOrder(2)] public string SceneId { get; set; } = string.Empty;
        [MemoryPackOrder(3)] public PlayerSnapshot Player { get; set; } = new();
        [MemoryPackOrder(4)] public List<InventoryStack> Inventory { get; set; } = new();
        [MemoryPackOrder(5)] public List<string> DefeatedEnemyIds { get; set; } = new();
        [MemoryPackOrder(6)] public long SavedAtUnixMilliseconds { get; set; }

        // V2 示例：新增字段使用全新编号。V1 存档读入时保持默认值 false。
        [MemoryPackOrder(7)] public bool TutorialCompleted { get; set; }
    }

    [MemoryPackable(GenerateType.VersionTolerant)]
    public partial class PlayerSnapshot
    {
        [MemoryPackOrder(0)] public Vector3 Position { get; set; }
        [MemoryPackOrder(1)] public Quaternion Rotation { get; set; } = Quaternion.identity;
        [MemoryPackOrder(2)] public int Health { get; set; }
        [MemoryPackOrder(3)] public int Gold { get; set; }
        [MemoryPackOrder(4)] public string EquippedWeaponId { get; set; } = string.Empty;
    }

    [MemoryPackable(GenerateType.VersionTolerant)]
    public partial class InventoryStack
    {
        [MemoryPackOrder(0)] public string ItemId { get; set; } = string.Empty;
        [MemoryPackOrder(1)] public int Quantity { get; set; }
    }
}
