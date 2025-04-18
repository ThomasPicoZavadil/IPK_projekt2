using System.Text.RegularExpressions;

public class AuthState : IClientState
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
                string authMessage = $"AUTH {username} AS {displayName} USING {secret}\r\n";
                await context.SendMessageAsync(authMessage); // Send the formatted message to the server
                Console.WriteLine("Switching to AuthState.");
                context.SetState(new AuthState());
            }
            else
            {
                Console.WriteLine("Usage: /auth {Username} {Secret} {DisplayName}");
            }
        }
        else
        {
            Console.WriteLine("Please authenticate first using: /auth {Username} {Secret} {DisplayName}");
        }
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
            Console.WriteLine($"[AuthState] Server: {message}");
        }
    }
}
