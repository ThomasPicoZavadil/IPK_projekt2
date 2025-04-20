// Author: xzavadt00
// This class represents the open state of the client in the TCP protocol.
// It handles user input for sending messages, joining channels, renaming the display name, and disconnecting.
// It also processes server messages such as errors, incoming messages, and termination signals.

public class OpenState : IClientState
{
    // Handles user input while in the open state
    public async Task HandleInput(ClientContext context, string input)
    {
        if (input.StartsWith("/join "))
        {
            // Handle the /join command to join a specific channel
            string[] parts = input.Split(' ', 2);
            if (parts.Length == 2)
            {
                string channelName = parts[1];
                string joinMessage = $"JOIN {channelName} AS {context.DisplayName}\r";
                await context.SendMessageAsync(joinMessage);

                // Transition to the JoinState after sending the join request
                context.SetState(new JoinState(channelName));
            }
            else
            {
                Console.WriteLine("Usage: /join {channel name}\n");
            }
        }
        else if (input.StartsWith("/bye"))
        {
            // Handle the /bye command to disconnect and exit
            string byeMessage = $"BYE FROM {context.DisplayName}\r";
            await context.SendMessageAsync(byeMessage);

            // Gracefully stop listening and exit the application
            context.StopListening();
            Environment.Exit(0);
        }
        else if (input.StartsWith("/rename "))
        {
            // Handle the /rename command to change the display name
            string[] parts = input.Split(' ', 2);
            if (parts.Length == 2)
            {
                string newDisplayName = parts[1];
                context.DisplayName = newDisplayName; // Update the DisplayName locally
            }
            else
            {
                Console.WriteLine("Usage: /rename {new display name}\n");
            }
        }
        else if (input.StartsWith("/help"))
        {
            // Display a list of supported commands
            Console.WriteLine("Supported commands:");
            Console.WriteLine("/join {channel name} - Join a specific channel.");
            Console.WriteLine("/bye - Disconnect from the server and exit the application.");
            Console.WriteLine("/rename {new display name} - Change your display name locally.");
            Console.WriteLine("/help - Show this help message with supported commands.\n");
        }
        else if (input.StartsWith("/auth "))
        {
            // Prevent re-authentication in the open state
            Console.WriteLine("ERROR: You are already authenticated. No need to re-authenticate.\n");
        }
        else if (string.IsNullOrWhiteSpace(input))
        {
            // Handle empty or invalid commands
            Console.WriteLine($"ERROR: Unknown command: {input}\n");
        }
        else
        {
            // Validate and send a regular message
            if (input.Contains("\n"))
            {
                Console.WriteLine("ERROR: Message contains invalid line feed (LF) characters.\n");
                return;
            }

            // Format the message and send it to the server
            string formattedMessage = $"MSG FROM {context.DisplayName} IS {input}\r";
            await context.SendMessageAsync(formattedMessage);
        }
    }

    // Handles server messages while in the open state
    public void HandleServerMessage(ClientContext context, string message)
    {
        // Split the input string by the delimiter "\r" to handle multiple messages
        string[] messages = message.Split(new[] { "\r" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string msg in messages)
        {
            if (msg.StartsWith("BYE"))
            {
                // Handle server termination of the connection
                Console.WriteLine("Server has terminated the connection. Goodbye.\n");

                // Gracefully stop listening and exit the application
                context.StopListening();
                Environment.Exit(0);
            }
            else if (msg.StartsWith("ERR FROM "))
            {
                // Parse and handle error messages from the server
                string[] parts = msg.Split(new[] { "ERR FROM ", " IS " }, StringSplitOptions.None);
                if (parts.Length == 3)
                {
                    string displayName = parts[1].Trim();
                    string messageContent = parts[2].Trim();
                    Console.WriteLine($"ERROR FROM {displayName}: {messageContent}\n");
                }
                else
                {
                    Console.WriteLine($"[OpenState] Malformed ERR message: {msg}\n");
                }
            }
            else if (msg.StartsWith("MSG FROM "))
            {
                // Parse and display incoming messages from other users
                string[] parts = msg.Split(new[] { "MSG FROM ", " IS " }, StringSplitOptions.None);
                if (parts.Length == 3)
                {
                    string displayName = parts[1].Trim();
                    string messageContent = parts[2].Trim();
                    Console.WriteLine($"{displayName}: {messageContent}\n");
                }
                else
                {
                    Console.WriteLine($"[OpenState] Malformed MSG message: {msg}\n");
                }
            }
            else
            {
                // Handle invalid or unexpected messages
                Console.WriteLine($"ERROR: Invalid message received: {msg}\n");

                // Send an error response back to the server
                string errResponse = $"ERR FROM {context.DisplayName} IS Invalid message format\r\n";
                context.SendMessage(errResponse);

                // Gracefully stop listening and exit the application
                context.StopListening();
                Environment.Exit(0);
            }
        }
    }
}
