using System;
using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Utils;
using MemoryPack;
using MessagePipe;
using UnityEngine;

/// <summary>
/// LiteNetLib 客户端：连接、收发 MemoryPack 包、移动同步
/// </summary>
public class NetworkService : INetEventListener, IDisposable
{
    private NetManager _client;
    private NetPeer _server;
    private readonly NetDataWriter _writer = new();
    private readonly GameSession _session;
    private readonly IPublisher<NetworkStateMessage> _netStatePub;
    private readonly IPublisher<PlayerSpawnedMessage> _spawnPub;
    private Action<MoveSnapshotPacket> _onMoveSnapshot;
    private bool _running;

    public bool IsConnected => _server != null && _server.ConnectionState == ConnectionState.Connected;

    public NetworkService(
        GameSession session,
        IPublisher<NetworkStateMessage> netStatePub,
        IPublisher<PlayerSpawnedMessage> spawnPub)
    {
        _session = session;
        _netStatePub = netStatePub;
        _spawnPub = spawnPub;
    }

    public void SetMoveHandler(Action<MoveSnapshotPacket> handler) => _onMoveSnapshot = handler;

    public void Connect(string host, int port)
    {
        if (_client != null) return;

        _client = new NetManager(this)
        {
            AutoRecycle = true,
            UpdateTime = 15
        };
        _client.Start();
        _client.Connect(host, port, GameConstants.ConnectionKey);
        _running = true;
        PollLoop().Forget();
        Debug.Log($"[Net] Connecting {host}:{port}");
    }

    public void Disconnect()
    {
        _running = false;
        _client?.Stop();
        _client = null;
        _server = null;
        _netStatePub.Publish(new NetworkStateMessage(false, "disconnected"));
    }

    public void SendMove(Vector3 pos, float yaw, uint tick)
    {
        if (!IsConnected) return;
        var pkt = new MovePacket
        {
            PlayerId = _session.LocalPlayerId,
            X = pos.x, Y = pos.y, Z = pos.z,
            Yaw = yaw,
            Tick = tick
        };
        Send(PacketId.Move, pkt, DeliveryMethod.Sequenced);
    }

    private void Send<T>(PacketId id, T packet, DeliveryMethod method)
    {
        var body = MemoryPackSerializer.Serialize(packet);
        _writer.Reset();
        _writer.Put((byte)id);
        _writer.PutBytesWithLength(body);
        _server.Send(_writer, method);
    }

    private async UniTaskVoid PollLoop()
    {
        while (_running && _client != null)
        {
            _client.PollEvents();
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }

    // ----- INetEventListener -----
    public void OnPeerConnected(NetPeer peer)
    {
        _server = peer;
        _netStatePub.Publish(new NetworkStateMessage(true, peer.Address.ToString()));
        var join = new JoinPacket { Name = _session.PlayerName };
        Send(PacketId.Join, join, DeliveryMethod.ReliableOrdered);
        Debug.Log("[Net] Connected, Join sent");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        _server = null;
        _netStatePub.Publish(new NetworkStateMessage(false, disconnectInfo.Reason.ToString()));
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        if (reader.AvailableBytes < 1) { reader.Recycle(); return; }
        var id = (PacketId)reader.GetByte();
        var body = reader.GetBytesWithLength();
        reader.Recycle();

        switch (id)
        {
            case PacketId.JoinAck:
            {
                var ack = MemoryPackSerializer.Deserialize<JoinAckPacket>(body);
                _session.LocalPlayerId = ack.PlayerId;
                _spawnPub.Publish(new PlayerSpawnedMessage(ack.PlayerId, true));
                Debug.Log($"[Net] JoinAck id={ack.PlayerId}");
                break;
            }
            case PacketId.MoveSnapshot:
            {
                var snap = MemoryPackSerializer.Deserialize<MoveSnapshotPacket>(body);
                _onMoveSnapshot?.Invoke(snap);
                break;
            }
        }
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) =>
        Debug.LogWarning($"[Net] Error {socketError}");

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
    public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
    public void OnConnectionRequest(ConnectionRequest request) => request.Reject();

    public void Dispose() => Disconnect();
}
