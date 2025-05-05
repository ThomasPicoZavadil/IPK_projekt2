// Author: xzavadt00
// This class represents the join state of the client in the TCP protocol.
// It handles server responses related to joining a channel and transitions the client to the appropriate state.

using System;

public class JoinState : IClientState
{
    private readonly string channelName; // The name of the channel the client is attempting to join

    // Constructor to initialize the JoinState with the specified channel name
    public JoinState(string channelName)
    {
        this.channelName = channelName;
    }

    // Handles user input while in the join state
    public async Task HandleInput(ClientContext context, string input)
    {
        Console.WriteLine("ERROR: Waiting for server response. You cannot send messages yet.\n");
    }

    // Handles server messages while in the join state
    public void HandleServerMessage(ClientContext context, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Console.WriteLine("ERROR: Received an empty message from the server.\n");
            return;
        }

        if (message.StartsWith("REPLY OK IS "))
        {
            HandleSuccessResponse(context, message);
        }
        else if (message.StartsWith("REPLY NOK IS "))
        {
            HandleFailureResponse(message);
        }
        else if (message.StartsWith("ERR FROM "))
        {
            HandleErrorMessage(message);
        }
        else if (message.StartsWith("BYE"))
        {
            HandleServerTermination(context);
        }
        else
        {
            HandleUnexpectedResponse(message);
        }
    }

    // Handles successful join response from the server
    private void HandleSuccessResponse(ClientContext context, string message)
    {
        string messageContent = message.Substring("REPLY OK IS ".Length);
        Console.WriteLine($"Action Success: {messageContent}\n");

        // Transition to the OpenState after successfully joining the channel
        context.SetState(new OpenState());
    }

    // Handles failed join response from the server
    private void HandleFailureResponse(string message)
    {
        string messageContent = message.Substring("REPLY NOK IS ".Length);
        Console.WriteLine($"Action Failure: {messageContent}\n");
    }

    // Handles error messages from the server
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
            Console.WriteLine($"[JoinState] Malformed ERR message: {message}\n");
        }
    }

    // Handles server termination of the connection
    private void HandleServerTermination(ClientContext context)
    {
        Console.Error.WriteLine("Server has terminated the connection. Goodbye.\n");

        // Gracefully stop listening for messages and exit the application
        context.StopListening();
        Environment.Exit(0);
    }

    // Handles unexpected server responses
    private void HandleUnexpectedResponse(string message)
    {
        Console.WriteLine($"[JoinState] Unexpected server response: {message}\n");
    }
}