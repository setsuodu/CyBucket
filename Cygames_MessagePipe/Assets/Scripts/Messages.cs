using System;
using UnityEngine;

/// <summary>
/// MessagePipe 消息定义
/// 推荐使用 struct / record / class + readonly，避免不必要的分配
/// </summary>
public readonly struct PlayerDamagedMessage
{
    public readonly int Damage;
    public readonly Vector3 HitPoint;
    public readonly string Source;

    public PlayerDamagedMessage(int damage, Vector3 hitPoint, string source)
    {
        Damage = damage;
        HitPoint = hitPoint;
        Source = source;
    }
}

public readonly struct PlayerDiedMessage
{
    public readonly string Reason;
    public readonly float SurvivalTime;

    public PlayerDiedMessage(string reason, float survivalTime)
    {
        Reason = reason;
        SurvivalTime = survivalTime;
    }
}

public readonly struct ScoreChangedMessage
{
    public readonly int OldScore;
    public readonly int NewScore;
    public readonly int Delta;

    public ScoreChangedMessage(int oldScore, int newScore)
    {
        OldScore = oldScore;
        NewScore = newScore;
        Delta = newScore - oldScore;
    }
}

/// <summary>
/// 请求-响应示例消息
/// </summary>
public readonly struct GetPlayerStatusRequest
{
    public readonly int PlayerId;
    public GetPlayerStatusRequest(int playerId) => PlayerId = playerId;
}

public readonly struct GetPlayerStatusResponse
{
    public readonly int Hp;
    public readonly int Score;
    public readonly bool IsAlive;

    public GetPlayerStatusResponse(int hp, int score, bool isAlive)
    {
        Hp = hp;
        Score = score;
        IsAlive = isAlive;
    }
}
