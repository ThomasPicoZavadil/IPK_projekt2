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
        Console.WriteLine($"[JoinState] Waiting for server response. You cannot send messages yet.");
    }

    public void HandleServerMessage(ClientContext context, string message)
    {
        if (message.StartsWith("REPLY OK IS "))
        {
            string messageContent = message.Substring("REPLY OK IS ".Length);
            Console.WriteLine($"Action Success: {messageContent}");
            context.SetState(new OpenState());
        }
        else if (message.StartsWith("REPLY NOK IS "))
        {
            string messageContent = message.Substring("REPLY NOK IS ".Length);
            Console.WriteLine($"Action Failure: {messageContent}");
        }
        else if (message == "ERR")
        {
            Console.WriteLine("Error received from server. Terminating connection.");
            context.StopListening(); // Gracefully stop listening for messages
            Environment.Exit(0); // Exit the application
        }
        else if (message == "BYE")
        {
            Console.WriteLine("Server has terminated the connection. Goodbye.");
            context.StopListening(); // Gracefully stop listening for messages
            Environment.Exit(0); // Exit the application
        }
        else
        {
            Console.WriteLine($"[JoinState] Unexpected server response: {message}");
        }
    }
}