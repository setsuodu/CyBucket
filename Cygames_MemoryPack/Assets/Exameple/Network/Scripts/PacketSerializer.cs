using System;
using MemoryPack;

public static class PacketSerializer
{
    /// <summary>对象 → 字节数组（发送用）</summary>
    public static byte[] Serialize<T>(T packet) where T : INetworkPacket
    {
        return MemoryPackSerializer.Serialize(packet);
    }

    /// <summary>字节数组 → 对象（接收用）</summary>
    public static T Deserialize<T>(ReadOnlySpan<byte> data) where T : INetworkPacket
    {
        return MemoryPackSerializer.Deserialize<T>(data);
    }

    /// <summary>多态反序列化（不知道具体类型时用）</summary>
    public static INetworkPacket Deserialize(ReadOnlySpan<byte> data)
    {
        return MemoryPackSerializer.Deserialize<INetworkPacket>(data);
    }
}