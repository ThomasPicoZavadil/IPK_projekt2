// Author: xzavadt00
// This class represents the start state of the client in the TCP protocol.
// It handles user input for authentication and processes server messages related to errors or termination.

public class StartState : IClientState
{
    // Handles user input while in the start state
    public async Task HandleInput(ClientContext context, string input)
    {
        if (input.StartsWith("/auth "))
        {
            // Parse the /auth command: /auth {Username} {Secret} {DisplayName}
            string[] parts = input.Split(' ', 4);
            if (parts.Length == 4)
            {
                string username = parts[1];
                string secret = parts[2];
                string displayName = parts[3];

                // Save the DisplayName in the context for future use
                context.DisplayName = displayName;

                // Format the authentication message to send to the server
                string authMessage = $"AUTH {username} AS {displayName} USING {secret}\r";

                // Send the authentication message to the server
                await context.SendMessageAsync(authMessage);

                // Transition to the AuthState after sending the authentication request
                context.SetState(new AuthState());
            }
            else
            {
                // Inform the user about the correct usage of the /auth command
                Console.WriteLine("Usage: /auth {Username} {Secret} {DisplayName}\n");
            }
        }
        else if (input.StartsWith("/help"))
        {
            // Provide help information for the /auth command
            Console.WriteLine("/auth {Username} {Secret} {DisplayName} - Authenticate with the server.\n");
        }
        else
        {
            // Inform the user that they must authenticate first
            Console.WriteLine("ERROR: authorize using /auth first.\n");
        }
    }

    // Handles server messages while in the start state
    public void HandleServerMessage(ClientContext context, string message)
    {
        if (message.StartsWith("ERR FROM "))
        {
            // Parse and handle error messages from the server
            // Expected format: "ERR FROM {DisplayName} IS {MessageContent}"
            string[] parts = message.Split(new[] { "ERR FROM ", " IS " }, StringSplitOptions.None);
            if (parts.Length == 3)
            {
                string displayName = parts[1].Trim();
                string messageContent = parts[2].Trim();
                Console.WriteLine($"ERROR FROM {displayName}: {messageContent}\n");
            }
            else
            {
                // Handle malformed error messages
                Console.WriteLine($"[StartState] Malformed ERR message: {message}\n");
            }
        }
        else if (message.StartsWith("BYE"))
        {
            // Handle server termination of the connection
            Console.WriteLine("Server has terminated the connection. Goodbye.\n");

            // Gracefully stop listening for messages and exit the application
            context.StopListening();
            Environment.Exit(0);
        }
        else
        {
            // Handle unexpected server responses
            Console.WriteLine($"[StartState] Server: {message}\n");
        }
    }
}
