using UnityEngine;
using UnityEngine.UI;

public class NetworkPacketDemo : MonoBehaviour
{
    [SerializeField] private Text logText;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) TestMovePacket();
        if (Input.GetKeyDown(KeyCode.Alpha2)) TestAttackPacket();
        if (Input.GetKeyDown(KeyCode.Alpha3)) TestChatPacket();
        if (Input.GetKeyDown(KeyCode.Alpha4)) TestSnapshotPacket();
        if (Input.GetKeyDown(KeyCode.Alpha5)) TestPolymorphic();
    }

    private void TestMovePacket()
    {
        var packet = new PlayerMovePacket
        {
            PlayerId = 1001,
            Position = new Vector3(10.5f, 0, -3.2f),
            Velocity = new Vector3(2.1f, 0, 0),
            Yaw = 90f,
            Tick = 12345
        };

        byte[] bytes = PacketSerializer.Serialize(packet);
        var restored = PacketSerializer.Deserialize<PlayerMovePacket>(bytes);

        Log($"[Move] 序列化 {bytes.Length} bytes\n" +
            $"还原: Id={restored.PlayerId} Pos={restored.Position} Tick={restored.Tick}");
    }

    private void TestAttackPacket()
    {
        var packet = new PlayerAttackPacket
        {
            AttackerId = 1001,
            TargetId = 2002,
            SkillId = 7,
            HitPoint = new Vector3(1, 1.5f, 2)
        };

        byte[] bytes = PacketSerializer.Serialize(packet);
        var restored = PacketSerializer.Deserialize<PlayerAttackPacket>(bytes);

        Log($"[Attack] {bytes.Length} bytes → Target={restored.TargetId} Skill={restored.SkillId}");
    }

    private void TestChatPacket()
    {
        var packet = new ChatPacket
        {
            SenderId = 1001,
            Message = "Hello MemoryPack! 你好"
        };

        byte[] bytes = PacketSerializer.Serialize(packet);
        var restored = PacketSerializer.Deserialize<ChatPacket>(bytes);

        Log($"[Chat] {bytes.Length} bytes → \"{restored.Message}\"");
    }

    private void TestSnapshotPacket()
    {
        var packet = new SyncSnapshotPacket
        {
            Tick = 99999,
            Players = new[]
            {
                new PlayerState { PlayerId = 1, Position = Vector3.zero, Hp = 100, Mp = 50 },
                new PlayerState { PlayerId = 2, Position = Vector3.one,  Hp = 80,  Mp = 30 },
                new PlayerState { PlayerId = 3, Position = Vector3.up,   Hp = 60,  Mp = 10 },
            }
        };

        byte[] bytes = PacketSerializer.Serialize(packet);
        var restored = PacketSerializer.Deserialize<SyncSnapshotPacket>(bytes);

        Log($"[Snapshot] {bytes.Length} bytes，玩家数={restored.Players.Length}");
    }

    /// <summary>演示 Union 多态：发送端不知道具体类型，接收端自动识别</summary>
    private void TestPolymorphic()
    {
        INetworkPacket packet = new PlayerMovePacket
        {
            PlayerId = 42,
            Position = new Vector3(1, 2, 3),
            Tick = 777
        };

        byte[] bytes = PacketSerializer.Serialize(packet);

        // 关键：用基类/接口反序列化，MemoryPack 会根据 Union 自动还原正确类型
        INetworkPacket restored = PacketSerializer.Deserialize(bytes);

        Log($"[Polymorphic] 实际类型 = {restored.GetType().Name}");
    }

    private void Log(string msg)
    {
        Debug.Log(msg);
        if (logText != null)
            logText.text = msg;
    }
}