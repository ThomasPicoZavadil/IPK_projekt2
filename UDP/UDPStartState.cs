using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class UDPStartState : UDPClientState
{
    private const byte AUTH_TYPE = 0x02; // Type for AUTH message
    private const byte CONFIRM_MESSAGE = 0x00; // Expected confirmation message from the server

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

                context.DisplayName = displayName;

                // Construct the AUTH message
                byte[] authMessage = ConstructAuthMessage(username, secret, displayName, context);

                // Send the AUTH message and wait for confirmation
                await SendAuthWithRetries(context, authMessage, context.ServerAddress, context.ServerPort, context.Timeout, context.Retransmissions);
            }
        }
        else if (input == "/help")
        {
            Console.WriteLine("Usage: /auth {username} {secret} {DisplayName}");
        }
        else
        {
            Console.WriteLine("ERROR: Please authorize first");
        }
    }

    public void HandleServerMessage(ClientContext context, string message)
    {
        Console.WriteLine($"[UDPStartState] Server: {message}");
    }

    public async Task SendAuthWithRetries(ClientContext context, byte[] authMessage, string serverAddress, int serverPort, int timeout, int retransmissions)
    {
        if (string.IsNullOrEmpty(serverAddress))
        {
            Console.WriteLine("ERROR: Server address is null or empty. Cannot send AUTH message.");
            return;
        }

        using (UdpClient client = new UdpClient())
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(serverAddress), serverPort);

            for (int attempt = 0; attempt <= retransmissions; attempt++)
            {
                try
                {
                    // Send the AUTH message
                    await client.SendAsync(authMessage, authMessage.Length, remoteEndPoint);
                    Console.WriteLine($"AUTH message sent (attempt {attempt + 1}/{retransmissions + 1}).");

                    // Wait for the CONFIRM message
                    client.Client.ReceiveTimeout = timeout;
                    UdpReceiveResult result = await client.ReceiveAsync();
                    byte[] receivedData = result.Buffer;

                    // Process the CONFIRM message
                    if (receivedData.Length >= 3 && receivedData[0] == 0x00)
                    {
                        ushort refMessageId = (ushort)((receivedData[1] << 8) | receivedData[2]);
                        Console.WriteLine($"CONFIRM message received with Ref_MessageID: {refMessageId}");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Invalid CONFIRM message received.");
                    }
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    Console.WriteLine($"Timeout waiting for CONFIRM message (attempt {attempt + 1}/{retransmissions + 1}).");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");
                }
            }

            Console.WriteLine("ERROR: Maximum retransmissions reached. AUTH failed.");
        }
    }

    private byte[] ConstructAuthMessage(string username, string secret, string displayName, ClientContext context)
    {
        // Increment the global message ID counter
        ushort messageId = context.MessageIdCounter++;

        // Convert fields to bytes and append a null byte (0) to each
        byte[] usernameBytes = Encoding.UTF8.GetBytes(username + "\0");
        byte[] displayNameBytes = Encoding.UTF8.GetBytes(displayName + "\0");
        byte[] secretBytes = Encoding.UTF8.GetBytes(secret + "\0");

        // Calculate the total length of the message
        int totalLength = 1 + 2 + usernameBytes.Length + displayNameBytes.Length + secretBytes.Length;

        // Create a buffer for the message
        byte[] message = new byte[totalLength];

        // Add the Type (1 byte)
        message[0] = AUTH_TYPE; // 0x02

        // Add the Message ID (2 bytes, big-endian)
        message[1] = (byte)(messageId >> 8); // High byte
        message[2] = (byte)(messageId & 0xFF); // Low byte

        // Add the Username (null-terminated)
        Buffer.BlockCopy(usernameBytes, 0, message, 3, usernameBytes.Length);

        // Add the DisplayName (null-terminated)
        Buffer.BlockCopy(displayNameBytes, 0, message, 3 + usernameBytes.Length, displayNameBytes.Length);

        // Add the Secret (null-terminated)
        Buffer.BlockCopy(secretBytes, 0, message, 3 + usernameBytes.Length + displayNameBytes.Length, secretBytes.Length);

        return message;
    }
}