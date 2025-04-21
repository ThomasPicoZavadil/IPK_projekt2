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

        string? protocol = null;
        string? server = null;
        int port = 4567; // Default port
        int timeout = 250; // Default timeout for UDP in ms
        int retransmissions = 3; // Default retransmissions for UDP

        // Parse command-line arguments
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-t":
                    protocol = args[++i];
                    break;
                case "-s":
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    {
                        server = args[++i];
                    }
                    else
                    {
                        Console.WriteLine("ERROR: Missing or malformed server address after -s.");
                        PrintHelp();
                        return;
                    }
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

        // Validate mandatory arguments
        if (string.IsNullOrEmpty(protocol) || string.IsNullOrEmpty(server))
        {
            Console.WriteLine("ERROR: Both -t (protocol) and -s (server) arguments are mandatory.");
            PrintHelp();
            return;
        }

        if (protocol == "tcp")
        {
            await RunTcpClient(server, port);
        }
        else if (protocol == "udp")
        {
            Console.WriteLine("UDP support not implemented.");
            return;
        }
        else
        {
            Console.WriteLine("ERROR: Unsupported protocol. Use 'tcp' or 'udp'.");
            PrintHelp();
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
        string resolvedServer = ResolveServerAddress(server);

        try
        {
            using TcpClient client = new TcpClient(resolvedServer, port);
            using NetworkStream stream = client.GetStream();
            var context = new ClientContext(stream);

            while (true)
            {
                string? input = Console.ReadLine();

                if (input == null) // Handle Ctrl+D (EOF)
                {
                    Console.WriteLine("EOF detected. Exiting...");
                    context.GracefulExit();
                    break;
                }

                context.ProcessInput(input);

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
