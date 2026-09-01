using MemoryPack;
using UnityEngine;

/// <summary>
/// 所有网络包基类（可选，方便做 Union 多态）
/// </summary>
[MemoryPackable]
[MemoryPackUnion(0, typeof(PlayerMovePacket))]
[MemoryPackUnion(1, typeof(PlayerAttackPacket))]
[MemoryPackUnion(2, typeof(ChatPacket))]
[MemoryPackUnion(3, typeof(SyncSnapshotPacket))]
public partial interface INetworkPacket { }

/// <summary>玩家移动包（高频）</summary>
[MemoryPackable]
public partial class PlayerMovePacket : INetworkPacket
{
    [MemoryPackOrder(0)] public int PlayerId;
    [MemoryPackOrder(1)] public Vector3 Position;
    [MemoryPackOrder(2)] public Vector3 Velocity;
    [MemoryPackOrder(3)] public float Yaw;
    [MemoryPackOrder(4)] public uint Tick;          // 帧号
}

/// <summary>攻击包</summary>
[MemoryPackable]
public partial class PlayerAttackPacket : INetworkPacket
{
    [MemoryPackOrder(0)] public int AttackerId;
    [MemoryPackOrder(1)] public int TargetId;
    [MemoryPackOrder(2)] public int SkillId;
    [MemoryPackOrder(3)] public Vector3 HitPoint;
}

/// <summary>聊天包</summary>
[MemoryPackable]
public partial class ChatPacket : INetworkPacket
{
    [MemoryPackOrder(0)] public int SenderId;
    [MemoryPackOrder(1)] public string Message;
}

/// <summary>完整状态快照（断线重连 / 关键帧用）</summary>
[MemoryPackable]
public partial class SyncSnapshotPacket : INetworkPacket
{
    [MemoryPackOrder(0)] public uint Tick;
    [MemoryPackOrder(1)] public PlayerState[] Players;
}

[MemoryPackable]
public partial class PlayerState
{
    [MemoryPackOrder(0)] public int PlayerId;
    [MemoryPackOrder(1)] public Vector3 Position;
    [MemoryPackOrder(2)] public int Hp;
    [MemoryPackOrder(3)] public int Mp;
}