using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class ClientContext
{
    private NetworkStream stream;
    private IClientState currentState;
    private CancellationTokenSource cancellationTokenSource;
    public string? DisplayName { get; set; } // Add this property to store the DisplayName

    public ClientContext(NetworkStream stream)
    {
        this.stream = stream;
        this.currentState = new StartState();
        this.cancellationTokenSource = new CancellationTokenSource();
        StartListeningForServerMessages();
    }

    public void SendMessage(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        stream.Write(data, 0, data.Length);
    }

    public void ProcessInput(string input)
    {
        currentState.HandleInput(this, input);
    }

    public void ProcessServerMessage(string message)
    {
        currentState.HandleServerMessage(this, message);
    }

    public void SetState(IClientState newState)
    {
        currentState = newState;
    }

    public async Task SendMessageAsync(string message)
    {
        if (stream != null && stream.CanWrite)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            await stream.WriteAsync(data, 0, data.Length);
        }
    }

    private async void StartListeningForServerMessages()
    {
        try
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    string message = await reader.ReadLineAsync();
                    if (message != null)
                    {
                        ProcessServerMessage(message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while reading server messages: {ex.Message}");
        }
    }

    public void StopListening()
    {
        cancellationTokenSource.Cancel();
    }
}
