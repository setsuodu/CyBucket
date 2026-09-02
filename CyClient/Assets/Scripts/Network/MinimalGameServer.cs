using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using MemoryPack;
using UnityEngine;

/// <summary>
/// 极简同进程服务器：只做 Join + 移动广播
/// 挂到场景即可本地联调（也可打独立 Server 场景）
/// </summary>
public class MinimalGameServer : MonoBehaviour, INetEventListener
{
    [SerializeField] private int port = GameConstants.DefaultPort;

    private NetManager _server;
    private readonly NetDataWriter _writer = new();
    private readonly Dictionary<NetPeer, int> _peerToId = new();
    private int _nextId = 1;

    private void Start()
    {
        _server = new NetManager(this) { AutoRecycle = true, UpdateTime = 15 };
        if (!_server.Start(port))
        {
            Debug.LogError($"[Server] Bind {port} failed");
            enabled = false;
            return;
        }
        Debug.Log($"[Server] Listening :{port}");
    }

    private void Update() => _server?.PollEvents();

    private void OnDestroy() => _server?.Stop();

    public void OnConnectionRequest(ConnectionRequest request)
    {
        if (_server.ConnectedPeersCount < 32)
            request.AcceptIfKey(GameConstants.ConnectionKey);
        else
            request.Reject();
    }

    public void OnPeerConnected(NetPeer peer) =>
        Debug.Log($"[Server] Peer connected {peer.Address}");

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _peerToId.Remove(peer);
        Debug.Log($"[Server] Peer disconnected {disconnectInfo.Reason}");
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        if (reader.AvailableBytes < 1) { reader.Recycle(); return; }
        var id = (PacketId)reader.GetByte();
        var body = reader.GetBytesWithLength();
        reader.Recycle();

        switch (id)
        {
            case PacketId.Join:
            {
                var join = MemoryPackSerializer.Deserialize<JoinPacket>(body);
                var pid = _nextId++;
                _peerToId[peer] = pid;
                var ack = new JoinAckPacket { PlayerId = pid };
                SendTo(peer, PacketId.JoinAck, ack, DeliveryMethod.ReliableOrdered);
                Debug.Log($"[Server] Join {join.Name} -> id {pid}");
                break;
            }
            case PacketId.Move:
            {
                var move = MemoryPackSerializer.Deserialize<MovePacket>(body);
                var snap = new MoveSnapshotPacket
                {
                    PlayerId = move.PlayerId,
                    X = move.X, Y = move.Y, Z = move.Z,
                    Yaw = move.Yaw,
                    Tick = move.Tick
                };
                // 广播给其他人
                foreach (var kv in _peerToId)
                {
                    if (kv.Key == peer) continue;
                    SendTo(kv.Key, PacketId.MoveSnapshot, snap, DeliveryMethod.Sequenced);
                }
                break;
            }
        }
    }

    private void SendTo<T>(NetPeer peer, PacketId id, T packet, DeliveryMethod method)
    {
        var body = MemoryPackSerializer.Serialize(packet);
        _writer.Reset();
        _writer.Put((byte)id);
        _writer.PutBytesWithLength(body);
        peer.Send(_writer, method);
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) =>
        Debug.LogWarning($"[Server] {socketError}");

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
}
