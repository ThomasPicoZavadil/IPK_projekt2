using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class UDPAuthState : IClientState
{
    private const byte REPLY_TYPE = 0x01; // Type for REPLY message

    public async Task HandleInput(ClientContext context, string input)
    {
        Console.WriteLine("You are already authenticated. No further input is required in this state.");
    }

    public void HandleServerMessage(ClientContext context, string message)
    {
        Console.WriteLine($"[UDPAuthState] Server: {message}");
    }
}
