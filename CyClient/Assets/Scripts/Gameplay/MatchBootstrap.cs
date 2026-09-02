using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;

/// <summary>
/// 匹配 → 联网 → JoinAck → AB/占位生成玩家 → 移动同步
/// </summary>
public class MatchBootstrap : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject editorFallbackPrefab;

    private NetworkService _net;
    private GameSession _session;
    private IDisposable _bag;
    private readonly Dictionary<int, RemotePlayerView> _remotes = new();

    public void Init(
        NetworkService net,
        GameSession session,
        ISubscriber<PlayerSpawnedMessage> spawnSub,
        ISubscriber<MatchStartMessage> matchSub)
    {
        _net = net;
        _session = session;
        _bag = DisposableBag.Create(
            matchSub.Subscribe(m => _net.Connect(m.Host, m.Port)),
            spawnSub.Subscribe(OnSpawn));
        _net.SetMoveHandler(OnRemoteMove);
    }

    private void OnSpawn(PlayerSpawnedMessage msg)
    {
        if (msg.IsLocal)
            SpawnLocalAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    private async UniTaskVoid SpawnLocalAsync(CancellationToken ct)
    {
        var go = await LoadPlayerPrefabAsync(ct);
        go.name = $"LocalPlayer_{_session.LocalPlayerId}";
        go.transform.position = spawnPoint ? spawnPoint.position : Vector3.zero;
        var pc = go.GetComponent<PlayerController>() ?? go.AddComponent<PlayerController>();
        pc.Bind(_net);
        Debug.Log("[Match] Local player spawned");
    }

    private void OnRemoteMove(MoveSnapshotPacket snap)
    {
        if (snap.PlayerId == _session.LocalPlayerId) return;
        if (!_remotes.TryGetValue(snap.PlayerId, out var view))
        {
            SpawnRemoteAsync(snap).Forget();
            return;
        }
        view.ApplySnapshot(snap);
    }

    private async UniTaskVoid SpawnRemoteAsync(MoveSnapshotPacket snap)
    {
        if (_remotes.ContainsKey(snap.PlayerId)) return;
        var go = await LoadPlayerPrefabAsync(this.GetCancellationTokenOnDestroy());
        go.name = $"Remote_{snap.PlayerId}";
        var view = go.GetComponent<RemotePlayerView>() ?? go.AddComponent<RemotePlayerView>();
        view.Setup(snap.PlayerId, new Vector3(snap.X, snap.Y, snap.Z));
        var pc = go.GetComponent<PlayerController>();
        if (pc) Destroy(pc);
        _remotes[snap.PlayerId] = view;
    }

    private async UniTask<GameObject> LoadPlayerPrefabAsync(CancellationToken ct)
    {
        try
        {
            using (await ABManager.Instance.LoadBundleHandleAsync(GameConstants.AbPlayer, ct))
            {
                var prefab = await ABManager.Instance.LoadAssetAsync<GameObject>(
                    GameConstants.AbPlayer, GameConstants.AssetPlayer, ct);
                if (prefab != null) return Instantiate(prefab);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Match] AB fail: {e.Message}");
        }

        if (editorFallbackPrefab != null) return Instantiate(editorFallbackPrefab);
        var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        cap.AddComponent<PlayerController>();
        return cap;
    }

    private void OnDestroy() => _bag?.Dispose();
}
