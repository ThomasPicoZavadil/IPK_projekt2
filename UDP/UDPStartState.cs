using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class UDPStartState : IUDPClientState
{
    private const byte AUTH_MESSAGE = 0x02; // Type for AUTH message
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

                // Pass the existing UdpClient and RemoteEndPoint to SendAuthWithRetries
                await SendAuthWithRetries(context, context.UdpClient, context.RemoteEndPoint, authMessage, context.Timeout, context.Retransmissions);
            }
            else
            {
                Console.WriteLine("Usage: /auth {username} {secret} {DisplayName}");
            }
        }
        else if (input == "/help")
        {
            Console.WriteLine("Usage: /auth {username} {secret} {DisplayName}");
        }
        else
        {
            Console.WriteLine("ERROR: Please authorize first.");
        }
    }
    
    // Handles server messages (invalid for this state)
    public void HandleServerMessage(ClientContext context, string message)
    {
        Console.WriteLine("[UDPAuthState] ERROR: Received an invalid message type for this state.");
    }

    public void HandleUDPMessage(ClientContext context, byte[] data)
    {
        // Ensure the message is not null or empty
        if (data == null || data.Length == 0)
        {
            Console.WriteLine("[UDPStartState] ERROR: Received an empty message.");
            return;
        }

        // Convert the byte array to a string for display
        string message = Encoding.UTF8.GetString(data);

        // Print the received message
        Console.WriteLine($"[UDPStartState] Received message: {message}");
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
        message[0] = AUTH_MESSAGE; // 0x02

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

    private async Task SendAuthWithRetries(ClientContext context, UdpClient udpClient, IPEndPoint remoteEndPoint, byte[] authMessage, int timeout, int retransmissions)
    {
        for (int attempt = 0; attempt <= retransmissions; attempt++)
        {
            try
            {
                // Send the AUTH message
                await udpClient.SendAsync(authMessage, authMessage.Length, remoteEndPoint);
                Console.WriteLine($"[UDPStartState] Sent AUTH message (attempt {attempt + 1}/{retransmissions + 1}).");

                // Wait for the CONFIRM message
                udpClient.Client.ReceiveTimeout = timeout;
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                byte[] receivedData = result.Buffer;

                // Check if the received message is a CONFIRM message
                if (receivedData.Length >= 3 && receivedData[0] == CONFIRM_MESSAGE)
                {
                    ushort refMessageId = (ushort)((receivedData[1] << 8) | receivedData[2]);
                    if (refMessageId == context.MessageIdCounter - 1)
                    {
                        Console.WriteLine("[UDPStartState] Received CONFIRM message. Authentication successful.");
                        context.SetState(new UDPAuthState());
                        return;
                    }
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                Console.WriteLine($"[UDPStartState] Timeout waiting for CONFIRM message (attempt {attempt + 1}/{retransmissions + 1}).");
            }
        }

        Console.WriteLine("[UDPStartState] Authentication failed after maximum retransmissions.");
    }
}