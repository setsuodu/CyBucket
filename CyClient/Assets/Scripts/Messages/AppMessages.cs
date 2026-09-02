public readonly struct LoginSuccessMessage
{
    public readonly string PlayerName;
    public LoginSuccessMessage(string name) => PlayerName = name;
}

public readonly struct MatchStartMessage
{
    public readonly string Host;
    public readonly int Port;
    public MatchStartMessage(string host, int port)
    {
        Host = host;
        Port = port;
    }
}

public readonly struct PlayerSpawnedMessage
{
    public readonly int PlayerId;
    public readonly bool IsLocal;
    public PlayerSpawnedMessage(int id, bool isLocal)
    {
        PlayerId = id;
        IsLocal = isLocal;
    }
}

public readonly struct NetworkStateMessage
{
    public readonly bool Connected;
    public readonly string Detail;
    public NetworkStateMessage(bool connected, string detail)
    {
        Connected = connected;
        Detail = detail;
    }
}
