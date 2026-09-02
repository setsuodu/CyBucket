using MemoryPack;
using UnityEngine;

public enum PacketId : byte
{
    Join = 1,
    JoinAck = 2,
    Move = 10,
    MoveSnapshot = 11,
}

[MemoryPackable]
public partial struct JoinPacket
{
    public string Name;
}

[MemoryPackable]
public partial struct JoinAckPacket
{
    public int PlayerId;
}

[MemoryPackable]
public partial struct MovePacket
{
    public int PlayerId;
    public float X, Y, Z;
    public float Yaw;
    public uint Tick;
}

[MemoryPackable]
public partial struct MoveSnapshotPacket
{
    public int PlayerId;
    public float X, Y, Z;
    public float Yaw;
    public uint Tick;
}
