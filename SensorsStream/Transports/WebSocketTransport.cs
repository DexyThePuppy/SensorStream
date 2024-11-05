using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fleck;
using Newtonsoft.Json;
using SensorStream.Monitor;
using System.Text.RegularExpressions;

namespace SensorStream.Transports
{
    public class WebSocketTransport : ITransport, IDisposable
    {
        private WebSocketServer server;
        private readonly ConcurrentDictionary<Guid, IWebSocketConnection> activeSockets;
        private volatile string lastData;
        private readonly SemaphoreSlim socketLock;
        private bool isDisposed;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly int port;
        private readonly ManualResetEventSlim _stopEvent;

        public WebSocketTransport(int port)
        {
            if (port <= 0 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535");

            this.port = port;
            activeSockets = new ConcurrentDictionary<Guid, IWebSocketConnection>();
            socketLock = new SemaphoreSlim(1, 1);
            _cancellationTokenSource = new CancellationTokenSource();
            _stopEvent = new ManualResetEventSlim(false);
        }

        public async Task StartAsync()
        {
            try
            {
                server = new WebSocketServer($"ws://0.0.0.0:{port}");
                server.Start(socket =>
                {
                    socket.OnOpen = () => OnSocketOpen(socket);
                    socket.OnClose = () => OnSocketClose(socket);
                    socket.OnMessage = async message => await HandleMessageAsync(socket, message);
                    socket.OnError = exception => OnSocketError(socket, exception);
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to start WebSocket server", ex);
            }
        }

        public void Start()
        {
            StartAsync().GetAwaiter().GetResult();
        }

        private void OnSocketOpen(IWebSocketConnection socket)
        {
            activeSockets.TryAdd(socket.ConnectionInfo.Id, socket);
        }

        private void OnSocketClose(IWebSocketConnection socket)
        {
            activeSockets.TryRemove(socket.ConnectionInfo.Id, out _);
        }

        private void OnSocketError(IWebSocketConnection socket, Exception ex)
        {
            // Log the error (replace with your logging framework)
            Console.WriteLine($"Socket error: {ex.Message}");
            activeSockets.TryRemove(socket.ConnectionInfo.Id, out _);
        }

        private async Task HandleMessageAsync(IWebSocketConnection socket, string message)
        {
            if (string.IsNullOrEmpty(lastData))
            {
                await socket.Send("No data available");
                return;
            }

            try
            {
                var data = JsonConvert.DeserializeObject<List<HardwareContent>>(lastData);
                var response = ProcessMessage(message, data);
                if (!string.IsNullOrEmpty(response))
                {
                    await socket.Send(response);
                }
            }
            catch (JsonException ex)
            {
                await socket.Send($"Error processing data: {ex.Message}");
            }
            catch (Exception ex)
            {
                await socket.Send($"Internal error: {ex.Message}");
            }
        }

        private string ProcessMessage(string message, List<HardwareContent> data)
        {
            var parts = message.ToLower().Split('/', StringSplitOptions.RemoveEmptyEntries);

            // If no specific command is provided, return the full JSON data
            if (parts.Length == 0)
            {
                return JsonConvert.SerializeObject(data);
            }

            bool returnJson = parts.Last() == "json";
            if (returnJson)
            {
                parts = parts.Take(parts.Length - 1).ToArray();
            }

            string result = parts[0] switch
            {
                "system" => HandleSystemCommand(parts.Skip(1).ToArray(), data),
                _ => HandleHardwareCommand(parts, data)
            };

            if (returnJson && result != null)
            {
                return JsonConvert.SerializeObject(new { command = message, result });
            }

            return result;
        }

        private string HandleSystemCommand(string[] parts, List<HardwareContent> data)
        {
            if (parts.Length == 0) return null;

            if (parts[0] == "components")
            {
                if (parts.Length > 1 && parts[1] == "all")
                {
                    return GenerateFullComponentList(data);
                }
                return string.Join(",", data.Select(h => h.type.ToLower()));
            }

            return null;
        }

        private string GenerateFullComponentList(List<HardwareContent> data)
        {
            var commands = new List<string>();
            var indices = new Dictionary<string, int>
            {
                { "cpu", 0 },
                { "gpu", 0 },
                { "ram", 0 },
                { "memory", 0 },
                { "storage", 0 },
                { "network", 0 },
                { "motherboard", 0 }
            };
            
            foreach (var hardware in data)
            {
                var componentType = hardware.type.ToLower();
                
                // Convert any GPU type to just "gpu"
                if (componentType.Contains("gpu"))
                {
                    componentType = "gpu";
                }

                var currentIndex = indices[componentType];

                commands.Add($"{componentType}/{currentIndex} = Lists all {hardware.type} data");
                commands.Add($"{componentType}/{currentIndex}/name = {hardware.name}");
                commands.Add($"{componentType}/{currentIndex}/sensorcount = {hardware.sensorCount}");

                if (hardware.sensors != null)
                {
                    var groupedSensors = hardware.sensors
                        .Select(s => new { 
                            Sensor = s, 
                            Path = SimplifySensorPath(componentType, s.type, s.name),
                            Category = GetSensorCategory(s.type)
                        })
                        .GroupBy(s => s.Category)
                        .OrderBy(g => GetCategoryOrder(g.Key));

                    foreach (var group in groupedSensors)
                    {
                        var sortedSensors = group
                            .OrderBy(s => GetSensorOrder(s.Path))
                            .ThenBy(s => ExtractNumber(s.Path));

                        foreach (var sensor in sortedSensors)
                        {
                            commands.Add($"{componentType}/{currentIndex}/{sensor.Path} = {FormatSensorValue(sensor.Sensor.value)}");
                        }
                        commands.Add(string.Empty);
                    }
                }

                commands.Add(string.Empty);
                indices[componentType]++;
            }
            return string.Join("\n", commands);
        }

        private string GetSensorCategory(string type)
        {
            if (type.Contains("temperature")) return "temperature";
            if (type.Contains("clock")) return "clock";
            if (type.Contains("load")) return "load";
            if (type.Contains("power")) return "power";
            if (type.Contains("voltage")) return "voltage";
            if (type.Contains("fan")) return "fan";
            if (type.Contains("throughput")) return "throughput";
            if (type.Contains("data")) return "data";
            return "other";
        }

        private int GetCategoryOrder(string category)
        {
            return category switch
            {
                "load" => 1,
                "temperature" => 2,
                "clock" => 3,
                "power" => 4,
                "voltage" => 5,
                "fan" => 6,
                "throughput" => 7,
                "data" => 8,
                _ => 9
            };
        }

        private string GetSensorOrder(string path)
        {
            // Extract the base name without numbers
            return Regex.Replace(path, @"\d+", "");
        }

        private int ExtractNumber(string path)
        {
            var match = Regex.Match(path, @"\d+");
            return match.Success ? int.Parse(match.Value) : 0;
        }

        private string SimplifySensorPath(string componentType, string sensorType, string sensorName)
        {
            // Remove common prefixes from names
            sensorName = sensorName?.ToLower() ?? "";
            
            // Get the category from the sensor type
            var category = sensorType?.ToLower() ?? "";
            if (category.Contains("/"))
            {
                var parts = category.Split('/');
                category = parts[parts.Length - 2];
            }

            // Convert GPU type to just "gpu"
            if (componentType.Contains("gpu"))
            {
                componentType = "gpu";
            }

            // Special handling for CPU sensors
            if (componentType == "cpu")
            {
                if (sensorType.Contains("load"))
                    return $"load/{NormalizeName(sensorName)}";
                else if (sensorType.Contains("temperature"))
                    return $"temperature/{NormalizeName(sensorName)}";
                else if (sensorType.Contains("clock"))
                    return $"clock/{NormalizeName(sensorName)}";
                else if (sensorType.Contains("voltage"))
                    return $"voltage/{NormalizeName(sensorName)}";
                else if (sensorType.Contains("power"))
                    return $"power/{NormalizeName(sensorName)}";
                else if (sensorType.Contains("factor"))
                    return $"factor/{NormalizeName(sensorName)}";
                else if (sensorType.Contains("current"))
                    return $"current/{NormalizeName(sensorName)}";
            }
            // For other components, skip "load" category
            else if (category == "load")
            {
                return NormalizeName(sensorName);
            }

            // Clean up the name
            var name = NormalizeName(sensorName);
            return $"{category}/{name}";
        }

        private string NormalizeName(string name)
        {
            return name.ToLower()
                .Replace("gpu ", "")
                .Replace("cpu ", "")
                .Replace("core #", "core")
                .Replace(" (smu)", "smu")
                .Replace(" vid", "vid")
                .Replace("d3d ", "")
                .Replace(" memory", "mem")
                .Replace("controller", "ctrl")
                .Replace(" engine", "")
                .Replace(" processing", "proc")
                .Replace(" encode", "enc")
                .Replace(" decode", "dec")
                .Replace(" ", "")
                .Replace("#", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace(".", "");
        }

        private string FormatSensorValue(object value)
        {
            if (value == null) return string.Empty;
            
            if (value is double d)
            {
                if (d > 1_000_000)
                    return d.ToString("N0");
                return d.ToString("F2").Replace(",", ".");
            }
            
            return value.ToString().Replace(",", ".");
        }

        private string HandleHardwareCommand(string[] parts, List<HardwareContent> data)
        {
            if (parts.Length < 2) return null;

            // Extract hardware type and index
            string hardwareType = parts[0];
            if (!int.TryParse(parts[1], out int index)) return null;

            // Find the hardware component
            var hardwareList = data.Where(h => 
            {
                var type = h.type.ToLower();
                // Special handling for GPU types
                if (hardwareType == "gpu" && type.Contains("gpu"))
                    return true;
                return type == hardwareType;
            }).ToList();

            if (index >= hardwareList.Count) return null;
            var hardware = hardwareList[index];

            // If only hardware and index provided, return full hardware data
            if (parts.Length == 2)
            {
                return JsonConvert.SerializeObject(new[] { hardware });
            }

            // Handle specific property requests
            if (parts.Length == 3)
            {
                switch (parts[2].ToLower())
                {
                    case "name":
                        return hardware.name;
                    case "sensorcount":
                        return hardware.sensorCount.ToString();
                    case "sensors":
                        return JsonConvert.SerializeObject(hardware.sensors);
                }
            }

            // Handle sensor value requests (e.g., network/1/networkutilization)
            if (parts.Length >= 3)
            {
                string sensorPath = string.Join("/", parts.Skip(2));
                var sensor = hardware.sensors?.FirstOrDefault(s => 
                {
                    // Create the sensor path in the same format as the request
                    string sensorPathNormalized = SimplifySensorPath(hardwareType, s.type, s.name)
                        .ToLower()
                        .Replace(" ", "");
                    
                    return sensorPathNormalized.Equals(sensorPath.ToLower(), StringComparison.OrdinalIgnoreCase);
                });

                if (sensor != null)
                {
                    return FormatSensorValue(sensor.value);
                }
            }

            return string.Empty;
        }

        private string NormalizeSensorPath(string path)
        {
            return path.ToLower()
                .Replace(" ", "")
                .Replace("#", "")
                .Replace("(", "")
                .Replace(")", "");
        }

        public async Task SendMessageAsync(string msg)
        {
            if (string.IsNullOrEmpty(msg) || isDisposed)
                return;

            try
            {
                await socketLock.WaitAsync(_cancellationTokenSource.Token);
                try
                {
                    lastData = msg;
                    var tasks = activeSockets.Values
                        .Where(socket => socket.IsAvailable)
                        .Select(socket => SendToSocketAsync(socket, msg))
                        .ToArray(); // Use ToArray() instead of ToList()

                    await Task.WhenAll(tasks.Where(t => t != null));
                }
                finally
                {
                    socketLock.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, ignore
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in sendMessage: {ex.Message}");
            }
        }

        private async Task SendToSocketAsync(IWebSocketConnection socket, string msg)
        {
            try
            {
                await socket.Send(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message: {ex.Message}");
                activeSockets.TryRemove(socket.ConnectionInfo.Id, out _);
            }
        }

        public void sendMessage(string msg)
        {
            SendMessageAsync(msg).GetAwaiter().GetResult();
        }

        public async Task StopAsync()
        {
            if (isDisposed) return;

            try
            {
                _cancellationTokenSource.Cancel();
                _stopEvent.Set();

                var closeTasks = activeSockets.Values.Select(async socket =>
                {
                    try
                    {
                        await Task.Run(() => socket.Close());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error closing socket: {ex.Message}");
                    }
                });

                await Task.WhenAll(closeTasks);
                activeSockets.Clear();
                
                await Task.Run(() => server?.Dispose());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during stop: {ex.Message}");
            }
        }

        public void Stop()
        {
            StopAsync().GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            Stop();
            socketLock.Dispose();
            _stopEvent.Dispose();
            _cancellationTokenSource.Dispose();
            isDisposed = true;
            GC.SuppressFinalize(this);
        }

        private string NormalizeSensorName(string type, string name)
        {
            if (type.ToLower().Contains("gpu"))
            {
                // Handle GPU fan speeds
                if (name.ToLower().Contains("fan", StringComparison.OrdinalIgnoreCase))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(name, @"(\d+)");
                    if (match.Success)
                    {
                        return $"fan/{match.Groups[1].Value}";
                    }
                    return "fan/1";  // Default to fan/1 if no number found
                }

                // Handle D3D related metrics
                if (name.ToLower().StartsWith("d3d"))
                {
                    return $"d3d/{name.ToLower().Replace("d3d", "").Replace(" ", "")}";
                }

                // Handle power related metrics
                if (name.ToLower().Contains("power"))
                {
                    return "power";
                }

                // Handle memory related metrics
                if (name.ToLower().Contains("memory"))
                {
                    return name.ToLower()
                        .Replace("gpu", "")
                        .Replace(" ", "");
                }

                // Handle other GPU metrics
                return name.ToLower()
                    .Replace("gpu", "")
                    .Replace(" ", "");
            }
            else if (type.ToLower() == "cpu")
            {
                // Handle CPU core measurements
                if (name.ToLower().Contains("core #"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(name, @"#(\d+)");
                    if (match.Success)
                    {
                        string coreNum = match.Groups[1].Value;
                        
                        // Check the sensor type to determine the prefix
                        if (type.ToLower().Contains("voltage"))
                            return $"voltage/core{coreNum}";
                        else if (type.ToLower().Contains("clock"))
                            return $"clock/core{coreNum}";
                        else if (type.ToLower().Contains("factor"))
                            return $"factor/core{coreNum}";
                        else if (type.ToLower().Contains("power"))
                            return $"power/core{coreNum}";
                        else
                            return $"load/core{coreNum}";
                    }
                }
                
                // Handle CPU package measurements
                if (name.ToLower().Contains("package"))
                    return $"package/{name.ToLower().Replace(" ", "")}";
                
                // Handle CPU voltage measurements
                if (type.ToLower().Contains("voltage"))
                    return $"voltage/{name.ToLower().Replace(" ", "")}";
                
                // Handle CPU clock measurements
                if (type.ToLower().Contains("clock"))
                    return $"clock/{name.ToLower().Replace(" ", "")}";
                
                // Handle CPU power measurements
                if (type.ToLower().Contains("power"))
                    return $"power/{name.ToLower().Replace(" ", "")}";
                
                // Handle CPU temperature
                if (name.ToLower().Contains("temperature"))
                    return $"temperature/{name.ToLower().Replace(" ", "")}";
                
                // Default case
                return name.ToLower()
                    .Replace("cpu", "")
                    .Replace(" ", "")
                    .Replace("#", "")
                    .Replace("(", "")
                    .Replace(")", "");
            }

            // Extract core number if present (e.g., "Core #1" -> "1")
            var coreNumber = "";
            if (name.Contains("#"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(name, @"#(\d+)");
                if (match.Success)
                {
                    coreNumber = match.Groups[1].Value;
                }
            }

            // Handle specific sensor types
            switch (type.ToLower())
            {
                case "clock" when name.StartsWith("Core"):
                    return $"{coreNumber}/clock";
                case "factor" when name.StartsWith("Core"):
                    return $"{coreNumber}/factor";
                case "power" when name.StartsWith("Core"):
                    return $"{coreNumber}/power";
                case "voltage" when name.Contains("VID"):
                    return $"{coreNumber}/voltage";
                case "temperature":
                    return "temp";
                default:
                    // Handle other cases as before
                    return name.ToLower()
                        .Replace(" ", "")
                        .Replace("#", "")
                        .Replace("(", "")
                        .Replace(")", "");
            }
        }

        public void UpdateData(string data)
        {
            if (string.IsNullOrEmpty(data))
                return;
            
            lastData = data;
        }
    }
}