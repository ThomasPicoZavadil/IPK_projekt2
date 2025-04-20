public class StartState : IClientState
{
    public async Task HandleInput(ClientContext context, string input)
    {
        if (input.StartsWith("/auth "))
        {
            string[] parts = input.Split(' ', 4);
            if (parts.Length == 4)
            {
                string username = parts[1];
                string secret = parts[2];
                string displayName = parts[3];

                context.DisplayName = displayName; // Save the DisplayName in the context

                string authMessage = $"AUTH {username} AS {displayName} USING {secret}\r";
                await context.SendMessageAsync(authMessage); // Send the formatted message to the server
                context.SetState(new AuthState());
            }
            else
            {
                Console.WriteLine("Usage: /auth {Username} {Secret} {DisplayName}\n");
            }
        }
        else if (input.StartsWith("/help"))
        {
            Console.WriteLine("/auth {Username} {Secret} {DisplayName} - Authenticate with the server.\n");
        }
        else
        {
            Console.WriteLine("ERROR: authorize using /auth first.\n");
        }
    }

    public void HandleServerMessage(ClientContext context, string message)
    {
        if (message.StartsWith("ERR FROM "))
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
            Console.WriteLine($"[StartState] Server: {message}\n");
        }
    }
}
