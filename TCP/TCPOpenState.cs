// Author: xzavadt00
// This class represents the open state of the client in the TCP protocol.
// It handles user input for sending messages, joining channels, renaming the display name, and disconnecting.
// It also processes server messages such as errors, incoming messages, and termination signals.

public class OpenState : IClientState
{
    // Handles user input while in the open state
    public async Task HandleInput(ClientContext context, string input)
    {
        // Check if the input is null or empty
        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("ERROR: Input cannot be empty.\n");
            return;
        }

        // Handle the /join command
        if (input.StartsWith("/join "))
        {
            await HandleJoinCommand(context, input);
        }
        // Handle the /rename command
        else if (input.StartsWith("/rename "))
        {
            HandleRenameCommand(context, input);
        }
        // Display help information
        else if (input.StartsWith("/help"))
        {
            DisplayHelp();
        }
        // Prevent re-authentication in the open state
        else if (input.StartsWith("/auth "))
        {
            Console.WriteLine("ERROR: You are already authenticated. No need to re-authenticate.\n");
        }
        // Handle regular messages
        else
        {
            await HandleRegularMessage(context, input);
        }
    }

    // Handles the /join command to join a specific channel
    private async Task HandleJoinCommand(ClientContext context, string input)
    {
        // Split the input into parts: /join {channel name}
        string[] parts = input.Split(' ', 2);
        if (parts.Length == 2)
        {
            string channelName = parts[1];
            string joinMessage = $"JOIN {channelName} AS {context.DisplayName}\r";

            // Send the join request to the server
            await context.SendMessageAsync(joinMessage);

            // Transition to the JoinState after sending the join request
            context.SetState(new JoinState(channelName));
        }
        else
        {
            // Display usage information if the command format is incorrect
            Console.WriteLine("Usage: /join {channel name}\n");
        }
    }

    // Handles the /rename command to change the display name
    private void HandleRenameCommand(ClientContext context, string input)
    {
        // Split the input into parts: /rename {new display name}
        string[] parts = input.Split(' ', 2);
        if (parts.Length == 2)
        {
            string newDisplayName = parts[1];

            // Update the display name locally
            context.DisplayName = newDisplayName;
            Console.Error.WriteLine($"Display name updated to: {newDisplayName}\n");
        }
        else
        {
            Console.WriteLine("Usage: /rename {new display name}\n");
        }
    }

    // Displays help information for supported commands
    private void DisplayHelp()
    {
        Console.WriteLine("Supported commands:");
        Console.WriteLine("/join {channel name} - Join a specific channel.");
        Console.WriteLine("/bye - Disconnect from the server and exit the application.");
        Console.WriteLine("/rename {new display name} - Change your display name locally.");
        Console.WriteLine("/help - Show this help message with supported commands.\n");
    }

    // Handles regular messages sent by the user
    private async Task HandleRegularMessage(ClientContext context, string input)
    {
        // Check if the message contains invalid line feed (LF) characters
        if (input.Contains("\n"))
        {
            Console.WriteLine("ERROR: Message contains invalid line feed (LF) characters.\n");
            return;
        }

        // Format the message and send it to the server
        string formattedMessage = $"MSG FROM {context.DisplayName} IS {input}\r";
        await context.SendMessageAsync(formattedMessage);
    }

    // Handles server messages while in the open state
    public void HandleServerMessage(ClientContext context, string message)
    {
        // Split the message into individual parts using the carriage return (\r) delimiter
        string[] messages = message.Split(new[] { "\r" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string msg in messages)
        {
            // Handle server termination messages
            if (msg.StartsWith("BYE"))
            {
                HandleServerTermination(context);
            }
            // Handle error messages from the server
            else if (msg.StartsWith("ERR FROM "))
            {
                HandleErrorMessage(msg);
            }
            // Handle incoming messages from other users
            else if (msg.StartsWith("MSG FROM "))
            {
                HandleIncomingMessage(msg);
            }
            // Handle invalid or unexpected messages
            else
            {
                HandleInvalidMessage(context, msg);
            }
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
            Console.WriteLine($"[OpenState] Malformed ERR message: {message}\n");
        }
    }

    // Handles incoming messages from other users
    private void HandleIncomingMessage(string message)
    {
        // Expected format: "MSG FROM {DisplayName} IS {MessageContent}"
        string[] parts = message.Split(new[] { "MSG FROM ", " IS " }, StringSplitOptions.None);
        if (parts.Length == 3)
        {
            string displayName = parts[1].Trim();
            string messageContent = parts[2].Trim();
            Console.WriteLine($"{displayName}: {messageContent}\n");
        }
        else
        {
            // Handle malformed incoming messages
            Console.WriteLine($"[OpenState] Malformed MSG message: {message}\n");
        }
    }

    // Handles invalid or unexpected messages
    private void HandleInvalidMessage(ClientContext context, string message)
    {
        Console.WriteLine($"ERROR: Invalid message received: {message}\n");

        // Send an error response back to the server
        string errResponse = $"ERR FROM {context.DisplayName} IS Invalid message format\r\n";
        context.SendMessage(errResponse);

        // Stop listening for messages and exit the application
        context.StopListening();
        Environment.Exit(0);
    }
}
