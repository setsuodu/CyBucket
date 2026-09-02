using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;

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
        var go = await LoadPlayerAsync(ct);
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
        var go = await LoadPlayerAsync(this.GetCancellationTokenOnDestroy());
        go.name = $"Remote_{snap.PlayerId}";
        var view = go.GetComponent<RemotePlayerView>() ?? go.AddComponent<RemotePlayerView>();
        view.Setup(snap.PlayerId, new Vector3(snap.X, snap.Y, snap.Z));
        var pc = go.GetComponent<PlayerController>();
        if (pc) Destroy(pc);
        _remotes[snap.PlayerId] = view;
    }

    /// <summary>
    /// 规范：经 ResManager。Editor 走 AssetDatabase；路径约定见 GameConstants。
    /// </summary>
    private async UniTask<GameObject> LoadPlayerAsync(CancellationToken ct)
    {
        try
        {
            var go = await ResManager.InstantiateAsync(
                GameConstants.AbPlayer,
                GameConstants.AssetPlayer,
                null,
                ct);
            if (go != null) return go;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Match] ResManager: {e.Message}");
        }

        if (editorFallbackPrefab != null)
            return Instantiate(editorFallbackPrefab);

        var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        cap.AddComponent<PlayerController>();
        return cap;
    }

    private void OnDestroy() => _bag?.Dispose();
}
