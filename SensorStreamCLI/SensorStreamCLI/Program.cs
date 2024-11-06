using LibreHardwareMonitor.Hardware;
using Fleck;
using Newtonsoft.Json;

class Program
{
    private static bool _isRunning = true;
    private static WebSocketServer? _server;
    private static Computer? _computer;
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("*Tail wag* Starting SensorStream CLI...");
        
        // Parse command line arguments
        int port = 8546;
        if (args.Length > 0 && int.TryParse(args[0], out int customPort))
        {
            port = customPort;
        }

        try
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsStorageEnabled = true,
                IsNetworkEnabled = true
            };

            _computer.Open();
            
            _server = new WebSocketServer($"ws://0.0.0.0:{port}");
            Console.WriteLine($"*Happy bark* WebSocket server starting on port {port}!");

            _server.Start(socket =>
            {
                socket.OnOpen = () => Console.WriteLine("*Excited tail wag* New connection established!");
                socket.OnClose = () => Console.WriteLine("*Sad puppy eyes* Connection closed");
                socket.OnMessage = message => HandleMessage(socket, message);
            });

            Console.WriteLine("*Alert ears* Press Ctrl+C to stop the server");
            
            // Handle graceful shutdown
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _isRunning = false;
            };

            while (_isRunning)
            {
                await Task.Delay(100);
            }
        }
        finally
        {
            CleanupResources();
        }
    }

    private static void HandleMessage(IWebSocketConnection socket, string message)
    {
        try
        {
            if (_computer == null) return;

            var response = ProcessCommand(message, _computer);
            socket.Send(JsonConvert.SerializeObject(new { command = message, result = response }));
        }
        catch (Exception ex)
        {
            socket.Send(JsonConvert.SerializeObject(new { error = ex.Message }));
        }
    }

    private static string? ProcessCommand(string command, Computer computer)
    {
        var parts = command.Split('/');
        if (parts.Length < 2) return null;

        foreach (var hardware in computer.Hardware)
        {
            hardware.Update();
            
            if (MatchesHardwareType(hardware, parts[0]))
            {
                foreach (var sensor in hardware.Sensors)
                {
                    if (MatchesSensorType(sensor, parts))
                    {
                        return sensor.Value?.ToString();
                    }
                }
            }
        }

        return null;
    }

    private static bool MatchesHardwareType(IHardware hardware, string type)
    {
        return hardware.HardwareType.ToString().ToLower().Contains(type.ToLower());
    }

    private static bool MatchesSensorType(ISensor sensor, string[] parts)
    {
        return parts.Skip(1).All(part => 
            sensor.Name.ToLower().Contains(part.ToLower()) || 
            sensor.SensorType.ToString().ToLower().Contains(part.ToLower()));
    }

    private static void CleanupResources()
    {
        Console.WriteLine("*Gentle woof* Cleaning up...");
        _computer?.Close();
        _server?.Dispose();
        Console.WriteLine("*Sleepy yawn* Goodbye!");
    }
}
