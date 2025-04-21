public interface IClientState
{
    Task HandleInput(ClientContext context, string input);
    void HandleServerMessage(ClientContext context, string message);
}

