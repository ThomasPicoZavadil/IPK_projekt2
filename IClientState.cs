public interface IClientState
{
    Task HandleInput(ClientContext context, string input);
    void HandleServerMessage(ClientContext context, string message);
}

public interface IUDPClientState : IClientState
{
    void HandleUDPMessage(ClientContext context, byte[] data);
}

