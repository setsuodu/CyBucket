using UnityEngine;

/// <summary>
/// 本地玩家：WASD 移动，定期上报 NetworkService
/// </summary>
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    private NetworkService _net;
    private uint _tick;
    private float _sendAcc;

    public void Bind(NetworkService net) => _net = net;

    private void Update()
    {
        var h = Input.GetAxisRaw("Horizontal");
        var v = Input.GetAxisRaw("Vertical");
        var dir = new Vector3(h, 0, v).normalized;
        if (dir.sqrMagnitude > 0.01f)
        {
            transform.position += dir * (speed * Time.deltaTime);
            transform.forward = dir;
        }

        _sendAcc += Time.deltaTime;
        if (_sendAcc >= 0.05f && _net != null) // 20Hz
        {
            _sendAcc = 0;
            _tick++;
            _net.SendMove(transform.position, transform.eulerAngles.y, _tick);
        }
    }
}
