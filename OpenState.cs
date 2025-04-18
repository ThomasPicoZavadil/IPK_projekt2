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
                string joinMessage = $"JOIN {channelName} AS {context.DisplayName}\r\n";
                await context.SendMessageAsync(joinMessage);
                Console.WriteLine($"Joining channel: {channelName}");
                context.SetState(new JoinState(channelName)); // Transition to JoinState
            }
            else
            {
                Console.WriteLine("Usage: /join {channel name}");
            }
        }
        else if (input.StartsWith("/bye"))
        {
            string byeMessage = $"BYE FROM {context.DisplayName}\r\n";
            await context.SendMessageAsync(byeMessage);
            Console.WriteLine("Goodbye! Terminating connection.");
            context.StopListening(); // Gracefully stop listening for messages
            Environment.Exit(0); // Exit the application
        }
        else
        {
            // Treat any input that doesn't start with "/" as a message
            string formattedMessage = $"MSG FROM {context.DisplayName} IS {input}\r\n";
            await context.SendMessageAsync(formattedMessage);
            Console.WriteLine(formattedMessage);
            Console.WriteLine($"Message sent: {input}");
        }
    }

    public void HandleServerMessage(ClientContext context, string message)
    {
        if (message == "ERR")
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
        else if (message.StartsWith("MSG FROM "))
        {
            // Parse the message in the format "MSG FROM {DisplayName} IS {MessageContent}\r\n"
            string[] parts = message.Split(new[] { "MSG FROM ", " IS " }, StringSplitOptions.None);
            if (parts.Length == 3)
            {
                string displayName = parts[1].Trim();
                string messageContent = parts[2].Trim();
                Console.WriteLine($"{displayName}: {messageContent}");
            }
            else
            {
                Console.WriteLine($"[OpenState] Malformed MSG message: {message}");
            }
        }
        else
        {
            Console.WriteLine($"[OpenState] Server: {message}");
        }
    }
}
