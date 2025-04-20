using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class ClientContext
{
    private NetworkStream? stream; // Nullable for UDP
    private IClientState currentState;
    private CancellationTokenSource cancellationTokenSource;
    public string? DisplayName { get; set; } // Add this property to store the DisplayName

    // Properties for UDP
    public string? ServerAddress { get; private set; }
    public int ServerPort { get; set; }
    public int Timeout { get; private set; } // Timeout in milliseconds
    public int Retransmissions { get; private set; } // Number of retransmissions
    public ushort MessageIdCounter { get; set; } = 0;

    // Constructor for TCP
    public ClientContext(NetworkStream stream)
    {
        this.stream = stream;
        this.currentState = new StartState();
        this.cancellationTokenSource = new CancellationTokenSource();
        StartListeningForServerMessages();

        // Handle Ctrl+C for graceful exit
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; // Prevent immediate termination
            GracefulExit(); // Call the graceful exit method
        };
    }

    // Constructor for UDP
    public ClientContext(string protocol, string serverAddress, int serverPort, int timeout, int retransmissions)
    {
        if (protocol != "udp")
        {
            throw new ArgumentException("Invalid protocol for this constructor. Use 'udp'.");
        }

        this.ServerAddress = serverAddress;
        this.ServerPort = serverPort;
        this.Timeout = timeout;
        this.Retransmissions = retransmissions;
        this.currentState = new UDPStartState();
        this.cancellationTokenSource = new CancellationTokenSource();

        // Handle Ctrl+C for graceful exit
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; // Prevent immediate termination
            GracefulExit(); // Call the graceful exit method
        };
    }

    public void GracefulExit()
    {
        try
        {
            if (stream != null && stream.CanWrite)
            {
                string byeMessage = $"BYE FROM {DisplayName}\r";
                SendMessageAsync(byeMessage);
                StopListening(); // Gracefully stop listening for messages
                Environment.Exit(0); // Exit the application
            }

            // Stop listening for messages
            StopListening();

            // Dispose of the stream and other resources
            stream?.Close();
            stream?.Dispose();
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            Console.WriteLine("Closed the network stream and cleaned up resources.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during graceful exit: {ex.Message}");
        }
        finally
        {
            Console.WriteLine("Exiting the program.");
            Environment.Exit(0); // Exit the program
        }
    }

    public void SendMessage(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        stream.Write(data, 0, data.Length);
    }

    public void ProcessInput(string input)
    {
        if (input == null) // Handle Ctrl+D (EOF)
        {
            Console.WriteLine("EOF detected. Cleaning up and exiting...");
            GracefulExit();
            return;
        }

        currentState.HandleInput(this, input);
    }

    public void ProcessServerMessage(string message)
    {
        currentState.HandleServerMessage(this, message);
    }

    public void SetState(IClientState newState)
    {
        Console.WriteLine($"Changing state from {currentState.GetType().Name} to {newState.GetType().Name}");
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
