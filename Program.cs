using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "-h")
        {
            PrintHelp();
            return;
        }

        string protocol = "tcp";
        string server = "127.0.0.1";
        int port = 4567;
        int timeout = 100; // Default timeout for UDP in ms
        int retransmissions = 1; // Default retransmissions for UDP

        // Parse command-line arguments
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-t":
                    protocol = args[++i];
                    break;
                case "-s":
                    server = args[++i];
                    break;
                case "-p":
                    port = int.Parse(args[++i]);
                    break;
                case "-d":
                    timeout = int.Parse(args[++i]);
                    break;
                case "-r":
                    retransmissions = int.Parse(args[++i]);
                    break;
            }
        }

        if (protocol == "tcp")
        {
            await RunTcpClient(server, port);
        }
        else if (protocol == "udp")
        {
            RunUdpClient(server, port, timeout, retransmissions);
        }
        else
        {
            Console.WriteLine("Unsupported protocol. Use 'tcp' or 'udp'.");
        }
    }

    static void PrintHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("./ipk25chat-client -h");
        Console.WriteLine("./ipk25chat-client -t tcp -s <server> [-p <port>]");
        Console.WriteLine("./ipk25chat-client -t udp -s <server> [-p <port>] [-d <timeout>] [-r <retransmissions>]");
    }

    static async Task RunTcpClient(string server, int port)
    {
        Console.WriteLine($"Resolving server address: {server}...");
        string resolvedServer = ResolveServerAddress(server);

        Console.WriteLine($"Connecting to TCP server at {resolvedServer}:{port}...");

        try
        {
            using TcpClient client = new TcpClient(resolvedServer, port);
            using NetworkStream stream = client.GetStream();
            var context = new ClientContext(stream);

            Console.WriteLine("Connected. You can now enter commands.");

            while (true)
            {
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                context.ProcessInput(input.Trim());

                if (stream.DataAvailable)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    context.ProcessServerMessage(response);
                }
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Connection error: {ex.Message}");
        }
    }

    static void RunUdpClient(string server, int port, int timeout, int retransmissions)
    {
        Console.WriteLine($"Resolving server address: {server}...");
        string resolvedServer = ResolveServerAddress(server);

        Console.WriteLine($"Connecting to UDP server at {resolvedServer}:{port} with timeout {timeout}ms and {retransmissions} retransmissions...");

        try
        {
            using UdpClient client = new UdpClient();
            client.Connect(resolvedServer, port);

            Console.WriteLine("Connected. You can now enter commands.");

            while (true)
            {
                Console.Write("> ");
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                byte[] data = Encoding.UTF8.GetBytes(input.Trim());
                client.Send(data, data.Length);

                for (int i = 0; i <= retransmissions; i++)
                {
                    client.Client.ReceiveTimeout = timeout;

                    try
                    {
                        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                        byte[] response = client.Receive(ref remoteEndPoint);
                        string responseMessage = Encoding.UTF8.GetString(response);
                        Console.WriteLine($"Server: {responseMessage}");
                        break;
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                    {
                        if (i == retransmissions)
                        {
                            Console.WriteLine("No response from server. Retransmissions exhausted.");
                        }
                        else
                        {
                            Console.WriteLine("No response from server. Retrying...");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static string ResolveServerAddress(string server)
    {
        try
        {
            IPAddress[] addresses = Dns.GetHostAddresses(server);
            return addresses[0].ToString(); // Use the first resolved IP address
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to resolve server address: {ex.Message}");
            Environment.Exit(1); // Exit the program if resolution fails
            return string.Empty; // This will never be reached
        }
    }
}
