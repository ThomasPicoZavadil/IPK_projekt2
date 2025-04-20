public class OpenState : IClientState
{
    public async Task HandleInput(ClientContext context, string input)
    {
        if (input.StartsWith("/join "))
        {
            string[] parts = input.Split(' ', 2);
            if (parts.Length == 2)
            {
                string channelName = parts[1];
                string joinMessage = $"JOIN {channelName} AS {context.DisplayName}\r";
                await context.SendMessageAsync(joinMessage);
                context.SetState(new JoinState(channelName)); // Transition to JoinState
            }
            else
            {
                Console.WriteLine("Usage: /join {channel name}\n");
            }
        }
        else if (input.StartsWith("/bye"))
        {
            string byeMessage = $"BYE FROM {context.DisplayName}\r";
            await context.SendMessageAsync(byeMessage);
            context.StopListening(); // Gracefully stop listening for messages
            Environment.Exit(0); // Exit the application
        }
        else if (input.StartsWith("/rename "))
        {
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
            Console.WriteLine("Supported commands:");
            Console.WriteLine("/join {channel name} - Join a specific channel.");
            Console.WriteLine("/bye - Disconnect from the server and exit the application.");
            Console.WriteLine("/rename {new display name} - Change your display name locally.");
            Console.WriteLine("/help - Show this help message with supported commands.\n");
        }
        else if (input.StartsWith("/auth "))
        {
            Console.WriteLine("ERROR: You are already authenticated. No need to re-authenticate.\n");
        }
        else if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine($"ERROR: Unknown command: {input}\n");
        }
        else
        {
            // Validate the message content
            if (input.Contains("\n"))
            {
                Console.WriteLine("ERROR: Message contains invalid line feed (LF) characters.\n");
                return;
            }
            // Treat any input that doesn't start with "/" as a message
            string formattedMessage = $"MSG FROM {context.DisplayName} IS {input}\r";
            await context.SendMessageAsync(formattedMessage);
        }
    }

    public void HandleServerMessage(ClientContext context, string message)
    {
        // Split the input string by the delimiter "\r\n" to handle multiple messages
        string[] messages = message.Split(new[] { "\r" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string msg in messages)
        {
            if (msg.StartsWith("BYE"))
            {
                Console.WriteLine("Server has terminated the connection. Goodbye.\n");
                context.StopListening(); // Gracefully stop listening for messages
                Environment.Exit(0); // Exit the application
            }
            else if (msg.StartsWith("ERR FROM "))
            {
                // Parse the message in the format "ERR FROM {DisplayName} IS {MessageContent}"
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
                // Parse the message in the format "MSG FROM {DisplayName} IS {MessageContent}"
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
                // Handle invalid messages
                Console.WriteLine($"ERROR: Invalid message received: {msg}\n");

                // Send an ERR response back to the server
                string errResponse = $"ERR FROM {context.DisplayName} IS Invalid message format\r\n";
                context.SendMessage(errResponse);
                context.StopListening(); // Gracefully stop listening for messages
                Environment.Exit(0); // Exit the application
            }
        }
    }
}
