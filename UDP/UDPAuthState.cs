using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class UDPAuthState : IUDPClientState
{
    private const byte REPLY_MESSAGE = 0x02; // Type for AUTH message
    private const byte CONFIRM_MESSAGE = 0x00; // Expected confirmation message from the server

    // Handles UDP messages received from the server
    public void HandleUDPMessage(ClientContext context, byte[] data)
    {
        // Ensure the message is not null or empty
        if (data == null || data.Length < 6)
        {
            Console.WriteLine("[UDPAuthState] ERROR: Received an invalid or too short message.");
            return;
        }

        // Check if the message type is REPLY (0x01)
        if (data[0] != 0x01)
        {
            Console.WriteLine("[UDPAuthState] ERROR: Received message is not a REPLY message.");
            return;
        }

        // Extract fields from the REPLY message
        ushort messageId = (ushort)((data[1] << 8) | data[2]); // MessageID (2 bytes)
        byte result = data[3]; // Result (1 byte)
        ushort refMessageId = (ushort)((data[4] << 8) | data[5]); // Ref_MessageID (2 bytes)

        // Ensure RemoteEndPoint is not null before sending the CONFIRM message
        if (context.RemoteEndPoint == null)
        {
            Console.WriteLine("[UDPAuthState] ERROR: RemoteEndPoint is not initialized.");
            return;
        }

        // Send CONFIRM message to the server
        SendConfirmMessage(context, refMessageId, context.RemoteEndPoint);

        // Extract MessageContents (null-terminated string starting at byte 6)
        string messageContents = Encoding.UTF8.GetString(data, 6, data.Length - 6).TrimEnd('\0');

        // Print the appropriate message based on the Result field
        if (result == 1)
        {
            Console.WriteLine($"Action Success: {messageContents}");

            // Transition to the next state (e.g., UDPOpenState)
            // context.SetState(new UDPOpenState());
        }
        else if (result == 0)
        {
            Console.WriteLine($"Action Failure: {messageContents}");

            // Transition back to the UDPStartState
            context.SetState(new UDPStartState());
        }
        else
        {
            Console.WriteLine($"[UDPAuthState] ERROR: Invalid Result value in REPLY message: {result}");
        }
    }

    // Sends a CONFIRM message to the server
    private void SendConfirmMessage(ClientContext context, ushort refMessageId, IPEndPoint remoteEndPoint)
    {
        // Construct the CONFIRM message
        byte[] confirmMessage = new byte[3];
        confirmMessage[0] = CONFIRM_MESSAGE; // Message type
        confirmMessage[1] = (byte)(refMessageId >> 8); // High byte of Ref_MessageID
        confirmMessage[2] = (byte)(refMessageId & 0xFF); // Low byte of Ref_MessageID

        try
        {
            // Ensure UdpClient is not null before sending the CONFIRM message
            if (context.UdpClient == null)
            {
                Console.WriteLine("[UDPAuthState] ERROR: UdpClient is not initialized.");
                return;
            }

            // Send the CONFIRM message to the server
            context.UdpClient.Send(confirmMessage, confirmMessage.Length, remoteEndPoint);
            Console.WriteLine("[UDPAuthState] Sent CONFIRM message to the server.");

            // Update the ClientContext to use the server's port for future communication
            context.RemoteEndPoint = remoteEndPoint;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UDPAuthState] ERROR: Failed to send CONFIRM message: {ex.Message}");
        }
    }

    // Handles server messages (invalid for this state)
    public void HandleServerMessage(ClientContext context, string message)
    {
        Console.WriteLine("[UDPAuthState] ERROR: Received an invalid message type for this state.");
    }

    // Handles user input (waiting for verification)
    public async Task HandleInput(ClientContext context, string input)
    {
        Console.WriteLine("[UDPAuthState] Waiting for verification. No input is allowed in this state.");
    }
}