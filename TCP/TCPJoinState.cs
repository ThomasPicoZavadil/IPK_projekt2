using System;

public class JoinState : IClientState
{
    private string channelName;

    public JoinState(string channelName)
    {
        this.channelName = channelName;
    }

    public async Task HandleInput(ClientContext context, string input)
    {
        Console.WriteLine($"ERROR: Waiting for server response. You cannot send messages yet.\n");
    }

    public void HandleServerMessage(ClientContext context, string message)
    {
        if (message.StartsWith("REPLY OK IS "))
        {
            string messageContent = message.Substring("REPLY OK IS ".Length);
            Console.WriteLine($"Action Success: {messageContent}\n");
            context.SetState(new OpenState());
        }
        else if (message.StartsWith("REPLY NOK IS "))
        {
            string messageContent = message.Substring("REPLY NOK IS ".Length);
            Console.WriteLine($"Action Failure: {messageContent}\n");
        }
        else if (message.StartsWith("ERR FROM "))
        {
            // Parse the message in the format "ERR FROM {DisplayName} IS {MessageContent}"
            string[] parts = message.Split(new[] { "ERR FROM ", " IS " }, StringSplitOptions.None);
            if (parts.Length == 3)
            {
                string displayName = parts[1].Trim();
                string messageContent = parts[2].Trim();
                Console.WriteLine($"ERROR FROM {displayName}: {messageContent}\n");
            }
            else
            {
                Console.WriteLine($"[OpenState] Malformed ERR message: {message}\n");
            }
        }
        else if (message.StartsWith("BYE"))
        {
            Console.WriteLine("Server has terminated the connection. Goodbye.\n");
            context.StopListening(); // Gracefully stop listening for messages
            Environment.Exit(0); // Exit the application
        }
        else
        {
            Console.WriteLine($"[JoinState] Unexpected server response: {message}\n");
        }
    }
}