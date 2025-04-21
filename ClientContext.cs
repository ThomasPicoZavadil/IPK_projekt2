using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class ClientContext
{
    // Network stream for TCP communication (nullable for UDP)
    private NetworkStream? stream;

    // Current state of the client (e.g., StartState, AuthState, etc.)
    private IClientState currentState;

    // Token source for managing cancellation of tasks
    private CancellationTokenSource cancellationTokenSource;

    // Display name of the client
    public string? DisplayName { get; set; }

    // Properties for UDP communication
    public UdpClient? UdpClient { get; private set; } // UDP client instance
    public IPEndPoint? RemoteEndPoint { get; set; } // Remote server endpoint
    public string? ServerAddress { get; private set; } // Server address
    public int ServerPort { get; set; } // Server port
    public int Timeout { get; private set; } // Timeout in milliseconds
    public int Retransmissions { get; private set; } // Number of retransmissions
    public ushort MessageIdCounter { get; set; } = 0; // Counter for message IDs

    // Constructor for TCP communication
    public ClientContext(NetworkStream stream)
    {
        this.stream = stream;
        this.currentState = new StartState(); // Initialize the client in the StartState
        this.cancellationTokenSource = new CancellationTokenSource();
        StartListeningForServerMessages(); // Start listening for server messages

        // Handle Ctrl+C for graceful exit
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; // Prevent immediate termination
            GracefulExit(); // Call the graceful exit method
        };
    }

    // Constructor for UDP communication
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
        this.currentState = new UDPStartState(); // Initialize the client in the UDPStartState
        this.cancellationTokenSource = new CancellationTokenSource();

        // Handle Ctrl+C for graceful exit
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; // Prevent immediate termination
            GracefulExit(); // Call the graceful exit method
        };
    }

    // Gracefully exits the application
    public void GracefulExit()
    {
        try
        {
            // If the stream is initialized and writable, send a BYE message
            if (stream != null && stream.CanWrite)
            {
                string byeMessage = $"BYE FROM {DisplayName}\r";
                SendMessageAsync(byeMessage);
                StopListening(); // Stop listening for messages
                Environment.Exit(0); // Exit the application
            }

            // Close the UDP client if it exists
            Environment.Exit(0); // Exit the application
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

    // Sends a message synchronously over the TCP stream
    public void SendMessage(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        stream.Write(data, 0, data.Length);
    }

    // Processes user input and delegates it to the current state
    public void ProcessInput(string input)
    {
        if (input == null) // Handle Ctrl+D (EOF)
        {
            Console.WriteLine("EOF detected. Cleaning up and exiting...");
            GracefulExit();
            return;
        }

        currentState.HandleInput(this, input); // Delegate input handling to the current state
    }

    // Processes server messages and delegates them to the current state
    public void ProcessServerMessage(string message)
    {
        currentState.HandleServerMessage(this, message); // Delegate message handling to the current state
    }

    // Processes UDP messages and delegates them to the current state (if it supports UDP)
    public void ProcessUDPMessage(byte[] data)
    {
        if (currentState is IUDPClientState udpState)
        {
            udpState.HandleUDPMessage(this, data); // Delegate UDP message handling to the current state
        }
        else
        {
            Console.WriteLine("[ClientContext] ERROR: Current state does not support UDP messages.");
        }
    }

    // Sets the current state of the client
    public void SetState(IClientState newState)
    {
        currentState = newState;
    }

    // Sends a message asynchronously over the TCP stream
    public async Task SendMessageAsync(string message)
    {
        if (stream != null && stream.CanWrite)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            await stream.WriteAsync(data, 0, data.Length);
        }
    }

    // Starts listening for server messages asynchronously
    private async void StartListeningForServerMessages()
    {
        try
        {
            // Ensure the stream is not null before creating the StreamReader
            if (stream == null)
            {
                Console.WriteLine("[ClientContext] ERROR: Network stream is not initialized.");
                return;
            }

            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    string? message = await reader.ReadLineAsync(); // Read a line from the stream
                    if (!string.IsNullOrEmpty(message)) // Check if the message is not null or empty
                    {
                        ProcessServerMessage(message); // Process the server message
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while reading server messages: {ex.Message}");
        }
    }

    // Stops listening for server messages
    public void StopListening()
    {
        cancellationTokenSource.Cancel(); // Cancel the listening task
    }
}
