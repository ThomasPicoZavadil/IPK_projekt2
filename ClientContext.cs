using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class ClientContext
{
    // Network stream for TCP communication
    private NetworkStream? stream;

    // Current state of the client (e.g., StartState, AuthState, etc.)
    private IClientState currentState;

    // Token source for managing cancellation of tasks
    private CancellationTokenSource cancellationTokenSource;

    // Display name of the client
    public string? DisplayName { get; set; }

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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during graceful exit: {ex.Message}");
        }
        finally
        {
            Console.Error.WriteLine("Exiting the program.");
            Environment.Exit(0); // Exit the program
        }
    }

    // Sends a message synchronously over the TCP stream
    public void SendMessage(string message)
    {
        if (stream == null)
        {
            Console.WriteLine("[ClientContext] ERROR: Network stream is not initialized.");
            return;
        }

        byte[] data = Encoding.UTF8.GetBytes(message);
        stream.Write(data, 0, data.Length);
    }

    // Processes user input and delegates it to the current state
    public void ProcessInput(string input)
    {
        if (input == null) // Handle Ctrl+D (EOF)
        {
            Console.Error.WriteLine("EOF detected. Cleaning up and exiting...");
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
