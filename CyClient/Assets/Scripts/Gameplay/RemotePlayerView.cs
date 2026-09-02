using UnityEngine;

/// <summary>
/// 远端玩家插值
/// </summary>
public class RemotePlayerView : MonoBehaviour
{
    private Vector3 _target;
    private float _yaw;
    public int PlayerId { get; private set; }

    public void Setup(int id, Vector3 pos)
    {
        PlayerId = id;
        _target = pos;
        transform.position = pos;
    }

    public void ApplySnapshot(MoveSnapshotPacket snap)
    {
        _target = new Vector3(snap.X, snap.Y, snap.Z);
        _yaw = snap.Yaw;
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, _target, 12f * Time.deltaTime);
        var e = transform.eulerAngles;
        e.y = Mathf.LerpAngle(e.y, _yaw, 12f * Time.deltaTime);
        transform.eulerAngles = e;
    }
}
