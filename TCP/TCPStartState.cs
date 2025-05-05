// Author: xzavadt00
// This class represents the start state of the client in the TCP protocol.
// It handles user input for authentication and processes server messages related to errors or termination.

public class StartState : IClientState
{
    // Handles user input while in the start state
    public async Task HandleInput(ClientContext context, string input)
    {
        // Check if the input is null or empty
        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("ERROR: Input cannot be empty.\n");
            return;
        }

        // Handle the /auth command
        if (input.StartsWith("/auth "))
        {
            await HandleAuthCommand(context, input);
        }
        // Display help information
        else if (input.StartsWith("/help"))
        {
            DisplayHelp();
        }
        // Handle invalid commands
        else
        {
            Console.WriteLine("ERROR: authorize using /auth first.\n");
        }
    }

    // Handles the /auth command to authenticate the user
    private async Task HandleAuthCommand(ClientContext context, string input)
    {
        // Split the input into parts: /auth {Username} {Secret} {DisplayName}
        string[] parts = input.Split(' ', 4);
        if (parts.Length == 4)
        {
            string username = parts[1];
            string secret = parts[2];
            string displayName = parts[3];

            // Set the display name in the client context
            context.DisplayName = displayName;

            // Construct the authentication message
            string authMessage = $"AUTH {username} AS {displayName} USING {secret}\r";

            // Send the authentication message to the server
            await context.SendMessageAsync(authMessage);

            Console.Error.WriteLine("Authentication message sent. Waiting for server response...\n");

            // Transition to the AuthState after sending the authentication request
            context.SetState(new AuthState());
        }
        else
        {
            // Display usage information if the command format is incorrect
            Console.WriteLine("Usage: /auth {Username} {Secret} {DisplayName}\n");
        }
    }

    // Displays help information for the /auth command
    private void DisplayHelp()
    {
        Console.WriteLine("/auth {Username} {Secret} {DisplayName} - Authenticate with the server.\n");
    }

    // Handles server messages while in the start state
    public void HandleServerMessage(ClientContext context, string message)
    {
        // Check if the message is null or empty
        if (string.IsNullOrWhiteSpace(message))
        {
            Console.WriteLine("ERROR: Received an empty message from the server.\n");
            return;
        }

        // Handle error messages from the server
        if (message.StartsWith("ERR FROM "))
        {
            HandleErrorMessage(message);
        }
        // Handle server termination messages
        else if (message.StartsWith("BYE"))
        {
            HandleServerTermination(context);
        }
        // Handle unexpected or unrecognized server messages
        else
        {
            HandleUnexpectedResponse(message);
        }
    }

    // Handles error messages sent by the server
    private void HandleErrorMessage(string message)
    {
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

    // Handles server termination messages
    private void HandleServerTermination(ClientContext context)
    {
        Console.Error.WriteLine("Server has terminated the connection. Goodbye.\n");

        // Stop listening for messages and exit the application
        context.StopListening();
        Environment.Exit(0);
    }

    // Handles unexpected server responses
    private void HandleUnexpectedResponse(string message)
    {
        Console.WriteLine($"[StartState] Server: {message}\n");
    }
}
