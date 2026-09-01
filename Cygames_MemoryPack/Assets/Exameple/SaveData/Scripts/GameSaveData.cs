using System;
using System.Collections.Generic;
using MemoryPack;
using UnityEngine;

/// <summary>
/// MemoryPack 要求：
/// 1. 加 [MemoryPackable]
/// 2. 必须是 partial class
/// 3. 字段/属性需要 [MemoryPackOrder]（推荐，尤其是要做版本兼容时）
/// </summary>
[MemoryPackable]
public partial class GameSaveData
{
    [MemoryPackOrder(0)] public int Version { get; set; } = 1;
    [MemoryPackOrder(1)] public string PlayerName { get; set; } = "Player";
    [MemoryPackOrder(2)] public int Level { get; set; } = 1;
    [MemoryPackOrder(3)] public int Gold { get; set; }
    [MemoryPackOrder(4)] public Vector3 Position { get; set; }
    [MemoryPackOrder(5)] public List<ItemStack> Inventory { get; set; } = new();
    [MemoryPackOrder(6)] public long SavedAtUnixMs { get; set; }
}

[MemoryPackable]
public partial class ItemStack
{
    [MemoryPackOrder(0)] public string ItemId { get; set; } = "";
    [MemoryPackOrder(1)] public int Count { get; set; }
}