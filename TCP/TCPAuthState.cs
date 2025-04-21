// Author: xzavadt00
// This class represents the authentication state of the client in the TCP protocol.
// It handles user input for authentication and processes server messages related to authentication.

using System;

public class AuthState : IClientState
{
    // Handles user input while in the authentication state
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
            HandleAuthCommand(context, input);
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
    private async void HandleAuthCommand(ClientContext context, string input)
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

            Console.WriteLine("Authentication message sent. Waiting for server response...\n");
        }
        else
        {
            Console.WriteLine("Usage: /auth {Username} {Secret} {DisplayName}\n");
        }
    }

    // Displays help information for the /auth command
    private void DisplayHelp()
    {
        Console.WriteLine("/auth {Username} {Secret} {DisplayName} - Authenticate with the server.\n");
    }

    // Handles server messages while in the authentication state
    public void HandleServerMessage(ClientContext context, string message)
    {
        // Check if the message is null or empty
        if (string.IsNullOrWhiteSpace(message))
        {
            Console.WriteLine("ERROR: Received an empty message from the server.\n");
            return;
        }

        // Handle successful authentication response
        if (message.StartsWith("REPLY OK IS "))
        {
            HandleSuccessResponse(context, message);
        }
        // Handle failed authentication response
        else if (message.StartsWith("REPLY NOK IS "))
        {
            HandleFailureResponse(message);
        }
        // Handle error messages from the server
        else if (message.StartsWith("ERR FROM "))
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
            Console.WriteLine($"[AuthState] Server: {message}\n");
        }
    }

    // Handles successful authentication response from the server
    private void HandleSuccessResponse(ClientContext context, string message)
    {
        // Extract the success message content
        string messageContent = message.Substring("REPLY OK IS ".Length);
        Console.WriteLine($"Action Success: {messageContent}\n");

        // Transition to the OpenState after successful authentication
        context.SetState(new OpenState());
    }

    // Handles failed authentication response from the server
    private void HandleFailureResponse(string message)
    {
        // Extract the failure message content
        string messageContent = message.Substring("REPLY NOK IS ".Length);
        Console.WriteLine($"Action Failure: {messageContent}\n");
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
            Console.WriteLine($"[AuthState] Malformed ERR message: {message}\n");
        }
    }

    // Handles server termination messages
    private void HandleServerTermination(ClientContext context)
    {
        Console.WriteLine("Server has terminated the connection. Goodbye.\n");

        // Stop listening for messages and exit the application
        context.StopListening();
        Environment.Exit(0);
    }
}
